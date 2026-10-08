using System;
using System.Collections;
using System.Collections.Generic;
using Generation;
using Spawner;
using UnityEngine;
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

        public LevelData Level { get; private set; }

        private void Start()
        {
            GenerateWorld();
        }

        public void GenerateWorld()
        {
            StartCoroutine(Generate());
        }

        private IEnumerator Generate()
        {
            Level = new LevelData(seed);
            float cursorY = 0f;

            for (int i = 0; i < difficultyProfile.LayerCount; i++)
            {
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
                var bounds = new Rect(-width / 2f, cursorY, width, height);
                float threat = difficultyProfile.EvaluateThreat(difficulty, width * height, pacingMultiplier, definition.ThreatMultiplier);

                var layer = new LayerContext(i, bounds, difficulty, threat, definition, random);
                foreach (var pack in generators)
                {
                    if (pack.generate && pack.generator != null && pack.Accepts(definition))
                        yield return StartCoroutine(pack.generator.Generate(layer));
                }

                Level.Layers.Add(layer);
                cursorY += height;
                yield return null;
            }

            if (Level.PlayerSpawns.Count == 0 && Level.Layers.Count > 0)
            {
                Rect first = Level.Layers[0].Bounds;
                Level.PlayerSpawns.Add(new Vector2(first.center.x, first.yMin + 1f));
            }

            if (levelSpawner != null) levelSpawner.Spawn(Level);
            if (playerSpawner != null)
            {
                foreach (var point in Level.PlayerSpawns) playerSpawner.AddSpawnPoint(point);
                playerSpawner.Spawn();
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
