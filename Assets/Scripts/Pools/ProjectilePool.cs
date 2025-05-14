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
        private List<HarpoonProjectile> _pool = new ();
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
                    HarpoonProjectile characterNew = obj.GetComponent<HarpoonProjectile>();
                    obj.SetActive(false);
                    _pool.Add(characterNew);
                }
            }
        }

        public HarpoonProjectile GetPooledObject(ProjectileDefinition projectileDefinitiop)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    HarpoonProjectile harpoonProjectile = _pool[i];
                    _pool[i].gameObject.SetActive(true);
                    harpoonProjectile.Init(projectileDefinitiop);
                    return harpoonProjectile;
                }
            }

            GameObject obj = Instantiate(projectileDefinitiop.Prefab);
            HarpoonProjectile characterNew = obj.GetComponent<HarpoonProjectile>();
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