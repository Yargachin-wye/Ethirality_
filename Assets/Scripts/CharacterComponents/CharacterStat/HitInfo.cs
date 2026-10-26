using UnityEngine;

namespace CharacterComponents.CharacterStat
{
    public readonly struct HitInfo
    {
        public readonly int Damage;
        public readonly Vector2 SourcePosition;
        public readonly Vector2 Direction;
        public readonly bool HasSource;
        public readonly bool HasDirection;

        public HitInfo(int damage)
        {
            Damage = damage;
            SourcePosition = default;
            Direction = default;
            HasSource = false;
            HasDirection = false;
        }

        public HitInfo(int damage, Vector2 sourcePosition, Vector2 direction)
        {
            Damage = damage;
            SourcePosition = sourcePosition;
            HasSource = true;
            HasDirection = direction.sqrMagnitude > 0.0001f;
            Direction = HasDirection ? direction.normalized : default;
        }
    }
}
