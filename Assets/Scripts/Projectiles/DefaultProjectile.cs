using CharacterComponents.CharacterStat;
using CharacterComponents.Moving;
using Definitions;
using UnityEngine;

namespace Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DefaultProjectile : BaseProjectile
    {
        [SerializeField] private Rigidbody2D rb2D;

        private float _timer;
        private bool _inited = false;
        private bool _isTriggered = false;

        public override void Init(ProjectileDefinition projectileDefinition)
        {
            base.Init(projectileDefinition);
            _timer = Definition.LifeTime;
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

        public override void Shoot(Vector2 direction, float speed, GameObject owner, Fraction fraction,
            bool hasOwner = true)
        {
            base.Shoot(direction, speed, owner, fraction, hasOwner);
            _inited = true;
            _isTriggered = false;

            if (rb2D.bodyType == RigidbodyType2D.Dynamic)
            {
                float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90));
                rb2D.AddForce(speed * Definition.SpeedMultiply * direction.normalized, ForceMode2D.Impulse);
            }

            if (Definition.Recoil != 0)
            {
                LumpMeatMovable lumpMeatMovable = owner.GetComponent<LumpMeatMovable>();
                if (lumpMeatMovable != null) lumpMeatMovable.Push(direction, Definition.Recoil);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTriggered) return;
            var triggerStats = other.GetComponent<Stats>();

            if (triggerStats == null ||
                triggerStats.Fraction == Fraction &&
                Fraction != Fraction.All)
            {
                return;
            }

            triggerStats.Damage(Definition.Damage);

            if (Definition.IsDestroyOnTrigger)
            {
                gameObject.SetActive(false);
                return;
            }

            _isTriggered = true;
        }
    }
}