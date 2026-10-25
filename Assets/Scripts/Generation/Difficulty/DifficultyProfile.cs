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
        [Header("Бюджет мобильности (опоры подъёма)")]
        [SerializeField, Tooltip("false = опоры размещаются как обычные объекты (без цепочки); только для сравнения 'до/после'")]
        private bool supportChain = true;
        [SerializeField, Min(1f), Tooltip("Опорная досягаемость игрока (ед.); не больше PlayerBuild.MaxVerticalGap слабейшей сборки (проверяет ReferenceGapMatchesPlayer)")]
        private float referenceMaxGap = 17.5f;
        [SerializeField, Range(0.1f, 1f), Tooltip("Макс. шаг цепочки опор как доля referenceMaxGap")]
        private float chainStepFraction = 0.8f;
        [SerializeField, Range(0.1f, 1f), Tooltip("Доля шага, по которой считается минимальное число опор (запас под горизонтальный разброс)")]
        private float chainPlanningSlack = 0.85f;
        [SerializeField, Min(0f), Tooltip("Высота первой опоры над низом слоя (>= PlacementRules.safeZoneHeight)")]
        private float chainEntryOffset = 6.1f;
        [SerializeField, Min(0f), Tooltip("Отступ последней опоры от верха слоя (>= PlacementRules.edgeMargin)")]
        private float chainExitOffset = 2.1f;
        [SerializeField, Min(0f), Tooltip("Отступ по x от края слоя для опор (>= PlacementRules.edgeMargin)")]
        private float chainEdgeReserve = 3f;
        [SerializeField, Range(0f, 1f), Tooltip("Доля полуширины, в которой лежат стыки цепочек соседних слоёв")]
        private float chainBoundarySpread = 0.3f;
        [SerializeField, Min(0f), Tooltip("Единиц mobility на единицу высоты слоя при d = 0")]
        private float maxMobilityDensity = 2.4f;
        [SerializeField, Min(0f), Tooltip("Единиц mobility на единицу высоты слоя при d = 1 (бюджет не опускается ниже минимума для проходимости)")]
        private float minMobilityDensity = 1.6f;
        [SerializeField, Range(0f, 1f), Tooltip("Доля опор цепочки, занятых вредными объектами, при d = 0")]
        private float minRiskShare = 0f;
        [SerializeField, Range(0f, 1f), Tooltip("Доля опор цепочки, занятых вредными объектами, при d = 1")]
        private float maxRiskShare = 0.5f;
        [SerializeField, Min(0.001f), Tooltip("Макс. прирост сложности между соседними слоями (pressure budget)")]
        private float maxDifficultyDeltaPerLayer = 0.05f;
        [Header("Формы слоя")]
        [SerializeField, Tooltip("Веса форм и параметры кластеров; пусто = значения по умолчанию")]
        private LayoutProfile layoutProfile;
        [SerializeField] private PacingBeat[] pacing =
        {
            new() {every = 4, offset = 3, threatMultiplier = 0.45f},
            new() {every = 10, offset = 9, threatMultiplier = 1.5f}
        };

        [SerializeField, Min(0f), Tooltip("Рост угрозы после выхода на максимум кривой (бесконечный режим): 1 + k * ln(1 + слоёв сверх кривой)")]
        private float endlessThreatGrowth = 0.5f;

        public int LayerCount => layerCount;
        public float MaxDifficultyDeltaPerLayer => maxDifficultyDeltaPerLayer;
        public LayoutProfile LayoutProfile => layoutProfile;
        public bool SupportChain => supportChain;
        public float ReferenceMaxGap => referenceMaxGap;
        public float ChainStepFraction => chainStepFraction;
        public float StepCap => referenceMaxGap * chainStepFraction;
        public float ChainEntryOffset => chainEntryOffset;
        public float ChainExitOffset => chainExitOffset;
        public float ChainEdgeReserve => chainEdgeReserve;
        public float ChainBoundarySpread => chainBoundarySpread;

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

        public int EvaluateMinSupports(float height)
        {
            float span = Mathf.Max(0f, height - chainEntryOffset - chainExitOffset);
            return Mathf.Max(2, Mathf.CeilToInt(span / (StepCap * chainPlanningSlack)) + 1);
        }

        public float EvaluateMobility(float difficulty, float height, int minSupports, float maxSupportMobility)
        {
            float curve = Mathf.Lerp(maxMobilityDensity, minMobilityDensity, difficulty) * height;
            return Mathf.Max(curve, minSupports * maxSupportMobility * 1.001f);
        }

        public float EvaluateRiskShare(float difficulty) => Mathf.Lerp(minRiskShare, maxRiskShare, difficulty);

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
