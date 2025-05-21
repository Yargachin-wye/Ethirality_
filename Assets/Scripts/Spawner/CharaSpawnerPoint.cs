using System;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using Pools;
using UnityEngine;

namespace Spawner
{
    public class CharaSpawnerPoint : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition characterDefinition;
        [SerializeField] private bool isSpawnOnStart;
        [SerializeField] private CharactersPool _charactersPool;

        private void Start()
        {
            Spawn();
        }

        public void Spawn()
        {
            Character character = _charactersPool.GetPooledObject(characterDefinition);
            character.gameObject.SetActive(true);
            character.transform.position = transform.position;
            character.Init(characterDefinition);
        }
    }
}