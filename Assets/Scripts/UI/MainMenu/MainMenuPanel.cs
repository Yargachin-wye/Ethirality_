using Bootstrapper;
using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.MainMenu
{
    public class MainMenuPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private SceneLoader sceneLoader;
        [Space]
        [SerializeField] private Button playBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button exitBtn;

        public override void Awake()
        {
            base.Awake();
            playBtn.onClick.AddListener(StartGamePlay);
            settingsBtn.onClick.AddListener(Settings);
            exitBtn.onClick.AddListener(ExitGame);
        }

        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
        }

        private void ExitGame()
        {
            if(!IsActive) return;
            Application.Quit();
        }

        private void Settings()
        {
            if(!IsActive) return;
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Settings });
        }

        private void StartGamePlay()
        {
            if(!IsActive) return;
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
        }
    }
}