using System;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Managers.Pools
{
    public class CharactersPool : MonoBehaviour
    {
        [SerializeField] private List<PoolsCharacterPack> poolsCharacterPack;
        private List<GameObject> _pool = new List<GameObject>();
        public Action AfterStart;
        public static CharactersPool Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("CharactersPool.instance != null");
                return;
            }

            Instance = this;
            InitPool();
        }

        private void Start()
        {
            
            AfterStart?.Invoke();
        }

        private void InitPool()
        {
            foreach (var pool in poolsCharacterPack)
            {
                for (int i = 0; i < pool.numOnStart; i++)
                {
                    GameObject obj = Instantiate(pool.characterDefinition.Prefab);
                    obj.SetActive(false);
                    pool.objectsList.Add(obj);
                }
            }
        }

        public Character GetPooledObject(CharacterDefinition characterDefinition)
        {
            foreach (var pool in poolsCharacterPack)
            {
                if (pool.characterDefinition == characterDefinition)
                {
                    for (int i = 0; i < pool.objectsList.Count; i++)
                    {
                        if (!pool.objectsList[i].activeInHierarchy)
                        {
                            Character characterFromPool = pool.objectsList[i].GetComponent<Character>();
                            pool.objectsList[i].SetActive(true);
                            characterFromPool.Init(characterDefinition);
                            return characterFromPool;
                        }
                    }

                    GameObject obj = Instantiate(characterDefinition.Prefab);
                    Character characterNew = obj.GetComponent<Character>();
                    characterNew.Init(characterDefinition);
                    pool.objectsList.Add(obj);
                    return characterNew;
                }
            }
            return null;
        }
    }

    [Serializable]
    public struct PoolsCharacterPack
    {
        public CharacterDefinition characterDefinition;
        public int numOnStart;
        [HideInInspector] public List<GameObject> objectsList;
    }
}