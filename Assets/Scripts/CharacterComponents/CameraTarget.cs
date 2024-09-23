using Camera;
using UnityEngine;

namespace CharacterComponents
{
    public class CameraTarget : BaseComponent
    {
        [SerializeField] private int priority = 0;
        [SerializeField] private bool setOnStart = true;

        private bool _isSet = false;

        private CameraController _cameraController;
        public int Priority => priority;

        public void Init(int priority)
        {
            this.priority = priority;
            _cameraController = CameraController.Instance;
            Set();
        }

        private void OnDisable()
        {
            if (_isSet)Delet();
        }

        private void Set()
        {
            _isSet = true;
            _cameraController.SetTarget(this);
        }

        private void Delet()
        {
            _isSet = false;
            _cameraController.DeletTarget(this);
        }
    }
}