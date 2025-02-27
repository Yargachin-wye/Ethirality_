using CharacterComponents.Food;

namespace CharacterComponents
{
    public class Eater : BaseCharacterComponent
    {
        public void Eat(BaseFood food)
        {
            food.OnEaten(character);
        }
    }
}