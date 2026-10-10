using Audio;
using CharacterComponents.CharacterStat;
using Constants;
using Definitions;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class TriggerDamage : BaseCharacterComponent
    {
        private static string[] collection = AudioConst.AllSounds;
        [SerializeField] private int dmg = 1;
        [SerializeField, Dropdown("collection")] private string hitSound = AudioConst.LaserTriggerHit;
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
            
            if (triggerStats.Fraction == Fraction.Player && !string.IsNullOrEmpty(hitSound))
                AudioManager.Instance.PlaySound(hitSound, AudioChannel.VFX, transform.position);
            triggerStats.Damage(dmg);

            if (isDestroyOnTrigger)
            {
                character.Stats.Dead();
            }
        }
    }
}