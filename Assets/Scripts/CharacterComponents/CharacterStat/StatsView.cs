using System;
using Definitions;
using UnityEngine;

namespace CharacterComponents.CharacterStat
{
    [RequireComponent(typeof(Stats))]
    public class StatsView : MonoBehaviour
    {
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


        private void OnCure(int obj)
        {
        }

        private void OnDmg(int obj)
        {
        }

        private void OnDeadAction()
        {
        }
    }
}