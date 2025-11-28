using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    [CreateAssetMenu(fileName = "CollectibleGenerator", menuName = "ChunkGenerators/Collectible")]
    public class CollectibleGenerator : BaseChunkGenerator
    {
        public float density = 0.02f;

        public override List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty)
        {
            List<PointData> points = new List<PointData>();
            int numItems = Mathf.RoundToInt(chunkHeight * density * (1f + difficulty * 0.3f));

            UnityEngine.Random.InitState(seed + (int)chunkYStart * 2);

            for (int i = 0; i < numItems; i++)
            {
                Vector2 pos = new Vector2(UnityEngine.Random.Range(-width / 2f, width / 2f), UnityEngine.Random.Range(0f, chunkHeight));
                points.Add(new PointData
                {
                    position = pos,
                    color = Color.yellow,
                    size = 0.8f,
                    tag = "Collectible"
                });
            }
            return points;
        }
    }
}