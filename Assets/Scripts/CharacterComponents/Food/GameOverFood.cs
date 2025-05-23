using Bootstrapper;
using Definitions;

namespace CharacterComponents.Food
{
    public class GameOverFood : BaseFood
    {
        public override void OnEaten(Character characterEater)
        {
            if (characterEater.Fraction == Fraction.Player)
            {
                GameplayManager.Instance.GameIsOver();
            }
        }

        public override void Init()
        {
        }
    }
}