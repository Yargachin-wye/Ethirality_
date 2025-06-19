using Audio;
using Bootstrapper;
using Bootstrapper.Saves;
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
        [SerializeField] private Button restartYesBtn;
        [SerializeField] private Button restartNoBtn;
        [SerializeField] private GameObject restartPanel;

        public override void Awake()
        {
            base.Awake();
            playBtn.onClick.AddListener(StartGamePlay);
            settingsBtn.onClick.AddListener(Settings);
            exitBtn.onClick.AddListener(ExitGame);

            guidesBtn.onClick.AddListener(OpenGuidesPanel);
            restartBtn.onClick.AddListener(() => OpenRestartRunPanel(true));
            restartYesBtn.onClick.AddListener(RestartRun);
            restartNoBtn.onClick.AddListener(() => OpenRestartRunPanel(false));
        }

        private void OpenGuidesPanel()
        {
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Guides });
        }

        private void OpenRestartRunPanel(bool isActive)
        {
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            restartPanel.SetActive(isActive);
        }

        private void RestartRun()
        {
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            SaveSystem.Instance.ResetGameData();
            StartGamePlay();
        }

        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            restartPanel.SetActive(false);
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
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
        }
    }
}