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

        public void OpenGamePlay()
        {
            SceneManager.LoadScene("GamePlay");
            GameAnalyticsBeh.Instance.StartRound();
        }
        
        public void OpenLobby()
        {
            SceneManager.LoadScene("Menu");
            GameAnalyticsBeh.Instance.CompleteRound();
        }
    }
}