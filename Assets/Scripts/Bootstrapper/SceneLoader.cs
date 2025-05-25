using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrapper
{
    public class SceneLoader : MonoBehaviour
    {
        private void Start()
        {
            OpenLobby();
        }

        public void OpenLobby()
        {
            SceneManager.LoadSceneAsync("Menu");
        }

        public void Load(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}