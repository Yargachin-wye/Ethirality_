using CharacterComponents;
using CharacterComponents.Animations;
using CharacterComponents.CharacterStat;
using Definitions;
using Projectiles;
using UnityEngine;

namespace Improvements
{
    public class Shield : BaseImprovementComponent
    {
        private Transform _target;
            
        [SerializeField] private bool isDestroyOnTrigger = false;
        [SerializeField] private int numberTouches = 1;

        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _target = character.transform;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var triggerStats = other.GetComponent<BaseProjectile>();
            if (triggerStats == null) return;
            if (triggerStats.Fraction == improvement.Fraction &&
                improvement.Fraction != Fraction.All)
            {
                return;
            }


            triggerStats.gameObject.SetActive(false);

            if (isDestroyOnTrigger)
            {
                numberTouches--;
                if (numberTouches <= 0) improvement.Remove();
            }
        }
    }
}