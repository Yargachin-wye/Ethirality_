using System;
using UnityEngine;

namespace UI
{
    public class MainUI : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameOverMenu;
        [SerializeField] private GameObject backGround;

        private void Awake()
        {
            
        }

        public void ExitGame()
        {
            Application.Quit();
        }   
        
        public void StartGamePlay()
        {
            mainMenu.SetActive(false);
            sceneLoader.OpenGamePlay();
            backGround.SetActive(false);
        }

        public void OpenMainMenu()
        {
            mainMenu.SetActive(true);
            gameOverMenu.SetActive(false);
            backGround.SetActive(true);
        }

        public void OpenGameOverMenu()
        {
            mainMenu.SetActive(false);
            gameOverMenu.SetActive(true);
            backGround.SetActive(true);
        }
    }
}