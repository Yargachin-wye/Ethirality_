using CharacterComponents;
using Definitions;
using UniRx;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public abstract class BaseImprovementComponent : MonoBehaviour
    {
        
        [SerializeField, HideInInspector] public Improvement improvement;
        
        private void Awake()
        {
            
        }

        public virtual void OnRemove()
        {
            
        }
        
        public virtual void OnValidate()
        {
            if (improvement == null) improvement = transform.root.GetComponent<Improvement>();
        }
        public abstract void SetPlayer(ImprovementDefinition definition,Character character, ImprovementsComponent improvementsComponent);

    }
}