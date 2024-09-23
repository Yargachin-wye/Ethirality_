using System;
using System.Collections.Generic;
using Definitions;
using Managers.Pools;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class Shooter : BaseComponent
    {
        [SerializeField] private bool _hasLumpMeatMovable;

        private ProjectilePool _projectilePool;
        private ShooterPack _shooterPack;
        private float _timer;
        private Fraction fraction;
        private List<ShootPack> _shoots = new();

        public void Init(ShooterPack shooterPack, Fraction fraction)
        {
            this.fraction = fraction;
            enabled = shooterPack.isShooter;
            _projectilePool = ProjectilePool.Instance;
            _shooterPack = shooterPack;
        }

        private void FixedUpdate()
        {
            if (_timer >= 0) _timer -= Time.fixedDeltaTime;
            
            if (_hasLumpMeatMovable && character.LumpMeatMovable.IsFreeze)
            {
                _shoots.Clear();
                return;
            }

            if (_shoots.Count < 1) return;

            ShootPack shot = _shoots[0];

            float angle = Mathf.Atan2(shot.Direction.y, shot.Direction.x) * Mathf.Rad2Deg;
            
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            character.rb2D.angularVelocity = 0;
            character.rb2D.SetRotation(Quaternion.Slerp(transform.rotation, rotation,
                Time.deltaTime * _shooterPack.rotationSpeed));

            float angleDifference = Quaternion.Angle(transform.rotation, rotation);
            
            if (angleDifference < 0.5f)
            {
                var pooledObject = _projectilePool.GetPooledObject(_shooterPack.projectileDefinition);
                pooledObject.transform.position = transform.position;
                pooledObject.Shoot(transform.right, _shooterPack.speed, gameObject, fraction);

                _shoots.Remove(_shoots[0]);
            }
        }

        public void Shoot(Vector2 direction)
        {
            if (_timer > 0) return;
            
            _timer = _shooterPack.shootDelay;
            
            _shoots.Add(new ShootPack { Direction = direction.normalized });
        }


        public struct ShootPack
        {
            public Vector2 Direction;
        }
    }
}