using System;
using UI;
using UnityEngine;

namespace Bootstrappers
{
    public class GameOver : MonoBehaviour
    {
        [SerializeField] private MainUI ui;
        [SerializeField] private SceneLoader sceneLoader;
        public static GameOver Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Game Over is more than one instance!");
            }

            Instance = this;
            
        }

        public void GameIsOver()
        {
            ui.OpenGameOverMenu();
            sceneLoader.OpenLobby();
        }
    }
}