using System;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    public sealed class LayoutPlan
    {
        public LayoutShape Shape { get; }
        public LayoutProfile Profile { get; }
        public float Amplitude { get; }
        public float Cycles { get; }
        public float Sign { get; }

        public LayoutPlan(LayoutShape shape, LayoutProfile profile, float amplitude, float cycles, float sign)
        {
            Shape = shape;
            Profile = profile;
            Amplitude = amplitude;
            Cycles = cycles;
            Sign = sign;
        }

        public bool UsesShapedPath => Shape != LayoutShape.Scatter;

        public static LayoutPlan Create(int seed, int layerIndex, Func<int, float> difficultyAt, LayoutProfile profile)
        {
            profile = profile != null ? profile : LayoutProfile.Fallback;
            var shape = PickShape(seed, layerIndex, difficultyAt, profile);
            var random = new Random(LayoutSeed(seed, layerIndex));
            var amp = profile.AmplitudeRange;
            float amplitude = Mathf.Lerp(amp.x, amp.y, (float) random.NextDouble());
            float sign = random.NextDouble() < 0.5 ? -1f : 1f;
            float cycles = shape switch
            {
                LayoutShape.Zigzag => 3 + random.Next(0, 3),
                LayoutShape.Serpentine => random.NextDouble() < 0.5 ? 1f : 1.5f,
                _ => 1f
            };
            return new LayoutPlan(shape, profile, amplitude, cycles, sign);
        }

        public static int LayoutSeed(int seed, int layerIndex)
        {
            unchecked { return (seed * 83492791) ^ (layerIndex * 668265263) ^ 0x1b873593; }
        }

        public static LayoutShape PickShape(int seed, int layerIndex, Func<int, float> difficultyAt, LayoutProfile profile)
        {
            LayoutShape? prev = null;
            int start = profile.AvoidRepeat ? 0 : layerIndex;
            LayoutShape shape = LayoutShape.Scatter;
            for (int j = start; j <= layerIndex; j++)
            {
                var random = new Random(LayoutSeed(seed, j) ^ 0x2545F491);
                shape = Choose(random, difficultyAt(j), profile, prev);
                prev = shape;
            }

            return shape;
        }

        private static LayoutShape Choose(Random random, float difficulty, LayoutProfile profile, LayoutShape? exclude)
        {
            float total = 0f;
            foreach (var e in profile.Shapes)
                if (Allowed(e, difficulty, exclude)) total += e.weight;

            if (total <= 0f)
            {
                if (exclude.HasValue) return Choose(random, difficulty, profile, null);
                return LayoutShape.Scatter;
            }

            double roll = random.NextDouble() * total;
            LayoutShape last = LayoutShape.Scatter;
            foreach (var e in profile.Shapes)
            {
                if (!Allowed(e, difficulty, exclude)) continue;
                last = e.shape;
                roll -= e.weight;
                if (roll <= 0) break;
            }

            return last;
        }

        private static bool Allowed(LayoutProfile.ShapeEntry e, float difficulty, LayoutShape? exclude) =>
            e.weight > 0f && difficulty >= e.minDifficulty && difficulty <= Mathf.Max(e.maxDifficulty, e.minDifficulty) &&
            (!exclude.HasValue || exclude.Value != e.shape);

        public float Offset(float t)
        {
            float env = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 5f);
            switch (Shape)
            {
                case LayoutShape.Zigzag:
                    return Sign * (Mathf.PingPong(t * Cycles, 1f) * 2f - 1f) * env;
                case LayoutShape.Serpentine:
                    return Sign * Mathf.Sin(2f * Mathf.PI * Cycles * t) * env;
                case LayoutShape.Fork:
                    return Sign * Mathf.Sin(Mathf.PI * t);
                default:
                    return 0f;
            }
        }

        public float TargetX(float t, float startX, float endX, float halfRange) =>
            Mathf.Lerp(startX, endX, t) + Offset(t) * Amplitude * halfRange;
    }
}
