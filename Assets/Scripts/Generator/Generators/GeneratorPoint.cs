using System.Collections;
using Managers.Generator.Generators;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Generator.Generators
{
    public class GeneratorPoint : Managers.Generator.Generator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private Transform pointPosition;

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            PointsContainer.Instance.AddPoint(pointsTypes, pointPosition.position);
            yield return null;
        }
    }
}