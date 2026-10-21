using System;
using System.Collections.Generic;
using CharacterComponents;
using CharacterComponents.CharacterStat;
using CharacterComponents.Moving;
using Definitions;
using Improvements;
using UnityEngine;

namespace Balance
{
    [Serializable]
    public class PlayerBuild
    {
        public string Name;
        public int DashDmg;
        public float DashDelay;
        public int HarpoonDmg;
        public float ShootDelay;
        public int SpikeCount;
        public int ShieldCount;
        public int MaxHp;
        public int StartHp;

        public float DashDps => DashDmg / DashDelay;
        public float HarpoonDps => HarpoonDmg / ShootDelay;
        public float Dps(BalanceScenario s) => DashDps + HarpoonDps + SpikeCount * s.SpikeDps;
        public float RangedMitigation(BalanceScenario s) => Mathf.Pow(1f - s.ShieldBlock, ShieldCount);

        public static PlayerBuild From(string name, GameObject playerPrefab,
            IEnumerable<ImprovementDefinition> improvements)
        {
            var build = new PlayerBuild { Name = name };

            if (playerPrefab != null)
            {
                var movable = playerPrefab.GetComponentInChildren<LumpMeatMovable>(true);
                if (movable != null)
                {
                    build.DashDmg = movable.DashDmg;
                    build.DashDelay = movable.DashDelay;
                }

                var shooter = playerPrefab.GetComponentInChildren<Shooter>(true);
                if (shooter != null)
                {
                    build.ShootDelay = shooter.ShootDelay;
                    if (shooter.ProjectileDefinition != null)
                        build.HarpoonDmg = shooter.ProjectileDefinition.Damage;
                }

                var stats = playerPrefab.GetComponentInChildren<Stats>(true);
                if (stats != null)
                {
                    build.MaxHp = stats.MaxHealth;
                    build.StartHp = stats.StartHp;
                }
            }

            if (improvements != null)
            {
                foreach (var improvement in improvements)
                {
                    GameObject prefab = improvement != null ? improvement.Prefab : null;
                    if (prefab == null) continue;

                    var arrow = prefab.GetComponentInChildren<ArrowUpImprovement>(true);
                    if (arrow != null) build.ShootDelay = arrow.ShotSpeed;

                    var dash = prefab.GetComponentInChildren<DashUpImprovement>(true);
                    if (dash != null) build.DashDelay = dash.DashSpeed;

                    var jaw = prefab.GetComponentInChildren<JawUpImprovement>(true);
                    if (jaw != null) build.DashDmg = jaw.Dmg;

                    if (prefab.GetComponentInChildren<TouchDamage>(true) != null) build.SpikeCount++;
                    if (prefab.GetComponentInChildren<Shield>(true) != null) build.ShieldCount++;
                }
            }

            return build;
        }
    }
}
