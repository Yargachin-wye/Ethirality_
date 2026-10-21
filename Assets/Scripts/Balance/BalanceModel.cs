using System.Collections.Generic;

namespace Balance
{
    public static class BalanceModel
    {
        public static float ExpectedDamage(IEnumerable<EnemyProfile> placed, PlayerBuild b, BalanceScenario s)
        {
            float damage = 0f;
            foreach (var e in placed)
            {
                if (e.IsRanged)
                    damage += s.ExposureRanged / e.ShootDelay * e.ProjectileDmg
                              * (e.IsHoming ? s.HitProbHoming : s.HitProbRanged)
                              * b.RangedMitigation(s);
                if (e.ContactDmg > 0)
                    damage += e.ContactDmg * (e.IsChaser || e.IsLeaper ? s.PChase : s.PContact);
            }
            return damage;
        }

        public static float ExpectedHeal(IEnumerable<EnemyProfile> placed, PlayerBuild b, BalanceScenario s)
        {
            float heal = 0f;
            foreach (var e in placed)
                if (e.HealOnEat > 0 && e.EatableByDash(b.DashDmg))
                    heal += e.HealOnEat * s.EatShare;
            return heal;
        }

        public static float ExpectedLevelUpHp(IEnumerable<EnemyProfile> placed, BalanceScenario s)
        {
            int count = 0;
            foreach (var e in placed)
                if (e.IsLevelUp) count++;
            return count * s.PPickHp * 1f;
        }

        public static float KillableHpPool(IEnumerable<EnemyProfile> placed)
        {
            float pool = 0f;
            foreach (var e in placed)
                pool += e.KillableHp;
            return pool;
        }

        public static float TimeToClear(IEnumerable<EnemyProfile> placed, PlayerBuild b, BalanceScenario s)
        {
            return KillableHpPool(placed) / b.Dps(s);
        }
    }
}
