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
                if (GameplayManager.Instance != null) GameplayManager.Instance.StopGameplay();
            }
        }

        public override void Init()
        {
        }
    }
}