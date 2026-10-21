using System;
using System.Collections;
using System.Collections.Generic;
using CharacterComponents;
using Generation;
using Spawner;
using UnityEngine;
using Utilities;

namespace Generator
{
    public class GeneratorsManager : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private DifficultyProfile difficultyProfile;
        [SerializeField] private List<LayerDefinition> layerDefinitions;
        [SerializeField] private List<GeneratorPack> generators;
        [SerializeField] private LevelSpawner levelSpawner;
        [SerializeField] private PlayerSpawner playerSpawner;

        [Header("Бесконечная генерация")]
        [SerializeField, Min(0f), Tooltip("Генерировать слои, пока верх мира ближе к игроку, чем это расстояние")]
        private float generateAhead = 40f;
        [SerializeField, Min(0f), Tooltip("Удалять слои, верх которых ниже игрока на это расстояние")]
        private float despawnBehind = 30f;

        private readonly Queue<LayerContext> _activeLayers = new();
        private Character _player;
        private float _cursorY;
        private int _nextLayerIndex;
        private float _lastPlayerY;
        private bool _generating;

        public LevelData Level { get; private set; }

        private void Start()
        {
            GenerateWorld();
        }

        private void Update()
        {
            if (GameplayPause.IsPaused || Level == null) return;

            if (_player != null && _player.gameObject.activeInHierarchy)
                _lastPlayerY = _player.transform.position.y;

            DespawnPassedLayers();

            if (!_generating && _cursorY < _lastPlayerY + generateAhead)
                StartCoroutine(GenerateNextLayer());
        }

        public void GenerateWorld()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Level = new LevelData(seed);
            _cursorY = 0f;
            _nextLayerIndex = 0;
            _generating = true;

            int before;
            do
            {
                before = _nextLayerIndex;
                yield return GenerateLayer();
            } while (_cursorY < generateAhead && _nextLayerIndex > before);

            if (Level.PlayerSpawns.Count == 0 && _activeLayers.Count > 0)
            {
                Rect first = _activeLayers.Peek().Bounds;
                Level.PlayerSpawns.Add(new Vector2(first.center.x, first.yMin + 1f));
            }

            if (playerSpawner != null)
            {
                foreach (var point in Level.PlayerSpawns) playerSpawner.AddSpawnPoint(point);
                _player = playerSpawner.Spawn();
                _lastPlayerY = _player.transform.position.y;
            }

            _generating = false;
        }

        private IEnumerator GenerateNextLayer()
        {
            _generating = true;
            yield return GenerateLayer();
            _generating = false;
        }

        private IEnumerator GenerateLayer()
        {
            var layer = LayerPlanner.Plan(seed, _nextLayerIndex, _cursorY, difficultyProfile,
                layerDefinitions, out _);
            if (layer == null) yield break;

            foreach (var pack in generators)
            {
                while (GameplayPause.IsPaused) yield return null;

                if (pack.generate && pack.generator != null && pack.Accepts(layer.Definition))
                    yield return StartCoroutine(pack.generator.Generate(layer));
            }

            Level.Layers.Add(layer);
            _activeLayers.Enqueue(layer);
            if (levelSpawner != null) levelSpawner.SpawnLayer(layer);

            _cursorY += layer.Bounds.height;
            _nextLayerIndex++;
            yield return null;
        }

        private void DespawnPassedLayers()
        {
            float limit = _lastPlayerY - despawnBehind;
            while (_activeLayers.Count > 1 && _activeLayers.Peek().Bounds.yMax < limit)
            {
                var layer = _activeLayers.Dequeue();
                Level.Layers.Remove(layer);
                if (levelSpawner != null) levelSpawner.DespawnLayer(layer);
            }
        }

        [Serializable]
        public struct GeneratorPack
        {
            public BaseGenerator generator;
            public bool generate;
            public List<LayerDefinition> onlyForLayers;

            public bool Accepts(LayerDefinition definition) =>
                onlyForLayers == null || onlyForLayers.Count == 0 || onlyForLayers.Contains(definition);
        }
    }
}
