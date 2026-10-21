using Utilities;
using UnityEngine;

namespace CharacterComponents.Moving
{
    public class FleerMoving : BaseCharacterComponent
    {
        [SerializeField] private float fleeRadius = 6f;
        [SerializeField] private float speed = 6f;
        [SerializeField] private float acceleration = 15f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float retargetInterval = 0.2f;

        private Vector2 _away;
        private bool _threatened;
        private float _retargetTimer;

        public float FleeRadius => fleeRadius;
        public float Speed => speed;

        public override void Init()
        {
            _threatened = false;
            _retargetTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            float dt = Time.fixedDeltaTime;
            Rigidbody2D rb = character.rb2D;
            _retargetTimer -= dt;
            if (_retargetTimer <= 0f)
            {
                _retargetTimer = retargetInterval;
                var threat = EnemyTargeting.FindClosestHostile(transform.position, fleeRadius, character.Fraction);
                _threatened = threat != null;
                if (_threatened) _away = (rb.position - (Vector2)threat.transform.position).normalized;
            }

            Vector2 desired = _threatened ? _away * speed : Vector2.zero;
            rb.velocity = Vector2.MoveTowards(rb.velocity, desired, acceleration * dt);
            if (_threatened && _away.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(_away.y, _away.x) * Mathf.Rad2Deg - 90f;
                rb.MoveRotation(Mathf.LerpAngle(rb.rotation, angle, Mathf.Clamp01(rotationSpeed * dt)));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fleeRadius);
        }
    }
}
