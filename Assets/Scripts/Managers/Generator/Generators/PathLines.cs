using System.Collections;
using System.Collections.Generic;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Managers.Generator.Generators
{
    public class PathLines : Generator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private GeneratorShell generatorShell1;
        [SerializeField] private GeneratorShell generatorShell2;
        [SerializeField] private WormLikeLineGenerator wormLikeLineGenerator;

        private List<Vector2[]> _lines = new();

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            _lines = new();

            Transform[] nodes1 = generatorShell1.waypointsPath.GetNodes();
            Transform[] nodes2 = generatorShell2.waypointsPath.GetNodes();

            int min1 = nodes1.Length / 2 + 2;
            int max1 = nodes1.Length;

            for (int i = min1; i < max1; i++)
            {
                Vector2[] line = wormLikeLineGenerator.GetPath(Random,
                    nodes1[i].position,
                    nodes2[i].position);

                _lines.Add(line);
                foreach (var point in line)
                {
                    PointsContainer.Instance.AddPoint(pointsTypes, point);
                }
            }

            Debug.Log("PathLinesGenerator Inited");
            yield return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (var line in _lines)
            {
                for (int i = 1; i < line.Length; i++)
                {
                    Gizmos.DrawLine(line[i - 1], line[i]);
                }
            }
        }
    }
}