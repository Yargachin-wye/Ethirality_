using System;
using Definitions;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterComponents
{
    public class Stats : MonoBehaviour
    {
        private int _currentHealth;
        private int _maxHealth;
        public Fraction Fraction { get;private set; }
        public Action OnDeadAction;

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
            return false;
        }
        
        public void Cure(int val)
        {
            
        }
    }
}