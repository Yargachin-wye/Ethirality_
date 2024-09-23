using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers.Pools
{
    public enum PointsTypes
    {
        Type1,
        Type2,
        Type3,
        Type4,
        Type5,
        Type6,
        Type7,
        Type8,
        Type9,
        Type10,
    }

    public class PointsContainer : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private List<PointsContainerPack> pointsContainerPackList;
        private Dictionary<PointsTypes, PointsContainerPack> _pointsListEditr = new();
#endif

        private Dictionary<Vector2, PointsTypes> _pointsList = new();
        public static PointsContainer Instance;

        private void Awake()
        {
#if UNITY_EDITOR
            _pointsListEditr = new();
            foreach (var point in pointsContainerPackList)
            {
                _pointsListEditr.Add(point.name, point);
            }
#endif
            if (Instance != null)
            {
                Debug.LogError("PointsContainer Instance != null !!!");
                return;
            }

            Instance = this;
        }

        public IEnumerator Clear()
        {
            _pointsList.Clear();
            _pointsList = new();
            yield return null;
        }

        public void AddPoint(PointsTypes pointType, Vector2 position)
        {
            if (_pointsList.ContainsKey(position))
            {
                Debug.LogError("^^^ PIZDEC");
                return;
            }

            _pointsList.Add(position, pointType);
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR

            foreach (var point in _pointsList)
            {
                PointsContainerPack pack = _pointsListEditr[point.Value];
                Gizmos.color = pack.color;
                Gizmos.DrawSphere(point.Key, pack.radius);
            }
#endif
        }

        [Serializable]
        public class PointsContainerPack
        {
            public PointsTypes name;
            public Color color = Color.white;
            public float radius = 1;
        }
    }
}