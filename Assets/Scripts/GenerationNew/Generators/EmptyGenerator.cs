using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    [CreateAssetMenu(fileName = "EmptyGenerator", menuName = "ChunkGenerators/Empty")]
    public class EmptyGenerator : BaseChunkGenerator
    {
        public float density = 0.01f; // Points per unit area

        public override List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty)
        {
            List<PointData> points = new List<PointData>();
            float area = chunkHeight * width;
            int numPoints = Mathf.RoundToInt(area * density * (1f + difficulty * 0.5f)); // Increase with difficulty

            UnityEngine.Random.InitState(seed + (int)chunkYStart);

            for (int i = 0; i < numPoints; i++)
            {
                Vector2 pos = new Vector2(UnityEngine.Random.Range(-width / 2f, width / 2f), UnityEngine.Random.Range(0f, chunkHeight));
                points.Add(new PointData
                {
                    position = pos,
                    color = Color.white,
                    size = 0.1f,
                    tag = "Background"
                });
            }
            return points;
        }
    }
}