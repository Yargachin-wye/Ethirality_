using System.Collections.Generic;
using Definitions;
using UnityEngine;

namespace Generation
{
    public static class LayoutDecorator
    {
        public struct Result
        {
            public int ThreatsPlaced;
            public int FoodPlaced;
        }

        public static Result Apply(LayerContext layer, PlacementRules rules, List<CharacterDefinition> threats,
            List<CharacterDefinition> food)
        {
            var result = new Result();
            var layout = layer.Layout;
            if (layout == null || !layout.UsesShapedPath || layer.Chain.Count < 2) return result;

            var profile = layout.Profile;
            var ctx = new Ctx
            {
                Layer = layer,
                Rules = rules,
                Threats = threats,
                Food = food,
                ThreatQuota = Mathf.FloorToInt(threats.Count * profile.ClusterThreatShare),
                FoodQuota = Mathf.FloorToInt(food.Count * profile.FoodTrailShare),
                Profile = profile
            };

            switch (layout.Shape)
            {
                case LayoutShape.Zigzag:
                    Gates(ctx, 1);
                    break;
                case LayoutShape.Serpentine:
                    Sentries(ctx);
                    Gates(ctx, 2);
                    break;
                case LayoutShape.Column:
                    Flanks(ctx);
                    Gates(ctx, 3);
                    break;
                case LayoutShape.Fork:
                    Decoy(ctx);
                    Gates(ctx, 2);
                    break;
            }

            FoodTrail(ctx);

            result.ThreatsPlaced = ctx.ThreatsPlaced;
            result.FoodPlaced = ctx.FoodPlaced;
            return result;
        }

        private sealed class Ctx
        {
            public LayerContext Layer;
            public PlacementRules Rules;
            public List<CharacterDefinition> Threats;
            public List<CharacterDefinition> Food;
            public LayoutProfile Profile;
            public int ThreatQuota;
            public int FoodQuota;
            public int ThreatsPlaced;
            public int FoodPlaced;
            public CharacterDefinition Motif;
            public bool? MotifRanged;

            public bool ThreatLeft => ThreatsPlaced < ThreatQuota && Threats.Count > 0;
            public bool FoodLeft => FoodPlaced < FoodQuota && Food.Count > 0;
        }

        private static void BeginFormation(Ctx c, bool? ranged)
        {
            c.MotifRanged = ranged;
            c.Motif = PickMotif(c);
        }

        private static CharacterDefinition PickMotif(Ctx c)
        {
            var counts = new Dictionary<CharacterDefinition, int>();
            CharacterDefinition best = null, bestAny = null;
            int bestCount = 0, bestAnyCount = 0;
            foreach (var def in c.Threats)
            {
                counts.TryGetValue(def, out int n);
                counts[def] = ++n;
                if (n > bestAnyCount) { bestAny = def; bestAnyCount = n; }
                if (c.MotifRanged.HasValue && c.Rules.IsRanged(def) != c.MotifRanged.Value) continue;
                if (n > bestCount) { best = def; bestCount = n; }
            }

            return best != null ? best : bestAny;
        }

        private static bool PlaceThreat(Ctx c, Vector2 point)
        {
            if (!c.ThreatLeft) return false;
            int idx = c.Motif != null ? c.Threats.IndexOf(c.Motif) : -1;
            if (idx < 0)
            {
                c.Motif = PickMotif(c);
                idx = c.Motif != null ? c.Threats.IndexOf(c.Motif) : -1;
            }

            if (idx < 0 || !c.Rules.TryPlaceAt(c.Layer, c.Threats[idx], point)) return false;
            c.Threats.RemoveAt(idx);
            c.ThreatsPlaced++;
            return true;
        }

        private static bool PlaceFood(Ctx c, Vector2 point, bool force = false)
        {
            if (c.Food.Count == 0 || (!force && !c.FoodLeft)) return false;
            int idx = c.Layer.Random.Next(c.Food.Count);
            if (!c.Rules.TryPlaceAt(c.Layer, c.Food[idx], point)) return false;
            c.Food.RemoveAt(idx);
            c.FoodPlaced++;
            return true;
        }

