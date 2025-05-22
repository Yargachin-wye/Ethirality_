using UnityEngine;

namespace CharacterComponents.Food
{
    public abstract class BaseFood : BaseCharacterComponent
    {
        public abstract void OnEaten(Character characterEater);
    }
}