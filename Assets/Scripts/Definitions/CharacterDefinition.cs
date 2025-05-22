using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Definitions
{
    [CreateAssetMenu]
    public class CharacterDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private int resId;

        [SerializeField] private GameObject prefab;
        [SerializeField] private Fraction fraction;

        public GameObject Prefab => prefab;
        public Fraction Fraction => fraction;

        public void SetResId(int i)
        {
            resId = i;
        }
    }

    public enum Fraction
    {
        Player,
        Enemy,
        All
    }
}