using Bootstrappers;
using Definitions;

namespace CharacterComponents.Food
{
    public class GameOverFood : BaseFood
    {
        public override void OnEaten(Character characterEater)
        {
            base.OnEaten(characterEater);
            if (characterEater.Stats.Fraction == Fraction.Player)
            {
                GameOver.Instance.GameIsOver();
            }
        }
    }
}