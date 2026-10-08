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
        [SerializeField] private PacingBeat[] pacing =
        {
            new() {every = 5, offset = 4, threatMultiplier = 0.5f},
            new() {every = 10, offset = 9, threatMultiplier = 1.5f}
        };

        public int LayerCount => layerCount;

        public float EvaluateDifficulty(int layerIndex)
        {
            float t = layerCount > 1 ? layerIndex / (float) (layerCount - 1) : 0f;
            return Mathf.Clamp01(difficultyByProgress.Evaluate(t));
        }

        public float EvaluateThreat(float difficulty, float area, float pacingMultiplier, float layerMultiplier)
        {
            return Mathf.Lerp(minThreatPerArea, maxThreatPerArea, difficulty) * area * pacingMultiplier * layerMultiplier;
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
