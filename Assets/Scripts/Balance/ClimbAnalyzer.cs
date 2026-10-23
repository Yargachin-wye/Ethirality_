using System;
using System.Collections.Generic;
using UnityEngine;

namespace Balance
{
    public struct ClimbResult
    {
        public bool Climbable;
        public float MaxGap;
        public float ClimbTime;
        public int Hops;
    }

    public static class ClimbAnalyzer
    {
        public static ClimbResult Analyze(IReadOnlyList<Vector2> supports, Vector2 start, Vector2 end, PlayerBuild build,
            float height)
        {
            var nodes = new List<Vector2>(supports.Count + 2) { start };
            var sorted = new List<Vector2>(supports);
            sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            nodes.AddRange(sorted);
            nodes.Add(end);

            int m = nodes.Count;
            float maxGap = 0f;
            for (int i = 1; i < m; i++) maxGap = Mathf.Max(maxGap, nodes[i].y - nodes[i - 1].y);

            var best = new float[m];
            var prev = new int[m];
            for (int i = 1; i < m; i++) best[i] = float.PositiveInfinity;
            for (int t = 1; t < m; t++)
            {
                for (int s = 0; s < t; s++)
                {
                    if (float.IsPositiveInfinity(best[s]) || nodes[s].y > nodes[t].y) continue;
                    float cost = best[s] + build.EdgeTime(Vector2.Distance(nodes[s], nodes[t]));
                    if (cost < best[t])
                    {
                        best[t] = cost;
                        prev[t] = s;
                    }
                }
            }

            var result = new ClimbResult { MaxGap = maxGap, Climbable = !float.IsPositiveInfinity(best[m - 1]) };
            if (!result.Climbable)
            {
                result.ClimbTime = build.FreeClimbTime(height);
                return result;
            }

            result.ClimbTime = best[m - 1];
            for (int t = m - 1; t > 0; t = prev[t]) result.Hops++;
            return result;
        }
    }
}
