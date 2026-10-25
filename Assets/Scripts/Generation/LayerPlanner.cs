using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    public static class LayerPlanner
    {
        public static int LayerSeed(int seed, int layerIndex)
        {
            unchecked { return (seed * 73856093) ^ (layerIndex * 19349663) ^ 0x5bd1e995; }
        }

        public static LayerDefinition PickDefinition(Random random, float difficulty, IReadOnlyList<LayerDefinition> layerDefinitions)
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

        public static LayerContext Plan(int seed, int layerIndex, float cursorY, DifficultyProfile profile,
            IReadOnlyList<LayerDefinition> layerDefinitions, out float pacingMultiplier)
        {
            int i = layerIndex;
            var random = new Random(LayerSeed(seed, i));
            float difficulty = profile.EvaluateDifficulty(i);

            pacingMultiplier = 1f;
            LayerDefinition definition = null;
            if (profile.TryGetBeat(i, out var beat))
            {
                pacingMultiplier = beat.threatMultiplier;
                definition = beat.forcedLayer;
            }

            definition ??= PickDefinition(random, difficulty, layerDefinitions);
            if (definition == null) return null;

            float width = Mathf.Lerp(definition.WidthRange.x, definition.WidthRange.y, (float)random.NextDouble());
            float height = Mathf.Lerp(definition.HeightRange.x, definition.HeightRange.y, (float)random.NextDouble());
            var bounds = new Rect(-width / 2f, cursorY, width, height);
            float threat = profile.EvaluateThreat(difficulty, width * height, pacingMultiplier,
                definition.ThreatMultiplier * profile.EvaluateEndlessMultiplier(i));

            float food = profile.EvaluateFood(difficulty, width * height);

            var layout = LayoutPlan.Create(seed, i, profile.EvaluateDifficulty, profile.LayoutProfile);
            return new LayerContext(i, bounds, difficulty, threat, food, definition, random,
                PlanClimb(seed, i, bounds, difficulty, profile, definition), layout);
        }

        public static float BoundaryU(int seed, int boundaryIndex)
        {
            unchecked
            {
                uint h = (uint) ((seed * 668265263) ^ (boundaryIndex * 374761393) ^ 0x27d4eb2f);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float) 0x1000000;
            }
        }

        public static float BoundaryX(Rect bounds, float u, DifficultyProfile profile)
        {
            float half = Mathf.Max(0f, bounds.width / 2f - profile.ChainEdgeReserve) * profile.ChainBoundarySpread;
            return bounds.center.x + (u * 2f - 1f) * half;
        }

        private static ClimbPlan PlanClimb(int seed, int index, Rect bounds, float difficulty, DifficultyProfile profile,
            LayerDefinition definition)
        {
            var table = definition.SpawnTable;
            float maxSupport = table != null ? table.MaxMobilityCost() : 0f;
            int minSupports = profile.EvaluateMinSupports(bounds.height);
            float budget = profile.EvaluateMobility(difficulty, bounds.height, minSupports, maxSupport);
            return new ClimbPlan(budget, profile.StepCap, minSupports, profile.EvaluateRiskShare(difficulty),
                profile.ChainEntryOffset, profile.ChainExitOffset, profile.ChainEdgeReserve,
                BoundaryX(bounds, BoundaryU(seed, index), profile),
                BoundaryX(bounds, BoundaryU(seed, index + 1), profile), profile.SupportChain);
        }
    }
}
