using System.Collections;
using System.Collections.Generic;
using Audio;
using Bootstrapper;
using Bootstrapper.Saves;
using Constants;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.ChoosingNextLevel
{
    public class ChoosingNextLevelPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private Button playBtn;
        [SerializeField] private Button backBtn;
        [SerializeField] private Image fieldDifficulty;

        public override void Awake()
        {
            base.Awake();
            playBtn.onClick.AddListener(StartOpenWorld);
            backBtn.onClick.AddListener(BackMenu);
        }


        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            fieldDifficulty.fillAmount = 0.5f;
        }
        
        private void StartOpenWorld()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);

            var level = ResManager.Instance.Level;
            
            StartCoroutine(StartLevel(level.levelName));
        }

        private IEnumerator StartLevel(string nextLevelName)
        {
            yield return StartCoroutine(sceneLoader.Load(nextLevelName));
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GamePlay });
            MessageBroker.Default.Publish(new StartRoundEvent());
            
            yield return new WaitForSeconds(0.1f);
            if (!SaveSystem.Instance.settingsData.isSeeGuides)
            {
                SaveSystem.Instance.settingsData.isSeeGuides = true;
                MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Guides });
            }
        }

        private void BackMenu()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);

            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}