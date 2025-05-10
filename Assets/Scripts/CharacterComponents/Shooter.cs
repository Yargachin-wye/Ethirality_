using System;
using System.Collections.Generic;
using Definitions;
using Managers.Pools;
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
        [SerializeField, HideInInspector] private LumpMeatMovable lumpMeatMovable;
        [SerializeField, HideInInspector] private bool hasLumpMeatMovable;

        private ProjectilePool _projectilePool;
        private float _timer;
        private List<ShootPack> _shoots = new();

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
                hasLumpMeatMovable = lumpMeatMovable != null;
            }
        }

        private void FixedUpdate()
        {
            if (_timer >= 0) _timer -= Time.fixedDeltaTime;

            if (hasLumpMeatMovable && lumpMeatMovable.IsFreeze)
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

                _shoots.Remove(_shoots[0]);
            }
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