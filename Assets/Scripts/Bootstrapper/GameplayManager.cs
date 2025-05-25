using Bootstrapper.Saves;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

namespace Bootstrapper
{
    public class GameplayManager : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;
        public static GameplayManager Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("GameplayManager is more than one instance!");
                return;
            }

            Instance = this;

            MessageBroker.Default
                .Receive<GameOverEvent>()
                .Subscribe(data => OnGameOver(data));
        }

        private void OnGameOver(GameOverEvent data)
        {
            SaveSystem.Instance.saveData.playerUpgradeResIds.Clear();
            SaveSystem.Instance.SaveGame();
            MessageBroker.Default.Publish(new StopGameplayEvent());
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GameOver });
            sceneLoader.OpenLobby();
        }

        public void GameIsOver()
        {
            MessageBroker.Default.Publish(new StopGameplayEvent());
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
            sceneLoader.OpenLobby();
        }
    }
}