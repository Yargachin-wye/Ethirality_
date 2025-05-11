using Bootstrappers;
using Definitions;

namespace CharacterComponents.Food
{
    public class GameOverFood : BaseFood
    {
        public override void OnEaten(Character characterEater)
        {
            base.OnEaten(characterEater);
            if (characterEater.Fraction == Fraction.Player)
            {
                GameOver.Instance.GameIsOver();
                character.Stats.Damage(character.Stats.MaxHealth);
            }
        }

        public override void Init()
        {
        }
    }
}