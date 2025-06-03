using System.Collections;
using System.Collections.Generic;
using Bootstrapper;
using Bootstrapper.Saves;
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
        [SerializeField] private Button randomLevelBtn;
        [SerializeField] private Button openWorldLevelBtn;
        [SerializeField] private Button backBtn;
        [SerializeField] private Image fieldDifficulty;

        public override void Awake()
        {
            base.Awake();
            randomLevelBtn.onClick.AddListener(StartRandomLevel);
            openWorldLevelBtn.onClick.AddListener(StartOpenWorld);
            backBtn.onClick.AddListener(BackMenu);
        }


        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            fieldDifficulty.fillAmount = (float)SaveSystem.Instance.saveData.currentDifficulty /
                                         ResManager.Instance.DifficultyLevelPacks.Length;
        }

        private void StartRandomLevel()
        {
            if(!IsActive) return;
            
            string nextLevelName = ResManager.Instance
                .DifficultyLevelPacks[SaveSystem.Instance.saveData.currentDifficulty]
                .randomLevelName;

            StartCoroutine(StartLevel(nextLevelName));
        }

        private void StartOpenWorld()
        {
            if(!IsActive) return;
            string nextLevelName = (ResManager.Instance
                .DifficultyLevelPacks[SaveSystem.Instance.saveData.currentDifficulty]
                .openWorldLevelName);
            StartCoroutine(StartLevel(nextLevelName));
        }

        private IEnumerator StartLevel(string nextLevelName)
        {
            yield return StartCoroutine(sceneLoader.Load(nextLevelName));
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GamePlay });
            MessageBroker.Default.Publish(new StartRoundEvent());
            if (SaveSystem.Instance.saveData.currentDifficulty == 0)
            {
                MessageBroker.Default.Publish(new GameStartEvent());
            }
        }

        private void BackMenu()
        {
            if(!IsActive) return;
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}