using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;
using Utilities;

namespace CharacterComponents.Moving
{
    public class ChargerMoving : BaseCharacterComponent
    {
        private enum State
        {
            Idle,
            Telegraph,
            Charge,
            Cooldown
        }

        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float telegraphTime = 0.7f;
        [SerializeField, Range(0f, 1f)] private float aimLockFraction = 0.5f;
        [SerializeField] private float chargeSpeed = 14f;
        [SerializeField] private float chargeDuration = 0.6f;
        [SerializeField] private float cooldown = 3f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private Color telegraphColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private SpriteRenderer spriteRenderer;

        private State _state;
        private float _timer;
        private Stats _target;
        private Vector2 _dir;
        private Color _baseColor = Color.white;
        private bool _colorCaptured;

        public float DetectionRange => detectionRange;
        public float TelegraphTime => telegraphTime;
        public float ChargeSpeed => chargeSpeed;
        public float ChargeDuration => chargeDuration;
        public float Cooldown => cooldown;

        public override void Init()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null && !_colorCaptured)
            {
                _baseColor = spriteRenderer.color;
                _colorCaptured = true;
            }

            SetState(State.Idle);
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            float dt = Time.fixedDeltaTime;
            Rigidbody2D rb = character.rb2D;

            switch (_state)
            {
                case State.Idle:
                    rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, 20f * dt);
                    _target = EnemyTargeting.FindClosestHostile(transform.position, detectionRange, character.Fraction);
                    if (_target != null)
                    {
                        _dir = DirectionTo(_target);
                        SetState(State.Telegraph);
                    }
                    break;

                case State.Telegraph:
                    rb.velocity = Vector2.zero;
                    _timer += dt;
                    if (_target != null && _target.gameObject.activeInHierarchy && _timer < telegraphTime * aimLockFraction)
                        _dir = DirectionTo(_target);
                    Face(_dir, dt);
                    SetTelegraphTint(Mathf.Clamp01(_timer / telegraphTime));
                    if (_timer >= telegraphTime) SetState(State.Charge);
                    break;

                case State.Charge:
                    rb.velocity = _dir * chargeSpeed;
                    _timer += dt;
                    if (_timer >= chargeDuration) SetState(State.Cooldown);
                    break;

                case State.Cooldown:
                    rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, 20f * dt);
                    _timer += dt;
                    if (_timer >= cooldown) SetState(State.Idle);
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != State.Charge) return;
            var stats = other.GetComponent<Stats>();
            if (stats == null || stats.Fraction == character.Fraction) return;
            SetState(State.Cooldown);
        }

        private void SetState(State state)
        {
            _state = state;
            _timer = 0f;
            if (state != State.Telegraph) SetTelegraphTint(0f);
        }

        private Vector2 DirectionTo(Stats target)
        {
            Vector2 dir = (Vector2)target.transform.position - character.rb2D.position;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : _dir;
        }

        private void Face(Vector2 dir, float dt)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            character.rb2D.MoveRotation(Mathf.LerpAngle(character.rb2D.rotation, angle,
                Mathf.Clamp01(rotationSpeed * dt)));
        }

        private void SetTelegraphTint(float t)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(_baseColor, telegraphColor, t);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
