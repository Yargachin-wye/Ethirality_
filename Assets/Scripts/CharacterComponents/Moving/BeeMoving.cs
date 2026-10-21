using System.Collections;
using UnityEngine;
using Utilities;

namespace CharacterComponents
{
    public class BeeMoving : BaseCharacterComponent
    {
        [SerializeField] private float minTimer = .2f;
        [SerializeField] private float maxTimer = 1;

        [SerializeField] private float minSpeed = 2;
        [SerializeField] private float maxSpeed = 10;

        [SerializeField] private float minRotationSpeed = 5;
        [SerializeField] private float maxRotationSpeed = 10;
        private float _speed;
        private float _rotationSpeed = 1;
        private float _dir = 1;

        public float AverageSpeed => (minSpeed + maxSpeed) * 0.5f;

        private void FixedUpdate()
        {
            if (GameplayPause.IsPaused) return;

            Vector3 difference = _dir * transform.right;
            difference.Normalize();
            float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotZ - 90);

            character.rb2D.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation,
                _rotationSpeed * Time.fixedDeltaTime));
            character.rb2D.velocity = transform.up * _speed;
        }

        private IEnumerator Timer()
        {
            float timer = Random.Range(minTimer, maxTimer);
            _speed = Random.Range(minSpeed, maxSpeed);
            _rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            if (Random.Range(0, 2) < 1)
            {
                _dir = 1;
            }
            else
            {
                _dir = -1;
            }

            float elapsed = 0f;
            while (elapsed < timer)
            {
                if (!GameplayPause.IsPaused) elapsed += Time.deltaTime;
                yield return null;
            }

            StartCoroutine(Timer());
        }

        public override void Init()
        {
            StartCoroutine(Timer());
        }
    }
}