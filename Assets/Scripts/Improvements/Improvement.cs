using System;
using CharacterComponents;
using Definitions;
using UniRx;
using UniRxEvents.Improvement;
using Unity.VisualScripting;
using UnityEngine;

namespace Improvements
{
    public class Improvement : BaseImprovementComponent
    {
        [SerializeField] public Rigidbody2D rb2D;
        [SerializeField] private BaseImprovementComponent[] baseCharacterImprovement;
        public BaseImprovementComponent[] BaseCharacterImprovement => baseCharacterImprovement;

        public Fraction Fraction { get; private set; }
        private float _gravityScale;
        public ImprovementDefinition Definition { get; private set; }
        public Character Character { get; private set; }
        public Action<Improvement> OnDestroyAction { get; set; }

        private void Validate()
        {
            baseCharacterImprovement = GetComponentsInChildren<BaseImprovementComponent>(true);
        }

        private void Awake()
        {
            Validate();
        }

        public override void OnAddImp(AddImprovementEvent data)
        {
            
        }

        public override void OnRemoveImp(RemoveImprovementEvent data)
        {
            
        }

        public override void OnValidate()
        {
            base.OnValidate();
            Validate();
        }

        private void OnDestroy()
        {
            OnDestroyAction?.Invoke(this);
        }

        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            Definition = definition;
            Fraction = definition.Fraction;
            Character = character;
            
            foreach (var component in baseCharacterImprovement)
            {
                if (component is Improvement) continue;
                component.SetPlayer(definition, character, improvementsComponent);
            }
        }
    }
}