using Audio;
using CharacterComponents.CharacterStat;
using Constants;
using Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class TriggerDamage : BaseCharacterComponent
    {
        [SerializeField] private int dmg = 1;
        [SerializeField] private Fraction fraction = Fraction.Enemy;
        [SerializeField] private bool isDestroyOnTrigger = true;

        public override void Init()
        {
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var triggerStats = other.GetComponent<Stats>();
            if (triggerStats == null) return;
            if (triggerStats.Fraction == fraction &&
                fraction != Fraction.All)
            {
                return;
            }
            
            if (triggerStats.Fraction == Fraction.Player)
                AudioManager.Instance.PlaySound(AudioConst.DamageHitSound, AudioChannel.VFX, transform.position);
            triggerStats.Damage(dmg);

            if (isDestroyOnTrigger)
            {
                character.Stats.Dead();
            }
        }
    }
}