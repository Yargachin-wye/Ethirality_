using CharacterComponents;
using CharacterComponents.Animations;
using Definitions;
using Projectiles;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class TargetTowards : BaseImprovementComponent
    {
        private Transform _target;
        private bool _hasTarget;

        private void LateUpdate()
        {
            if (!_target) return;
            Vector2 directionToCenter = transform.position - _target.position;

            if (directionToCenter.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;

                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }


        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _hasTarget = true;
            _target = character.transform;
        }
    }
}