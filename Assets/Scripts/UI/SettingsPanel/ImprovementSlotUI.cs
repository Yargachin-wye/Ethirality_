using System;
using Improvements;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.SettingsPanel
{
    public class ImprovementSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image selectionFrame;

        public Improvement LinkedImprovement { get; private set; }
        public int SlotIndex { get; private set; }
        public Action<ImprovementSlotUI> OnSlotClicked { get; set; }

        public void SetView(Sprite icon, string description)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (descriptionText != null)
                descriptionText.text = description;
        }

        public void SetSlotData(Improvement improvement, int index)
        {
            LinkedImprovement = improvement;
            SlotIndex = index;
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
                selectionFrame.enabled = selected;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(this);
        }
    }
}
