using System.Collections.Generic;
using Definitions;
using UnityEditor;
using UnityEngine;

namespace Balance.Editor
{
    public static class BalanceScenarioFactory
    {
        private const string ScenarioPath = "Assets/Infos/Balance/OpenWorld.asset";
        private const string DifficultyProfilePath = "Assets/Infos/Generation/New Difficulty Profile.asset";
        private const string LayerDefinitionPath = "Assets/Infos/Generation/New Layer Definition.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player/Player.prefab";
        private const string ImprovementsFolder = "Assets/Infos/Improvements";

        [MenuItem("Tools/Balance/Create Default Scenario")]
        public static void CreateDefault()
        {
            if (AssetDatabase.LoadAssetAtPath<BalanceScenario>(ScenarioPath) != null)
            {
                Debug.Log($"[BalanceScenarioFactory] Сценарий уже существует: {ScenarioPath}");
                return;
            }

            var difficultyProfile = AssetDatabase.LoadAssetAtPath<Generation.DifficultyProfile>(DifficultyProfilePath);
            var layerDefinition = AssetDatabase.LoadAssetAtPath<Generation.LayerDefinition>(LayerDefinitionPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (difficultyProfile == null || layerDefinition == null || playerPrefab == null)
            {
                Debug.LogError("[BalanceScenarioFactory] Не найдены обязательные ассеты: " +
                             $"{DifficultyProfilePath}, {LayerDefinitionPath}, {PlayerPrefabPath}");
                return;
            }

            var improvements = new List<ImprovementDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:ImprovementDefinition", new[] { ImprovementsFolder }))
            {
                var improvement = AssetDatabase.LoadAssetAtPath<ImprovementDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (improvement != null) improvements.Add(improvement);
            }

            var scenario = ScriptableObject.CreateInstance<BalanceScenario>();
            var so = new SerializedObject(scenario);
            so.FindProperty("difficultyProfile").objectReferenceValue = difficultyProfile;
            var defs = so.FindProperty("layerDefinitions");
            defs.arraySize = 1;
            defs.GetArrayElementAtIndex(0).objectReferenceValue = layerDefinition;
            so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            var imps = so.FindProperty("improvements");
            imps.arraySize = improvements.Count;
            for (int i = 0; i < improvements.Count; i++)
                imps.GetArrayElementAtIndex(i).objectReferenceValue = improvements[i];
            so.FindProperty("seed").intValue = 1337;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/Infos/Balance"))
                AssetDatabase.CreateFolder("Assets/Infos", "Balance");

            AssetDatabase.CreateAsset(scenario, ScenarioPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BalanceScenarioFactory] Создан сценарий: {ScenarioPath} (улучшений: {improvements.Count})");
        }
    }
}
