using CharacterComponents;
using Definitions;
using Managers.Pools;
using UnityEngine;

namespace Managers
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition characterDefinition;
        [SerializeField] private bool isSpawnOnStart;
        [SerializeField] private CharactersPool _charactersPool;
    
        private void Start()
        {
            StartSpawn();
        }

        private void StartSpawn()
        {
            if (isSpawnOnStart)
            {
                Spawn();
            }
        }
        public void Spawn()
        {
            Character character = _charactersPool.GetPooledObject(characterDefinition);
            character.gameObject.SetActive(true);
            character.transform.position = transform.position;
        }
    }
}
