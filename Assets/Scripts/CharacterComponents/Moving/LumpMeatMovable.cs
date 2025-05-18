using System;
using CharacterComponents.Animations;
using CharacterComponents.Food;
using Definitions;
using Managers;
using UnityEngine;

namespace CharacterComponents
{
    [RequireComponent(typeof(Eater))]
    public class LumpMeatMovable : BaseCharacterComponent
    {
        [SerializeField] private LumpMeatAnimator lumpMeatAnimator;
        [SerializeField] private float dashTime;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float decelerationRate;
        [SerializeField] private float powerDash;
        [Space]
        [SerializeField] private Eater eater;

        private bool _isFreeze;
        private bool _isDash;
        private bool _isFirstFreeze = true;
        private float _gravityScale;
        private Vector2 _lookDirection;
        private float _dashTimer = 0;

        public bool IsFreeze => _isFreeze;

        public override void OnValidate()
        {
            base.OnValidate();
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

            Debug.Log($"lookDirection   :{_lookDirection}");
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

        public void Dash(Vector2 v2, float power)
        {
            _dashTimer = dashTime;
            character.rb2D.velocity = Vector2.zero;
            character.rb2D.AddForce(transform.right.normalized * power, ForceMode2D.Impulse);
        }

        public void Dash(Vector2 v2)
        {
            _dashTimer = dashTime;
            character.rb2D.velocity = Vector2.zero;
            character.rb2D.AddForce(transform.right.normalized * powerDash, ForceMode2D.Impulse);
        }

        public override void Init()
        {
            _gravityScale = character.rb2D.gravityScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isDash) return;

            BaseFood food = other.GetComponent<BaseFood>();
            if (food == null) return;

            eater.Eat(food);
        }
    }
}