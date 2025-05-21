using UnityEngine;

namespace CharacterComponents.Moving
{
    public class BotMoving : BaseCharacterComponent
    {
        [SerializeField] private float _rotationSpeed = 1;

        private void FixedUpdate()
        {
            Vector3 difference = Vector3.up;
            difference.Normalize();
            float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotZ - 90);

            character.rb2D.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }

        public override void Init()
        {
            
        }
    }
}