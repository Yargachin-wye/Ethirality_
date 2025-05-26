using System.Collections;
using Bootstrapper.Saves;
using UI;
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
        [SerializeField] private BasePanelUi gameOverPanel;
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
                .Subscribe(data => StartCoroutine(OnGameOver(data)));
        }

        private IEnumerator OnGameOver(GameOverEvent data)
        {
            SaveSystem.Instance.ResetGameData();
            SaveSystem.Instance.SaveGame();
            
            yield return new WaitForSeconds(1f);
            
            yield return gameOverPanel.FadeOut();
            
            MessageBroker.Default.Publish(new StopGameplayEvent());
            MessageBroker.Default.Publish(new SetActivePanelEvent { PanelName = UiConst.GameOver });

            
            yield return sceneLoader.LoadLobby();
            yield return new WaitForEndOfFrame();
        }

        public IEnumerator StopGameplay()
        {
            if (SaveSystem.Instance.saveData.currentDifficulty < ResManager.Instance.DifficultyLevelPacks.Length - 1)
            {
                SaveSystem.Instance.saveData.currentDifficulty++;
            }

            MessageBroker.Default.Publish(new StopGameplayEvent());
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
            yield return sceneLoader.LoadLobby();
        }
    }
}