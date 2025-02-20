using System.Collections.Generic;
using Camera;
using CharacterComponents;
using Definitions;
using Managers.Pools;
using UnityEngine;

namespace Pools
{
    public class ChunksController : MonoBehaviour
    {
        [SerializeField] private PointsContainer pointsContainer;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private CharactersPool _charactersPool;
        [SerializeField] private float distToChunk;
        private Dictionary<Vector2Int, List<SpawnPoint>> Chunks => pointsContainer.Chunks;
        private float ChunkSize => pointsContainer.ChunkSize;
        private List<Vector2Int> _loadedChunks = new();
        private List<Vector2Int> deloadChunks = new List<Vector2Int>();
        private bool _inited;

        private void Awake()
        {
            _inited = false;
        }

        private void Spawn(SpawnPoint spawnPoint)
        {
            if (spawnPoint.Type == PointsTypes.PlayerSpawn)
            {
                return;
            }

            Character character = _charactersPool.GetPooledObject(spawnPoint.CharacterDefinition);
            character.gameObject.SetActive(true);
            character.transform.position = spawnPoint.Position;
            spawnPoint.GameObject = character.gameObject;
            character.Init(spawnPoint.CharacterDefinition);
            character.OnDeadAction += () => ForgetSpawnPoint(spawnPoint);
        }

        public void ForgetSpawnPoint(SpawnPoint spawnPoint)
        {
            foreach (var chunk in Chunks)
            {
                if (chunk.Value.Contains(spawnPoint))
                {
                    chunk.Value.Remove(spawnPoint);
                    return;
                }
            }
        }

        private void Despawn(Vector2Int chunk, SpawnPoint spawnPoint)
        {
            Vector2Int newChunk = pointsContainer.GetChunkCoords(spawnPoint.GameObject.transform.position);
            if (chunk != newChunk)
            {
                Chunks[chunk].Remove(spawnPoint);

                if (Chunks.ContainsKey(newChunk))
                {
                    Chunks[newChunk].Add(spawnPoint);
                }
                else
                {
                    List<SpawnPoint> list = new List<SpawnPoint>();
                    list.Add(spawnPoint);
                    Chunks.Add(newChunk, list);
                }
            }

            spawnPoint.Position = spawnPoint.GameObject.transform.position;
            if (!_loadedChunks.Contains(newChunk)) spawnPoint.GameObject.SetActive(false);
        }

        public void Init()
        {
            _inited = true;
        }


        private void FixedUpdate()
        {
            if (!_inited) return;

            deloadChunks.Clear();

            foreach (var chunk in Chunks)
            {
                if (Vector2.Distance(
                        pointsContainer.GetChunkCoords(cameraController.transform.position),
                        new Vector2(chunk.Key.x, chunk.Key.y))
                    < distToChunk)
                {
                    if (!_loadedChunks.Contains(chunk.Key))
                    {
                        _loadedChunks.Add(chunk.Key);
                        foreach (var spawnPoint in chunk.Value) Spawn(spawnPoint);
                    }
                }
                else
                {
                    if (_loadedChunks.Contains(chunk.Key))
                    {
                        _loadedChunks.Remove(chunk.Key);
                        deloadChunks.Add(chunk.Key);
                    }
                }
            }

            foreach (var key in deloadChunks)
            {
                List<SpawnPoint> despawnPoints = new List<SpawnPoint>(Chunks[key]);
                foreach (var p in despawnPoints) Despawn(key, p);
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.red;
            foreach (var chunk in _loadedChunks)
            {
                Vector2Int chunkCoords = chunk;
                Vector2 chunkWorldPosition = new Vector2(chunkCoords.x * ChunkSize, chunkCoords.y * ChunkSize);
                Vector2 chunkSizeVector = new Vector2(ChunkSize, ChunkSize);
                Gizmos.DrawWireCube(chunkWorldPosition + chunkSizeVector / 2, chunkSizeVector);
            }
#endif
        }

        public class SpawnPoint
        {
            public Vector2 Position;
            public PointsTypes Type;
            public CharacterDefinition CharacterDefinition;
            public GameObject GameObject;
        }
    }
}