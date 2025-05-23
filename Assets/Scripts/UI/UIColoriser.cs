using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIColoriser : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Color color1;
        [SerializeField] private Color color2;
        [SerializeField] private Color color3;
        [SerializeField] private float speed = 1f;

        
        
        private Color[] _colors;
        private int _currentColorIndex = 0;
        private float t = 0f;

        void Start()
        {
            _colors = new Color[] { color1, color2, color3 };
            if (image != null)
            {
                image.color = _colors[0];
            }
        }

        void Update()
        {
            if (image == null || _colors.Length < 2) return;

            t += Time.deltaTime * speed;
            image.color = Color.Lerp(_colors[_currentColorIndex], _colors[(_currentColorIndex + 1) % _colors.Length], t);

            if (t >= 1f)
            {
                t = 0f;
                _currentColorIndex = (_currentColorIndex + 1) % _colors.Length;
            }
        }
    }
}