using CharacterComponents.Animations;
using CharacterComponents.CharacterStat;
using CharacterComponents.Food;
using Managers;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;

namespace CharacterComponents.Moving
{
    [RequireComponent(typeof(Eater))]
    public class LumpMeatMovable : BaseCharacterComponent
    {
        [SerializeField] private LumpMeatAnimator lumpMeatAnimator;
        [SerializeField] private float dashTime;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float decelerationRate;
        [SerializeField] private float powerDash;
        [SerializeField] private int dashDmg = 1;
        [Space]
        [SerializeField] private Eater eater;

        private bool _isFreeze;
        private bool _isDash;
        private bool _isFirstFreeze = true;
        private float _gravityScale;
        private Vector2 _lookDirection;
        private float _dashTimer = 0;

        public bool IsFreeze => _isFreeze;

        public void OnValidate()
        {
            if (eater == null) eater = GetComponent<Eater>();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _lookDirection.normalized * 2);
        }

        private void FixedUpdate()
        {
            if (_dashTimer > 0)
            {
                _dashTimer -= Time.fixedDeltaTime;
                MessageBroker.Default.Publish(new UpdateDashTimerEvent { DashTimer = _dashTimer / dashTime });
                
                _isDash = true;
                if (_dashTimer <= 0)
                {
                    if (lumpMeatAnimator.IsJawOpen) lumpMeatAnimator.CloseJaw();
                }
            }
            else
            {
                _isDash = false;
            }

            if (_isFreeze)
            {
                float angle = Mathf.Atan2(_lookDirection.y, _lookDirection.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                character.rb2D.angularVelocity = 0;
                character.rb2D.SetRotation(Quaternion.Slerp(transform.rotation, rotation,
                    Time.deltaTime * rotationSpeed));

                character.rb2D.velocity = Vector2.Lerp(character.rb2D.velocity, Vector2.zero,
                    1 - decelerationRate);
            }
        }

        public void Look(Vector2 direction)
        {
            _lookDirection = -direction;
            if (_lookDirection.magnitude < Inputer.JoysickMinMagnitude)
            {
                if (lumpMeatAnimator.IsJawOpen) lumpMeatAnimator.CloseJaw();
            }
            else
            {
                if (!lumpMeatAnimator.IsJawOpen) lumpMeatAnimator.OpenJaw();
            }
        }

        public void Freeze(bool freeze, Vector2 v2)
        {
            _isFreeze = freeze;

            if (_isFreeze)
            {
                character.rb2D.gravityScale = 0;
                if (_isFirstFreeze)
                {
                    _isFirstFreeze = false;
                }
            }
            else
            {
                if (!_isFirstFreeze)
                {
                    _isFirstFreeze = true;
                }

                character.rb2D.gravityScale = _gravityScale;
            }
        }

        public void Push(Vector2 v2, float power)
        {
            if (!lumpMeatAnimator.IsJawOpen) lumpMeatAnimator.OpenJaw();
            _dashTimer = dashTime;
            MessageBroker.Default.Publish(new UpdateDashTimerEvent { DashTimer = _dashTimer / dashTime });
            character.rb2D.velocity = Vector2.zero;
            character.rb2D.AddForce(transform.right.normalized * power, ForceMode2D.Impulse);
        }

        public void Dash(Vector2 v2)
        {
            if (_dashTimer > 0) return;
            _dashTimer = dashTime;
            MessageBroker.Default.Publish(new UpdateDashTimerEvent { DashTimer = _dashTimer / dashTime });
            character.rb2D.velocity = Vector2.zero;
            character.rb2D.AddForce(transform.right.normalized * powerDash, ForceMode2D.Impulse);
        }

        public override void Init()
        {
            _gravityScale = character.rb2D.gravityScale;
            MessageBroker.Default.Publish(new UpdateDashTimerEvent { DashTimer = 0 });
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isDash) return;

            BaseFood food = other.GetComponent<BaseFood>();
            Stats stats = other.GetComponent<Stats>();


            if (stats == null) return;
            if (food != null)
            {
                if (stats.CurrentHealth - dashDmg <= 1)
                {
                    eater.Eat(food);
                    stats.Dead();
                }
                else
                {
                    stats.Damage(dashDmg);
                }
            }
            else
            {
                stats.Damage(dashDmg);
            }
        }
    }
}