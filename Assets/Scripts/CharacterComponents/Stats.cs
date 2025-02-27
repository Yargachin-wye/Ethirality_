using System;
using Definitions;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterComponents
{
    public class Stats : BaseCharacterComponent
    {
        private int _currentHealth;
        private int _maxHealth;
        public Fraction Fraction { get;private set; }
        public Action OnDeadAction;
        public Action<int> OnDmgAction;
        public Action<int> OnCureAction;
        
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        
        public void Init(StatsPack statsPack, Fraction fraction)
        {
            _currentHealth = statsPack.startHp;
            _maxHealth = statsPack.maxHp;
            Fraction = fraction;
        }

        public bool Damage(int val)
        {
            _currentHealth -= val;
            if (_currentHealth <= 0)
            {
                OnDeadAction?.Invoke();
                return true;
            }
            OnDmgAction?.Invoke(val);
            return false;
        }
        
        public void Cure(int val)
        {
            _currentHealth += val;
            
            if (_currentHealth >= _maxHealth) return;
            
            OnCureAction?.Invoke(val);
        }
    }
}