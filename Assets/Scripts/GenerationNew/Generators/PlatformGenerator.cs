using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew.Generators
{
    [CreateAssetMenu(fileName = "PlatformGenerator", menuName = "ChunkGenerators/Platform")]
    public class PlatformGenerator : BaseChunkGenerator
    {
        public float baseDensity = 0.1f;
        public float platformLength = 10f;

        public override List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty)
        {
            List<PointData> points = new List<PointData>();
            float density = baseDensity * (1f + difficulty * 0.2f);

            int numPlatforms = Mathf.RoundToInt(chunkHeight * density);

            for (int i = 0; i < numPlatforms; i++)
            {
                float y = chunkYStart + i * (chunkHeight / numPlatforms);
                float noise = PerlinNoise(0f, y / 5f, seed);
                float slope = Mathf.Lerp(-1f, 1f, noise) * (difficulty * 0.5f); // Steeper with difficulty
                float xStart = UnityEngine.Random.Range(-width / 2f + platformLength / 2f, width / 2f - platformLength / 2f);

                // Create chain of points
                for (float dx = 0; dx < platformLength; dx += 0.5f)
                {
                    float dy = dx * slope;
                    Vector2 pos = new Vector2(xStart + dx, (y - chunkYStart) + dy);
                    if (Mathf.Abs(pos.x) > width / 2f) continue; // Clip to width
                    points.Add(new PointData
                    {
                        position = pos,
                        color = Color.green,
                        size = 0.3f,
                        tag = "Platform"
                    });
                }
            }
            return points;
        }
    }
}