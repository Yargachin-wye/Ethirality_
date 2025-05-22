using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LevelUp
{
    public class LevelUpSlot : MonoBehaviour
    {
        [SerializeField] public Button selectBtn;
        [SerializeField] public TMP_Text textPrice;
        [SerializeField] public TMP_Text textDescription;
        [SerializeField] public Image previewImage;
        [SerializeField] public Image frame;
        [Space]
        [SerializeField] public Color color1;
        [SerializeField] public Color color2;
        [SerializeField] public Color colorSeleted;

        public void SetView(Sprite sprite, string description, int price)
        {
            previewImage.sprite = sprite;
            textDescription.text = description;

            if (price < 0)
            {
                textPrice.color = color1;
            }
            else
            {
                textPrice.color = color2;
            }
        }

        public void Select(bool isSelect)
        {
            frame.color = isSelect ? colorSeleted : Color.clear;
        }
    }
}