using Generation;
using UnityEngine;

namespace Balance
{
    public struct EntityResources
    {
        public float Threat;
        public float Food;
        public float Mobility;
        public MobilityKind Kind;
    }

    public static class EntityResourceModel
    {
        public static EntityResources Derive(EnemyProfile p, EnemySpawnTable.SpawnKind pool, PlayerBuild reference,
            BalanceScenario s)
        {
            var result = new EntityResources();
            float gap = s.DifficultyProfile != null ? s.DifficultyProfile.ReferenceMaxGap : reference.MaxVerticalGap;

            if (p.IsThreat)
            {
                float damage = BalanceModel.ExpectedDamage(new[] { p }, reference, s);
                float effort = p.KillableHp / Mathf.Max(reference.Dps(s), 0.01f);
                result.Threat = s.ThreatPerExpectedDamage * damage + s.ThreatPerEffort * effort;
            }

            if (p.IsLevelUp) result.Food = s.PPickHp;
            else if (p.IsEdible && p.HealOnEat > 0 && p.EatableByDash(reference.DashDmg)) result.Food = p.HealOnEat;

            bool mobile = p.MoveSpeed > 0f;
            if (pool == EnemySpawnTable.SpawnKind.Food || mobile)
            {
                result.Kind = p.IsEdible ? MobilityKind.EatDash : MobilityKind.None;
                result.Mobility = result.Kind == MobilityKind.EatDash ? s.EatDashMobilityShare * gap : 0f;
            }
            else
            {
                result.Kind = !p.Damageable && !p.IsThreat ? MobilityKind.SolidPlatform : MobilityKind.HarpoonAnchor;
                result.Mobility = gap;
            }

            return result;
        }

        public static EnemySpawnTable.SpawnKind PoolFor(EntityResources derived, EnemySpawnTable.SpawnKind legacyKind) =>
            legacyKind == EnemySpawnTable.SpawnKind.Threat && derived.Threat <= 0f && derived.Mobility > 0f
                ? EnemySpawnTable.SpawnKind.Mobility
                : legacyKind;
    }
}
