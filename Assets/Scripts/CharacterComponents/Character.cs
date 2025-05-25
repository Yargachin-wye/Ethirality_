using System;
using System.Collections.Generic;
using CharacterComponents.CharacterStat;
using Definitions;
using Pools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Stats))]
    public class Character : BaseCharacterComponent
    {
        [SerializeField] public Rigidbody2D rb2D;
        [SerializeField] private BaseCharacterComponent[] baseCharacterComponent;
        [SerializeField] private Stats stats;

        public Fraction Fraction { get; private set; }
        public Action OnDeadAction;
        private float _gravityScale;

        public Stats Stats => stats;

        public override void Init()
        {
        }

        public override void Init(CharacterDefinition characterDefinition)
        {
            Fraction = characterDefinition.Fraction;

            foreach (var component in baseCharacterComponent)
            {
                if (component is Character) continue;
                component.Init();
            }

            stats.OnDeadAction += OnDead;
        }

        private void OnDead()
        {
            OnDeadAction?.Invoke();
            gameObject.SetActive(false);
        }

        public void OnValidate()
        {
            baseCharacterComponent = GetComponentsInChildren<BaseCharacterComponent>(true);
            if (stats) stats = GetComponentInChildren<Stats>();
        }

        public void Off()
        {
            gameObject.SetActive(false);
        }
    }
}