using System;
using System.Collections.Generic;
using System.Linq;
using Bootstrappers;
using Definitions;
using UnityEngine;
using Improvements;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Improvement;

namespace CharacterComponents
{
    public class ImprovementsComponent : BaseCharacterComponent
    {
        private static readonly int MaxTwistersOnLayer = 4;
        [SerializeField] private float radius = 2;
        [SerializeField] private float rotationSpeed = 30f;
        [Space]
        [Space]
        [SerializeField] private List<int> startImprovements;
        [SerializeField] private List<ImprovementDefinition> improvementDefinition;
        [SerializeField] private List<Improvement> improvements;

        private List<Leash> _leashs = new();
        private List<Twister> _twisters = new();

        private void Awake()
        {
            MessageBroker.Default
                .Receive<AddNewImprovementEvent>()
                .Subscribe(data => OnAddNewImprovement(data));

            MessageBroker.Default
                .Receive<AddHpEvent>()
                .Subscribe(data => OnAddHp(data));
        }

        private void OnAddHp(AddHpEvent data)
        {
            character.Stats.Cure(data.Hp);
        }

        private void OnAddNewImprovement(AddNewImprovementEvent data)
        {
            AddImprovement(data.Definition);
        }

        private void FixedUpdate()
        {
            for (int i = 0, twisterIndex = 0, layer = 0, twistersInLayer = MaxTwistersOnLayer;
                 twisterIndex < _twisters.Count;
                 layer++, twistersInLayer += 2)
            {
                for (int j = 0; j < twistersInLayer && twisterIndex < _twisters.Count; j++, twisterIndex++)
                {
                    float baseAngle = j * (360f / twistersInLayer);

                    int rotationDirection = (layer % 2 == 0) ? 1 : -1;
                    float angle = baseAngle + Time.time * rotationSpeed * rotationDirection;

                    float currentRadius = radius * (layer + 1);

                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * currentRadius;

                    _twisters[twisterIndex].transform.position = transform.position + offset;
                }
            }
        }

        public override void Init()
        {
            foreach (var impResId in startImprovements)
            {
                var data = ResManager.instance.Improvements[impResId];
                AddImprovement(data);
            }
        }

        public void AddImprovement(ImprovementDefinition data)
        {
            GameObject impGobj = Instantiate(data.Prefab);
            var imp = impGobj.GetComponent<Improvement>();
            AddMoving(impGobj);

            imp.SetPlayer(data, character, this);
            imp.OnDestroyAction += OnDestroyImprovement;
            improvements.Add(imp);
            MessageBroker.Default.Publish(new AddImprovementEvent { Improvement = imp });
        }

        private void AddMoving(GameObject impGobj)
        {
            Leash leash = impGobj.GetComponent<Leash>();
            Twister twister = impGobj.GetComponent<Twister>();

            if (leash != null)
            {
                _leashs.Add(leash);
            }

            if (twister != null)
            {
                _twisters.Add(twister);
            }
        }

        private void RemoveMoving(GameObject impGobj)
        {
            Leash leash = impGobj.GetComponent<Leash>();
            Twister twister = impGobj.GetComponent<Twister>();

            if (leash != null)
            {
                _leashs.Remove(leash);
            }

            if (twister != null)
            {
                _twisters.Remove(twister);
            }
        }
        
        private void OnDestroyImprovement(Improvement improvement)
        {
            improvements.Remove(improvement);
            RemoveMoving(improvement.gameObject);
            MessageBroker.Default.Publish(new RemoveImprovementEvent { Improvement = improvement });
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}