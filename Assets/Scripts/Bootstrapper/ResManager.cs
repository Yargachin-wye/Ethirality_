﻿using System;
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
        [SerializeField] private DifficultyLevelPack[] difficultyLevelPacks;

        public CharacterDefinition[] Characters => characters;
        public ImprovementDefinition[] Improvements => improvements;
        public DifficultyLevelPack[] DifficultyLevelPacks => difficultyLevelPacks;

        private void Validate()
        {
            foreach (var difficultyLevelPacks in difficultyLevelPacks)
            {
                difficultyLevelPacks.Validate();
            }

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
            [SerializeField, HideInInspector] public List<string> levelsCollection;

            [SerializeField, Dropdown("levelsCollection")]
            public string levelName;
        }

        [Serializable]
        public class DifficultyLevelPack
        {
            public LevelPack[] openWorldLevelName;

            public LevelPack[] randomLevelName;

            private static List<string> FindAllLevels()
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                List<string> sceneNames = new List<string>();

                for (int i = 0; i < sceneCount; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    sceneNames.Add(name);
                }

                return sceneNames;
            }

            public void Validate()
            {
                foreach (var worldLevelName in openWorldLevelName)
                {
                    worldLevelName.levelsCollection = FindAllLevels();
                }

                foreach (var worldLevelName in randomLevelName)
                {
                    worldLevelName.levelsCollection = FindAllLevels();
                }
            }
        }
    }
}