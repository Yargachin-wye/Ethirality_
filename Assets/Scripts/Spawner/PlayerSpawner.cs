using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using Managers.Pools;
using Pools;
using UnityEngine;

namespace Spawner
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition characterDefinition;
        [SerializeField] private bool isSpawnOnStart;
        [SerializeField] private CharactersPool _charactersPool;

        private List<Vector2> _spawnPoints = new();
        public void Spawn()
        {
            Character character = _charactersPool.GetPooledObject(characterDefinition);
            character.gameObject.SetActive(true);
            character.transform.position = _spawnPoints[Random.Range(0, _spawnPoints.Count)];
            character.Init(characterDefinition);

            _spawnPoints.Clear();
        }

        public void AddSpawnPoint(Vector2 position)
        {
            _spawnPoints.Add(position);
        }
    }
}