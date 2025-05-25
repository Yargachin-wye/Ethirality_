using System;
using CharacterComponents;
using CharacterComponents.Animations;
using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DefaultProjectile : BaseProjectile
    {
        [SerializeField, HideInInspector] private Rigidbody2D rb2D;
        private Fraction _fraction;

        private ProjectileDefinition _projectileDefinition;

        private float _timer;
        private bool _inited = false;
        private bool _isTriggered = false;

        public ProjectileDefinition Definition => _projectileDefinition;

        public void Init(ProjectileDefinition projectileDefinition)
        {
            _projectileDefinition = projectileDefinition;
            rb2D.bodyType = _projectileDefinition.RigidbodyType2D;
            rb2D.gravityScale = _projectileDefinition.GravityScale;
            _timer = _projectileDefinition.LifeDelay;
        }

        private void FixedUpdate()
        {
            if (!_inited) return;
            if (_timer <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            _timer -= Time.fixedDeltaTime;
        }
        
        private void OnValidate()
        {
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        }
        
        public override void Shoot(Vector2 direction, float speed, GameObject owner, Fraction fraction, bool hasOwner = true)
        {
            _inited = true;
            _fraction = fraction;
            _isTriggered = false;

            if (rb2D.bodyType == RigidbodyType2D.Dynamic)
            {
                float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90));
                rb2D.AddForce(speed * _projectileDefinition.SpeedMultiply * direction.normalized, ForceMode2D.Impulse);
            }

            if (_projectileDefinition.Recoil != 0)
            {
                LumpMeatMovable lumpMeatMovable = owner.GetComponent<LumpMeatMovable>();
                if (lumpMeatMovable != null) lumpMeatMovable.Dash(direction, _projectileDefinition.Recoil);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;
            var _triggerStats = other.GetComponent<Stats>();

            if (_triggerStats == null ||
                _triggerStats.Fraction == _fraction &&
                _fraction != Fraction.All)
            {
                return;
            }

            if (_projectileDefinition.IsDestroyOnTrigger)
            {
                gameObject.SetActive(false);
                return;
            }

            _isTriggered = true;
            
        }
    }
}