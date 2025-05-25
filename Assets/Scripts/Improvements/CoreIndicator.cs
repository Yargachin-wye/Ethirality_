using CharacterComponents;
using Definitions;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class CoreIndicator : BaseImprovementComponent
    {
        [SerializeField] private Transform indicator;

        private void LateUpdate()
        {
            if (indicator == null)
                return;

            Vector2 directionToCenter = Vector2.zero - (Vector2)indicator.position;

            if (directionToCenter.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;

                indicator.rotation = Quaternion.Euler(0, 0, angle);
            }
        }


        public override void SetPlayer(ImprovementDefinition definition, Character character, ImprovementsComponent improvementsComponent)
        {
            
        }
    }
}