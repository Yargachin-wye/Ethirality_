using System;
using Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace CharacterComponents
{
    public abstract class BaseCharacterComponent : MonoBehaviour
    {
        [SerializeField, HideInInspector] public Character character;

        protected virtual void Awake()
        {
            Validate();
        }

        protected virtual void Validate()
        {
            character = transform.root.GetComponent<Character>();
        }

        public abstract void Init();

        public virtual void Init(CharacterDefinition characterDefinition)
        {
            
        }
    }
}