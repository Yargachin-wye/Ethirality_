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
            MessageBroker.Default
                .Receive<AddImprovementEvent>()
                .Subscribe(data => OnAddImp(data));
            
            MessageBroker.Default
                .Receive<RemoveImprovementEvent>()
                .Subscribe(data => OnRemoveImp(data));
        }

        public abstract void OnAddImp(AddImprovementEvent data);



        public abstract void OnRemoveImp(RemoveImprovementEvent data);
        
        public virtual void OnValidate()
        {
            if (improvement == null) improvement = transform.root.GetComponent<Improvement>();
        }
        public abstract void SetPlayer(ImprovementDefinition definition,Character character, ImprovementsComponent improvementsComponent);

    }
}