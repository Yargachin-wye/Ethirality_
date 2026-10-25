using System;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Layout Profile")]
    public class LayoutProfile : ScriptableObject
    {
        [Serializable]
        public struct ShapeEntry
        {
            public LayoutShape shape;
            [Min(0f)] public float weight;
            [Range(0f, 1f)] public float minDifficulty;
            [Range(0f, 1f)] public float maxDifficulty;
        }

        [SerializeField] private ShapeEntry[] shapes =
        {
            new() {shape = LayoutShape.Zigzag, weight = 3f, minDifficulty = 0f, maxDifficulty = 1f},
            new() {shape = LayoutShape.Serpentine, weight = 3f, minDifficulty = 0f, maxDifficulty = 1f},
            new() {shape = LayoutShape.Column, weight = 2f, minDifficulty = 0.15f, maxDifficulty = 1f},
            new() {shape = LayoutShape.Fork, weight = 2f, minDifficulty = 0.3f, maxDifficulty = 1f}
        };

        [Header("Маршрут")]
        [SerializeField, Tooltip("Доля доступной полуширины, на которую маршрут отклоняется от прямой (min, max)")]
        private Vector2 amplitudeRange = new(0.35f, 0.6f);
        [SerializeField, Range(0f, 1f), Tooltip("Случайный разброс опоры вокруг линии формы (доля горизонтального запаса шага)")]
        private float pathJitter = 0.25f;
        [SerializeField, Tooltip("Не повторять форму двух соседних слоёв")]
        private bool avoidRepeat = true;

        [Header("Кластеры")]
        [SerializeField, Range(0f, 1f), Tooltip("Доля брошенной еды, раскладываемой «крошками» вдоль пути")]
        private float foodTrailShare = 0.4f;
        [SerializeField, Range(0f, 1f), Tooltip("Доля брошенных угроз, раскладываемых формациями (остальное - случайный заполнитель)")]
        private float clusterThreatShare = 0.6f;
        [SerializeField, Min(2f), Tooltip("Просвет «ворот» между двумя угрозами")]
        private float gateGap = 4.5f;
        [SerializeField, Min(2f), Tooltip("Полуширина коридора Column")]
        private float flankHalfWidth = 5f;
        [SerializeField, Min(2f), Tooltip("Шаг угроз вдоль стен коридора Column по высоте")]
        private float flankSpacing = 7f;
        [SerializeField, Min(2f), Tooltip("Расстояние часового до опоры на внешнем изгибе Serpentine")]
        private float sentryDistance = 6.5f;
        [SerializeField, Range(0, 8), Tooltip("Еда приманки на ложной ветке Fork")]
        private int decoyFood = 4;
        [SerializeField, Range(0, 6), Tooltip("Охранники приманки на ложной ветке Fork")]
        private int decoyGuards = 3;

        public ShapeEntry[] Shapes => shapes;
        public Vector2 AmplitudeRange => amplitudeRange;
        public float PathJitter => pathJitter;
        public bool AvoidRepeat => avoidRepeat;
        public float FoodTrailShare => foodTrailShare;
        public float ClusterThreatShare => clusterThreatShare;
        public float GateGap => gateGap;
        public float FlankHalfWidth => flankHalfWidth;
        public float FlankSpacing => flankSpacing;
        public float SentryDistance => sentryDistance;
        public int DecoyFood => decoyFood;
        public int DecoyGuards => decoyGuards;

        private static LayoutProfile _fallback;

        public static LayoutProfile Fallback
        {
            get
            {
                if (_fallback == null)
                {
                    _fallback = CreateInstance<LayoutProfile>();
                    _fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return _fallback;
            }
        }
    }
}
