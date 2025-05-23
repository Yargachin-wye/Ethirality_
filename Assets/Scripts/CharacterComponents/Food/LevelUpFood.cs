using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using Utilities;

namespace CharacterComponents.Food
{
    public class LevelUpFood : BaseFood
    {
        public override void Init()
        {
        }

        public override void OnEaten(Character characterEater)
        {
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.LevelUp });
            MessageBroker.Default.Publish(new LevelUpEvent());
        }
    }
}