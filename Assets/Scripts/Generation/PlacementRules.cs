using System;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class PlacementRules
    {
        [Header("Геометрия")]
        [SerializeField, Min(0f)] private float edgeMargin = 2f;
        [SerializeField, Min(0f)] private float minSpacing = 2f;

        [Header("Безопасная зона входа")]
        [SerializeField, Min(0f)] private float safeZoneHeight = 6f;
        [SerializeField, Min(0f)] private float rangedEntryClearance = 1.5f;

        [Header("Дальнобойные")]
        [SerializeField, Min(0f)] private float rangedMinRange = 2.5f;
        [SerializeField, Range(1, 8)] private int maxCoveringRanged = 2;

        private readonly List<Vector2> _placed = new();
        private readonly List<RangedSpot> _ranged = new();

        private static readonly Dictionary<CharacterDefinition, float> RangeCache = new();

        private struct RangedSpot
        {
            public Vector2 Position;
            public float Range;
        }

        public float EdgeMargin => edgeMargin;
        public float SafeZoneHeight => safeZoneHeight;

        public void Reset()
        {
            _placed.Clear();
            _ranged.Clear();
        }

        public bool TryPlace(LayerContext layer, CharacterDefinition definition, int attempts)
        {
            for (int i = 0; i < attempts; i++)
            {
                Vector2 point = layer.RandomPoint(edgeMargin);
                if (!Accepts(layer, point, definition)) continue;

                layer.AddSpawn(definition, point);
                _placed.Add(point);
                float range = GetAttackRange(definition);
                if (range >= rangedMinRange)
                    _ranged.Add(new RangedSpot { Position = point, Range = range });
                return true;
            }

            return false;
        }

        public bool Accepts(LayerContext layer, Vector2 point, CharacterDefinition definition)
        {
            Rect b = layer.Bounds;
            if (point.x < b.xMin + edgeMargin || point.x > b.xMax - edgeMargin ||
                point.y < b.yMin + edgeMargin || point.y > b.yMax - edgeMargin)
                return false;

            if (point.y < b.yMin + safeZoneHeight) return false;

            float sqrSpacing = minSpacing * minSpacing;
            foreach (Vector2 placed in _placed)
                if ((placed - point).sqrMagnitude < sqrSpacing) return false;

            float range = GetAttackRange(definition);
            if (range >= rangedMinRange)
            {
                if (Vector2.Distance(point, EntryPoint(layer)) < range + rangedEntryClearance)
                    return false;

                int covering = 0;
                foreach (RangedSpot spot in _ranged)
                    if (Vector2.Distance(point, spot.Position) < range + spot.Range &&
                        ++covering >= maxCoveringRanged)
                        return false;
            }

            return true;
        }

        public bool IsRanged(CharacterDefinition definition) => GetAttackRange(definition) >= rangedMinRange;

        public static Vector2 EntryPoint(LayerContext layer) =>
            new(layer.Bounds.center.x, layer.Bounds.yMin);

        public static float GetAttackRange(CharacterDefinition definition)
        {
            if (definition == null) return 0f;
            if (RangeCache.TryGetValue(definition, out float cached)) return cached;

            float range = 0f;
            GameObject prefab = definition.Prefab;
            if (prefab != null)
            {
                var shooter = prefab.GetComponentInChildren<EnemyShooter>(true);
                if (shooter != null)
                {
                    range = shooter.DetectionRange;
                }
                else if (prefab.GetComponentInChildren<TriggerDamage>(true) != null)
                {
                    foreach (var col in prefab.GetComponentsInChildren<Collider2D>(true))
                        range = Mathf.Max(range, col.bounds.extents.magnitude);
                }
            }

            RangeCache[definition] = range;
            return range;
        }
    }
}
