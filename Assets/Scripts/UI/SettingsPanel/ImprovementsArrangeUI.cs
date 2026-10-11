using System;
using System.Collections.Generic;
using Improvements;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Improvement;
using UnityEngine;

namespace UI.SettingsPanel
{
    public class ImprovementsArrangeUI : MonoBehaviour
    {
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsContainer;

        private readonly List<Improvement> _trackedImprovements = new();
        private readonly List<GameObject> _slots = new();

        private IDisposable _addSub;
        private IDisposable _removeSub;
        private IDisposable _stopRoundSub;

        private void Awake()
        {
            _addSub = MessageBroker.Default
                .Receive<AddImprovementEvent>()
                .Subscribe(OnAddImprovement);

            _removeSub = MessageBroker.Default
                .Receive<RemoveImprovementEvent>()
                .Subscribe(OnRemoveImprovement);

            _stopRoundSub = MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(_ => OnStopRound());

            if (slotsContainer != null)
                slotsContainer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _addSub?.Dispose();
            _removeSub?.Dispose();
            _stopRoundSub?.Dispose();
        }

        private void OnAddImprovement(AddImprovementEvent data)
        {
            _trackedImprovements.Add(data.Improvement);
        }

        private void OnRemoveImprovement(RemoveImprovementEvent data)
        {
            _trackedImprovements.Remove(data.Improvement);
        }

        private void OnStopRound()
        {
            _trackedImprovements.Clear();
            ClearSlots();
        }

        public void Show()
        {
            if (slotsContainer != null)
                slotsContainer.gameObject.SetActive(true);

            RefreshUI();
        }

        public void Hide()
        {
            if (slotsContainer != null)
                slotsContainer.gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            ClearSlots();
            _trackedImprovements.RemoveAll(t => t == null);

            foreach (var imp in _trackedImprovements)
            {
                if (imp.Definition == null) continue;

                var go = Instantiate(slotPrefab, slotsContainer);
                var slot = go.GetComponent<ImprovementSlotUI>();
                if (slot != null)
                    slot.SetView(imp.Definition.Preview, imp.Definition.Description);

                _slots.Add(go);
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                if (slot != null) Destroy(slot);
            }
            _slots.Clear();
        }
    }
}
