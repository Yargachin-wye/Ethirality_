using System;
using System.Collections.Generic;
using Definitions;
using Generation;
using UnityEngine;

namespace Balance
{
    [Serializable]
    public class SpawnInfo
    {
        public string Name;
        public Vector2 Position;
    }

    [Serializable]
    public class BuildLayerMetrics
    {
        public float Dps;
        public float TimeToClear;
        public float ExpectedDamage;
        public float ExpectedHeal;
        public float ExpectedLevelUpHp;
    }

    [Serializable]
    public class LayerReport
    {
        public int Index;
        public bool Empty;
        public float Difficulty;
        public float PacingMultiplier;
        public float Budget;
        public float FoodBudget;
        public float Height;
        public int Rolled;
        public int Placed;
        public float PlacementLoss;
        public float Spent;
        public float FoodSpent;
        public int ThreatPlaced;
        public int FoodPlaced;
        public Dictionary<EnemySpawnTable.EnemyRole, int> CountsByRole = new();
        public Dictionary<string, int> CountsByName = new();
        public int Rewards;
        public float KillableHp;
        public int Hazards;
        public Dictionary<string, BuildLayerMetrics> Metrics = new();
        public List<SpawnInfo> Spawns = new();
    }

    [Serializable]
    public class RunReport
    {
        public string BuildName;
        public int RunLength;
        public bool Died;
        public List<float> HpTrace = new();
    }

    public static class LayerSimulator
    {
        public static LayerReport SimulateLayer(BalanceScenario s, int index, float cursorY)
        {
            var report = new LayerReport { Index = index };

            var layer = LayerPlanner.Plan(s.Seed, index, cursorY, s.DifficultyProfile,
                s.LayerDefinitions, out float pacing);
            if (layer == null)
            {
                report.Empty = true;
                return report;
            }

            report.Difficulty = layer.Difficulty;
            report.PacingMultiplier = pacing;
            report.Budget = layer.ThreatBudget;
            report.FoodBudget = layer.FoodBudget;
            report.Height = layer.Bounds.height;

            var populated = LayerPopulator.Populate(layer, s.Placement, s.AttemptsPerEnemy);
            report.Rolled = populated.Rolled;
            report.Placed = populated.Placed;
            report.ThreatPlaced = populated.ThreatPlaced;
            report.FoodPlaced = populated.FoodPlaced;
            report.PlacementLoss = report.Rolled > 0 ? 1f - (float)report.Placed / report.Rolled : 0f;

            RewardPlanner.Generate(layer, s.Reward, s.Placement, s.AttemptsPerEnemy);

            var profiles = new Dictionary<CharacterDefinition, EnemyProfile>();
            var placed = new List<EnemyProfile>();
            foreach (SpawnRequest spawn in layer.Spawns)
            {
                if (spawn.Definition == null) continue;
                if (!profiles.TryGetValue(spawn.Definition, out var profile))
                {
                    profile = EnemyProfile.From(spawn.Definition);
                    profiles[spawn.Definition] = profile;
                }
                placed.Add(profile);

                report.Spawns.Add(new SpawnInfo { Name = spawn.Definition.name, Position = spawn.Position });
                if (layer.SpawnTable.TryGetEntry(spawn.Definition, out var entry))
                {
                    if (entry.kind == EnemySpawnTable.SpawnKind.Food) report.FoodSpent += entry.threatCost;
                    else report.Spent += entry.threatCost;
                }

                var role = spawn.Definition == s.Reward.RewardDefinition
                    ? EnemySpawnTable.EnemyRole.Reward
                    : RoleOf(layer.SpawnTable, spawn.Definition);
                report.CountsByRole.TryGetValue(role, out int roleCount);
                report.CountsByRole[role] = roleCount + 1;

                report.CountsByName.TryGetValue(profile.Name, out int nameCount);
                report.CountsByName[profile.Name] = nameCount + 1;

                if (profile.IsLevelUp) report.Rewards++;
                if (profile.IsHazard) report.Hazards++;
            }

            report.KillableHp = BalanceModel.KillableHpPool(placed);

            foreach (var build in s.Builds())
            {
                report.Metrics[build.Name] = new BuildLayerMetrics
                {
                    Dps = build.Dps(s),
                    TimeToClear = report.Placed > 0 ? BalanceModel.TimeToClear(placed, build, s) : 0f,
                    ExpectedDamage = BalanceModel.ExpectedDamage(placed, build, s),
                    ExpectedHeal = BalanceModel.ExpectedHeal(placed, build, s),
                    ExpectedLevelUpHp = BalanceModel.ExpectedLevelUpHp(placed, s)
                };
            }

            return report;
        }

        public static List<LayerReport> SimulateLayers(BalanceScenario s, int count)
        {
            var reports = new List<LayerReport>(count);
            float cursorY = 0f;
            for (int i = 0; i < count; i++)
            {
                var report = SimulateLayer(s, i, cursorY);
                reports.Add(report);
                if (!report.Empty) cursorY += report.Height;
            }
            return reports;
        }

        public static RunReport SimulateRun(BalanceScenario s, PlayerBuild b, IReadOnlyList<LayerReport> layers)
        {
            var run = new RunReport { BuildName = b.Name };
            float hp = b.StartHp;
            for (int i = 0; i < layers.Count; i++)
            {
                var m = layers[i].Metrics[b.Name];
                hp = Mathf.Min(b.MaxHp, hp - m.ExpectedDamage + m.ExpectedHeal + m.ExpectedLevelUpHp);
                run.HpTrace.Add(hp);
                if (hp <= 0f)
                {
                    run.RunLength = i;
                    run.Died = true;
                    return run;
                }
            }
            run.RunLength = layers.Count;
            run.Died = false;
            return run;
        }

        private static EnemySpawnTable.EnemyRole RoleOf(EnemySpawnTable table, CharacterDefinition definition)
        {
            if (table?.Entries != null)
                foreach (var e in table.Entries)
                    if (e.definition == definition) return e.role;
            return EnemySpawnTable.EnemyRole.Melee;
        }
    }
}
