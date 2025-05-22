using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents.Food
{
    public class Yummy : BaseFood
    {
        [SerializeField] private int hpCure = 1;

        public override void OnEaten(Character characterEater)
        {
            characterEater.Stats.Cure(hpCure);
        }

        public override void Init()
        {
            
        }
    }
}