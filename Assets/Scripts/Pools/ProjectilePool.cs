using System;
using System.Collections.Generic;
using Definitions;
using UnityEngine;

namespace Managers.Pools
{
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private List<PoolsProjectilrPack> poolsProjectilePack;
        private List<Projectile> _pool = new ();
        public Action AfterStart;
        public static ProjectilePool Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("CharactersPool.instance != null");
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitPool();
            AfterStart?.Invoke();
        }

        private void InitPool()
        {
            foreach (var pool in poolsProjectilePack)
            {
                for (int i = 0; i < pool.numOnStart; i++)
                {
                    GameObject obj = Instantiate(pool.projectileDefinitiop.Prefab);
                    Projectile characterNew = obj.GetComponent<Projectile>();
                    obj.SetActive(false);
                    _pool.Add(characterNew);
                }
            }
        }

        public Projectile GetPooledObject(ProjectileDefinition projectileDefinitiop)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    Projectile projectile = _pool[i];
                    _pool[i].gameObject.SetActive(true);
                    projectile.Init(projectileDefinitiop);
                    return projectile;
                }
            }

            GameObject obj = Instantiate(projectileDefinitiop.Prefab);
            Projectile characterNew = obj.GetComponent<Projectile>();
            characterNew.Init(projectileDefinitiop);
            _pool.Add(characterNew);
            return characterNew;
        }
    }

    [Serializable]
    public struct PoolsProjectilrPack
    {
        public ProjectileDefinition projectileDefinitiop;
        public int numOnStart;
    }
}