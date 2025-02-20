using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Definitions
{
    [CreateAssetMenu]
    public class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Fraction fraction;
        [SerializeField] private int cameraTargetPriority = -1;
        [SerializeField] private Rigidbody2DDefinitionPack rigidbody2DDefinitionPack;
        [SerializeField] private bool isPlayerControllable;
        [SerializeField] private LumpMeatMovablePack lumpMeatMovablePack;
        [SerializeField] private ReachingToStartMovablePack reachingToStartMovablePack;
        [SerializeField] private StatsPack statsPack;

        [FormerlySerializedAs("shootablePack")] [SerializeField]
        private ShooterPack shooterPack;

        public StatsPack StatsPack => statsPack;
        public GameObject Prefab => prefab;
        public Fraction Fraction => fraction;
        public int CameraTargetPriority => cameraTargetPriority;
        public Rigidbody2DDefinitionPack Rigidbody2DDefinitionPack => rigidbody2DDefinitionPack;
        public bool IsPlayerControllable => isPlayerControllable;
        public ShooterPack ShooterPack => shooterPack;
        public LumpMeatMovablePack LumpMeatMovablePack => lumpMeatMovablePack;
        public ReachingToStartMovablePack ReachingToStartMovablePack => reachingToStartMovablePack;
    }

    [Serializable]
    public struct ShooterPack
    {
        public bool isShooter;

        public ProjectileDefinition projectileDefinition;
        public float speed;
        public float rotationSpeed;
        public float shootDelay;
    }

    [Serializable]
    public struct Rigidbody2DDefinitionPack
    {
        public RigidbodyType2D rigidbodyType2D;
        public RigidbodyConstraints2D rigidbodyConstraints2D;
        public bool freezRotation;
        public float gravityScale;
    }

    [Serializable]
    public struct LumpMeatMovablePack
    {
        public bool isEnable;
        public float rotationSpeed;
        public float decelerationRate;
        public float powerDash;
    }

    [Serializable]
    public struct ReachingToStartMovablePack
    {
        public bool isEnable;
        public float speed;
    }
    [Serializable]
    public struct StatsPack
    {
        public int startHp;
        public int maxHp;
    }

    public enum Fraction
    {
        Player,
        Enemy,
        All
    }
}