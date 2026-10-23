using System.Collections.Generic;
using Balance;
using CharacterComponents;
using CharacterComponents.CharacterStat;
using CharacterComponents.Moving;
using Definitions;
using Generation;
using Improvements;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Utilities;

namespace BalanceTests
{
    public class BalanceInvariantsTests
    {
        [Test]
        public void SpawnTableIntegrity()
        {
            var s = BalanceTestContext.Scenario;
            Assert.NotNull(s, "Сценарий баланса не найден");
            foreach (var layerDef in s.LayerDefinitions)
            {
                Assert.NotNull(layerDef, "LayerDefinition == null в сценарии");
                Assert.NotNull(layerDef.SpawnTable, $"У {layerDef.name} нет SpawnTable");
                foreach (var e in layerDef.SpawnTable.Entries)
                {
                    Assert.NotNull(e.definition, "Entry без definition");
                    Assert.NotNull(e.definition.Prefab, $"У {e.definition.name} нет prefab");
                    Assert.NotNull(e.definition.Prefab.GetComponentInChildren<Stats>(true),
                        $"У префаба {e.definition.Prefab.name} нет Stats");
                    Assert.IsTrue(e.IsMigrated, $"{e.definition.name}: запись не мигрирована (threat/food/mobility == 0)");
                    Assert.IsTrue(e.ThreatValue > 0f || e.FoodValue > 0f || e.MobilityValue > 0f,
                        $"{e.definition.name}: нет ни одного ресурса > 0");
                    Assert.Greater(e.CostFor(e.kind), 0f, $"{e.definition.name}: ресурс пула {e.kind} <= 0");
                    if (e.kind == EnemySpawnTable.SpawnKind.Mobility)
                        Assert.AreEqual(0, e.maxPerLayer, $"{e.definition.name}: у опор не должно быть maxPerLayer");
                    Assert.Greater(e.weight, 0f, $"{e.definition.name}: weight <= 0");
                    Assert.LessOrEqual(e.unlockDifficulty, e.fullWeightDifficulty,
                        $"{e.definition.name}: unlock > fullWeight");
                    Assert.IsTrue(e.retireDifficulty == 0f || e.retireDifficulty > e.fullWeightDifficulty,
                        $"{e.definition.name}: retireDifficulty между 0 и fullWeight");
                }
            }
        }

        [Test]
        public void DifficultyMonotonicAndBounded()
        {
            var profile = BalanceTestContext.Scenario.DifficultyProfile;
            Assert.NotNull(profile);
            float prev = profile.EvaluateDifficulty(0);
            for (int i = 1; i < profile.LayerCount; i++)
            {
                float d = profile.EvaluateDifficulty(i);
                Assert.GreaterOrEqual(d, prev - 1e-6f, $"Сложность упала на слое {i}");
                Assert.LessOrEqual(d - prev, profile.MaxDifficultyDeltaPerLayer + 1e-6f,
                    $"Прирост сложности выше лимита на слое {i}");
                prev = d;
            }
        }

        [Test]
        public void BudgetNeverExceeded()
        {
            foreach (var r in BalanceTestContext.Layers)
            {
                if (r.Empty) continue;
                Assert.LessOrEqual(r.SpentThreat, r.ThreatBudget + 1e-3f,
                    $"Слой {r.Index}: spentThreat {r.SpentThreat} > threatBudget {r.ThreatBudget}");
                Assert.LessOrEqual(r.SpentFood, r.FoodBudget + 1e-3f,
                    $"Слой {r.Index}: spentFood {r.SpentFood} > foodBudget {r.FoodBudget}");
                Assert.LessOrEqual(r.SpentMobility, r.MobilityBudget + 1e-3f,
                    $"Слой {r.Index}: spentMobility {r.SpentMobility} > mobilityBudget {r.MobilityBudget}");
            }
        }

        [Test]
        public void ClimbabilityGuaranteed()
        {
            var bad = new List<string>();
            foreach (var r in BalanceTestContext.Layers)
            {
                if (r.Empty) continue;
                foreach (var b in BalanceTestContext.Builds)
                    if (!r.Metrics[b.Name].Climbable)
                        bad.Add($"слой {r.Index}/{b.Name} (MaxGap {r.Metrics[b.Name].MaxGap:0.#}, MaxVerticalGap {b.MaxVerticalGap:0.#})");
            }
            Assert.IsEmpty(bad, "Нет цепочки опор с зазором <= MaxVerticalGap: " + string.Join(", ", bad));
        }

