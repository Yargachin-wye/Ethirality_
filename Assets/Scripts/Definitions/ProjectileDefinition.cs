using UnityEngine;
using UnityEngine.Serialization;

namespace Definitions
{
    [CreateAssetMenu]
    public class ProjectileDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float speedMultiply = 2;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 2;
        [Header("Физика")] [SerializeField] private RigidbodyType2D rigidbodyType2D;
        [SerializeField] private float gravityScale;
        [Header("Выстрел")] [SerializeField] private float recoil;

        [Header("Столкновение")] [SerializeField]
        private bool isDestroyOnTrigger;

        [SerializeField] private bool isAttachedOnTrigger;
        [SerializeField] private float attachedForceDelay = 0;
        [SerializeField] private float targetForce = 0;
        [SerializeField] private float ownerForce = 0;
        public GameObject Prefab => prefab;
        public float SpeedMultiply => speedMultiply;
        public RigidbodyType2D RigidbodyType2D => rigidbodyType2D;
        public float GravityScale => gravityScale;
        public float LifeTime => lifeTime;
        public float Recoil => recoil;
        public bool IsDestroyOnTrigger => isDestroyOnTrigger;
        public bool IsAttachedOnTrigger => isAttachedOnTrigger;
        public float AttachedForceDelay => attachedForceDelay;
        public float TargetForce => targetForce;
        public float OwnerForce => ownerForce;
        public int Damage => damage;
    }

    public struct RopePack
    {
    }
}