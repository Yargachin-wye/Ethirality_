using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class BaseCharacterComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector] public Character character;

        public virtual void OnValidate()
        {
            if (character == null) character = transform.root.GetComponent<Character>();
        }
    }
}