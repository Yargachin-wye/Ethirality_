using CharacterComponents;
using CharacterComponents.Animations;
using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;

namespace Improvements
{
    public class TouchDamage : BaseImprovementComponent
    {
        private Transform _target;
        private bool _hasTarget;

        [SerializeField] private int dmg = 1;
        [SerializeField] private bool isDestroyOnTrigger = false;
        [SerializeField] private int numberTouches = 1;

        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _hasTarget = true;
            _target = character.transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var triggerStats = other.GetComponent<Stats>();
            if (triggerStats == null) return;
            if (triggerStats.Fraction == improvement.Fraction &&
                improvement.Fraction != Fraction.All)
            {
                return;
            }


            triggerStats.Damage(dmg);

            if (isDestroyOnTrigger)
            {
                numberTouches--;
                if (numberTouches <= 0) improvement.Remove();
            }
        }
    }
}