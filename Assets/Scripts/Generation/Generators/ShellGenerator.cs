using System;
using System.Collections;
using Generator;
using Managers.Generator;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Generation.Generators
{
    [RequireComponent(typeof(WaypointsPath))]
    public class ShellGenerator : BaseGenerator
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private GameObject prefab;
        [SerializeField] private float centerMaxRadius;
        [SerializeField] private float radius;
        [SerializeField] private int numPoints;
        [SerializeField] private float maxDeviation;

        [SerializeField, HideInInspector] public WaypointsPath waypointsPath;
        private GameObject[] _points = new GameObject[0];

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);

            double angle = Random.NextDouble() * Math.PI * 2;
            double distance = Random.NextDouble() * centerMaxRadius;

            float x = Position.x + (float)(distance * Math.Cos(angle));
            float y = Position.y + (float)(distance * Math.Sin(angle));
            GenerateIrregularCircle(new Vector2(x, y), radius, numPoints, maxDeviation);
            yield return StartCoroutine(waypointsPath.Generate());
            Debug.Log("GeneratorShell Inited");
            yield return null;
        }

        private void OnValidate()
        {
            if (waypointsPath == null) waypointsPath = GetComponent<WaypointsPath>();
        }

        void GenerateIrregularCircle(Vector2 center, float radius, int numPoints, float maxDeviation)
        {
            foreach (var point in _points)
            {
                Destroy(point);
            }

            _points = new GameObject[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints;

                float currentRadius = radius + ((float)Random.NextDouble() * 2 - 1) * maxDeviation;

                float x = center.x + currentRadius * Mathf.Cos(angle);
                float y = center.y + currentRadius * Mathf.Sin(angle);

                GameObject obj = Instantiate(prefab, transform);
                obj.transform.position = new Vector2(x, y);
                _points[i] = obj;
            }
        }
    }
}