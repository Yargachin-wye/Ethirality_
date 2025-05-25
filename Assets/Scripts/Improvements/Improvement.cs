using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<BaseImprovementComponent> baseCharacterImprovement;
        public List<BaseImprovementComponent> BaseCharacterImprovement => baseCharacterImprovement;

        public Fraction Fraction { get; private set; }
        private float _gravityScale;
        public ImprovementDefinition Definition { get; private set; }
        public Character Character { get; private set; }
        public Action<Improvement, bool> OnDestroyAction { get; set; }

        private void Validate()
        {
            baseCharacterImprovement = GetComponentsInChildren<BaseImprovementComponent>(true).ToList();
            baseCharacterImprovement.Remove(this);
        }

        private void Awake()
        {
            Validate();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            Validate();
        }

        private void OnDestroy()
        {
            OnDestroyAction?.Invoke(this, false);
        }

        public override void SetPlayer(ImprovementDefinition definition, Character character,
            ImprovementsComponent improvementsComponent)
        {
            Definition = definition;
            Fraction = definition.Fraction;
            Character = character;

            foreach (var component in baseCharacterImprovement)
            {
                component.SetPlayer(definition, character, improvementsComponent);
            }
        }

        public void Remove()
        {
            foreach (var component in baseCharacterImprovement)
            {
                component.OnRemove();
            }

            Destroy(gameObject);
            OnDestroyAction?.Invoke(this, true);
        }
    }
}