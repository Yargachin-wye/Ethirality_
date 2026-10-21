using System;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Difficulty Profile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Serializable]
        public struct PacingBeat
        {
            [Min(1)] public int every;
            public int offset;
            public float threatMultiplier;
            public LayerDefinition forcedLayer;
        }

        [SerializeField, Min(1)] private int layerCount = 20;
        [SerializeField] private AnimationCurve difficultyByProgress = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField, Min(0f)] private float minThreatPerArea = 0.02f;
        [SerializeField, Min(0f)] private float maxThreatPerArea = 0.12f;
        [Header("Бюджет еды (отдельно от угроз)")]
        [SerializeField, Min(0f)] private float minFoodPerArea = 0.012f;
        [SerializeField, Min(0f)] private float maxFoodPerArea = 0.03f;
        [SerializeField, Min(0f), Tooltip("Потолок бюджета еды на слой (0 = без потолка); держит PlacementLoss в цели")]
        private float maxFoodBudget = 150f;
        [SerializeField, Min(0.001f), Tooltip("Макс. прирост сложности между соседними слоями (pressure budget)")]
        private float maxDifficultyDeltaPerLayer = 0.05f;
        [SerializeField] private PacingBeat[] pacing =
        {
            new() {every = 4, offset = 3, threatMultiplier = 0.45f},
            new() {every = 10, offset = 9, threatMultiplier = 1.5f}
        };

        [SerializeField, Min(0f), Tooltip("Рост угрозы после выхода на максимум кривой (бесконечный режим): 1 + k * ln(1 + слоёв сверх кривой)")]
        private float endlessThreatGrowth = 0.5f;

        public int LayerCount => layerCount;
        public float MaxDifficultyDeltaPerLayer => maxDifficultyDeltaPerLayer;

        public float EvaluateEndlessMultiplier(int layerIndex) =>
            1f + endlessThreatGrowth * Mathf.Log(1f + Mathf.Max(0, layerIndex - layerCount + 1));

        private float[] _pressureClamped;

        public float EvaluateDifficulty(int layerIndex)
        {
            if (_pressureClamped == null || _pressureClamped.Length != layerCount)
                BuildPressureBudget();

            return _pressureClamped[Mathf.Clamp(layerIndex, 0, layerCount - 1)];
        }

        private void BuildPressureBudget()
        {
            _pressureClamped = new float[layerCount];
            float prev = 0f;
            for (int i = 0; i < layerCount; i++)
            {
                float t = layerCount > 1 ? i / (float) (layerCount - 1) : 0f;
                float raw = Mathf.Clamp01(difficultyByProgress.Evaluate(t));
                prev = _pressureClamped[i] = i == 0 ? raw : Mathf.Min(raw, prev + maxDifficultyDeltaPerLayer);
            }
        }

        private void OnValidate()
        {
            _pressureClamped = null;
        }

        public float EvaluateThreat(float difficulty, float area, float pacingMultiplier, float layerMultiplier)
        {
            return Mathf.Lerp(minThreatPerArea, maxThreatPerArea, difficulty) * area * pacingMultiplier * layerMultiplier;
        }

        public float EvaluateFood(float difficulty, float area)
        {
            float budget = Mathf.Lerp(minFoodPerArea, maxFoodPerArea, difficulty) * area;
            return maxFoodBudget > 0f ? Mathf.Min(budget, maxFoodBudget) : budget;
        }

        public bool TryGetBeat(int layerIndex, out PacingBeat beat)
        {
            for (int i = pacing.Length - 1; i >= 0; i--)
            {
                PacingBeat b = pacing[i];
                if (b.every > 0 && layerIndex % b.every == b.offset % b.every)
                {
                    beat = b;
                    return true;
                }
            }

            beat = default;
            return false;
        }
    }
}
