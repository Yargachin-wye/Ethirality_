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
    public class Shooter : BaseCharacterComponent
    {
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] private float speed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float shootDelay;
        [SerializeField] private float maxDistanceToProjectile;
        [SerializeField] private float ropeDecaySpeed = 1;
        [SerializeField, HideInInspector] private LumpMeatMovable lumpMeatMovable;

        private ProjectilePool _projectilePool;
        private float _timer;
        private List<ShootPack> _shoots = new();
        private List<Rope2D> _ropes2D = new();
        private Dictionary<HarpoonProjectile, Rope2D> _projectiles = new();

        private Fraction Fraction => character.Fraction;

        public override void Init()
        {
            _projectilePool = ProjectilePool.Instance;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (lumpMeatMovable == null)
            {
                lumpMeatMovable = GetComponent<LumpMeatMovable>();
            }
        }

        private void UpdateShots()
        {
            if (_timer >= 0) _timer -= Time.fixedDeltaTime;

            if (lumpMeatMovable.IsFreeze)
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
                Time.deltaTime * rotationSpeed));

            float angleDifference = Quaternion.Angle(transform.rotation, rotation);

            if (angleDifference < 0.5f)
            {
                var pooledObject = _projectilePool.GetPooledObject(projectileDefinition);
                pooledObject.transform.position = transform.position + transform.right;
                pooledObject.Shoot(transform.right, speed, gameObject, Fraction);

                pooledObject.OnBreakAway += OnProjectileBreakAway;
                pooledObject.OnDied += OnProjectileBreakAway;

                Rope2D rope2D = RopePool.Instance.GetPooledObject();

                rope2D.Set(transform, pooledObject.transform);

                _projectiles.Add(pooledObject, rope2D);

                _shoots.Remove(_shoots[0]);
            }
        }

        private void FixedUpdate()
        {
            UpdateShots();
            UpdateProjectiles();
            UpdateRopes();
        }

        private void UpdateProjectiles()
        {
            List<HarpoonProjectile> projectilesToRemove = new();
            foreach (var projectile in _projectiles)
            {
                float distToProjectile = Vector2.Distance(projectile.Key.transform.position, transform.position);
                if (distToProjectile > maxDistanceToProjectile)
                {
                    projectilesToRemove.Add(projectile.Key);
                }
            }

            foreach (var projectile in projectilesToRemove)
            {
                OnProjectileBreakAway(projectile);
            }
        }

        private void UpdateRopes()
        {
            List<Rope2D> ropesToRemove = new();
            foreach (Rope2D rope in _ropes2D)
            {
                rope.UpdateAlpha(rope.EndColorAlpha - Time.fixedDeltaTime * ropeDecaySpeed);
                if (rope.EndColorAlpha <= 0)
                {
                    ropesToRemove.Add(rope);
                }
            }

            foreach (var rope in ropesToRemove)
            {
                _ropes2D.Remove(rope);
                rope.gameObject.SetActive(false);
            }
        }

        private void OnProjectileBreakAway(HarpoonProjectile harpoonProjectile)
        {
            if (!_projectiles.ContainsKey(harpoonProjectile)) return;

            harpoonProjectile.OnDied -= OnProjectileBreakAway;
            harpoonProjectile.OnBreakAway -= OnProjectileBreakAway;
            harpoonProjectile.isFarAwayFromOwner = true;
            
            _projectiles[harpoonProjectile].UnpinLastPos();
            _ropes2D.Add(_projectiles[harpoonProjectile]);
            _projectiles.Remove(harpoonProjectile);
        }

        public void Shoot(Vector2 direction)
        {
            if (_timer > 0) return;

            _timer = shootDelay;
            _shoots.Add(new ShootPack { Direction = direction.normalized });
        }


        public struct ShootPack
        {
            public Vector2 Direction;
        }
    }
}