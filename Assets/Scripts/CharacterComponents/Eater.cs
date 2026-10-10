using Audio;
using CharacterComponents.Food;
using Constants;

namespace CharacterComponents
{
    public class Eater : BaseCharacterComponent
    {
        public override void Init()
        {
            
        }
        public void Eat(BaseFood food)
        {
            AudioManager.Instance.PlaySound(AudioConst.EatSound, AudioChannel.VFX, transform.position);
            food.OnEaten(character);
        }
        
    }
}