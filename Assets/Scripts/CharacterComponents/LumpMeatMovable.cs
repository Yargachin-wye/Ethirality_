using System;
using Definitions;
using UnityEngine;

namespace CharacterComponents
{
    public class LumpMeatMovable : BaseComponent
    {
        private LumpMeatMovablePack _lumpMeatMovablePack;
        private bool _isFreeze;
        private float _gravityScale;
        private Vector2 _lookDirection;
        
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
            }
            else character.rb2D.gravityScale = _gravityScale;
        }

        public void Dash(Vector2 v2, float power)
        {
            character.rb2D.AddForce(transform.right.normalized * power, ForceMode2D.Impulse);
        }

        public void Dash(Vector2 v2)
        {
            character.rb2D.AddForce(transform.right.normalized * _lumpMeatMovablePack.powerDash, ForceMode2D.Impulse);
        }

        public void Init(LumpMeatMovablePack lumpMeatMovablePack, float gravityScale)
        {
            enabled = lumpMeatMovablePack.isEnable;

            _lumpMeatMovablePack = lumpMeatMovablePack;
            _gravityScale = gravityScale;
        }
    }
}