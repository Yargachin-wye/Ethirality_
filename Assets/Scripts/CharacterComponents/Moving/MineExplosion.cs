using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;
using Utilities;

namespace CharacterComponents.Moving
{
    public class MineExplosion : BaseCharacterComponent
    {
        [SerializeField] private float triggerRadius = 3f;
        [SerializeField] private float fuseTime = 1f;
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private int dmg = 2;
        [SerializeField] private float checkInterval = 0.2f;
        [SerializeField] private Color armedColor = new(1f, 0.15f, 0.15f, 1f);
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool _armed;
        private float _timer;
        private float _checkTimer;
        private Color _baseColor = Color.white;
        private bool _colorCaptured;

        public float TriggerRadius => triggerRadius;
        public float FuseTime => fuseTime;
        public float ExplosionRadius => explosionRadius;
        public int Dmg => dmg;

        public override void Init()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null && !_colorCaptured)
            {
                _baseColor = spriteRenderer.color;
                _colorCaptured = true;
            }

            _armed = false;
            _timer = 0f;
            _checkTimer = 0f;
            SetTint(0f);
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            float dt = Time.fixedDeltaTime;
            if (!_armed)
            {
                _checkTimer -= dt;
                if (_checkTimer > 0f) return;
                _checkTimer = checkInterval;
                if (EnemyTargeting.FindClosestHostile(transform.position, triggerRadius, character.Fraction) != null)
                    _armed = true;
                return;
            }

            _timer += dt;
            SetTint(Mathf.PingPong(_timer * 6f, 1f));
            if (_timer >= fuseTime) Explode();
        }

        private void Explode()
        {
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
            {
                var stats = hit.GetComponent<Stats>();
                if (stats == null || stats == character.Stats || stats.Fraction != Fraction.Player) continue;
                stats.Damage(dmg, transform.position);
            }

            _armed = false;
            SetTint(0f);
            character.Stats.Dead();
        }

        private void SetTint(float t)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(_baseColor, armedColor, t);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
