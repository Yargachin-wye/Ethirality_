using System;
using System.Collections.Generic;
using Definitions;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Enemy Spawn Table")]
    public class EnemySpawnTable : ScriptableObject
    {
        public enum EnemyRole
        {
            Melee,
            Ranged,
            Tank,
            Swarm,
            Elite,
            Support,
            Ambusher
        }

        [Serializable]
        public struct Entry
        {
            public CharacterDefinition definition;
            public EnemyRole role;
            [Min(0.01f)] public float threatCost;
            [Min(0f)] public float weight;
            [Range(0f, 1f)] public float unlockDifficulty;
            [Range(0f, 1f)] public float fullWeightDifficulty;
            [Range(0f, 1f)] public float retireDifficulty;
            [Min(0)] public int maxPerLayer;
        }

        [SerializeField] private Entry[] entries;

        public IReadOnlyList<Entry> Entries => entries;

        private readonly List<int> _candidates = new();
        private readonly List<float> _weights = new();

        public List<CharacterDefinition> Roll(Random random, float difficulty, float budget, int maxCount = int.MaxValue)
        {
            var result = new List<CharacterDefinition>();
            var counts = new int[entries.Length];

            while (result.Count < maxCount)
            {
                _candidates.Clear();
                _weights.Clear();
                float total = 0f;
                for (int i = 0; i < entries.Length; i++)
                {
                    Entry e = entries[i];
                    if (e.definition == null || e.threatCost > budget) continue;
                    if (e.maxPerLayer > 0 && counts[i] >= e.maxPerLayer) continue;
                    float w = GetWeight(e, difficulty);
                    if (w <= 0f) continue;
                    _candidates.Add(i);
                    _weights.Add(w);
                    total += w;
                }

                if (_candidates.Count == 0) break;

                double roll = random.NextDouble() * total;
                int picked = _candidates[_candidates.Count - 1];
                for (int c = 0; c < _candidates.Count; c++)
                {
                    roll -= _weights[c];
                    if (roll <= 0) { picked = _candidates[c]; break; }
                }

                counts[picked]++;
                budget -= entries[picked].threatCost;
                result.Add(entries[picked].definition);
            }

            return result;
        }

        private static float GetWeight(Entry e, float difficulty)
        {
            if (difficulty < e.unlockDifficulty) return 0f;
            if (e.retireDifficulty > 0f && difficulty > e.retireDifficulty) return 0f;
            float ramp = e.fullWeightDifficulty > e.unlockDifficulty
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(e.unlockDifficulty, e.fullWeightDifficulty, difficulty))
                : 1f;
            return e.weight * ramp;
        }
    }
}
