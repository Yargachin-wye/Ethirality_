using Bootstrapper;
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
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private Button randomLevelBtn;
        [SerializeField] private Button openWorldLevelBtn;

        public override void Awake()
        {
            base.Awake();
            randomLevelBtn.onClick.AddListener(StartRandomLevel);
            openWorldLevelBtn.onClick.AddListener(StartOpenWorld);
        }
        
        protected override void OnPanelDisable()
        {
            
        }

        protected override void OnPanelEnable()
        {
            
        }
        
        private void StartRandomLevel()
        {
            MessageBroker.Default.Publish(new StartGameplayEvent());
        }

        private void StartOpenWorld()
        {
            sceneLoader.Load(ResManager.Instance.DifficultyLevelPacks[0].openWorldLevelName);
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GamePlay });
            MessageBroker.Default.Publish(new StartGameplayEvent());
        }

        
    }
}