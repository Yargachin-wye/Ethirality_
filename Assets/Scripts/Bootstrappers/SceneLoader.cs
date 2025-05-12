using GameAnalyticsSDK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrappers
{
    public class SceneLoader : MonoBehaviour
    {
        
        public void OpenGamePlay()
        {
            SceneManager.LoadScene("GamePlay");
            GameAnalyticsBeh.Instance.StartRound();
        }
        
        public void OpenLobby()
        {
            SceneManager.LoadScene("Lobby");
            GameAnalyticsBeh.Instance.CompleteRound();
        }
    }
}