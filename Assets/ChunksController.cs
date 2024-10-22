using System;
using System.Collections;
using System.Collections.Generic;
using Camera;
using CharacterComponents;
using Definitions;
using Managers.Pools;
using UnityEngine;

public class ChunksController : MonoBehaviour
{
    [SerializeField] private PointsContainer pointsContainer;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private CharactersPool _charactersPool;
    [SerializeField] private float distToChunk;
    private Dictionary<Vector2Int, List<SpawnPoint>> Chunks => pointsContainer.Chunks;
    private float ChunkSize => pointsContainer.ChunkSize;
    private List<Vector2Int> _loadedChunks = new();

    private bool _inited;

    private void Awake()
    {
        _inited = false;
    }

    private void Spawn(SpawnPoint spawnPoint)
    {
        Character character = _charactersPool.GetPooledObject(spawnPoint.CharacterDefinition);
        character.gameObject.SetActive(true);
        character.transform.position = spawnPoint.Position;
        spawnPoint.GameObject = character.gameObject;
    }

    private void Despawn(SpawnPoint spawnPoint)
    {
        spawnPoint.GameObject.SetActive(false);
    }

    public void Init()
    {
        _inited = true;
    }

    private void FixedUpdate()
    {
        if (!_inited) return;

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
                    foreach (var spawnPoint in chunk.Value) Despawn(spawnPoint);
                }
            }
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