        [Test]
        public void ChainRespectsStepFraction()
        {
            var s = BalanceTestContext.Scenario;
            float cap = s.DifficultyProfile.StepCap;
            foreach (var r in BalanceTestContext.Layers)
            {
                if (r.Empty) continue;
                Assert.GreaterOrEqual(r.ChainLength, r.MinSupports, $"Слой {r.Index}: опор в цепочке меньше K");
                Assert.LessOrEqual(r.ChainMaxStep, cap + 1e-3f, $"Слой {r.Index}: шаг цепочки {r.ChainMaxStep} > {cap}");
            }
        }

        [Test]
        public void LayerAdjacency()
        {
            var s = BalanceTestContext.Scenario;
            float gap = s.WeakestMaxVerticalGap();
            var layers = BalanceTestContext.Layers;
            for (int i = 0; i < layers.Count; i++)
            {
                var r = layers[i];
                if (r.Empty || r.ChainNodes.Count == 0) continue;
                Assert.GreaterOrEqual(r.ChainNodes[0].y - r.YMin, s.Placement.SafeZoneHeight - 1e-3f,
                    $"Слой {i}: первая опора ниже безопасной зоны");
                if (i + 1 >= layers.Count || layers[i + 1].Empty || layers[i + 1].ChainNodes.Count == 0) continue;
                float link = Vector2.Distance(r.ChainNodes[r.ChainNodes.Count - 1], layers[i + 1].ChainNodes[0]);
                Assert.LessOrEqual(link, gap, $"Слой {i}->{i + 1}: разрыв стыка {link:0.#} > MaxVerticalGap {gap:0.#}");
            }
        }

        [Test]
        public void ReferenceGapMatchesPlayer()
        {
            var s = BalanceTestContext.Scenario;
            var profile = s.DifficultyProfile;
            float weakest = s.WeakestMaxVerticalGap();
            Assert.LessOrEqual(profile.ReferenceMaxGap, weakest + 1e-4f, "referenceMaxGap больше досягаемости слабейшей сборки");
            Assert.GreaterOrEqual(profile.ReferenceMaxGap, weakest - 0.5f, "referenceMaxGap устарел: обновите по MaxVerticalGap");
            Assert.GreaterOrEqual(profile.ChainEntryOffset, s.Placement.SafeZoneHeight);
            Assert.GreaterOrEqual(profile.ChainExitOffset, s.Placement.EdgeMargin);
            Assert.GreaterOrEqual(profile.ChainEdgeReserve, s.Placement.EdgeMargin);
            foreach (var b in BalanceTestContext.Builds)
            {
                Assert.Greater(b.ReachDash, 0f);
                Assert.Greater(b.ReachHarpoon, 0f);
                Assert.AreEqual(Mathf.Max(b.ReachDash, b.ReachHarpoon), b.MaxVerticalGap, 1e-5f);
            }
        }

        [Test]
        public void MobilityAlwaysAvailable()
        {
            foreach (var layerDef in BalanceTestContext.Scenario.LayerDefinitions)
            {
                bool ok = false;
                foreach (var e in layerDef.SpawnTable.Entries)
                    if (e.kind == EnemySpawnTable.SpawnKind.Mobility && e.unlockDifficulty <= 0f && e.retireDifficulty <= 0f)
                        ok = true;
                Assert.IsTrue(ok, $"{layerDef.name}: нет опоры, доступной при любой сложности");
            }
        }

