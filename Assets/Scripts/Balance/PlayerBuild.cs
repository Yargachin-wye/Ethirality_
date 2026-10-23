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

        public float Mass = 1f;
        public float LinearDrag;
        public float GravityScale = 1f;
        public float PowerDash;
        public float DashTime;
        public float HarpoonPull;
        public float HarpoonAttachDelay;
        public float HarpoonRange;
        public float HarpoonProjectileSpeed;

        public float DashSpeed => Mass > 0f ? PowerDash / Mass : 0f;
        public float PullSpeed => Mass > 0f ? HarpoonPull / Mass : 0f;
        public float ReachDash => ApexHeight(DashSpeed);
        public float ReachHarpoon => Mathf.Min(HarpoonRange, ApexHeight(PullSpeed));
        public float MaxVerticalGap => Mathf.Max(ReachHarpoon, ReachDash);

        public float DashDps => DashDmg / DashDelay;
        public float HarpoonDps => HarpoonDmg / ShootDelay;
        public float Dps(BalanceScenario s) => DashDps + HarpoonDps + SpikeCount * s.SpikeDps;
        public float RangedMitigation(BalanceScenario s) => Mathf.Pow(1f - s.ShieldBlock, ShieldCount);

        public float ApexHeight(float launchSpeed) => Flight(launchSpeed, float.PositiveInfinity, out _);

        public float TravelTime(float launchSpeed, float distance)
        {
            Flight(launchSpeed, distance, out float time);
            return time;
        }

        private float Flight(float launchSpeed, float distance, out float time)
        {
            time = float.PositiveInfinity;
            if (launchSpeed <= 0f) return 0f;

            float dt = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
            float gravity = Physics2D.gravity.y * GravityScale;
            float damping = 1f / (1f + dt * LinearDrag);
            float y = 0f, v = launchSpeed, t = 0f;
            while (v > 0f)
            {
                v = (v + gravity * dt) * damping;
                y += Mathf.Max(v, 0f) * dt;
                t += dt;
                if (y >= distance)
                {
                    time = t;
                    return y;
                }
            }

            return y;
        }

        public float HarpoonTime(float distance)
        {
            if (distance > ReachHarpoon || HarpoonProjectileSpeed <= 0f) return float.PositiveInfinity;
            return ShootDelay + distance / HarpoonProjectileSpeed + HarpoonAttachDelay + TravelTime(PullSpeed, distance);
        }

        public float DashClimbTime(float distance)
        {
            if (distance > ReachDash) return float.PositiveInfinity;
            return DashDelay + TravelTime(DashSpeed, distance);
        }

        public float EdgeTime(float distance) => Mathf.Min(HarpoonTime(distance), DashClimbTime(distance));

        public float FreeClimbTime(float height) =>
            ReachDash > 0f ? Mathf.Ceil(height / ReachDash) * DashDelay + height / Mathf.Max(DashSpeed, 0.01f) : float.PositiveInfinity;

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
                    build.PowerDash = movable.PowerDash;
                    build.DashTime = movable.DashTime;
                }

                var body = playerPrefab.GetComponentInChildren<Rigidbody2D>(true);
                if (body != null)
                {
                    build.Mass = body.mass;
                    build.LinearDrag = body.drag;
                    build.GravityScale = body.gravityScale;
                }

                var shooter = playerPrefab.GetComponentInChildren<Shooter>(true);
                if (shooter != null)
                {
                    build.ShootDelay = shooter.ShootDelay;
                    build.HarpoonRange = shooter.MaxDistanceToProjectile;
                    var projectile = shooter.ProjectileDefinition;
                    if (projectile != null)
                    {
                        build.HarpoonDmg = projectile.Damage;
                        build.HarpoonPull = Mathf.Abs(projectile.OwnerForce);
                        build.HarpoonAttachDelay = projectile.AttachedForceDelay;
                        var projectileBody = projectile.Prefab != null ? projectile.Prefab.GetComponentInChildren<Rigidbody2D>(true) : null;
                        if (projectileBody != null && projectileBody.mass > 0f)
                            build.HarpoonProjectileSpeed = shooter.Speed * projectile.SpeedMultiply / projectileBody.mass;
                    }
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
