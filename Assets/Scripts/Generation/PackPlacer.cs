using System.Collections.Generic;
using Definitions;
using UnityEngine;

namespace Generation
{
    public static class PackPlacer
    {
        public static int Place(LayerContext layer, PlacementRules rules, List<CharacterDefinition> defs,
            Vector2Int packSize, float radius, int attempts)
        {
            var order = new List<CharacterDefinition>();
            var counts = new Dictionary<CharacterDefinition, int>();
            foreach (var def in defs)
            {
                if (!counts.TryGetValue(def, out int n)) order.Add(def);
                counts[def] = n + 1;
            }

            var random = layer.Random;
            int min = Mathf.Max(1, packSize.x), max = Mathf.Max(min, packSize.y);
            int placed = 0;
            foreach (var def in order)
            {
                bool solo = rules.IsRanged(def);
                for (int left = counts[def]; left > 0;)
                {
                    int size = solo ? 1 : Mathf.Min(left, random.Next(min, max + 1));
                    left -= size;
                    placed += PlacePack(layer, rules, def, size, radius, attempts);
                }
            }

            return placed;
        }

        private static int PlacePack(LayerContext layer, PlacementRules rules, CharacterDefinition def, int size,
            float radius, int attempts)
        {
            if (size == 1) return rules.TryPlace(layer, def, attempts) ? 1 : 0;

            Vector2 center = default;
            bool found = false;
            for (int i = 0; i < attempts && !found; i++)
            {
                center = layer.RandomPoint(rules.EdgeMargin);
                found = rules.TryPlaceAt(layer, def, center);
            }

            int placed = found ? 1 : 0;
            var random = layer.Random;
            for (int m = placed; m < size; m++)
            {
                bool ok = false;
                for (int i = 0; i < attempts && found && !ok; i++)
                {
                    float angle = (float) random.NextDouble() * Mathf.PI * 2f;
                    float r = radius * Mathf.Sqrt((float) random.NextDouble());
                    ok = rules.TryPlaceAt(layer, def, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
                }

                if (ok || rules.TryPlace(layer, def, attempts)) placed++;
            }

            return placed;
        }
    }
}
