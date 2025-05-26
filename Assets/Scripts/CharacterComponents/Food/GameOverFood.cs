using System;
using Bootstrapper;
using Definitions;
using UI;

namespace CharacterComponents.Food
{
    public class GameOverFood : BaseFood
    {
        public override void OnEaten(Character characterEater)
        {
            if (characterEater.Fraction == Fraction.Player)
            {
                UiCompass.Instance.RemoveExit(transform);
                StartCoroutine(GameplayManager.Instance.StopGameplay());
            }
        }

        protected override void Awake()
        {
            base.Awake();
            UiCompass.Instance.AddExit(transform);
        }

        private void OnEnable()
        {
            UiCompass.Instance.AddExit(transform);
        }

        public override void Init()
        {
        }
    }
}