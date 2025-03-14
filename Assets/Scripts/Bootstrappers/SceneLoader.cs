using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class SceneLoader : MonoBehaviour
    {
        public void OpenGamePlay()
        {
            SceneManager.LoadScene("GamePlay");
        }
        public void OpenLobby()
        {
            SceneManager.LoadScene("Lobby");
        }
    }
}