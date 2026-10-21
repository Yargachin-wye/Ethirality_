using System.Collections.Generic;
using System.Text;
using Balance;
using Balance.Editor;
using Generation;
using NUnit.Framework;

namespace BalanceTests
{
    [Category("Balance.Target")]
    public class BalanceTargetsTests
    {
        [Test]
        public void PlacementLossWithinTarget()
        {
            var s = BalanceTestContext.Scenario;
            var bad = new List<string>();
            int limit = System.Math.Min(40, BalanceTestContext.Layers.Count - 1);
            for (int i = 0; i <= limit; i++)
            {
                var r = BalanceTestContext.Layers[i];
                if (!r.Empty && r.PlacementLoss > s.MaxPlacementLoss)
                    bad.Add($"слой {r.Index}: loss {r.PlacementLoss:0.###}");
            }
            if (bad.Count > 0)
                BalanceTestContext.Warn(
                    $"Потери размещения выше цели {s.MaxPlacementLoss}: {string.Join(", ", bad)}");
        }

        [Test]
        public void RewardCadence()
        {
            var s = BalanceTestContext.Scenario;
            var layers = BalanceTestContext.Layers;
            var dry = new List<string>();
            for (int i = s.RewardCheckFromLayer; i + s.RewardWindow <= layers.Count; i++)
            {
                int sum = 0;
                for (int j = i; j < i + s.RewardWindow; j++) sum += layers[j].Rewards;
                if (sum == 0) dry.Add($"[{i}, {i + s.RewardWindow})");
            }
            if (dry.Count > 0)
                BalanceTestContext.Warn($"Окна без наград: {string.Join(", ", dry)}");
        }

        [Test]
        public void RunLengthTargets()
        {
            var s = BalanceTestContext.Scenario;
            var runs = new List<RunReport>();
            foreach (var b in BalanceTestContext.Builds)
                runs.Add(LayerSimulator.SimulateRun(s, b, BalanceTestContext.Layers));

            var actual = new StringBuilder();
            foreach (var r in runs)
                actual.Append($"{r.BuildName}={r.RunLength}({(r.Died ? "died" : "alive")}) ");

            var warnings = new List<string>();
            foreach (var r in runs)
            {
                if (r.BuildName == "Base" &&
                    (r.RunLength < s.RunLengthBaseMin || r.RunLength > s.RunLengthBaseMax))
                    warnings.Add($"Base.RunLength {r.RunLength} вне " +
                                 $"[{s.RunLengthBaseMin}, {s.RunLengthBaseMax}]. Факт: {actual}");
                if (r.BuildName == "Full" && r.RunLength < s.RunLengthFullMin)
                    warnings.Add($"Full.RunLength {r.RunLength} < {s.RunLengthFullMin}. Факт: {actual}");
            }
            if (warnings.Count > 0)
                BalanceTestContext.Warn(string.Join("\n", warnings));
        }

        [Test]
        public void RoleConsistency()
        {
            var s = BalanceTestContext.Scenario;
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

            var mismatches = BalanceReportGenerator.RoleMismatches(entries);
            if (mismatches.Count > 0)
                BalanceTestContext.Warn(
                    $"Несоответствия роли и профиля: {string.Join("; ", mismatches)}");
        }

        [Test]
        public void FirstRewardLayer()
        {
            int first = -1;
            foreach (var r in BalanceTestContext.Layers)
            {
                if (r.Rewards > 0) { first = r.Index; break; }
            }
            if (first < 0)
                BalanceTestContext.Warn("Ни один слой не содержит наград");
            else if (first > 3)
                BalanceTestContext.Warn($"Первая награда на слое {first} (> 3)");
        }
    }
}
