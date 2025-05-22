using Bootstrappers;
using Definitions;

namespace CharacterComponents.Food
{
    public class GameOverFood : BaseFood
    {
        public override void OnEaten(Character characterEater)
        {
            if (characterEater.Fraction == Fraction.Player)
            {
                GameOver.Instance.GameIsOver();
            }
        }

        public override void Init()
        {
        }
    }
}