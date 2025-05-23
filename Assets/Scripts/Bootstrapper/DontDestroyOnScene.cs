using UnityEngine;

namespace Bootstrapper
{
    public class DontDestroyOnScene : MonoBehaviour
    {
        private void Awake()
        { 
            DontDestroyOnLoad(this);
        }
    }
}