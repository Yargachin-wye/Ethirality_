using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Generation;
using UnityEditor;
using UnityEngine;

namespace Balance.Editor
{
    public static class BalanceReportGenerator
    {
        private const string ScenarioPath = "Assets/Infos/Balance/OpenWorld.asset";
        private const string ReportPath = "Assets/Docs/Balance/BalanceReport.md";
        private const string CsvPath = "Assets/Docs/Balance/layers.csv";

        [MenuItem("Tools/Balance/Generate Report")]
        public static void Generate()
        {
            var s = AssetDatabase.LoadAssetAtPath<BalanceScenario>(ScenarioPath);
            if (s == null)
            {
                Debug.LogError($"[BalanceReportGenerator] Сценарий не найден: {ScenarioPath}");
                return;
            }

            var layers = LayerSimulator.SimulateLayers(s, s.SimulatedLayers);
            var builds = s.Builds();
            var runs = new List<RunReport>();
            foreach (var b in builds)
                runs.Add(LayerSimulator.SimulateRun(s, b, layers));

            var entries = new List<(EnemySpawnTable.Entry entry, EnemyProfile profile)>();
            var seen = new HashSet<Definitions.CharacterDefinition>();
            foreach (var def in s.LayerDefinitions)
            {
                if (def?.SpawnTable?.Entries == null) continue;
                foreach (var e in def.SpawnTable.Entries)
                {
                    if (e.definition == null || seen.Contains(e.definition)) continue;
                    seen.Add(e.definition);
                    entries.Add((e, EnemyProfile.From(e.definition)));
                }
            }

            var sb = new StringBuilder();
            WriteScenario(sb, s);
            WriteEntities(sb, entries);
            WriteBuilds(sb, s, builds);
            WriteLayers(sb, s, builds, layers);
            WriteRuns(sb, s, builds, runs);
            WriteFindings(sb, s, builds, layers, runs, entries);

            if (!Directory.Exists("Assets/Docs/Balance"))
                Directory.CreateDirectory("Assets/Docs/Balance");
            File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
            File.WriteAllText(CsvPath, BuildCsv(s, builds, layers), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceReportGenerator] Отчёт записан: {ReportPath}, {CsvPath}");
        }

        private static void WriteScenario(StringBuilder sb, BalanceScenario s)
        {
            sb.AppendLine("# Отчёт по балансу");
            sb.AppendLine();
            sb.AppendLine("## Сценарий");
            sb.AppendLine();
            sb.AppendLine($"- Seed: {s.Seed}");
            sb.AppendLine($"- Слоёв симулировано: {s.SimulatedLayers}");
            sb.AppendLine($"- Попыток размещения на врага: {s.AttemptsPerEnemy}");
            sb.AppendLine($"- Награды (вне бюджета): {s.Reward.RewardDefinition?.name}, firstRewardLayer = {s.Reward.FirstRewardLayer}, " +
                          $"rewardEveryLayers = {s.Reward.RewardEveryLayers}, rewardsPerLayer = [{s.Reward.MinRewards}, {s.Reward.MaxRewards}]");
            sb.AppendLine("- Константы модели:");
            sb.AppendLine($"  - exposureRanged = {Fmt(s.ExposureRanged)}");
            sb.AppendLine($"  - hitProbRanged = {Fmt(s.HitProbRanged)}");
            sb.AppendLine($"  - hitProbHoming = {Fmt(s.HitProbHoming)}");
            sb.AppendLine($"  - pContact = {Fmt(s.PContact)}");
            sb.AppendLine($"  - pChase = {Fmt(s.PChase)}");
            sb.AppendLine($"  - eatShare = {Fmt(s.EatShare)}");
            sb.AppendLine($"  - pPickHp = {Fmt(s.PPickHp)}");
            sb.AppendLine($"  - spikeDps = {Fmt(s.SpikeDps)}");
            sb.AppendLine($"  - shieldBlock = {Fmt(s.ShieldBlock)}");
            sb.AppendLine("- Цели баланса:");
            sb.AppendLine($"  - runLengthBase = [{s.RunLengthBaseMin}, {s.RunLengthBaseMax}]");
            sb.AppendLine($"  - runLengthFull >= {s.RunLengthFullMin}");
            sb.AppendLine($"  - maxPlacementLoss = {Fmt(s.MaxPlacementLoss)}");
            sb.AppendLine($"  - rewardWindow = {s.RewardWindow}, rewardCheckFromLayer = {s.RewardCheckFromLayer}");
            sb.AppendLine();
        }

