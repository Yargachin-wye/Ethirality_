using System;
using System.Collections;
using System.Collections.Generic;
using CharacterComponents;
using Generation;
using Spawner;
using UnityEngine;
using Utilities;
using Random = System.Random;

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
            int i = _nextLayerIndex;
            var random = new Random(LayerSeed(seed, i));
            float difficulty = difficultyProfile.EvaluateDifficulty(i);

            float pacingMultiplier = 1f;
            LayerDefinition definition = null;
            if (difficultyProfile.TryGetBeat(i, out var beat))
            {
                pacingMultiplier = beat.threatMultiplier;
                definition = beat.forcedLayer;
            }

            definition ??= PickDefinition(random, difficulty);
            if (definition == null) yield break;

            float width = Mathf.Lerp(definition.WidthRange.x, definition.WidthRange.y, (float) random.NextDouble());
            float height = Mathf.Lerp(definition.HeightRange.x, definition.HeightRange.y, (float) random.NextDouble());
            var bounds = new Rect(-width / 2f, _cursorY, width, height);
            float threat = difficultyProfile.EvaluateThreat(difficulty, width * height, pacingMultiplier,
                definition.ThreatMultiplier * difficultyProfile.EvaluateEndlessMultiplier(i));

            var layer = new LayerContext(i, bounds, difficulty, threat, definition, random);
            foreach (var pack in generators)
            {
                while (GameplayPause.IsPaused) yield return null;

                if (pack.generate && pack.generator != null && pack.Accepts(definition))
                    yield return StartCoroutine(pack.generator.Generate(layer));
            }

            Level.Layers.Add(layer);
            _activeLayers.Enqueue(layer);
            if (levelSpawner != null) levelSpawner.SpawnLayer(layer);

            _cursorY += height;
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

        private LayerDefinition PickDefinition(Random random, float difficulty)
        {
            float total = 0f;
            foreach (var d in layerDefinitions)
                if (d != null && d.IsAvailable(difficulty)) total += d.Weight;

            if (total <= 0f) return null;

            double roll = random.NextDouble() * total;
            LayerDefinition last = null;
            foreach (var d in layerDefinitions)
            {
                if (d == null || !d.IsAvailable(difficulty)) continue;
                last = d;
                roll -= d.Weight;
                if (roll <= 0) break;
            }

            return last;
        }

        private static int LayerSeed(int seed, int layerIndex)
        {
            unchecked { return (seed * 73856093) ^ (layerIndex * 19349663) ^ 0x5bd1e995; }
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
