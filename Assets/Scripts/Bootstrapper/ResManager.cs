using System;
using System.Collections.Generic;
using Definitions;
using EditorAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Bootstrapper
{
    public class ResManager : MonoBehaviour
    {
        public static ResManager Instance;
        [SerializeField] private CharacterDefinition[] characters;
        [SerializeField] private ImprovementDefinition[] improvements;
        [Space]
        [SerializeField] private LevelPack openWorldLevelName;

        public CharacterDefinition[] Characters => characters;
        public ImprovementDefinition[] Improvements => improvements;
        public LevelPack Level => openWorldLevelName;

        private void Validate()
        {
            openWorldLevelName.FindAllLevels();
            
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
            if (Instance != null)
            {
                Debug.LogError("More than one instance of ResourceManager");
                return;
            }

            Instance = this;
            Validate();
        }

        private void Start()
        {
        }


        private void OnValidate()
        {
            Validate();
        }

        [Serializable]
        public class LevelPack
        {
            public void FindAllLevels()
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                levelsCollection = new List<string>();

                for (int i = 0; i < sceneCount; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    levelsCollection.Add(name);
                }
            }

            [SerializeField, HideInInspector] public List<string> levelsCollection;

            [SerializeField, Dropdown("levelsCollection")]
            public string levelName;
        }
    }
}