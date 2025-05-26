using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrapper
{
    public class SceneLoader : MonoBehaviour
    {
        
        public IEnumerator LoadLobby()
        {
            yield return LoadSceneAsync("Menu");
        }

        public IEnumerator Load(string sceneName)
        {
            yield return LoadSceneAsync(sceneName);
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}