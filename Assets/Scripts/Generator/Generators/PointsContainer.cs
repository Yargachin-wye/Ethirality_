using System;
using System.Collections;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using UnityEngine;
using Random = System.Random;

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

    public class PointsContainer : Generator.Generator
    {
        [SerializeField] private List<PointsContainerPack> pointsContainerPackList;
        [SerializeField] private ChunksController chunksController;
        private Dictionary<PointsTypes, PointsContainerPack> _pointsDict = new();

        [SerializeField] private float distToPlayerForSpawn = 10;
        [SerializeField] private int chunkSize = 10;
        public int ChunkSize => chunkSize;

        public static PointsContainer Instance;

        public Dictionary<Vector2Int, List<ChunksController.SpawnPoint>> Chunks = new Dictionary<Vector2Int, List<ChunksController.SpawnPoint>>();
        private Dictionary<Vector2, PointsTypes> _pointsList = new Dictionary<Vector2, PointsTypes>();


        private void Awake()
        {
            _pointsDict = new();
            foreach (var point in pointsContainerPackList)
            {
                _pointsDict.Add(point.name, point);
            }

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
            Chunks.Clear();
            _pointsList = new();
            Chunks = new();
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

            if (!Chunks.ContainsKey(chunkCoords))
            {
                Chunks[chunkCoords] = new List<ChunksController.SpawnPoint>();
            }

            ChunksController.SpawnPoint sp = new ChunksController.SpawnPoint
            {
                Position = position,
                Type = pointType
            };

            Chunks[chunkCoords].Add(sp);
        }

        public Vector2Int GetChunkCoords(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
        }

        public List<ChunksController.SpawnPoint> GetPointsInChunk(Vector2Int chunkCoords)
        {
            if (Chunks.ContainsKey(chunkCoords))
            {
                return Chunks[chunkCoords];
            }

            return new List<ChunksController.SpawnPoint>();
        }

        

        public override IEnumerator Init(Random random, Vector2 position)
        {
            yield return base.Init(random, position);
            foreach (var chunk in Chunks)
            {
                foreach (var spawnPoint in chunk.Value)
                {
                    PointsContainerPack pack = _pointsDict[spawnPoint.Type];

                    spawnPoint.CharacterDefinition =
                        SelectRandomWithProbability(pack.characterDefinitionsPool, pack.probabilities, Random);
                }
            }

            chunksController.Init();
            yield return null;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR

            foreach (var point in _pointsList)
            {
                PointsContainerPack pack = _pointsDict[point.Value];
                Gizmos.color = pack.color;
                Gizmos.DrawSphere(point.Key, pack.radius);
            }

            Gizmos.color = Color.green;
            foreach (var chunk in Chunks)
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
            public float[] probabilities;
            public CharacterDefinition[] characterDefinitionsPool;
        }

        private static CharacterDefinition SelectRandomWithProbability(CharacterDefinition[] elements,
            float[] probabilities, Random rand)
        {
            double randomValue = rand.NextDouble();

            double cumulativeProbability = 0.0;

            for (int i = 0; i < elements.Length; i++)
            {
                cumulativeProbability += probabilities[i];

                if (randomValue <= cumulativeProbability)
                {
                    return elements[i];
                }
            }

            return elements[elements.Length - 1];
        }
    }
}