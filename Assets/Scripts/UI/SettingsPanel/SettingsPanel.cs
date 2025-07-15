using System;
using Audio;
using Constants;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities;

namespace UI.SettingsPanel
{
    public class SettingsPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private Button exitGameplayBtn;
        [SerializeField] private Button backBtn;
        private bool _isGameplay;
        private string _previousPanel = "";

        public override void Awake()
        {
            base.Awake();

            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => _isGameplay = true);

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => _isGameplay = false);

            exitGameplayBtn.onClick.AddListener(ExitGameplay);
            backBtn.onClick.AddListener(ExitSettings);
        }

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleSettings();
            }
        }

        private void ToggleSettings()
        {
            if(_previousPanel == UiConst.Guides) return;
            if (IsActive)
            {
                MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = _previousPanel });
            }
            else
            {
                MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Settings });
            }
        }

        protected override void OnPanelEnable()
        {
            exitGameplayBtn.gameObject.SetActive(_isGameplay);
            Time.timeScale = 0;
        }

        protected override void OnPanelDisable()
        {
            if (IsActive) Time.timeScale = 1;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
        }

        private void ExitSettings()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new OpenUiPanelEvent
                { PanelName = _isGameplay ? UiConst.GamePlay : UiConst.MainMenu });
        }

        public override void OpenPanel(OpenUiPanelEvent data)
        {
            base.OpenPanel(data);
            if (data.PanelName != panelName) _previousPanel = data.PanelName;
        }

        private void ExitGameplay()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new StopRoundEvent());
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}