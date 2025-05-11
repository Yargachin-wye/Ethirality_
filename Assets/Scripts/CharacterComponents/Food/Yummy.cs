using UnityEngine;

namespace CharacterComponents.Food
{
    public class Yummy : BaseFood
    {
        [SerializeField] private int hp = 1;

        public override void OnEaten(Character characterEater)
        {
            base.OnEaten(characterEater);
            characterEater.Stats.Cure(hp);
            character.Stats.Damage(character.Stats.MaxHealth);
        }

        public override void Init()
        {
            
        }
    }
}