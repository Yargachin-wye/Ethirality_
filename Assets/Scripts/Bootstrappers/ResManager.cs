using System;
using Definitions;
using UnityEngine;

namespace Bootstrappers
{
    public class ResManager : MonoBehaviour
    {
        public static ResManager instance;
        [SerializeField] private CharacterDefinition[] characters;
        [SerializeField] private ImprovementDefinition[] improvements;
        
        public CharacterDefinition[] Characters => characters;
        public ImprovementDefinition[] Improvements => improvements;

        private void Validate()
        {
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i].SetResId(i);
            }
            for (int i = 0; i < improvements.Length; i++)
            {
                improvements[i].SetResId(i);
            }
        }

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("More than one instance of ResourceManager");
                return;
            }

            instance = this;
            Validate();
        }

        private void Start()
        {
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}