using UnityEngine;

namespace CharacterComponents.Food
{
    public class Poison : BaseFood
    {
        [SerializeField] private int dmg = 1;

        public override void OnEaten(Character characterEater)
        {
            base.OnEaten(characterEater);
            characterEater.Stats.Damage(dmg);
        }
    }
}