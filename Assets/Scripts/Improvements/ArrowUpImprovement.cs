using CharacterComponents;
using Definitions;
using UnityEngine;

namespace Improvements
{
    public class ArrowUpImprovement : BaseImprovementComponent
    {
        [SerializeField] private float shotSpeed = 1.5f;
        
        public override void SetPlayer(ImprovementDefinition definition, Character character, ImprovementsComponent improvementsComponent)
        {
            character.GetComponent<Shooter>().SetSpeed(shotSpeed);
        }
    }
}