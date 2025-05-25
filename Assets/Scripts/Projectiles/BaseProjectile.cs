using Definitions;
using UnityEngine;

namespace Projectiles
{
    public abstract class BaseProjectile : MonoBehaviour
    {
        public ProjectileDefinition Definition { get; protected set; }

        public virtual void Init(ProjectileDefinition projectileDefinition)
        {
            Definition = projectileDefinition;
        }

        public abstract void Shoot(Vector2 direction, float speed, GameObject owner, Fraction fraction,
            bool hasOwner = true);
    }
}