        [Test]
        public void ResourceValuesMatchProfile()
        {
            var s = BalanceTestContext.Scenario;
            var reference = BalanceTestContext.Builds[0];
            foreach (var layerDef in s.LayerDefinitions)
                foreach (var e in layerDef.SpawnTable.Entries)
                {
                    var d = EntityResourceModel.Derive(EnemyProfile.From(e.definition), e.kind, reference, s);
                    string n = e.definition.name;
                    Assert.AreEqual(d.Threat * EnemySpawnTable.Entry.Scale(e.threatScale), e.ThreatValue, 1e-3f, $"{n}: threat != profile × scale");
                    Assert.AreEqual(d.Food * EnemySpawnTable.Entry.Scale(e.foodScale), e.FoodValue, 1e-3f, $"{n}: food != profile × scale");
                    Assert.AreEqual(d.Mobility * EnemySpawnTable.Entry.Scale(e.mobilityScale), e.MobilityValue, 1e-3f, $"{n}: mobility != profile × scale");
                    Assert.AreEqual(d.Kind, e.mobilityKind, $"{n}: mobilityKind");
                }
        }

        [Test]
        public void Deterministic()
        {
            var s = BalanceTestContext.Scenario;
            var first = LayerSimulator.SimulateLayers(s, s.SimulatedLayers);
            var second = LayerSimulator.SimulateLayers(s, s.SimulatedLayers);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Spawns.Count, second[i].Spawns.Count,
                    $"Слой {i}: разное число спавнов");
                for (int j = 0; j < first[i].Spawns.Count; j++)
                {
                    Assert.AreEqual(first[i].Spawns[j].Name, second[i].Spawns[j].Name,
                        $"Слой {i}, спавн {j}: имя отличается");
                    Assert.AreEqual(first[i].Spawns[j].Position, second[i].Spawns[j].Position,
                        $"Слой {i}, спавн {j}: позиция отличается");
                }
            }

