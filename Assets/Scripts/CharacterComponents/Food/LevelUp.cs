using UniRx;
using UniRxEvents.GamePlay;

namespace CharacterComponents.Food
{
    public class LevelUp : BaseFood
    {
        public override void Init()
        {
            
        }

        public override void OnEaten(Character characterEater)
        {
            MessageBroker.Default.Publish(new LevelUpEvent());
        }
    }
}