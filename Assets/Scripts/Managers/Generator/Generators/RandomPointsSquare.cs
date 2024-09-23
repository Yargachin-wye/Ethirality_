using System.Collections;
using System.Collections.Generic;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Managers.Generator.Generators
{
    public class RandomPointsSquare : Generator
    {
        [SerializeField] private PointsTypes pointsTypes;

        [SerializeField] private int maxPoints;

        [SerializeField] private Vector2 size;
        [SerializeField] private bool isInWaypointsPath1;
        [SerializeField] private bool isOutWaypointsPath1;
        [SerializeField] private bool isInWaypointsPath2;
        [SerializeField] private bool isOutWaypointsPath2;
        [SerializeField] private bool isMandatoryGenerationEachPoint;
        [SerializeField] private WaypointsPath waypointsPath1;
        [SerializeField] private WaypointsPath waypointsPath2;
        int maxGenIters = 999999;
        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            for (int i = 0; i < maxPoints; i++)
            {
                float randomX = (float)Random.NextDouble() * size.x + Position.x - size.x / 2;
                float randomY = (float)Random.NextDouble() * size.y + Position.y - size.y / 2;
                Vector2 randomPosition = new Vector2(randomX, randomY);

                if (isInWaypointsPath1 && !waypointsPath1.IsPointInsideContour(randomPosition) ||
                    isOutWaypointsPath1 && waypointsPath1.IsPointInsideContour(randomPosition) ||
                    isInWaypointsPath2 && !waypointsPath2.IsPointInsideContour(randomPosition) ||
                    isOutWaypointsPath2 && waypointsPath2.IsPointInsideContour(randomPosition))
                {
                    if (isMandatoryGenerationEachPoint) i--;
                    continue;
                }

                PointsContainer.Instance.AddPoint(pointsTypes, randomPosition);
                if (i == maxGenIters)
                {
                    Debug.LogError($"maxGenIters in RandomPointsSquare");
                    yield return null;
                }
            }

            Debug.Log("RandomPointsSquare Inited");
            yield return null;
        }
    }
}