            var copy = Object.Instantiate(s);
            var so = new SerializedObject(copy);
            so.FindProperty("seed").intValue = s.Seed + 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            var other = LayerSimulator.SimulateLayers(copy, s.SimulatedLayers);
            bool differs = false;
            for (int i = 0; i < first.Count && !differs; i++)
            {
                if (first[i].Spawns.Count != other[i].Spawns.Count) { differs = true; break; }
                for (int j = 0; j < first[i].Spawns.Count; j++)
                    if (first[i].Spawns[j].Name != other[i].Spawns[j].Name ||
                        first[i].Spawns[j].Position != other[i].Spawns[j].Position)
                    {
                        differs = true;
                        break;
                    }
            }
            Object.DestroyImmediate(copy);
            Assert.IsTrue(differs, "Сид+1 дал идентичную генерацию");
        }

        [Test]
        public void PacingPriority()
        {
            var profile = BalanceTestContext.Scenario.DifficultyProfile;
            for (int i = 0; i < 60; i++)
            {
                if (i % 4 != 3 || i % 10 != 9) continue;
                Assert.IsTrue(profile.TryGetBeat(i, out var beat), $"Слой {i}: бит не найден");
                Assert.AreEqual(10, beat.every, $"Слой {i}: ожидался бит every=10, получен {beat.every}");
            }
        }

        [Test]
        public void ImprovementsIntegrity()
        {
            var guids = AssetDatabase.FindAssets("t:ImprovementDefinition");
            Assert.IsNotEmpty(guids, "ImprovementDefinition не найдены");
            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<ImprovementDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                Assert.NotNull(def);
                Assert.NotNull(def.Prefab, $"{def.name}: нет Prefab");
                Assert.NotNull(def.Prefab.GetComponentInChildren<Improvement>(true),
                    $"{def.name}: на префабе нет компонента Improvement");
                Assert.Contains(def.ImprovementName, ImprovementsConst.AllImps,
                    $"{def.name}: неизвестное ImprovementName '{def.ImprovementName}'");
                Assert.GreaterOrEqual(def.MaxLevel, 1, $"{def.name}: MaxLevel < 1");
                if (def.IsPassive)
                    Assert.AreEqual(1, def.MaxLevel, $"{def.name}: пассивное, но MaxLevel != 1");
            }
        }

        [Test]
        public void PlayerBuildDerivation()
        {
            var s = BalanceTestContext.Scenario;
            Assert.NotNull(s.PlayerPrefab, "В сценарии нет playerPrefab");

            var movable = s.PlayerPrefab.GetComponentInChildren<LumpMeatMovable>(true);
            var shooter = s.PlayerPrefab.GetComponentInChildren<Shooter>(true);
            Assert.NotNull(movable, "На префабе игрока нет LumpMeatMovable");
            Assert.NotNull(shooter, "На префабе игрока нет Shooter");

            var builds = s.Builds();
            PlayerBuild b = builds[0], passive = builds[1], full = builds[2];
            Assert.AreEqual("Base", b.Name);
            Assert.AreEqual("Passive", passive.Name);
            Assert.AreEqual("Full", full.Name);

            Assert.AreEqual(movable.DashDmg, b.DashDmg);
            Assert.AreEqual(movable.DashDelay, b.DashDelay, 1e-6f);
            Assert.AreEqual(b.DashDmg / b.DashDelay, b.DashDps, 1e-6f);

            var arrow = FindImprovement(ImprovementsConst.ArrowUp);
            var dash = FindImprovement(ImprovementsConst.DashUp);
            var jaw = FindImprovement(ImprovementsConst.JawUp);
            Assert.NotNull(arrow, "Нет улучшения ArrowUp");
            Assert.NotNull(dash, "Нет улучшения DashUp");
            Assert.NotNull(jaw, "Нет улучшения JawUp");

            var arrowComp = arrow.Prefab.GetComponentInChildren<ArrowUpImprovement>(true);
            var dashComp = dash.Prefab.GetComponentInChildren<DashUpImprovement>(true);
            var jawComp = jaw.Prefab.GetComponentInChildren<JawUpImprovement>(true);
            Assert.NotNull(arrowComp);
            Assert.NotNull(dashComp);
            Assert.NotNull(jawComp);

            Assert.AreEqual(arrowComp.ShotSpeed, passive.ShootDelay, 1e-6f);
            Assert.AreEqual(dashComp.DashSpeed, passive.DashDelay, 1e-6f);
            Assert.AreEqual(jawComp.Dmg, passive.DashDmg);
            Assert.AreEqual(b.HarpoonDmg, passive.HarpoonDmg);
            Assert.AreEqual(0, passive.SpikeCount);
            Assert.AreEqual(0, passive.ShieldCount);

            Assert.AreEqual(5, full.SpikeCount);
            Assert.AreEqual(5, full.ShieldCount);
        }

        [Test]
        public void HunterEnemyProfiles()
        {
            var chaser = ProfileOf("Chaser");
            Assert.IsTrue(chaser.IsChaser);
            Assert.AreEqual(2, chaser.Hp);
            Assert.AreEqual(1, chaser.ContactDmg);
            Assert.IsFalse(chaser.IsRanged);
            Assert.IsFalse(chaser.IsHazard);
            Assert.Greater(chaser.ChaseRadius, 0f);
            Assert.IsTrue(chaser.IsEdible);
            Assert.IsTrue(chaser.EatableByDash(1), "Chaser должен съедаться рывком без улучшений");

            var movable = BalanceTestContext.Scenario.PlayerPrefab.GetComponentInChildren<LumpMeatMovable>(true);
            var playerBody = BalanceTestContext.Scenario.PlayerPrefab.GetComponentInChildren<Rigidbody2D>(true);
            float dashSpeed = movable.PowerDash / playerBody.mass;
            Assert.Less(chaser.MoveSpeed, dashSpeed, "От Chaser можно убежать рывком");

            var charger = ProfileOf("Charger");
            Assert.IsTrue(charger.IsCharger);
            Assert.AreEqual(0.7f, charger.ChargeTelegraph, 1e-6f);
            Assert.AreEqual(3f, charger.ChargeCooldown, 1e-6f);
            Assert.AreEqual(2, charger.ContactDmg);
            Assert.IsFalse(charger.IsRanged);

            var spitter = ProfileOf("Spitter");
            Assert.IsTrue(spitter.IsRanged);
            Assert.IsTrue(spitter.IsHoming);
            Assert.AreEqual(1, spitter.ProjectileDmg);
            Assert.Less(spitter.ProjectileSpeed, ProfileOf("Pear").ProjectileSpeed, "Снаряд Spitter медленнее Pear");
            Assert.IsFalse(ProfileOf("Pear").IsHoming);
        }

        [Test]
        public void BehaviorEnemyProfiles()
        {
            var mine = ProfileOf("Mine");
            Assert.IsTrue(mine.IsMine);
            Assert.AreEqual(2, mine.ContactDmg);
            Assert.Greater(mine.MineFuse, 0f);
            Assert.IsTrue(mine.KillableByDamage);
            Assert.IsFalse(mine.IsEdible);
            Assert.IsFalse(mine.IsHazard);

            var leaper = ProfileOf("Leaper");
            Assert.IsTrue(leaper.IsLeaper);
            Assert.AreEqual(1, leaper.ContactDmg);
            Assert.Greater(leaper.HopRange, 0f);

            var runner = ProfileOf("Runner");
            Assert.IsTrue(runner.IsFleer);
            Assert.IsFalse(runner.IsThreat);
            Assert.AreEqual(2, runner.HealOnEat);
            Assert.IsTrue(runner.EatableByDash(1));

            var table = BalanceTestContext.Scenario.LayerDefinitions[0].SpawnTable;
            Assert.IsTrue(table.TryGetEntry(DefinitionOf("Mine"), out var e));
            Assert.AreEqual(EnemySpawnTable.EnemyRole.Ambusher, e.role);
            Assert.IsTrue(table.TryGetEntry(DefinitionOf("Leaper"), out e));
            Assert.AreEqual(EnemySpawnTable.EnemyRole.Melee, e.role);
            Assert.IsTrue(table.TryGetEntry(DefinitionOf("Runner"), out e));
            Assert.AreEqual(EnemySpawnTable.SpawnKind.Food, e.kind);
            Assert.AreEqual(EnemySpawnTable.EnemyRole.Swarm, e.role);
        }

        [Test]
        public void HunterEnemiesInSpawnTable()
        {
            var table = BalanceTestContext.Scenario.LayerDefinitions[0].SpawnTable;
            var expected = new Dictionary<string, EnemySpawnTable.EnemyRole>
            {
                { "Chaser", EnemySpawnTable.EnemyRole.Hunter },
                { "Charger", EnemySpawnTable.EnemyRole.Ambusher },
                { "Spitter", EnemySpawnTable.EnemyRole.Ranged }
            };
            foreach (var kv in expected)
            {
                Assert.IsTrue(table.TryGetEntry(DefinitionOf(kv.Key), out var entry), $"{kv.Key} нет в SpawnTable");
                Assert.AreEqual(EnemySpawnTable.SpawnKind.Threat, entry.kind);
                Assert.AreEqual(kv.Value, entry.role);
                Assert.Greater(entry.maxPerLayer, 0);
            }
        }

        [Test]
        public void ExpectedDamageUsesChaseProbability()
        {
            var s = BalanceTestContext.Scenario;
            var b = BalanceTestContext.Builds[0];
            var chaser = new EnemyProfile { ContactDmg = 1, IsChaser = true };
            var trap = new EnemyProfile { ContactDmg = 1 };
            Assert.AreEqual(s.PChase, BalanceModel.ExpectedDamage(new[] { chaser }, b, s), 1e-6f);
            Assert.AreEqual(s.PContact, BalanceModel.ExpectedDamage(new[] { trap }, b, s), 1e-6f);

            var plain = new EnemyProfile { IsRanged = true, ShootDelay = 4f, ProjectileDmg = 1 };
            var homing = new EnemyProfile { IsRanged = true, ShootDelay = 4f, ProjectileDmg = 1, IsHoming = true };
            Assert.AreEqual(s.ExposureRanged / 4f * s.HitProbRanged, BalanceModel.ExpectedDamage(new[] { plain }, b, s), 1e-6f);
            Assert.AreEqual(s.ExposureRanged / 4f * s.HitProbHoming, BalanceModel.ExpectedDamage(new[] { homing }, b, s), 1e-6f);
        }

        private static CharacterDefinition DefinitionOf(string name)
        {
            var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"Assets/Infos/Characters/{name}.asset");
            Assert.NotNull(def, $"Нет CharacterDefinition {name}");
            return def;
        }

        private static EnemyProfile ProfileOf(string name) => EnemyProfile.From(DefinitionOf(name));

        private static ImprovementDefinition FindImprovement(string improvementName)
        {
            foreach (var imp in BalanceTestContext.Scenario.Improvements)
                if (imp != null && imp.ImprovementName == improvementName)
                    return imp;
            return null;
        }
    }
}
