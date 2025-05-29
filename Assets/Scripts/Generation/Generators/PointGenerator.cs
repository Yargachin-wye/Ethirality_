using System.Collections;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Generator.Generators
{
    public class PointGenerator : BaseGenerator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private Transform pointPosition;

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            PointsContainerGenerator.Instance.AddPoint(pointsTypes, pointPosition.position);
            yield return null;
        }
    }
}