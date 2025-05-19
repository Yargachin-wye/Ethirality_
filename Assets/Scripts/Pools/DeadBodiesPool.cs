using System;
using System.Collections.Generic;
using CharacterComponents;
using Definitions;
using UnityEngine;

namespace Pools
{
    public class DeadBodiesPool : MonoBehaviour
    {
        [SerializeField] private List<PoolsBodyPack> poolsCharacterPack;
        private List<GameObject> _pool = new List<GameObject>();
        public Action AfterStart;
        public static DeadBodiesPool Instance;

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
                    GameObject gobj = Instantiate(pool.deadBodyInfo.prefab);
                    gobj.SetActive(false);
                    pool.objectsList.Add(gobj);
                }
            }
        }

        public DeadBody GetPooledObject(DeadBodyInfo deadBodyInfo)
        {
            foreach (var pool in poolsCharacterPack)
            {
                if (pool.deadBodyInfo == deadBodyInfo)
                {
                    for (int i = 0; i < pool.objectsList.Count; i++)
                    {
                        if (!pool.objectsList[i].activeInHierarchy)
                        {
                            DeadBody characterFromPool = pool.objectsList[i].GetComponent<DeadBody>();
                            pool.objectsList[i].SetActive(true);
                            return characterFromPool;
                        }
                    }

                    GameObject obj = Instantiate(deadBodyInfo.prefab);
                    DeadBody characterNew = obj.GetComponent<DeadBody>();
                    pool.objectsList.Add(obj);
                    return characterNew;
                }
            }
            return null;
        }
    }

    [Serializable]
    public struct PoolsBodyPack
    {
        public DeadBodyInfo deadBodyInfo;
        public int numOnStart;
        [HideInInspector] public List<GameObject> objectsList;
    }
}