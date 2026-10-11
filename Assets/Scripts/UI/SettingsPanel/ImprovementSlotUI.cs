using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.SettingsPanel
{
    public class ImprovementSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;

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
    }
}
