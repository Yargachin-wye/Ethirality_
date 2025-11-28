using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    // Example Generator 2: Obstacles (walls, spikes, moving lines - simulated as point clusters)
    [CreateAssetMenu(fileName = "ObstacleGenerator", menuName = "ChunkGenerators/Obstacle")]
    public class ObstacleGenerator : BaseChunkGenerator
    {
        public float baseDensity = 0.05f;
        public float clusterSize = 5f; // Size of obstacle clusters

        public override List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty)
        {
            List<PointData> points = new List<PointData>();
            float density = baseDensity * (1f + difficulty); // Increase density with difficulty

            int numObstacles = Mathf.RoundToInt(chunkHeight * density);

            for (int i = 0; i < numObstacles; i++)
            {
                float y = chunkYStart + i * (chunkHeight / numObstacles);
                float noise = PerlinNoise(0f, y / 10f, seed);
                float xCenter = Mathf.Lerp(-width / 2f + clusterSize, width / 2f - clusterSize, noise);

                // Create a cluster (e.g., vertical wall or spike)
                for (float dy = 0; dy < clusterSize; dy += 0.5f)
                {
                    for (float dx = -0.5f; dx <= 0.5f; dx += 0.5f)
                    {
                        Vector2 pos = new Vector2(xCenter + dx, (y - chunkYStart) + dy);
                        points.Add(new PointData
                        {
                            position = pos,
                            color = Color.red,
                            size = 0.5f,
                            tag = "Obstacle"
                        });
                    }
                }
            }
            return points;
        }
    }
}