using UniRx;
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
        }

        public void GameIsOver()
        {
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GameOver });
            sceneLoader.OpenLobby();
        }
    }
}