using System;
using Audio;
using Bootstrapper.Saves;
using Constants;
using Definitions;
using EditorAttributes;
using Pools;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

namespace CharacterComponents.CharacterStat
{
    public class Stats : BaseCharacterComponent
    {
        private static string[] collection = AudioConst.AllSounds;
        [SerializeField] private int startHp;
        [SerializeField] private int maxHp;
        [SerializeField] private bool isImmortal;
        [SerializeField] private bool isInvulnerable;
        [Space]
        [SerializeField,Dropdown("collection")] private string dmgSound;
        [SerializeField,Dropdown("collection")] private string deadSound;
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
            if (Fraction == Fraction.Player)
            {
                _currentHealth = SaveSystem.Instance.saveData.hp;
            }
            else
            {
                _currentHealth = startHp;
            }
        }

        public void Dead()
        {
            OnDeadAction?.Invoke();
            DeadBody dbi = DeadBodiesPool.Instance.GetPooledObject(deadBodyInfo);
            dbi.gameObject.SetActive(true);
            dbi.transform.position = transform.position;
            dbi.transform.rotation = transform.rotation;

            if (Fraction == Fraction.Player)
            {
                MessageBroker.Default.Publish(new GameOverEvent());
            }
        }

        public bool Damage(int val)
        {
            if (isInvulnerable) return false;
            _currentHealth -= val;

            if (Fraction == Fraction.Player) SaveSystem.Instance.saveData.hp = _currentHealth;

            if (_currentHealth <= 0 && !isImmortal)
            {
                Dead();
                AudioManager.Instance.PlaySound(deadSound, AudioChannel.VFX, transform.position);
                return true;
            }

            AudioManager.Instance.PlaySound(dmgSound, AudioChannel.VFX, transform.position);
            OnDmgAction?.Invoke(val);
            return false;
        }

        public void Cure(int val)
        {
            _currentHealth += val;
            if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;
            if (Fraction == Fraction.Player) SaveSystem.Instance.saveData.hp = _currentHealth;

            OnCureAction?.Invoke(val);
        }
    }
}