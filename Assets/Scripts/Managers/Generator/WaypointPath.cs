using System.Collections;
using System.Collections.Generic;
using Managers.Pools;
using UnityEngine;

namespace Managers.Generator
{
    public class WaypointsPath : MonoBehaviour
    {
        [SerializeField] private PointsTypes pointsTypes;
        [SerializeField] private bool hasSpawnPoints;
        [SerializeField] private float distToObstaclesTrigger = 1.5f;
        [SerializeField] private float smoothnessFactor = 1.0f;
        [SerializeField] private bool smoothRoute = true;
        [SerializeField] private Color gizmosColor = Color.red;
        [SerializeField] private Material waypointMaterial;

        private int numPoints;
        private Vector3[] points;
        private float[] distances;

        private int p0n;
        private int p1n;
        private int p2n;
        private int p3n;

        private float i;
        private Vector3 P0;
        private Vector3 P1;
        private Vector3 P2;
        private Vector3 P3;
        private bool initedConture = false;


        public float gizmosVisualisationSubsteps = 100;
        public float Length { get; private set; }
        public List<Transform> obstacles = new List<Transform>();
        public List<Transform> nodes = new List<Transform>();

        public Transform[] GetNodes()
        {
            return GetComponentsInChildren<Transform>();
        }

        public IEnumerator Generate()
        {
            Transform[] pathTransforms = GetComponentsInChildren<Transform>();
            nodes.Clear();

            for (int i = 0; i < pathTransforms.Length; i++)
            {
                if (pathTransforms[i] != transform)
                {
                    nodes.Add(pathTransforms[i]);
                }
            }

            if (nodes.Count > 1)
            {
                numPoints = nodes.Count;
                CachePositionsAndDistances();
                Length = distances[distances.Length - 1];
            }

            initedConture = true;

            for (float dist = 0; dist < Length; dist += Length / gizmosVisualisationSubsteps)
            {
                _contour.Add(GetRoutePosition(dist));
            }

            numPoints = nodes.Count;
            if (hasSpawnPoints) SetPoints();
            yield return null;
        }

        public RoutePoint GetRoutePoint(float dist)
        {
            Vector3 p1 = GetRoutePosition(dist);
            Vector3 p2 = GetRoutePosition(dist + 0.1f);
            Vector3 delta = p2 - p1;
            return new RoutePoint(p1, delta.normalized);
        }

        public Vector3 GetRoutePosition(float dist)
        {
            int point = 0;

            if (Length == 0)
            {
                Length = distances[distances.Length - 1];
            }

            dist = Mathf.Repeat(dist, Length);

            while (distances[point] < dist)
            {
                ++point;
            }

            p1n = ((point - 1) + numPoints) % numPoints;
            p2n = point;

            i = Mathf.InverseLerp(distances[p1n], distances[p2n], dist);

            Vector3 currentPosition = Vector3.zero;

            if (smoothRoute)
            {
                p0n = ((point - 2) + numPoints) % numPoints;
                p3n = (point + 1) % numPoints;

                p2n = p2n % numPoints;

                P0 = points[p0n];
                P1 = points[p1n];
                P2 = points[p2n];
                P3 = points[p3n];

                currentPosition = CatmullRom(P0, P1, P2, P3, i);
            }
            else
            {
                p1n = ((point - 1) + numPoints) % numPoints;
                p2n = point;

                currentPosition = Vector3.Lerp(points[p1n], points[p2n], i);
            }

            foreach (Transform obstacle in obstacles)
            {
                if (IsPointInsideObstacle(currentPosition, obstacle, distToObstaclesTrigger))
                {
                    currentPosition = CalculateAlternativePosition(currentPosition, obstacle);
                }
            }

            return currentPosition;
        }

        private bool IsPointInsideObstacle(Vector3 point, Transform obstacle, float obstacleRadius)
        {
            float distanceToObstacle = Vector3.Distance(point, obstacle.position);

            return distanceToObstacle <= obstacleRadius;
        }

