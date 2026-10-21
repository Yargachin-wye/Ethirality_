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
            Ambusher,
            Reward,
            Hunter
        }

        public enum SpawnKind
        {
            Threat,
            Food
        }

        [Serializable]
        public struct Entry
        {
            public CharacterDefinition definition;
            public SpawnKind kind;
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

        public bool TryGetEntry(CharacterDefinition definition, out Entry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].definition != definition) continue;
                entry = entries[i];
                return true;
            }

            entry = default;
            return false;
        }

        public List<CharacterDefinition> RollFood(Random random, float difficulty, float foodBudget, int maxCount = int.MaxValue) =>
            Roll(SpawnKind.Food, random, difficulty, foodBudget, maxCount);

        public List<CharacterDefinition> Roll(Random random, float difficulty, float budget, int maxCount = int.MaxValue) =>
            Roll(SpawnKind.Threat, random, difficulty, budget, maxCount);

        private List<CharacterDefinition> Roll(SpawnKind kind, Random random, float difficulty, float budget, int maxCount)
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
                    if (e.definition == null || e.kind != kind || e.threatCost > budget) continue;
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

        public List<CharacterDefinition> Roll(Random random, float difficulty, float budget,
            IReadOnlyList<LayerDefinition.RoleShare> composition, int maxCount = int.MaxValue)
        {
            if (composition == null || composition.Count == 0)
                return Roll(random, difficulty, budget, maxCount);

            var result = new List<CharacterDefinition>();
            var counts = new int[entries.Length];
            int roleCount = composition.Count;
            var shares = new float[roleCount];
            var quotas = new float[roleCount];
            var active = new bool[roleCount];

            float totalShare = 0f;
            for (int i = 0; i < roleCount; i++)
            {
                float s = composition[i].share;
                if (s <= 0f || !RoleAvailable(composition[i].role, difficulty)) continue;
                shares[i] = s;
                active[i] = true;
                totalShare += s;
            }

            if (totalShare <= 0f) return result;

            float remaining = budget;
            for (int i = 0; i < roleCount; i++)
                if (active[i]) quotas[i] = budget * shares[i] / totalShare;

            while (result.Count < maxCount && remaining > 0f)
            {
                int roleIdx = PickRole(random, quotas, active);
                if (roleIdx < 0) break;

                float cap = Mathf.Min(quotas[roleIdx], remaining);
                int picked = RollInRole(random, composition[roleIdx].role, difficulty, cap, counts);
                if (picked < 0)
                {
                    RedistributeQuota(roleIdx, quotas, shares, active);
                    continue;
                }

                counts[picked]++;
                float cost = entries[picked].threatCost;
                quotas[roleIdx] -= cost;
                remaining -= cost;
                result.Add(entries[picked].definition);
            }

            return result;
        }

        private bool RoleAvailable(EnemyRole role, float difficulty)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].kind == SpawnKind.Threat && entries[i].role == role && entries[i].definition != null
                    && GetWeight(entries[i], difficulty) > 0f)
                    return true;
            return false;
        }

        private static int PickRole(Random random, float[] quotas, bool[] active)
        {
            float total = 0f;
            int last = -1;
            for (int i = 0; i < quotas.Length; i++)
                if (active[i] && quotas[i] > 0f) { total += quotas[i]; last = i; }

            if (last < 0) return -1;

            double roll = random.NextDouble() * total;
            for (int i = 0; i < quotas.Length; i++)
            {
                if (!active[i] || quotas[i] <= 0f) continue;
                roll -= quotas[i];
                if (roll <= 0) return i;
            }
            return last;
        }

        private int RollInRole(Random random, EnemyRole role, float difficulty, float cap, int[] counts)
        {
            _candidates.Clear();
            _weights.Clear();
            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                Entry e = entries[i];
                if (e.kind != SpawnKind.Threat || e.role != role || e.definition == null || e.threatCost > cap) continue;
                if (e.maxPerLayer > 0 && counts[i] >= e.maxPerLayer) continue;
                float w = GetWeight(e, difficulty);
                if (w <= 0f) continue;
                _candidates.Add(i);
                _weights.Add(w);
                total += w;
            }

            if (_candidates.Count == 0) return -1;

            double roll = random.NextDouble() * total;
            int picked = _candidates[_candidates.Count - 1];
            for (int c = 0; c < _candidates.Count; c++)
            {
                roll -= _weights[c];
                if (roll <= 0) { picked = _candidates[c]; break; }
            }
            return picked;
        }

        private static void RedistributeQuota(int exhausted, float[] quotas, float[] shares, bool[] active)
        {
            float leftover = quotas[exhausted];
            quotas[exhausted] = 0f;
            active[exhausted] = false;
            if (leftover <= 0f) return;

            float total = 0f;
            for (int i = 0; i < shares.Length; i++)
                if (active[i]) total += shares[i];
            if (total <= 0f) return;

            for (int i = 0; i < shares.Length; i++)
                if (active[i]) quotas[i] += leftover * shares[i] / total;
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
