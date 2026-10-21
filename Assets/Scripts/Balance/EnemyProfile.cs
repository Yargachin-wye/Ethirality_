using System;
using CharacterComponents;
using CharacterComponents.CharacterStat;
using CharacterComponents.Food;
using CharacterComponents.Moving;
using Definitions;
using UnityEngine;

namespace Balance
{
    [Serializable]
    public class EnemyProfile
    {
        public string Name;
        public CharacterDefinition Definition;
        public int Hp;
        public bool IsInvulnerable;
        public bool IsImmortal;
        public int ContactDmg;
        public bool ContactOneShot;
        public bool IsRanged;
        public float ShootDelay;
        public float DetectionRange;
        public int ProjectileDmg;
        public float ProjectileSpeed;
        public int HealOnEat;
        public int PoisonOnEat;
        public bool IsLevelUp;
        public bool IsExit;
        public bool IsEdible;
        public float MoveSpeed;
        public bool IsChaser;
        public float ChaseRadius;
        public bool IsCharger;
        public float ChargeTelegraph;
        public float ChargeCooldown;
        public bool IsHoming;

        public bool Damageable => !IsInvulnerable;
        public bool DiesFromDamage => !IsImmortal;
        public bool KillableByDamage => Damageable && DiesFromDamage;
        public bool IsHazard => !KillableByDamage && (ContactDmg > 0 || IsRanged);
        public bool IsThreat => ContactDmg > 0 || IsRanged;
        public int KillableHp => KillableByDamage ? Hp : 0;

        public bool EatableByDash(int dashDmg) => IsEdible && (Hp - dashDmg <= 1 || Damageable);

        public static EnemyProfile From(CharacterDefinition def)
        {
            var profile = new EnemyProfile { Name = def.name, Definition = def };
            GameObject prefab = def.Prefab;
            if (prefab == null) return profile;

            var stats = prefab.GetComponentInChildren<Stats>(true);
            if (stats != null)
            {
                profile.Hp = stats.StartHp;
                profile.IsInvulnerable = stats.IsInvulnerable;
                profile.IsImmortal = stats.IsImmortal;
            }

            var trigger = prefab.GetComponentInChildren<TriggerDamage>(true);
            if (trigger != null && trigger.TargetFraction != Fraction.Player)
            {
                profile.ContactDmg = trigger.Dmg;
                profile.ContactOneShot = trigger.IsDestroyOnTrigger;
            }

            var shooter = prefab.GetComponentInChildren<EnemyShooter>(true);
            if (shooter != null)
            {
                profile.IsRanged = true;
                profile.ShootDelay = shooter.ShootDelay;
                profile.DetectionRange = shooter.DetectionRange;
                profile.IsHoming = shooter.IsHoming;
                if (shooter.ProjectileDefinition != null)
                {
                    profile.ProjectileDmg = shooter.ProjectileDefinition.Damage;
                    profile.ProjectileSpeed = shooter.Speed * shooter.ProjectileDefinition.SpeedMultiply;
                }
            }

            var yummy = prefab.GetComponentInChildren<Yummy>(true);
            if (yummy != null) profile.HealOnEat = yummy.HpCure;

            var poison = prefab.GetComponentInChildren<Poison>(true);
            if (poison != null) profile.PoisonOnEat = poison.Dmg;

            profile.IsLevelUp = prefab.GetComponentInChildren<LevelUpFood>(true) != null;
            profile.IsExit = prefab.GetComponentInChildren<GameOverFood>(true) != null;
            profile.IsEdible = prefab.GetComponentInChildren<BaseFood>(true) != null;

            var bee = prefab.GetComponentInChildren<BeeMoving>(true);
            if (bee != null) profile.MoveSpeed = bee.AverageSpeed;

            var chaser = prefab.GetComponentInChildren<ChaserMoving>(true);
            if (chaser != null)
            {
                profile.IsChaser = true;
                profile.ChaseRadius = chaser.ChaseRadius;
                profile.MoveSpeed = chaser.Speed;
            }

            var charger = prefab.GetComponentInChildren<ChargerMoving>(true);
            if (charger != null)
            {
                profile.IsCharger = true;
                profile.DetectionRange = charger.DetectionRange;
                profile.ChargeTelegraph = charger.TelegraphTime;
                profile.ChargeCooldown = charger.Cooldown;
                profile.MoveSpeed = charger.ChargeSpeed;
            }

            return profile;
        }
    }
}
