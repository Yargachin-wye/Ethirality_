using System;
using System.Collections.Generic;
using CharacterComponents.Animations;
using Definitions;
using Managers.Pools;
using Pools;
using Projectiles;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class EnemyShooter : BaseCharacterComponent
    {
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] private float speed = 1;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float shootDelay;

        private ProjectilePool _projectilePool;
        private float _timer;

        private Fraction Fraction => character.Fraction;

        public override void Init()
        {
            _projectilePool = ProjectilePool.Instance;
        }

        private void FixedUpdate()
        {
            if (_timer >= 0)
            {
                _timer -= Time.fixedDeltaTime;
                return;
            }

            Shoot();
        }

        public void Shoot()
        {
            if (_timer > 0) return;
            
            _timer = shootDelay;
            
            var pooledObject = _projectilePool.GetPooledObject(projectileDefinition);
            pooledObject.transform.position = transform.position + transform.right;
            Vector2 direction = new Vector2();
            pooledObject.Shoot(direction, speed, gameObject, Fraction);
        }
    }
}