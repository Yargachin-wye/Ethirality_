using System;
using CharacterComponents;
using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;

namespace Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class HarpoonProjectile : BaseProjectile
    {
        [SerializeField, HideInInspector] private Rigidbody2D rb2D;
        private Fraction _fraction;
        
        private GameObject _owner;
        private GameObject _trigger;
        private Stats _triggerStats;
        private FixedJoint2D joint;
        public Action<HarpoonProjectile> OnBreakAway;
        public Action<HarpoonProjectile> OnDied;

        private float _timer;
        private float _attachedForceTimer;
        private bool _inited = false;
        private bool _isAttached;
        private bool _isForceOnAttached = false;
        private bool _isJoined = false;
        private bool _isTrigered = false;

        public bool isFarAwayFromOwner = false;
        private bool _hasOwner;

        public override void Init(ProjectileDefinition projectileDefinition)
        {
            base.Init(projectileDefinition);
            isFarAwayFromOwner = false;
            _isForceOnAttached = false;
            rb2D.bodyType = Definition.RigidbodyType2D;
            rb2D.gravityScale = Definition.GravityScale;
            _timer = Definition.LifeDelay;
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

            if (!_isForceOnAttached) return;
            if (isFarAwayFromOwner) return;
            if (_attachedForceTimer <= 0 && !_isAttached)
            {
                _isAttached = true;
                Rigidbody2D rb2Down = _owner.GetComponent<Rigidbody2D>();
                Rigidbody2D rb2Dtr = _trigger.GetComponent<Rigidbody2D>();

                _triggerStats.Damage(Definition.Damage);

                if (_hasOwner && rb2Down != null)
                {
                    rb2Down.velocity = Vector2.zero;
                    rb2Down.angularVelocity = 0f;
                    rb2Down.AddForce(
                        (_owner.transform.position - _trigger.transform.position).normalized *
                        Definition.OwnerForce,
                        ForceMode2D.Impulse);
                }

                if (_hasOwner && rb2Dtr != null)
                {
                    rb2Dtr.AddForce(
                        (_trigger.transform.position - _owner.transform.position).normalized *
                        Definition.TargetForce,
                        ForceMode2D.Impulse);
                }

                OnBreakAway?.Invoke(this);
            }

            _attachedForceTimer -= Time.fixedDeltaTime;
        }

        private void OnDrawGizmos()
        {
            if (!_inited) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _owner.transform.position);
        }

        private void OnValidate()
        {
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
        }

        private void OnDisable()
        {
            if (_isTrigered) _triggerStats.OnDeadAction -= OnTriggerDead;
            OnDied?.Invoke(this);
            if (_isJoined)
            {
                _isJoined = false;
                Destroy(joint);
            }
        }

        public override void Shoot(Vector2 direction, float speed, GameObject owner, Fraction fraction, bool hasOwner = true)
        {
            _inited = true;
            _hasOwner = hasOwner;
            _owner = owner;
            _fraction = fraction;
            _isTrigered = false;
            _isAttached = false;
            _isForceOnAttached = false;

            if (rb2D.bodyType == RigidbodyType2D.Dynamic)
            {
                float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90));
                rb2D.AddForce(speed * Definition.SpeedMultiply * direction.normalized, ForceMode2D.Impulse);
            }

            if (Definition.Recoil != 0)
            {
                LumpMeatMovable lumpMeatMovable = owner.GetComponent<LumpMeatMovable>();
                if (lumpMeatMovable != null) lumpMeatMovable.Dash(direction, Definition.Recoil);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isTrigered) return;
            _triggerStats = other.GetComponent<Stats>();

            if (_triggerStats == null ||
                _triggerStats.Fraction == _fraction &&
                _fraction != Fraction.All)
            {
                return;
            }

            if (Definition.IsDestroyOnTrigger)
            {
                gameObject.SetActive(false);
                return;
            }

            _triggerStats.OnDeadAction += OnTriggerDead;
            _isTrigered = true;
            _isAttached = false;
            _isForceOnAttached = false;
            _trigger = other.gameObject;

            if (Definition.IsAttachedOnTrigger && !_isJoined)
            {
                if (other.GetComponent<Rigidbody2D>() != null)
                {
                    joint = gameObject.AddComponent<FixedJoint2D>();
                    _isJoined = true;

                    joint.connectedBody = other.GetComponent<Rigidbody2D>();
                    joint.autoConfigureConnectedAnchor = false;
                }
            }

            if (Definition.TargetForce != 0 ||
                Definition.OwnerForce != 0)
            {
                _isForceOnAttached = true;
                _attachedForceTimer = Definition.AttachedForceDelay;
            }
        }

        private void OnTriggerDead()
        {
            _triggerStats.OnDeadAction -= OnTriggerDead;
            gameObject.SetActive(false);
        }
    }
}