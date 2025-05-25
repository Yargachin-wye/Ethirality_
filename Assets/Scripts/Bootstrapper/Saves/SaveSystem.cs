using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bootstrapper.Saves
{
    [Serializable]
    public struct SaveGameData
    {
        public string version;
        public int currentDifficulty;
        public int hp;
        public List<int> playerUpgradeResIds;

        public SaveGameData(SaveGameData defaultSaveData)
        {
            version = defaultSaveData.version;
            playerUpgradeResIds = defaultSaveData.playerUpgradeResIds;
            hp = defaultSaveData.hp;
            currentDifficulty = defaultSaveData.currentDifficulty;
        }
    }

    [Serializable]
    public struct SaveSettingsData
    {
        public float musicVolume;
        public float sfxVolume;
    }

    public class SaveSystem : MonoBehaviour
    {
        [SerializeField] private SaveGameData defaultSaveData;
        [SerializeField] private SaveSettingsData defaultSettingsData;

        private static string SaveGamePath => Path.Combine(Application.persistentDataPath, "savegame.json");
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "settings.json");

        public SaveGameData saveData;

        public SaveSettingsData SettingsData { get; private set; }
        public static SaveSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("SaveSystem.Instance != null On Awake!");
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            LoadSaves();
            LoadSettings();
        }

        public void Validate()
        {
        }

        private void OnApplicationQuit()
        {
            SaveGame();
            SaveSettings();
        }

        public void SaveGame()
        {
            Debug.LogWarning($"^^^ SAVE GAME");
            try
            {
                string jsonData = JsonUtility.ToJson(saveData, prettyPrint: true);
                File.WriteAllText(SaveGamePath, jsonData);
                Debug.Log($"Game saved successfully to {SaveGamePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                string jsonData = JsonUtility.ToJson(SettingsData, prettyPrint: true);
                File.WriteAllText(SettingsPath, jsonData);
                Debug.Log($"Settings saved successfully to {SettingsPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }

        public void LoadSaves()
        {
            Debug.LogWarning($"^^^ LOAD GAME");
            try
            {
                if (File.Exists(SaveGamePath))
                {
                    string jsonData = File.ReadAllText(SaveGamePath);
                    saveData = JsonUtility.FromJson<SaveGameData>(jsonData);
                    HandleVersionMigration();
                    Debug.Log("Game loaded successfully");
                }
                else
                {
                    saveData = new SaveGameData(defaultSaveData);
                    Debug.Log("No save file found, creating new one");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string jsonData = File.ReadAllText(SettingsPath);
                    SettingsData = JsonUtility.FromJson<SaveSettingsData>(jsonData);
                    Debug.Log("Settings loaded successfully");
                }
                else
                {
                    SettingsData = new SaveSettingsData { musicVolume = 1.0f, sfxVolume = 1.0f };
                    Debug.Log("No settings file found, using default values");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveGamePath))
                {
                    File.Delete(SaveGamePath);
                    Debug.Log("Save file deleted");
                }

                if (File.Exists(SettingsPath))
                {
                    File.Delete(SettingsPath);
                    Debug.Log("Settings file deleted");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save/settings file: {e.Message}");
            }
        }

        private void HandleVersionMigration()
        {
            if (saveData.version != Application.version)
            {
                // Debug.Log($"The version[ {saveData.version} ] is outdated !!!");
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Save System/Delete All Save Files")]
        private static void DeleteAllSavesEditor()
        {
            DeleteSave();
            UnityEditor.EditorUtility.DisplayDialog("Save System", "All save files deleted", "OK");
        }

        [UnityEditor.MenuItem("Tools/Save System/Open Save Folder")]
        private static void OpenSaveFolderEditor()
        {
            string folderPath = Application.persistentDataPath.Replace("/", "\\");
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
#endif
        public void ResetGameData()
        {
            Instance.saveData.playerUpgradeResIds.Clear();
            Instance.saveData.currentDifficulty = 0;
            Instance.saveData.hp = defaultSaveData.hp;
        }
    }
}