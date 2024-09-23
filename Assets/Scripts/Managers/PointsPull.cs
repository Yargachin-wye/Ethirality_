using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class PointsPull : MonoBehaviour
    {
        Dictionary<string, GizmosPoint> _points = new Dictionary<string, GizmosPoint>();

        public void AddPoint()
        {
            if (!_points.ContainsKey(gameObject.name))
            {
                
            }
            else
            {
                
            }
        }

        private void OnDrawGizmos()
        {
        }

        public struct GizmosPoint
        {
            public Color Color;
            public List<Vector2> Points;
        }
    }
}