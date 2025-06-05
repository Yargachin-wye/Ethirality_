using CharacterComponents;
using CharacterComponents.Moving;
using Definitions;
using UnityEngine;

namespace Improvements
{
    public class DashUpImprovement : BaseImprovementComponent
    {
        [SerializeField] private float dashSpeed = 1.5f; 
        public override void SetPlayer(ImprovementDefinition definition, Character character, ImprovementsComponent improvementsComponent)
        {
            character.GetComponent<LumpMeatMovable>().SetDashDelay(dashSpeed);
        }
    }
}