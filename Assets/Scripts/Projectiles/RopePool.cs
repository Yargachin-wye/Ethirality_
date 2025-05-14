using System;
using System.Collections.Generic;
using CharacterComponents.Animations;
using Managers.Pools;
using UnityEngine;

namespace Projectiles
{
    public class RopePool : MonoBehaviour
    {
        private List<Rope2D> _pool = new();
        [SerializeField] private GameObject prefab;
        [SerializeField] private int numOnStart;
        public Action AfterStart;
        public static RopePool Instance;

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
        }

        private void InitPool()
        {
            for (int i = 0; i < numOnStart; i++)
            {
                GameObject obj = Instantiate(prefab);
                Rope2D rope = obj.GetComponent<Rope2D>();
                obj.SetActive(false);
                _pool.Add(rope);
            }
        }

        public Rope2D GetPooledObject()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    Rope2D harpoonProjectile = _pool[i];
                    _pool[i].gameObject.SetActive(true);
                    return harpoonProjectile;
                }
            }

            GameObject obj = Instantiate(prefab);
            Rope2D rope = obj.GetComponent<Rope2D>();
            _pool.Add(rope);
            return rope;
        }
    }
}