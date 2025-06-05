using CharacterComponents;
using CharacterComponents.Moving;
using Definitions;
using UnityEngine;

namespace Improvements
{
    public class JawUpImprovement : BaseImprovementComponent
    {
        [SerializeField] private int dmg = 2;
        
        public override void SetPlayer(ImprovementDefinition definition, Character character, ImprovementsComponent improvementsComponent)
        {
            character.GetComponent<LumpMeatMovable>().SetDmg(dmg);
        }
    }
}