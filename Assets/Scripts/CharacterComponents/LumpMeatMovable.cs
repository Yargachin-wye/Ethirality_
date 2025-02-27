using System;
using CharacterComponents.Animations;
using CharacterComponents.Food;
using Definitions;
using UnityEngine;

namespace CharacterComponents
{
    public class LumpMeatMovable : BaseCharacterComponent
    {
        [SerializeField] private LumpMeatAnimator lumpMeatAnimator;
        [SerializeField] private float dashTime;
        private LumpMeatMovablePack _lumpMeatMovablePack;
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
                    Time.deltaTime * _lumpMeatMovablePack.rotationSpeed));

                character.rb2D.velocity = Vector2.Lerp(character.rb2D.velocity, Vector2.zero,
                    1 - _lumpMeatMovablePack.decelerationRate);
            }
        }

        public void Look(Vector2 direction)
        {
            _lookDirection = -direction;
        }

        public void Freeze(bool freeze, Vector2 v2)
        {
            _isFreeze = freeze;

            if (_isFreeze)
            {
                character.rb2D.gravityScale = 0;
                if (_isFirstFreeze)
                {
                    lumpMeatAnimator.OpenJaw();
                    _isFirstFreeze = false;
                }
            }
            else
            {
                if (!_isFirstFreeze)
                {
                    lumpMeatAnimator.CloseJaw();
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
            character.rb2D.AddForce(transform.right.normalized * _lumpMeatMovablePack.powerDash, ForceMode2D.Impulse);
        }

        public void Init(LumpMeatMovablePack lumpMeatMovablePack, float gravityScale)
        {
            enabled = lumpMeatMovablePack.isEnable;

            _lumpMeatMovablePack = lumpMeatMovablePack;
            _gravityScale = gravityScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isDash) return;

            BaseFood food = other.GetComponent<BaseFood>();
            if (food == null) return;

            character.Eater.Eat(food);
        }
    }
}