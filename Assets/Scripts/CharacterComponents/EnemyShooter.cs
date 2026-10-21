using System.Collections.Generic;
using Audio;
using CharacterComponents.CharacterStat;
using Constants;
using Definitions;
using EditorAttributes;
using Pools;
using Projectiles;
using UnityEngine;
using Utilities;

namespace CharacterComponents
{
    public class EnemyShooter : BaseCharacterComponent
    {
        private static string[] collection = AudioConst.AllSounds;
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] private float speed = 1;
        [SerializeField] private float shootDelay;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField,Dropdown("collection")] private string shotSound;
        [SerializeField] private float homingTurnRate;
        private ProjectilePool _projectilePool;
        private float _timer;
        private readonly List<HomingShot> _homingShots = new();

        private struct HomingShot
        {
            public BaseProjectile Projectile;
            public Rigidbody2D Body;
            public Stats Target;
        }

        public float HomingTurnRate => homingTurnRate;
        public bool IsHoming => homingTurnRate > 0f;
        public float DetectionRange => detectionRange;
        public float ShootDelay => shootDelay;
        public float Speed => speed;
        public ProjectileDefinition ProjectileDefinition => projectileDefinition;

        private Fraction Fraction => character.Fraction;

        public override void Init()
        {
            _projectilePool = ProjectilePool.Instance;
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            if (_homingShots.Count > 0) SteerHomingShots();

            if (_timer > 0)
            {
                _timer -= Time.fixedDeltaTime;
                return;
            }

            var target = FindClosestTarget();
            if (target != null)
            {
                Shoot(target);
                _timer = shootDelay;
            }
        }

        private Stats FindClosestTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
            Stats closest = null;
            float minDistance = Mathf.Infinity;

            foreach (var hit in hits)
            {
                Stats stats = hit.GetComponent<Stats>();
                if (stats == null || stats.character == null) continue;
                if (stats.character.Fraction == Fraction) continue;

                float distance = Vector2.Distance(transform.position, stats.transform.position);
                if (distance < minDistance)
                {
                    closest = stats;
                    minDistance = distance;
                }
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        public void Shoot(Stats target)
        {
            var pooledObject = _projectilePool.GetPooledObject(projectileDefinition);
            pooledObject.transform.position = transform.position + transform.right;

            Vector2 direction = (target.transform.position - transform.position).normalized;

            pooledObject.Shoot(direction, speed, gameObject, Fraction);
            if (IsHoming) TrackHomingShot(pooledObject, target);
            AudioManager.Instance.PlaySound(shotSound, AudioChannel.VFX, transform.position);
        }

        private void TrackHomingShot(BaseProjectile projectile, Stats target)
        {
            var body = projectile.GetComponent<Rigidbody2D>();
            if (body == null) return;
            _homingShots.RemoveAll(h => h.Projectile == projectile);
            _homingShots.Add(new HomingShot { Projectile = projectile, Body = body, Target = target });
        }

        private void SteerHomingShots()
        {
            float maxRadians = homingTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
            for (int i = _homingShots.Count - 1; i >= 0; i--)
            {
                HomingShot shot = _homingShots[i];
                if (shot.Projectile == null || !shot.Projectile.gameObject.activeInHierarchy ||
                    shot.Target == null || !shot.Target.gameObject.activeInHierarchy)
                {
                    _homingShots.RemoveAt(i);
                    continue;
                }

                Vector2 velocity = shot.Body.velocity;
                float magnitude = velocity.magnitude;
                if (magnitude < 0.01f) continue;

                Vector2 toTarget = (Vector2)shot.Target.transform.position - shot.Body.position;
                Vector2 steered = ((Vector2)Vector3.RotateTowards(velocity, toTarget, maxRadians, 0f)).normalized;
                shot.Body.velocity = steered * magnitude;
                shot.Body.MoveRotation(Mathf.Atan2(steered.y, steered.x) * Mathf.Rad2Deg);
            }
        }
    }
}