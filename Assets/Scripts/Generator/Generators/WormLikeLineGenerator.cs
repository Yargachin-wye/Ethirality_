using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace Managers.Generator.Generators
{
    public class WormLikeLineGenerator : Generator
    {
        [SerializeField] private int length = 50;
        [SerializeField] private float stepSize = 2;
        private Vector2[] _points = new Vector2[0];

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            Debug.Log("WormLikeLineGenerator Inited");
            yield return null;
        }

        private void OnDrawGizmos()
        {
            if (_points == null || _points.Length == 0) return;

            Gizmos.color = Color.red;
            for (int i = 1; i < _points.Length; i++)
            {
                Gizmos.DrawLine(_points[i - 1], _points[i]);
            }
        }

        public Vector2[] GetPath(Random r,Vector2 start, Vector2 end)
        {
            if (Mathf.Abs(stepSize) < 0.05f) return null;
            List<Vector2> path = new List<Vector2>();
            Vector2 currentPosition = start;
            path.Add(currentPosition);

            while (Vector2.Distance(currentPosition, end) > stepSize)
            {
                Vector2 direction = (end - currentPosition).normalized;
                float randomAngle = (float)(r.NextDouble() * Mathf.PI / 1.5f - Mathf.PI / 3);

                Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle * Mathf.Rad2Deg) * direction;
                currentPosition += randomDirection * stepSize;
                path.Add(currentPosition);
            }

            path.Add(end); // Ensure the end point is included
            return path.ToArray();
        }
    }
}