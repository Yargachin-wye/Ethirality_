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
        private static CharactersPool CharactersPool => CharactersPool.Instance;

        private void Start()
        {
            Spawn();
        }

        public void Spawn()
        {
            Character character = CharactersPool.GetPooledObject(characterDefinition);
            character.gameObject.SetActive(true);
            character.transform.position = transform.position;
            character.Init(characterDefinition);
        }
    }
}