using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;
using Utilities;

namespace CharacterComponents.Moving
{
    public class ChaserMoving : BaseCharacterComponent
    {
        [SerializeField] private float chaseRadius = 8f;
        [SerializeField] private float speed = 4f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float recoilTime = 1f;
        [SerializeField] private float retargetInterval = 0.2f;

        private Stats _target;
        private float _retargetTimer;
        private float _recoilTimer;

        public float ChaseRadius => chaseRadius;
        public float Speed => speed;

        public override void Init()
        {
            _target = null;
            _retargetTimer = 0f;
            _recoilTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            float dt = Time.fixedDeltaTime;
            _retargetTimer -= dt;
            if (_retargetTimer <= 0f)
            {
                _target = EnemyTargeting.FindClosestHostile(transform.position, chaseRadius, character.Fraction);
                _retargetTimer = retargetInterval;
            }

            Vector2 dir = Vector2.zero;
            if (_target != null && _target.gameObject.activeInHierarchy)
            {
                dir = ((Vector2)_target.transform.position - character.rb2D.position).normalized;
                if (_recoilTimer > 0f) dir = -dir;
            }

            if (_recoilTimer > 0f) _recoilTimer -= dt;

            character.rb2D.velocity = Vector2.MoveTowards(character.rb2D.velocity, dir * speed, acceleration * dt);
            if (dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                character.rb2D.MoveRotation(Mathf.LerpAngle(character.rb2D.rotation, angle,
                    Mathf.Clamp01(rotationSpeed * dt)));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var stats = other.GetComponent<Stats>();
            if (stats == null || stats.Fraction == character.Fraction) return;
            _recoilTimer = recoilTime;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseRadius);
        }
    }
}
