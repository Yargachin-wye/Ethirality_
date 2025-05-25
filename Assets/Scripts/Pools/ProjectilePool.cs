using System;
using System.Collections.Generic;
using Definitions;
using Projectiles;
using UnityEngine;

namespace Pools
{
    public class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private List<PoolsProjectilrPack> poolsProjectilePack;
        private List<BaseProjectile> _pool = new ();
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
                    BaseProjectile characterNew = obj.GetComponent<BaseProjectile>();
                    obj.SetActive(false);
                    _pool.Add(characterNew);
                }
            }
        }

        public BaseProjectile GetPooledObject(ProjectileDefinition projectileDefinition)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf && _pool[i].Definition == projectileDefinition)
                {
                    BaseProjectile harpoonProjectile = _pool[i];
                    _pool[i].gameObject.SetActive(true);
                    harpoonProjectile.Init(projectileDefinition);
                    return harpoonProjectile;
                }
            }

            GameObject obj = Instantiate(projectileDefinition.Prefab);
            BaseProjectile newProjectile = obj.GetComponent<BaseProjectile>();
            newProjectile.Init(projectileDefinition);
            _pool.Add(newProjectile);
            return newProjectile;
        }
    }

    [Serializable]
    public struct PoolsProjectilrPack
    {
        public ProjectileDefinition projectileDefinitiop;
        public int numOnStart;
    }
}