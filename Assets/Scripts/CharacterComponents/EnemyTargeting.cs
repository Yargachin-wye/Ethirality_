using CharacterComponents.CharacterStat;
using Definitions;
using UnityEngine;

namespace CharacterComponents
{
    public static class EnemyTargeting
    {
        public static Stats FindClosestHostile(Vector2 origin, float radius, Fraction ownFraction)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
            Stats closest = null;
            float minDistance = Mathf.Infinity;

            foreach (var hit in hits)
            {
                Stats stats = hit.GetComponent<Stats>();
                if (stats == null || stats.character == null || stats.Fraction != Fraction.Player ||
                    stats.Fraction == ownFraction) continue;

                float distance = Vector2.Distance(origin, stats.transform.position);
                if (distance < minDistance)
                {
                    closest = stats;
                    minDistance = distance;
                }
            }

            return closest;
        }
    }
}
