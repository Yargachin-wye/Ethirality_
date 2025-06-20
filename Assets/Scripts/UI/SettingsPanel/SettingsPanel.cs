﻿using Audio;
using Constants;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
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

        protected override void OnPanelEnable()
        {
            exitGameplayBtn.gameObject.SetActive(_isGameplay);
            Time.timeScale = 0;
        }

        protected override void OnPanelDisable()
        {
            Time.timeScale = 1;
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


        private void ExitGameplay()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}