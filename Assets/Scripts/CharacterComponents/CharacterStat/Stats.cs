using System;
using Definitions;
using Pools;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterComponents
{
    public class Stats : BaseCharacterComponent
    {
        [SerializeField] private int startHp;
        [SerializeField] private int maxHp;
        [SerializeField] private bool isImmortal;
        [SerializeField] private bool isInvulnerable;
        [Space]
        [SerializeField] private DeadBodyInfo deadBodyInfo;

        private int _currentHealth;
        public Fraction Fraction => character.Fraction;
        public Action OnDeadAction;
        public Action<int> OnDmgAction;
        public Action<int> OnCureAction;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHp;

        public override void Init()
        {
            _currentHealth = startHp;
        }

        public void Dead()
        {
            OnDeadAction?.Invoke();
            DeadBody dbi = DeadBodiesPool.Instance.GetPooledObject(deadBodyInfo);
            dbi.gameObject.SetActive(true);
            dbi.transform.position = transform.position;
            dbi.transform.rotation = transform.rotation;
        }

        public bool Damage(int val)
        {
            if (isInvulnerable) return false;
            _currentHealth -= val;

            if (_currentHealth <= 0 && !isImmortal)
            {
                Dead();
                return true;
            }

            OnDmgAction?.Invoke(val);
            return false;
        }

        public void Cure(int val)
        {
            _currentHealth += val;

            // if (_currentHealth >= _maxHealth) return;

            OnCureAction?.Invoke(val);
        }
    }
}