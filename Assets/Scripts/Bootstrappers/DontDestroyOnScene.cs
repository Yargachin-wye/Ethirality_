using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Bootstrappers
{
    public class DontDestroyOnScene : MonoBehaviour
    {
        private void Awake()
        { 
            DontDestroyOnLoad(this);
        }
    }
}