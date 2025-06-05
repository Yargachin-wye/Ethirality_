using System;
using System.Collections.Generic;
using Bootstrapper;
using Bootstrapper.Saves;
using CharacterComponents.Animations;
using Definitions;
using UnityEngine;
using Improvements;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Improvement;
using Utilities;

namespace CharacterComponents
{
    public class ImprovementsComponent : BaseCharacterComponent
    {
        private static readonly int MaxTwistersOnLayer = 4;
        [SerializeField] private Character characterForImp;
        [SerializeField] private float radius = 2;
        [SerializeField] private float rotationSpeed = 30f;
        [Space]
        [SerializeField] private List<Improvement> improvements;
        [SerializeField] private LumpMeatAnimator lumpMeatAnimator;

        private List<Leash> _leashs = new();
        private List<Twister> _twisters = new();

        private SaveSystem Saves => SaveSystem.Instance;

        private IDisposable _addNewImprovementSubscription;
        private IDisposable _addHpSubscription;

        private void OnDestroy()
        {
            _addNewImprovementSubscription?.Dispose();
            _addHpSubscription?.Dispose();
        }

        public override void Init()
        {
        }

        private void Start()
        {
            _addNewImprovementSubscription = MessageBroker.Default
                .Receive<AddNewImprovementEvent>()
                .Subscribe(data => OnAddNewImprovement(data));

            _addHpSubscription = MessageBroker.Default
                .Receive<AddHpEvent>()
                .Subscribe(data => OnAddHp(data));

            foreach (var impResId in Saves.saveData.playerUpgradeResIds)
            {
                var data = ResManager.Instance.Improvements[impResId];
                AddImprovement(data);
            }
        }

        private void OnAddHp(AddHpEvent data)
        {
            character.Stats.Cure(data.Hp);
        }

        private void OnAddNewImprovement(AddNewImprovementEvent data)
        {
            Debug.Log("!!! AddImprovement 0");
            AddNewImprovement(data.Definition);
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


        public void AddNewImprovement(ImprovementDefinition data)
        {
            Saves.saveData.playerUpgradeResIds.Add(data.ResId);
            AddImprovement(data);
        }

        public void AddImprovement(ImprovementDefinition data)
        {
            if (data.IsPassive)
            {
                switch (data.ImprovementName)
                {
                    case ImprovementsConst.DashUp:
                        lumpMeatAnimator.SetDashUp(true);
                        break;
                    case ImprovementsConst.ArrowUp:
                        lumpMeatAnimator.SetArrowUp(true);
                        break;
                    case ImprovementsConst.JawUp:
                        lumpMeatAnimator.SetJawUp(true);
                        break;
                }
                
            }

            GameObject impGobj = Instantiate(data.Prefab);
            var imp = impGobj.GetComponent<Improvement>();
            AddMoving(impGobj);

            var charact = GetComponent<Character>();
            if (charact == null)
            {
                Debug.LogError($"{name}: ^^^ Improvement component doesn't have an improvement component.");
                return;
            }

            imp.SetPlayer(data, charact, this);
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

        private void OnDestroyImprovement(Improvement improvement, bool isRemove)
        {
            if (isRemove && Saves.saveData.playerUpgradeResIds.Contains(improvement.Definition.ResId))
                Saves.saveData.playerUpgradeResIds.Remove(improvement.Definition.ResId);

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