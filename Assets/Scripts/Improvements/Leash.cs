using System;
using CharacterComponents;
using CharacterComponents.Animations;
using Definitions;
using Projectiles;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class Leash : BaseImprovementComponent
    {
        private Transform _target;
        private bool _hasTarget;

        [SerializeField] private float maxDistance = 2.0f;
        [SerializeField] private float followSpeed = 100f;
        private Rope2D _rope2D;
        
        private void FixedUpdate()
        {
            if (!_hasTarget) return;

            float distance = Vector2.Distance(transform.position, _target.position);

            if (distance > maxDistance)
            {
                Vector2 direction = (_target.position - transform.position).normalized;
                float distanceOver = distance - maxDistance;
                float speed = followSpeed * distanceOver;

                improvement.rb2D.velocity = (speed * Time.fixedDeltaTime * direction);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
        
        public override void OnRemove()
        {
            base.OnRemove();
            Debug.Log("^^^^ OnRemove");
            _rope2D.gameObject.SetActive(false);
        }


        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _hasTarget = true;
            _target = character.transform;
            _rope2D = RopePool.Instance.GetPooledObject();

            _rope2D.Set(transform, _target, 50);
        }
    }
}