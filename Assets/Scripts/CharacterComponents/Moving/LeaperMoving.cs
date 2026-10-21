using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;
using Utilities;

namespace CharacterComponents.Moving
{
    public class LeaperMoving : BaseCharacterComponent
    {
        private enum State
        {
            Idle,
            Crouch,
            Hop,
            Rest
        }

        [SerializeField] private float detectionRange = 9f;
        [SerializeField] private float crouchTime = 0.4f;
        [SerializeField] private float hopSpeed = 10f;
        [SerializeField] private float hopDuration = 0.35f;
        [SerializeField] private float restTime = 1.2f;
        [SerializeField] private float rotationSpeed = 12f;

        private State _state;
        private float _timer;
        private Vector2 _dir;

        public float DetectionRange => detectionRange;
        public float HopSpeed => hopSpeed;
        public float HopRange => hopSpeed * hopDuration;
        public float RestTime => restTime;

        public override void Init()
        {
            SetState(State.Idle);
        }

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            float dt = Time.fixedDeltaTime;
            Rigidbody2D rb = character.rb2D;
            _timer += dt;

            switch (_state)
            {
                case State.Idle:
                    rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, 20f * dt);
                    var target = EnemyTargeting.FindClosestHostile(transform.position, detectionRange, character.Fraction);
                    if (target != null)
                    {
                        _dir = ((Vector2)target.transform.position - rb.position).normalized;
                        SetState(State.Crouch);
                    }
                    break;

                case State.Crouch:
                    rb.velocity = Vector2.zero;
                    Face(_dir, dt);
                    if (_timer >= crouchTime) SetState(State.Hop);
                    break;

                case State.Hop:
                    rb.velocity = _dir * hopSpeed;
                    if (_timer >= hopDuration) SetState(State.Rest);
                    break;

                case State.Rest:
                    rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, 25f * dt);
                    if (_timer >= restTime) SetState(State.Idle);
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state != State.Hop) return;
            var stats = other.GetComponent<Stats>();
            if (stats == null || stats.Fraction == character.Fraction) return;
            SetState(State.Rest);
        }

        private void SetState(State state)
        {
            _state = state;
            _timer = 0f;
        }

        private void Face(Vector2 dir, float dt)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            character.rb2D.MoveRotation(Mathf.LerpAngle(character.rb2D.rotation, angle,
                Mathf.Clamp01(rotationSpeed * dt)));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
