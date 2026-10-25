using System.Collections.Generic;
using Definitions;
using UnityEngine;

namespace Generation
{
    public static class SupportChain
    {
        private const float ReachSafety = 0.98f;
        private const float YJitter = 0.1f;

        public static int Build(LayerContext layer, PlacementRules rules, List<CharacterDefinition> safe,
            List<CharacterDefinition> threats)
        {
            ClimbPlan climb = layer.Climb;
            EnemySpawnTable table = layer.SpawnTable;
            if (climb == null || !climb.ChainEnabled || safe.Count == 0 || table == null) return 0;

            var random = layer.Random;
            Rect b = layer.Bounds;
            int n = safe.Count;
            float xLo = b.xMin + climb.EdgeReserve, xHi = b.xMax - climb.EdgeReserve;
            float y0 = b.yMin + climb.EntryOffset, y1 = b.yMax - climb.ExitOffset;

            var ys = new float[n];
            if (n == 1) ys[0] = (y0 + y1) / 2f;
            else
            {
                float dy = (y1 - y0) / (n - 1);
                for (int k = 0; k < n; k++)
                {
                    bool edge = k == 0 || k == n - 1;
                    float jitter = edge ? 0f : ((float) random.NextDouble() * 2f - 1f) * YJitter * dy;
                    ys[k] = y0 + dy * k + jitter;
                }
            }

            var dxMax = new float[Mathf.Max(0, n - 1)];
            var suffix = new float[n];
            for (int j = 0; j < n - 1; j++)
            {
                float step = ys[j + 1] - ys[j];
                dxMax[j] = Mathf.Sqrt(Mathf.Max(0f, climb.StepCap * climb.StepCap - step * step)) * ReachSafety;
            }

            for (int k = n - 2; k >= 0; k--) suffix[k] = suffix[k + 1] + dxMax[k];

            var layout = layer.Layout;
            bool shaped = layout != null && layout.UsesShapedPath;
            var xs = new float[n];
            float xEnd = Mathf.Clamp(climb.EndX, xLo, xHi);
            xs[0] = Mathf.Clamp(climb.StartX, xLo, xHi);
            for (int k = 1; k < n; k++)
            {
                if (k == n - 1)
                {
                    xs[k] = xEnd;
                    break;
                }

                float lo = Mathf.Max(xs[k - 1] - dxMax[k - 1], xEnd - suffix[k], xLo);
                float hi = Mathf.Min(xs[k - 1] + dxMax[k - 1], xEnd + suffix[k], xHi);
                float t = (float) random.NextDouble();
                if (lo > hi) xs[k] = (lo + hi) / 2f;
                else if (shaped)
                {
                    float u = (ys[k] - y0) / Mathf.Max(0.001f, y1 - y0);
                    float target = layout.TargetX(u, climb.StartX, xEnd, (xHi - xLo) / 2f);
                    float spread = (hi - lo) / 2f * layout.Profile.PathJitter * (t * 2f - 1f);
                    xs[k] = Mathf.Clamp(target + spread, lo, hi);
                }
                else xs[k] = Mathf.Lerp(lo, hi, t);
            }

            var risky = new List<int>();
            int placed = 0;
            for (int k = 0; k < n; k++)
            {
                var point = new Vector2(xs[k], ys[k]);
                bool wantRisk = (float) random.NextDouble() < climb.RiskShare && k > 0 && k < n - 1;

                if (wantRisk)
                {
                    risky.Clear();
                    for (int i = 0; i < threats.Count; i++)
                        if (table.TryGetEntry(threats[i], out var e) && e.IsSupport) risky.Add(i);

                    if (risky.Count > 0)
                    {
                        int idx = risky[random.Next(risky.Count)];
                        var def = threats[idx];
                        if (rules.TryPlaceAt(layer, def, point))
                        {
                            threats.RemoveAt(idx);
                            layer.Chain.Add(new LayerContext.ChainNode(def, point, true));
                            placed++;
                            continue;
                        }
                    }
                }

                var support = safe[0];
                if (!rules.TryPlaceAt(layer, support, point)) continue;
                safe.RemoveAt(0);
                layer.Chain.Add(new LayerContext.ChainNode(support, point, false));
                placed++;
            }

            return placed;
        }
    }
}
