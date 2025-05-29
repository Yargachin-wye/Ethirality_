using System.Collections;
using System.Collections.Generic;
using Generator;
using Generator.Generators;
using Managers.Pools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Generation.Generators
{
    public class PathLinesGenerator : BaseGenerator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [FormerlySerializedAs("baseGeneratorShell1")] [FormerlySerializedAs("generatorShell1")] [SerializeField] private ShellGenerator shell1;
        [FormerlySerializedAs("baseGeneratorShell2")] [FormerlySerializedAs("generatorShell2")] [SerializeField] private ShellGenerator shell2;
        [FormerlySerializedAs("wormLikeLineGenerator")] [FormerlySerializedAs("wormLikeLineBaseGeneratorGenerator")] [FormerlySerializedAs("wormLikeLineBaseGenerator")] [SerializeField] private WormLineGenerator wormLineGenerator;

        private List<Vector2[]> _lines = new();

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            _lines = new();

            Transform[] nodes1 = shell1.waypointsPath.GetNodes();
            Transform[] nodes2 = shell2.waypointsPath.GetNodes();

            int min1 = nodes1.Length / 2 + 2;
            int max1 = nodes1.Length;

            for (int i = min1; i < max1; i++)
            {
                Vector2[] line = wormLineGenerator.GetPath(Random,
                    nodes1[i].position,
                    nodes2[i].position);

                _lines.Add(line);
                foreach (var point in line)
                {
                    PointsContainerGenerator.Instance.AddPoint(pointsTypes, point);
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