        private Vector3 CalculateAlternativePosition(Vector3 currentPosition, Transform obstacle)
        {
            Vector3 obstacleCenter = obstacle.position;
            Vector3 toObstacle = obstacleCenter - currentPosition;
            toObstacle.y = 0f;
            float distanceToObstacle = toObstacle.magnitude;

            Vector3 normalizedToObstacle = toObstacle.normalized;

            Rigidbody rg = obstacle.GetComponent<Rigidbody>();
            float smooth = smoothnessFactor;
            if (rg.velocity.magnitude > 0.5f)
            {
                smooth = rg.velocity.magnitude / 15;
                smooth = smooth > 1 ? 1 : smooth;
                smooth = smoothnessFactor / (10 * smooth);
            }

            Vector3 altPos = currentPosition + normalizedToObstacle * (distanceToObstacle - distToObstaclesTrigger);
            Vector3 altLerpPos = Vector3.Lerp(currentPosition, altPos, smooth);
            Vector3 res = new Vector3(altLerpPos.x, currentPosition.y, altLerpPos.z);

            return res;
        }

        public struct RoutePoint
        {
            public Vector3 position;
            public Vector3 direction;

            public RoutePoint(Vector3 position, Vector3 direction)
            {
                this.position = position;
                this.direction = direction;
            }
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            return 0.5f *
                   ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
        }

        private List<Vector2> _contour = new();

        public bool IsPointInsideContour(Vector2 point)
        {
            int count = _contour.Count;
            if (count < 3)
            {
                Debug.Log($"count: {count}");
                return false;
            }

            bool isInside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 p1 = _contour[i];
                Vector2 p2 = _contour[j];

                if (((p1.y > point.y) != (p2.y > point.y)) &&
                    (point.x < (p2.x - p1.x) * (point.y - p1.y) / (p2.y - p1.y) + p1.x))
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        private void CachePositionsAndDistances()
        {
            points = new Vector3[nodes.Count + 1];
            distances = new float[nodes.Count + 1];

            float accumulateDistance = 0;
            for (int i = 0; i < points.Length; ++i)
            {
                var t1 = nodes[(i) % nodes.Count];
                var t2 = nodes[(i + 1) % nodes.Count];
                if (t1 != null && t2 != null)
                {
                    Vector3 p1 = t1.position;
                    Vector3 p2 = t2.position;
                    points[i] = nodes[i % nodes.Count].position;
                    distances[i] = accumulateDistance;
                    accumulateDistance += (p1 - p2).magnitude;
                }
            }

            Debug.Log("CachePositionsAndDistances");
        }

        private void OnDrawGizmos()
        {
            // DrawGizmos();
        }

        private void SetPoints()
        {
            if (nodes.Count > 1)
            {
                Gizmos.color = gizmosColor;
                waypointMaterial.color = gizmosColor;
                Vector3 prev = nodes[0].position;
                if (smoothRoute)
                {
                    for (float dist = 0; dist < Length; dist += Length / gizmosVisualisationSubsteps)
                    {
                        Vector3 next = GetRoutePosition(dist + 1);
                        PointsContainer.Instance.AddPoint(pointsTypes, new Vector2(next.x, next.y));
                    }
                }
                else
                {
                    for (int n = 0; n < nodes.Count; ++n)
                    {
                        Vector3 next = nodes[(n + 1) % nodes.Count].position;
                        PointsContainer.Instance.AddPoint(pointsTypes, new Vector2(next.x, next.y));
                    }
                }
            }
        }

        private void DrawGizmos()
        {
            if (nodes.Count > 1)
            {
                Gizmos.color = gizmosColor;
                waypointMaterial.color = gizmosColor;
                Vector3 prev = nodes[0].position;
                if (smoothRoute)
                {
                    for (float dist = 0; dist < Length; dist += Length / gizmosVisualisationSubsteps)
                    {
                        Vector3 next = GetRoutePosition(dist + 1);
                        Gizmos.DrawLine(prev, next);
                        prev = next;
                    }

                    Gizmos.DrawLine(prev, nodes[0].position);
                }
                else
                {
                    for (int n = 0; n < nodes.Count; ++n)
                    {
                        Vector3 next = nodes[(n + 1) % nodes.Count].position;
                        Gizmos.DrawLine(prev, next);
                        prev = next;
                    }
                }
            }
        }
    }
}