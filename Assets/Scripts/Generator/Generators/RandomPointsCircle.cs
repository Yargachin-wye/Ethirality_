using System.Collections;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Managers.Generator.Generators
{
    public class RandomPointsCircle : Generator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private int maxPoints;
        
        [SerializeField] private bool isCircle;
        [SerializeField] private float centerMaxRadius;
        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            for (int i = 0; i < maxPoints; i++)
            {
                float randomRadius = (float)Random.NextDouble() * centerMaxRadius;
                float randomAngle = (float)Random.NextDouble() * 2 * Mathf.PI;
                Vector2 randomPosition = Position + new Vector2(randomRadius * Mathf.Cos(randomAngle),
                    randomRadius * Mathf.Sin(randomAngle));
                PointsContainer.Instance.AddPoint(pointsTypes, randomPosition);
            }
            Debug.Log("RandomPointsCircle Inited");
            yield return null;
        }
    }
}