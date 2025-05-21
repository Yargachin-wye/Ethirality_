using System;
using Definitions;
using UnityEngine;

namespace CharacterComponents.CharacterStat
{
    [RequireComponent(typeof(Stats))]
    public class StatsView : BaseCharacterComponent
    {
        [SerializeField] private ParticleSystem dmgParticleSystem;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] hpSprites;
        [SerializeField, HideInInspector] private Stats stats;


        private void OnValidate() => Validate();

        private void Validate()
        {
            if (stats == null) stats = GetComponent<Stats>();
            if (hpSprites.Length != stats.MaxHealth)
            {
                Array.Resize(ref hpSprites, stats.MaxHealth);
            }
        }

        private void Awake()
        {
            Validate();
            stats.OnDmgAction += OnDmg;
            stats.OnCureAction += OnCure;
            stats.OnDeadAction += OnDeadAction;
        }

        public override void Init()
        {
            spriteRenderer.sprite = hpSprites[0];
        }

        private void OnCure(int obj)
        {
        }

        private void OnDmg(int obj)
        {
            dmgParticleSystem.Play();
            int id = stats.MaxHealth - stats.CurrentHealth;
            if (id < 0) id = 0;
            if (id >= hpSprites.Length) id = hpSprites.Length - 1;
            spriteRenderer.sprite = hpSprites[id];
        }

        private void OnDeadAction()
        {
        }
    }
}