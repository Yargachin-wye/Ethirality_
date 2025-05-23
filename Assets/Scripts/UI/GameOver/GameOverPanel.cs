using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.GameOver
{
    public class GameOverPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private Button exitToMenuBtn;
        public override void Awake()
        {
            base.Awake();
            exitToMenuBtn.onClick.AddListener(ExitToMenu);
        }
        
        protected override void OnPanelDisable()
        {
            
        }

        protected override void OnPanelEnable()
        {
            
        }
        
        private void ExitToMenu()
        {
            MessageBroker.Default.Publish(new OpenUiPanelEvent
                { PanelName = UiConst.MainMenu });
        }
    }
}