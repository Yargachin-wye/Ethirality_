using CharacterComponents;
using CharacterComponents.Animations;
using Definitions;
using Projectiles;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class Twister : BaseImprovementComponent
    {
        private Transform _target;
        private bool _hasTarget;


        int _currentId;
        int _count;

        private void FixedUpdate()
        {
            if (!_hasTarget) return;
        }
        
        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _hasTarget = true;
            _target = character.transform;
        }
    }
}