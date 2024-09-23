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

        [FormerlySerializedAs("shootablePack")] [SerializeField]
        private ShooterPack shooterPack;

        public GameObject Prefab => prefab;
        public Fraction Fraction => fraction;
        public int CameraTargetPriority => cameraTargetPriority;
        public Rigidbody2DDefinitionPack Rigidbody2DDefinitionPack => rigidbody2DDefinitionPack;
        public bool IsPlayerControllable => isPlayerControllable;
        public ShooterPack ShooterPack => shooterPack;
        public LumpMeatMovablePack LumpMeatMovablePack => lumpMeatMovablePack;
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
        public bool isLumpMeatMovable;
        public float rotationSpeed;
        public float decelerationRate;
        public float powerDash;
    }

    public enum Fraction
    {
        Player,
        Enemy,
        All
    }
}