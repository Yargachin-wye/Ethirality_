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
        public float ClimbTime;
        public float MaxGap;
        public bool Climbable;
        public float ClimbPenaltyHp;
    }

    [Serializable]
    public class LayerReport
    {
        public int Index;
        public bool Empty;
        public float Difficulty;
        public float PacingMultiplier;
        public float ThreatBudget;
        public float FoodBudget;
        public float MobilityBudget;
        public float Height;
        public float YMin;
        public int Rolled;
        public int Placed;
        public float PlacementLoss;
        public float SpentThreat;
        public float SpentFood;
        public float SpentMobility;
        public int ThreatPlaced;
        public int FoodPlaced;
        public int MobilityPlaced;
        public int MinSupports;
        public int SupportCount;
        public int ChainLength;
        public float ChainMaxStep;
        public float ChainMobility;
        public float HarmfulChainMobility;
        public List<Vector2> ChainNodes = new();
        public Vector2 ClimbStart;
        public Vector2 ClimbEnd;
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
        public List<float> ClimbTrace = new();
        public float ClimbTime;
        public float ClimbPenaltyHp;
        public int UnclimbableLayers;
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
            report.ThreatBudget = layer.ThreatBudget;
            report.FoodBudget = layer.FoodBudget;
            report.MobilityBudget = layer.MobilityBudget;
            report.MinSupports = layer.Climb?.MinSupports ?? 0;
            report.Height = layer.Bounds.height;
            report.YMin = layer.Bounds.yMin;

            var populated = LayerPopulator.Populate(layer, s.Placement, s.AttemptsPerEnemy);
            report.Rolled = populated.Rolled;
            report.Placed = populated.Placed;
            report.ThreatPlaced = populated.ThreatPlaced;
            report.FoodPlaced = populated.FoodPlaced;
            report.MobilityPlaced = populated.MobilityPlaced;
            report.PlacementLoss = report.Rolled > 0 ? 1f - (float)report.Placed / report.Rolled : 0f;

            RewardPlanner.Generate(layer, s.Reward, s.Placement, s.AttemptsPerEnemy);

            var profiles = new Dictionary<CharacterDefinition, EnemyProfile>();
            var placed = new List<EnemyProfile>();
            var supports = new List<Vector2>();
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
                    if (entry.kind == EnemySpawnTable.SpawnKind.Food) report.SpentFood += entry.FoodValue;
                    else if (entry.kind == EnemySpawnTable.SpawnKind.Mobility) report.SpentMobility += entry.MobilityValue;
                    else report.SpentThreat += entry.ThreatValue;
                    if (entry.IsSupport) supports.Add(spawn.Position);
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
            report.SupportCount = supports.Count;
            FillChain(report, layer);

            Rect bounds = layer.Bounds;
            float startX = layer.Climb?.StartX ?? bounds.center.x, endX = layer.Climb?.EndX ?? bounds.center.x;
            report.ClimbStart = new Vector2(startX, bounds.yMin - (layer.Climb?.ExitOffset ?? 0f));
            report.ClimbEnd = new Vector2(endX, bounds.yMax + (layer.Climb?.EntryOffset ?? 0f));

            foreach (var build in s.Builds())
            {
                var climb = ClimbAnalyzer.Analyze(supports, report.ClimbStart, report.ClimbEnd, build, report.Height);
                report.Metrics[build.Name] = new BuildLayerMetrics
                {
                    ClimbTime = climb.ClimbTime,
                    MaxGap = climb.MaxGap,
                    Climbable = climb.Climbable,
                    ClimbPenaltyHp = climb.Climbable ? 0f : s.UnclimbablePenaltyHp,
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
                hp = Mathf.Min(b.MaxHp, hp - m.ExpectedDamage - m.ClimbPenaltyHp + m.ExpectedHeal + m.ExpectedLevelUpHp);
                run.HpTrace.Add(hp);
                run.ClimbTrace.Add(m.ClimbTime);
                run.ClimbTime += m.ClimbTime;
                run.ClimbPenaltyHp += m.ClimbPenaltyHp;
                if (!m.Climbable) run.UnclimbableLayers++;
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

        private static void FillChain(LayerReport report, LayerContext layer)
        {
            report.ChainLength = layer.Chain.Count;
            for (int i = 0; i < layer.Chain.Count; i++)
            {
                var node = layer.Chain[i];
                report.ChainNodes.Add(node.Position);
                if (i > 0)
                    report.ChainMaxStep = Mathf.Max(report.ChainMaxStep,
                        Vector2.Distance(layer.Chain[i - 1].Position, node.Position));
                if (layer.SpawnTable == null || !layer.SpawnTable.TryGetEntry(node.Definition, out var entry)) continue;
                report.ChainMobility += entry.MobilityValue;
                if (node.Harmful) report.HarmfulChainMobility += entry.MobilityValue;
            }
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
