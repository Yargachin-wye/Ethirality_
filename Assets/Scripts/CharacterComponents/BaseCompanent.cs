using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public class BaseComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector] public Character character;

        public virtual void OnValidate()
        {
            if (character == null) character = GetComponent<Character>();
        }
    }
}