// Generator for boundaries (left and right walls)

using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    [CreateAssetMenu(fileName = "BoundaryGenerator", menuName = "ChunkGenerators/Boundary")]
    public class BoundaryGenerator : BaseChunkGenerator
    {
        public float wallDensity = 1f; // Points per unit height

        public override List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty)
        {
            List<PointData> points = new List<PointData>();

            // Left wall
            for (float y = 0; y < chunkHeight; y += 1f / wallDensity)
            {
                float noise = PerlinNoise(-width / 2f, chunkYStart + y, seed) * 2f; // Organic wiggle
                Vector2 pos = new Vector2(-width / 2f + noise, y);
                points.Add(new PointData { position = pos, color = Color.gray, size = 0.5f, tag = "Wall" });
            }

            // Right wall
            for (float y = 0; y < chunkHeight; y += 1f / wallDensity)
            {
                float noise = PerlinNoise(width / 2f, chunkYStart + y, seed) * 2f;
                Vector2 pos = new Vector2(width / 2f - noise, y);
                points.Add(new PointData { position = pos, color = Color.gray, size = 0.5f, tag = "Wall" });
            }

            return points;
        }
    }
}