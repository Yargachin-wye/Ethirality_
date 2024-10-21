using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Definitions;
using UnityEngine;

namespace Managers.Pools
{
    public enum PointsTypes
    {
        InCore,
        Shell1,
        Shell2,
        Type4,
        Flagellum,
        Food,
        PlayerSpawn,
        Shell0,
        PlayerExit,
        Type10,
    }

    public class PointsContainer : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private List<PointsContainerPack> pointsContainerPackList;
        private Dictionary<PointsTypes, PointsContainerPack> _pointsListEditr = new();
#endif
        [SerializeField] private float distToPlayerForSpawn = 10;
        [SerializeField] private int chunkSize = 10;

        public static PointsContainer Instance;

        private Dictionary<Vector2Int, List<Vector2>> _chunks = new Dictionary<Vector2Int, List<Vector2>>();
        private Dictionary<Vector2, PointsTypes> _pointsList = new Dictionary<Vector2, PointsTypes>();


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
            _chunks.Clear();
            _pointsList = new();
            _chunks = new();
            yield return null;
        }

        public void AddPoint(PointsTypes pointType, Vector2 position)
        {
            if (_pointsList.ContainsKey(position))
            {
                Debug.LogError("^^^ Error: Point already exists.");
                return;
            }

            _pointsList.Add(position, pointType);

            Vector2Int chunkCoords = GetChunkCoords(position);

            if (!_chunks.ContainsKey(chunkCoords))
            {
                _chunks[chunkCoords] = new List<Vector2>();
            }

            _chunks[chunkCoords].Add(position);
        }

        private Vector2Int GetChunkCoords(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
        }

        public List<Vector2> GetPointsInChunk(Vector2Int chunkCoords)
        {
            if (_chunks.ContainsKey(chunkCoords))
            {
                return _chunks[chunkCoords];
            }
            else
            {
                return new List<Vector2>();
            }
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

            Gizmos.color = Color.green;
            foreach (var chunk in _chunks)
            {
                Vector2Int chunkCoords = chunk.Key;
                Vector2 chunkWorldPosition = new Vector2(chunkCoords.x * chunkSize, chunkCoords.y * chunkSize);
                Vector2 chunkSizeVector = new Vector2(chunkSize, chunkSize);
                Gizmos.DrawWireCube(chunkWorldPosition + chunkSizeVector / 2, chunkSizeVector);
            }
#endif
        }

        [Serializable]
        public class PointsContainerPack
        {
            public PointsTypes name;
            public Color color = Color.white;
            public float radius = 1;
            public List<CharacterDefinition> characterDefinitionsPool;
        }
    }
}