using Camera;
using UnityEngine;

namespace CharacterComponents
{
    public class CameraTarget : BaseCharacterComponent
    {
        [SerializeField] private int priority = 0;
        [SerializeField] private bool setOnStart = true;

        private bool _isSet = false;

        private static CameraController CameraController => CameraController.Instance;
        public int Priority => priority;

        public override void Init()
        {
            Set();
        }

        private void OnDisable()
        {
            if (_isSet)Delete();
        }

        private void Set()
        {
            _isSet = true;
            CameraController.SetTarget(this);
        }

        private void Delete()
        {
            _isSet = false;
            CameraController.DeleteTarget(this);
        }
    }
}