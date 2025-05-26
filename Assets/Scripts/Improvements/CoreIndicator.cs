using CharacterComponents;
using Definitions;
using UI;
using UniRxEvents.Improvement;
using UnityEngine;

namespace Improvements
{
    public class CoreIndicator : MonoBehaviour
    {
        [SerializeField] private Transform indicator;
        private Vector2 Direction => UiCompass.Instance.Direction;

        private void LateUpdate()
        {
            if (indicator == null)
                return;

            if (Direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

                indicator.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }
}