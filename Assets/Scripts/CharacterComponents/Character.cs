using System;
using Definitions;
using Pools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Character : BaseCharacterComponent
    {
        [SerializeField, HideInInspector] public Rigidbody2D rb2D;
        [SerializeField] private PlayerControllable playerControllable;
        [SerializeField] private LumpMeatMovable lumpMeatMovable;
        [SerializeField] private ReachingToStartMovable reachingToStartMovable;
        [SerializeField] private CameraTarget cameraTarget;
        [SerializeField] private Shooter shooter;
        [SerializeField] private Eater eater;
        [SerializeField] private Stats stats;

        public Stats Stats => stats;
        public PlayerControllable PlayerControllable => playerControllable;
        public LumpMeatMovable LumpMeatMovable => lumpMeatMovable;
        public ReachingToStartMovable ReachingToStartMovable => reachingToStartMovable;
        public CameraTarget CameraTarget => cameraTarget;
        public Shooter Shooter => shooter;
        public Eater Eater => eater;
        public Action OnDeadAction;

        private float _gravityScale;
        public CharacterDefinition characterDefinition;

        public void Init(CharacterDefinition characterDefinition)
        {
            stats.Init(characterDefinition.StatsPack, characterDefinition.Fraction);
            rb2D.bodyType = characterDefinition.Rigidbody2DDefinitionPack.rigidbodyType2D;
            rb2D.gravityScale = characterDefinition.Rigidbody2DDefinitionPack.gravityScale;
            rb2D.constraints = characterDefinition.Rigidbody2DDefinitionPack.rigidbodyConstraints2D;
            rb2D.freezeRotation = characterDefinition.Rigidbody2DDefinitionPack.freezRotation;

            this.characterDefinition = characterDefinition;

            if (characterDefinition.CameraTargetPriority > 0)
            {
                cameraTarget.Init(characterDefinition.CameraTargetPriority);
            }

            if (characterDefinition.ShooterPack.isShooter)
            {
                shooter.Init(characterDefinition.ShooterPack, characterDefinition.Fraction);
            }

            if (characterDefinition.LumpMeatMovablePack.isEnable)
            {
                lumpMeatMovable.Init(
                    characterDefinition.LumpMeatMovablePack
                    , characterDefinition.Rigidbody2DDefinitionPack.gravityScale
                );
            }

            if (characterDefinition.ReachingToStartMovablePack.isEnable)
            {
                reachingToStartMovable.Init(characterDefinition.ReachingToStartMovablePack);
            }

            if (characterDefinition.IsPlayerControllable)
            {
                playerControllable.enabled = characterDefinition.IsPlayerControllable;
            }

            stats.OnDeadAction += OnDead;
        }

        private void OnDead()
        {
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
            if (playerControllable == null) playerControllable = GetComponent<PlayerControllable>();
            if (cameraTarget == null) cameraTarget = GetComponent<CameraTarget>();
            if (shooter == null) shooter = GetComponent<Shooter>();
            if (eater == null) eater = GetComponent<Eater>();
            if (stats == null) stats = GetComponent<Stats>();
            if (lumpMeatMovable == null) lumpMeatMovable = GetComponent<LumpMeatMovable>();
        }

        public void Off()
        {
            gameObject.SetActive(false);
        }
    }
}