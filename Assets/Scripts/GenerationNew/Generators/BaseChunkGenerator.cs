using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    public abstract class BaseChunkGenerator : ScriptableObject, IChunkGenerator
    {
        public abstract List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty);

        protected float PerlinNoise(float x, float y, int seed)
        {
            UnityEngine.Random.InitState(seed);
            float offset = UnityEngine.Random.value * 1000f;
            return Mathf.PerlinNoise(x + offset, y + offset);
        }
    }
}