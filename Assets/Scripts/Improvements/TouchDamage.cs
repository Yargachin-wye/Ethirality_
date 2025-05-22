using CharacterComponents;
using CharacterComponents.Animations;
using Definitions;
using Projectiles;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class TouchDamage : BaseImprovementComponent
    {
        private Transform _target;
        private bool _hasTarget;

        [SerializeField] private int dmg = 1;
        [SerializeField] private bool isDestroyOnTrigger = false;


        public override void OnAddImp(AddImprovementEvent data)
        {
        }

        public override void OnRemoveImp(RemoveImprovementEvent data)
        {
        }

        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            _hasTarget = true;
            _target = character.transform;
            Rope2D rope2D = RopePool.Instance.GetPooledObject();

            rope2D.Set(transform, _target, 50);
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
                Destroy(gameObject);
            }
        }
    }
}