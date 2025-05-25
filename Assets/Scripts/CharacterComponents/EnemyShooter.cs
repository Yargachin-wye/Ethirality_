using CharacterComponents.CharacterStat;
using Definitions;
using Pools;
using UnityEngine;

namespace CharacterComponents
{
    public class EnemyShooter : BaseCharacterComponent
    {
        [SerializeField] private ProjectileDefinition projectileDefinition;
        [SerializeField] private float speed = 1;
        [SerializeField] private float shootDelay;
        [SerializeField] private float detectionRange = 10f;

        private ProjectilePool _projectilePool;
        private float _timer;

        private Fraction Fraction => character.Fraction;

        public override void Init()
        {
            _projectilePool = ProjectilePool.Instance;
        }

        private void FixedUpdate()
        {
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
            var allStats = FindObjectsOfType<Stats>();
            Stats closest = null;
            float minDistance = Mathf.Infinity;

            foreach (var stats in allStats)
            {
                if (stats == null || stats.character == null) continue;
                if (stats.character.Fraction == Fraction) continue;

                float distance = Vector2.Distance(transform.position, stats.transform.position);
                if (distance < minDistance && distance <= detectionRange)
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
        }
    }
}