        private static void WriteEntities(StringBuilder sb,
            List<(EnemySpawnTable.Entry entry, EnemyProfile profile)> entries)
        {
            sb.AppendLine("## Сущности");
            sb.AppendLine();
            sb.AppendLine("| Имя | Вид | Роль | threatCost | weight | unlock | full | retire | maxPerLayer | Hp | Damageable | DiesFromDamage | ContactDmg | Ranged delay | Ranged range | ProjDmg | HealOnEat | IsLevelUp | IsHazard | EatableByDash(1) | EatableByDash(2) |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var (e, p) in entries)
            {
                sb.AppendLine($"| {p.Name} | {e.kind} | {e.role} | {Fmt(e.threatCost)} | {Fmt(e.weight)} " +
                              $"| {Fmt(e.unlockDifficulty)} | {Fmt(e.fullWeightDifficulty)} | {Fmt(e.retireDifficulty)} " +
                              $"| {e.maxPerLayer} | {p.Hp} | {YN(p.Damageable)} | {YN(p.DiesFromDamage)} " +
                              $"| {p.ContactDmg} | {(p.IsRanged ? Fmt(p.ShootDelay) : "-")} " +
                              $"| {(p.IsRanged ? Fmt(p.DetectionRange) : "-")} | {(p.IsRanged ? p.ProjectileDmg.ToString() : "-")} " +
                              $"| {p.HealOnEat} | {YN(p.IsLevelUp)} | {YN(p.IsHazard)} " +
                              $"| {YN(p.EatableByDash(1))} | {YN(p.EatableByDash(2))} |");
            }
            sb.AppendLine();
        }

        private static void WriteBuilds(StringBuilder sb, BalanceScenario s, IReadOnlyList<PlayerBuild> builds)
        {
            sb.AppendLine("## Сборки игрока");
            sb.AppendLine();
            sb.AppendLine("| Сборка | DashDmg | DashDelay | HarpoonDmg | ShootDelay | Spike | Shield | DashDps | HarpoonDps | Dps | RangedMitigation |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var b in builds)
            {
                sb.AppendLine($"| {b.Name} | {b.DashDmg} | {Fmt(b.DashDelay)} | {b.HarpoonDmg} | {Fmt(b.ShootDelay)} " +
                              $"| {b.SpikeCount} | {b.ShieldCount} | {Fmt(b.DashDps)} | {Fmt(b.HarpoonDps)} " +
                              $"| {Fmt(b.Dps(s))} | {Fmt(b.RangedMitigation(s))} |");
            }
            sb.AppendLine();
        }

        private static void WriteLayers(StringBuilder sb, BalanceScenario s, IReadOnlyList<PlayerBuild> builds,
            List<LayerReport> layers)
        {
            sb.AppendLine("## Слои");
            sb.AppendLine();
            var header = new StringBuilder("| i | d | pacing | threatBudget | foodBudget | rolled | placed | loss% | threatSpent | foodSpent | rewards | hazards | killableHp | состав");
            foreach (var b in builds) header.Append($" | {b.Name} TTC | {b.Name} dmg | {b.Name} heal");
            header.Append(" |");
            sb.AppendLine(header.ToString());
            var sep = new StringBuilder("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var _ in builds) sep.Append("|---|---|---");
            sep.Append("|");
            sb.AppendLine(sep.ToString());

            foreach (var r in layers)
            {
                if (r.Empty)
                {
                    sb.AppendLine($"| {r.Index} | - | - | - | - | 0 | 0 | - | - | - | - | - | - | пустой |");
                    continue;
                }
                var comp = new StringBuilder();
                foreach (var kv in r.CountsByName)
                {
                    if (comp.Length > 0) comp.Append(", ");
                    comp.Append($"{kv.Key}×{kv.Value}");
                }
                var row = new StringBuilder($"| {r.Index} | {Fmt(r.Difficulty)} | {Fmt(r.PacingMultiplier)} " +
                                            $"| {Fmt(r.Budget)} | {Fmt(r.FoodBudget)} | {r.Rolled} | {r.Placed} | {Fmt(r.PlacementLoss * 100f)} " +
                                            $"| {Fmt(r.Spent)} | {Fmt(r.FoodSpent)} | {r.Rewards} | {r.Hazards} | {Fmt(r.KillableHp)} | {comp}");
                foreach (var b in builds)
                {
                    var m = r.Metrics[b.Name];
                    row.Append($" | {Fmt(m.TimeToClear)} | {Fmt(m.ExpectedDamage)} | {Fmt(m.ExpectedHeal)}");
                }
                row.Append(" |");
                sb.AppendLine(row.ToString());
            }
            sb.AppendLine();
        }

        private static void WriteRuns(StringBuilder sb, BalanceScenario s, IReadOnlyList<PlayerBuild> builds,
            List<RunReport> runs)
        {
            sb.AppendLine("## Забег");
            sb.AppendLine();
            sb.AppendLine("| Сборка | RunLength | Died |");
            sb.AppendLine("|---|---|---|");
            foreach (var r in runs)
                sb.AppendLine($"| {r.BuildName} | {r.RunLength} | {YN(r.Died)} |");
            sb.AppendLine();
            sb.AppendLine("### HP-трасса (первые 20 слоёв)");
            sb.AppendLine();
            var head = new StringBuilder("| Слой");
            foreach (var r in runs) head.Append($" | {r.BuildName}");
            head.Append(" |");
            sb.AppendLine(head.ToString());
            var sep = new StringBuilder("|---");
            foreach (var _ in runs) sep.Append("|---");
            sep.Append("|");
            sb.AppendLine(sep.ToString());
            int n = Mathf.Min(20, runs.Count > 0 ? runs[0].HpTrace.Count : 0);
            for (int i = 0; i < n; i++)
            {
                var row = new StringBuilder($"| {i}");
                foreach (var r in runs)
                    row.Append($" | {(i < r.HpTrace.Count ? Fmt(r.HpTrace[i]) : "-")}");
                row.Append(" |");
                sb.AppendLine(row.ToString());
            }
            sb.AppendLine();
        }

        private static void WriteFindings(StringBuilder sb, BalanceScenario s, IReadOnlyList<PlayerBuild> builds,
            List<LayerReport> layers, List<RunReport> runs,
            List<(EnemySpawnTable.Entry entry, EnemyProfile profile)> entries)
        {
            sb.AppendLine("## Находки");
            sb.AppendLine();

            var lossy = new List<string>();
            foreach (var r in layers)
                if (!r.Empty && r.PlacementLoss > s.MaxPlacementLoss)
                    lossy.Add($"слой {r.Index} (loss {Fmt(r.PlacementLoss * 100f)}%)");
            sb.AppendLine("### Слои с потерями размещения выше цели");
            if (lossy.Count == 0) sb.AppendLine("- Нет.");
            else foreach (var l in lossy) sb.AppendLine($"- {l}");
            sb.AppendLine();

            var dry = new List<string>();
            for (int i = s.RewardCheckFromLayer; i + s.RewardWindow <= layers.Count; i++)
            {
                int sum = 0;
                for (int j = i; j < i + s.RewardWindow; j++) sum += layers[j].Rewards;
                if (sum == 0) dry.Add($"[{i}, {i + s.RewardWindow})");
            }
            sb.AppendLine("### Окна без наград");
            if (dry.Count == 0) sb.AppendLine("- Нет.");
            else foreach (var d in dry) sb.AppendLine($"- {d}");
            sb.AppendLine();

            sb.AppendLine("### Сборки вне целевого коридора");
            var bad = new List<string>();
            foreach (var r in runs)
            {
                if (r.BuildName == "Base" && (r.RunLength < s.RunLengthBaseMin || r.RunLength > s.RunLengthBaseMax))
                    bad.Add($"Base: RunLength {r.RunLength} вне [{s.RunLengthBaseMin}, {s.RunLengthBaseMax}]");
                if (r.BuildName == "Full" && r.RunLength < s.RunLengthFullMin)
                    bad.Add($"Full: RunLength {r.RunLength} < {s.RunLengthFullMin}");
            }
            if (bad.Count == 0) sb.AppendLine("- Нет.");
            else foreach (var l in bad) sb.AppendLine($"- {l}");
            sb.AppendLine();

            sb.AppendLine("### Несоответствия роли и профиля");
            var mismatches = RoleMismatches(entries);
            if (mismatches.Count == 0) sb.AppendLine("- Нет.");
            else foreach (var m in mismatches) sb.AppendLine($"- {m}");
            sb.AppendLine();
        }

        public static List<string> RoleMismatches(List<(EnemySpawnTable.Entry entry, EnemyProfile profile)> entries)
        {
            var list = new List<string>();
            foreach (var (e, p) in entries)
            {
                if ((e.role == EnemySpawnTable.EnemyRole.Ranged) != p.IsRanged)
                    list.Add($"{p.Name}: role={e.role}, IsRanged={p.IsRanged}");
                if ((e.role == EnemySpawnTable.EnemyRole.Reward) != p.IsLevelUp)
                    list.Add($"{p.Name}: role={e.role}, IsLevelUp={p.IsLevelUp}");
                if (e.kind == EnemySpawnTable.SpawnKind.Food && p.IsThreat)
                    list.Add($"{p.Name}: kind=Food, но наносит урон (IsThreat)");
                if (p.IsHazard && (e.role == EnemySpawnTable.EnemyRole.Tank ||
                                   e.role == EnemySpawnTable.EnemyRole.Swarm ||
                                   e.role == EnemySpawnTable.EnemyRole.Melee))
                    list.Add($"{p.Name}: опасен (IsHazard), но role={e.role}");
                if ((e.role == EnemySpawnTable.EnemyRole.Hunter) != p.IsChaser)
                    list.Add($"{p.Name}: role={e.role}, IsChaser={p.IsChaser}");
                if (p.IsCharger && e.role != EnemySpawnTable.EnemyRole.Ambusher)
                    list.Add($"{p.Name}: IsCharger, но role={e.role}");
                if (e.role == EnemySpawnTable.EnemyRole.Swarm && e.threatCost > 2f)
                    list.Add($"{p.Name}: role=Swarm, но threatCost={Fmt(e.threatCost)} > 2");
            }
            return list;
        }

        private static string BuildCsv(BalanceScenario s, IReadOnlyList<PlayerBuild> builds,
            List<LayerReport> layers)
        {
            var sb = new StringBuilder();
            var head = new StringBuilder("i;difficulty;pacing;threatBudget;foodBudget;rolled;placed;loss;threatSpent;foodSpent;rewards;hazards;killableHp;composition");
            foreach (var b in builds) head.Append($";{b.Name}_ttc;{b.Name}_dmg;{b.Name}_heal");
            sb.AppendLine(head.ToString());
            foreach (var r in layers)
            {
                var comp = new StringBuilder();
                foreach (var kv in r.CountsByName)
                {
                    if (comp.Length > 0) comp.Append(", ");
                    comp.Append($"{kv.Key}x{kv.Value}");
                }
                var row = new StringBuilder($"{r.Index};{C(r.Difficulty)};{C(r.PacingMultiplier)};" +
                                            $"{C(r.Budget)};{C(r.FoodBudget)};{r.Rolled};{r.Placed};{C(r.PlacementLoss)};" +
                                            $"{C(r.Spent)};{C(r.FoodSpent)};{r.Rewards};{r.Hazards};{C(r.KillableHp)};{comp}");
                foreach (var b in builds)
                {
                    if (r.Empty || !r.Metrics.TryGetValue(b.Name, out var m))
                        row.Append(";;;");
                    else
                        row.Append($";{C(m.TimeToClear)};{C(m.ExpectedDamage)};{C(m.ExpectedHeal)}");
                }
                sb.AppendLine(row.ToString());
            }
            return sb.ToString();
        }

        private static string Fmt(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        private static string C(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);
        private static string YN(bool v) => v ? "да" : "нет";
    }
}