        private static void Gates(Ctx c, int stride)
        {
            var chain = c.Layer.Chain;
            float half = c.Profile.GateGap / 2f;
            BeginFormation(c, false);
            for (int k = 0; k + 1 < chain.Count && c.ThreatLeft; k += stride)
            {
                Vector2 a = chain[k].Position, b = chain[k + 1].Position;
                Vector2 mid = (a + b) / 2f;
                Vector2 dir = (b - a).normalized;
                Vector2 perp = new(-dir.y, dir.x);
                PlaceThreat(c, mid + perp * half);
                PlaceThreat(c, mid - perp * half);
            }
        }

        private static void Sentries(Ctx c)
        {
            var chain = c.Layer.Chain;
            var bounds = c.Layer.Bounds;
            BeginFormation(c, true);
            for (int k = 1; k + 1 < chain.Count && c.ThreatLeft; k++)
            {
                Vector2 prev = chain[k - 1].Position, p = chain[k].Position, next = chain[k + 1].Position;
                float bend = (p.x - prev.x) * (next.x - p.x);
                if (bend >= 0f) continue;
                float outward = Mathf.Sign(p.x - (prev.x + next.x) / 2f);
                float x = Mathf.Clamp(p.x + outward * c.Profile.SentryDistance, bounds.xMin, bounds.xMax);
                PlaceThreat(c, new Vector2(x, p.y));
            }
        }

        private static void Flanks(Ctx c)
        {
            var chain = c.Layer.Chain;
            float y0 = chain[0].Position.y, y1 = chain[chain.Count - 1].Position.y;
            float spacing = c.Profile.FlankSpacing;
            float side = c.Layer.Random.NextDouble() < 0.5 ? -1f : 1f;
            BeginFormation(c, false);
            for (float y = y0 + spacing / 2f; y < y1 && c.ThreatLeft; y += spacing)
            {
                float x = PathX(chain, y) + side * c.Profile.FlankHalfWidth;
                PlaceThreat(c, new Vector2(x, y));
                side = -side;
            }
        }

        private static void Decoy(Ctx c)
        {
            var chain = c.Layer.Chain;
            var bounds = c.Layer.Bounds;
            float yMid = (chain[0].Position.y + chain[chain.Count - 1].Position.y) / 2f;
            float pathX = PathX(chain, yMid);
            float side = pathX >= bounds.center.x ? -1f : 1f;
            float reach = Mathf.Max(0f, bounds.width / 2f - c.Rules.EdgeMargin - 3f);
            Vector2 center = new(bounds.center.x + side * reach * 0.8f, yMid);

            int placedFood = 0;
            for (int i = 0; i < c.Profile.DecoyFood; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, c.Profile.DecoyFood);
                var p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i == 0 ? 0f : 1.6f);
                if (PlaceFood(c, p, true)) placedFood++;
            }

            if (placedFood == 0) return;
            int guards = c.Profile.DecoyGuards;
            float phase = (float) c.Layer.Random.NextDouble() * Mathf.PI * 2f;
            BeginFormation(c, false);
            for (int i = 0; i < guards && c.ThreatLeft; i++)
            {
                float angle = phase + i * Mathf.PI * 2f / guards;
                PlaceThreat(c, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4.2f);
            }
        }

        private static void FoodTrail(Ctx c)
        {
            var chain = c.Layer.Chain;
            var random = c.Layer.Random;
            for (int k = 0; k + 1 < chain.Count && c.FoodLeft; k++)
            {
                Vector2 a = chain[k].Position, b = chain[k + 1].Position;
                Vector2 dir = (b - a).normalized;
                Vector2 perp = new(-dir.y, dir.x);
                for (int s = 1; s <= 3 && c.FoodLeft; s++)
                {
                    float off = s == 2 ? 0f : ((float) random.NextDouble() * 2f - 1f) * 0.8f;
                    PlaceFood(c, Vector2.Lerp(a, b, s / 4f) + perp * off);
                }
            }
        }

        private static float PathX(List<LayerContext.ChainNode> chain, float y)
        {
            for (int k = 0; k + 1 < chain.Count; k++)
            {
                Vector2 a = chain[k].Position, b = chain[k + 1].Position;
                if (y >= a.y && y <= b.y)
                    return Mathf.Lerp(a.x, b.x, Mathf.InverseLerp(a.y, b.y, y));
            }

            return y < chain[0].Position.y ? chain[0].Position.x : chain[chain.Count - 1].Position.x;
        }
    }
}
