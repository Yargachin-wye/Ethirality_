using System.Collections.Generic;
using Definitions;
using Generation;
using UnityEditor;
using UnityEngine;

namespace Balance.Editor
{
    public static class SpawnTableMigrator
    {
        private const string ScenarioPath = "Assets/Infos/Balance/OpenWorld.asset";

        [MenuItem("Tools/Balance/Migrate Spawn Tables")]
        public static void Migrate()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<BalanceScenario>(ScenarioPath);
            if (scenario == null)
            {
                Debug.LogError($"[SpawnTableMigrator] Сценарий не найден: {ScenarioPath}");
                return;
            }

            var reference = WeakestBuild(scenario);
            int tables = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemySpawnTable"))
            {
                var table = AssetDatabase.LoadAssetAtPath<EnemySpawnTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table == null) continue;
                MigrateTable(table, scenario, reference);
                tables++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpawnTableMigrator] Таблиц обработано: {tables}");
        }

        public static void MigrateTable(EnemySpawnTable table, BalanceScenario scenario, PlayerBuild reference)
        {
            var so = new SerializedObject(table);
            var entries = so.FindProperty("entries");
            var mobilityEntries = new List<SerializedProperty>();

            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                var def = e.FindPropertyRelative("definition").objectReferenceValue as CharacterDefinition;
                if (def == null) continue;

                var profile = EnemyProfile.From(def);
                var legacyKind = (EnemySpawnTable.SpawnKind) e.FindPropertyRelative("kind").enumValueIndex;
                var derived = EntityResourceModel.Derive(profile, legacyKind, reference, scenario);
                var pool = EntityResourceModel.PoolFor(derived, legacyKind);
                if (pool != legacyKind) derived = EntityResourceModel.Derive(profile, pool, reference, scenario);

                bool migrated = e.FindPropertyRelative("threat").floatValue > 0f ||
                                e.FindPropertyRelative("food").floatValue > 0f ||
                                e.FindPropertyRelative("mobility").floatValue > 0f;
                float legacyCost = e.FindPropertyRelative("threatCost").floatValue;

                float threatScale = e.FindPropertyRelative("threatScale").floatValue;
                if (!migrated)
                    threatScale = pool == EnemySpawnTable.SpawnKind.Threat && derived.Threat > 0f
                        ? legacyCost / derived.Threat
                        : 1f;

                float foodScale = migrated ? e.FindPropertyRelative("foodScale").floatValue : 1f;
                float mobilityScale = migrated ? e.FindPropertyRelative("mobilityScale").floatValue : 1f;

                e.FindPropertyRelative("kind").enumValueIndex = (int) pool;
                e.FindPropertyRelative("threatScale").floatValue = threatScale;
                e.FindPropertyRelative("foodScale").floatValue = foodScale;
                e.FindPropertyRelative("mobilityScale").floatValue = mobilityScale;
                e.FindPropertyRelative("threat").floatValue = derived.Threat * EnemySpawnTable.Entry.Scale(threatScale);
                e.FindPropertyRelative("food").floatValue = derived.Food * EnemySpawnTable.Entry.Scale(foodScale);
                e.FindPropertyRelative("mobility").floatValue = derived.Mobility * EnemySpawnTable.Entry.Scale(mobilityScale);
                e.FindPropertyRelative("mobilityKind").enumValueIndex = (int) derived.Kind;

                if (pool == EnemySpawnTable.SpawnKind.Mobility)
                {
                    e.FindPropertyRelative("maxPerLayer").intValue = 0;
                    mobilityEntries.Add(e);
                }
            }

            EnsureMobilityAlwaysAvailable(mobilityEntries);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
        }

        private static void EnsureMobilityAlwaysAvailable(List<SerializedProperty> mobilityEntries)
        {
            if (mobilityEntries.Count == 0) return;

            SerializedProperty earliest = null;
            foreach (var e in mobilityEntries)
            {
                float unlock = e.FindPropertyRelative("unlockDifficulty").floatValue;
                if (unlock <= 0f && e.FindPropertyRelative("retireDifficulty").floatValue <= 0f) return;
                if (earliest == null || unlock < earliest.FindPropertyRelative("unlockDifficulty").floatValue)
                    earliest = e;
            }

            earliest.FindPropertyRelative("unlockDifficulty").floatValue = 0f;
            earliest.FindPropertyRelative("fullWeightDifficulty").floatValue = 0f;
            earliest.FindPropertyRelative("retireDifficulty").floatValue = 0f;
        }

        public static PlayerBuild WeakestBuild(BalanceScenario scenario)
        {
            PlayerBuild weakest = null;
            foreach (var build in scenario.Builds())
                if (weakest == null || build.MaxVerticalGap < weakest.MaxVerticalGap) weakest = build;
            return weakest;
        }
    }
}
