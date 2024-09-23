using System.Collections;
using System.Threading.Tasks;
using Managers.Pools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Managers.Generator.Generators
{
    public class PerlinNoise : Generator
    {
        [SerializeField] private PointsTypes points1Types;
        [SerializeField] private PointsTypes points2Types;

        [SerializeField] private int maxPoints;

        [SerializeField] private Vector2 size;

        [SerializeField] private bool isMandatoryGenerationEachPoint;

        [FormerlySerializedAs("perlinDiv")] [SerializeField]
        private float perlinMult;

        [SerializeField, Range(0, 1)] private float perlinValMin;
        [SerializeField, Range(0, 1)] private float perlinValMax;

        [SerializeField] private float perlinOffsetX;
        [SerializeField] private float perlinOffsetY;
        [SerializeField] private bool isInWaypointsPath1;
        [SerializeField] private bool isOutWaypointsPath1;
        [SerializeField] private bool isInWaypointsPath2;
        [SerializeField] private bool isOutWaypointsPath2;

        [SerializeField] private WaypointsPath waypointsPath1;
        [SerializeField] private WaypointsPath waypointsPath2;
        [SerializeField, Range(0, 100)] private int chancePointType2;
        int maxGenIters = 999999;

        public override IEnumerator Init(System.Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            for (int i = 0; i < maxPoints; i++)
            {
                float randomX = (float)Random.NextDouble() * size.x + Position.x - size.x / 2;
                float randomY = (float)Random.NextDouble() * size.y + Position.y - size.y / 2;

                float xCoord = randomX * perlinMult + perlinOffsetX;
                float yCoord = randomY * perlinMult + perlinOffsetY;

                Vector2 randomPosition = new Vector2(randomX, randomY);

                float perlinNoise1 = Mathf.PerlinNoise(xCoord, yCoord);
                float perlinNoise2 = Mathf.PerlinNoise(yCoord, xCoord);
                float perlinNoise = (perlinNoise1 + perlinNoise2) / 2.0f;

                if (
                    isInWaypointsPath1 && !waypointsPath1.IsPointInsideContour(randomPosition) ||
                    isOutWaypointsPath1 && waypointsPath1.IsPointInsideContour(randomPosition) ||
                    isInWaypointsPath2 && !waypointsPath2.IsPointInsideContour(randomPosition) ||
                    isOutWaypointsPath2 && waypointsPath2.IsPointInsideContour(randomPosition) ||
                    perlinValMin > perlinNoise ||
                    perlinValMax < perlinNoise)
                {
                    if (isMandatoryGenerationEachPoint) i--;
                    continue;
                }

                if (Random.Next(0, 100) < chancePointType2)
                {
                    PointsContainer.Instance.AddPoint(points2Types, randomPosition);
                }
                else
                {
                    PointsContainer.Instance.AddPoint(points1Types, randomPosition);
                }


                if (i == maxGenIters)
                {
                    Debug.LogError($"maxGenIters in RandomPointsSquare");
                    yield return null;
                }
            }
            Debug.Log("PerlinNoise Inited");
            yield return null;
        }
    }
}