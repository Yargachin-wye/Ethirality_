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
        [SerializeField] private GameObject listSlotPrefab;
        [SerializeField] private GameObject circleSlotPrefab;
        [SerializeField] private Transform activeSlotsContainer;
        [SerializeField] private Transform listSlotsContainer;
        [SerializeField] private float circleRadius = 150f;
        [SerializeField] private int maxSlotsOnFirstLayer = 4;

        private readonly List<Improvement> _trackedImprovements = new();
        private readonly List<ImprovementSlotUI> _activeSlotUIs = new();
        private readonly List<GameObject> _listSlots = new();
        private ImprovementSlotUI _selectedSlot;

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

            if (activeSlotsContainer != null)
                activeSlotsContainer.gameObject.SetActive(false);
            if (listSlotsContainer != null)
                listSlotsContainer.gameObject.SetActive(false);
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
            if (activeSlotsContainer != null)
                activeSlotsContainer.gameObject.SetActive(true);
            if (listSlotsContainer != null)
                listSlotsContainer.gameObject.SetActive(true);

            _selectedSlot = null;
            RefreshUI();
        }

        public void Hide()
        {
            if (activeSlotsContainer != null)
                activeSlotsContainer.gameObject.SetActive(false);
            if (listSlotsContainer != null)
                listSlotsContainer.gameObject.SetActive(false);

            _selectedSlot = null;
        }

        private void RefreshUI()
        {
            ClearSlots();
            _trackedImprovements.RemoveAll(t => t == null);

            var activeImprovements = new List<Improvement>();

            foreach (var imp in _trackedImprovements)
            {
                if (imp == null || imp.Definition == null) continue;

                // All improvements go to the list
                var listGo = Instantiate(listSlotPrefab, listSlotsContainer);
                var listSlot = listGo.GetComponent<ImprovementSlotUI>();
                if (listSlot != null)
                    listSlot.SetView(imp.Definition.Preview, imp.Definition.Description);
                _listSlots.Add(listGo);

                if (!imp.Definition.IsPassive)
                    activeImprovements.Add(imp);
            }

            // Active — layered circular layout (4, 6, 8, ...)
            int slotIndex = 0;
            int slotsInLayer = maxSlotsOnFirstLayer;

            for (int layer = 0; slotIndex < activeImprovements.Count; layer++, slotsInLayer += 2)
            {
                float layerRadius = circleRadius * (layer + 1);

                for (int j = 0; j < slotsInLayer && slotIndex < activeImprovements.Count; j++, slotIndex++)
                {
                    var imp = activeImprovements[slotIndex];
                    var go = Instantiate(circleSlotPrefab, activeSlotsContainer);
                    var slot = go.GetComponent<ImprovementSlotUI>();
                    if (slot != null)
                    {
                        slot.SetView(imp.Definition.Preview, null);
                        slot.SetSlotData(imp, slotIndex);
                        slot.SetSelected(false);
                        slot.OnSlotClicked = OnSlotClicked;
                    }

                    var rt = go.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        float angle = (90f - j * (360f / slotsInLayer)) * Mathf.Deg2Rad;
                        rt.anchoredPosition = new Vector2(
                            Mathf.Cos(angle) * layerRadius,
                            Mathf.Sin(angle) * layerRadius
                        );
                    }

                    _activeSlotUIs.Add(slot);
                }
            }
        }

        private void OnSlotClicked(ImprovementSlotUI clickedSlot)
        {
            if (_selectedSlot == null)
            {
                _selectedSlot = clickedSlot;
                clickedSlot.SetSelected(true);
                return;
            }

            if (_selectedSlot == clickedSlot)
            {
                _selectedSlot.SetSelected(false);
                _selectedSlot = null;
                return;
            }

            SwapSlots(_selectedSlot, clickedSlot);
            _selectedSlot.SetSelected(false);
            _selectedSlot = null;
        }

        private void SwapSlots(ImprovementSlotUI slotA, ImprovementSlotUI slotB)
        {
            var impA = slotA.LinkedImprovement;
            var impB = slotB.LinkedImprovement;

            int idxA = _trackedImprovements.IndexOf(impA);
            int idxB = _trackedImprovements.IndexOf(impB);
            if (idxA >= 0 && idxB >= 0)
                (_trackedImprovements[idxA], _trackedImprovements[idxB]) = (_trackedImprovements[idxB], _trackedImprovements[idxA]);

            MessageBroker.Default.Publish(new SwapImprovementsEvent
            {
                ImprovementA = impA,
                ImprovementB = impB
            });

            RefreshUI();
        }

        private void ClearSlots()
        {
            foreach (var slot in _activeSlotUIs)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _activeSlotUIs.Clear();

            foreach (var slot in _listSlots)
            {
                if (slot != null) Destroy(slot);
            }
            _listSlots.Clear();
        }
    }
}
