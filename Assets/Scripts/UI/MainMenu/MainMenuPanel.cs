using Audio;
using Bootstrapper;
using Constants;
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
        [Space]
        [SerializeField] private Button restartBtn;
        [SerializeField] private Button guidesBtn;
        [SerializeField] private GameObject restartPanel;

        public override void Awake()
        {
            base.Awake();
            playBtn.onClick.AddListener(StartGamePlay);
            settingsBtn.onClick.AddListener(Settings);
            exitBtn.onClick.AddListener(ExitGame);

            guidesBtn.onClick.AddListener(OpenGuidesPanel);
            // There is no resumable run and therefore no "delete progress / restart" action.
            if (restartBtn != null) restartBtn.gameObject.SetActive(false);
            if (restartPanel != null) restartPanel.SetActive(false);
        }

        private void OpenGuidesPanel()
        {
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Guides });
        }

        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            if (restartPanel != null) restartPanel.SetActive(false);
        }

        private void ExitGame()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            Application.Quit();
        }

        private void Settings()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Settings });
        }

        private void StartGamePlay()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            GameSession.Instance.Session.End();
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
        }
    }
}