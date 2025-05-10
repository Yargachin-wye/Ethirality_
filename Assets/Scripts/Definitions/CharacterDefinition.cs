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
        [SerializeField] private bool isPlayerControllable;
        
        public GameObject Prefab => prefab;
        public Fraction Fraction => fraction;
        public int CameraTargetPriority => cameraTargetPriority;
        public bool IsPlayerControllable => isPlayerControllable;
    }
    public enum Fraction
    {
        Player,
        Enemy,
        All
    }
}