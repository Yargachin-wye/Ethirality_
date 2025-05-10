using CharacterComponents.Food;

namespace CharacterComponents
{
    public class Eater : BaseCharacterComponent
    {
        public override void Init()
        {
            
        }
        public void Eat(BaseFood food)
        {
            food.OnEaten(character);
        }

        
    }
}