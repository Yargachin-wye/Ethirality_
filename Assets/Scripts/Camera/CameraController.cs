using System.Collections.Generic;
using CharacterComponents;
using UnityEngine;

namespace Camera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float smoothSpeed = 0.125f;
        [SerializeField] private UnityEngine.Camera camera;
        [SerializeField] private float sizeSmoothSpeed = 5;

        private List<CameraTarget> _cameraTargetsList;
        private CameraTarget _nowTarget;
        public CameraTarget NowTarget=>_nowTarget;
        private float _z = 0;
        private float _size = 0;
        public static CameraController Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("CameraController.instance != null");
                return;
            }

            Instance = this;
            _cameraTargetsList = new List<CameraTarget>();
            _size = camera.orthographicSize;
            _z = transform.position.z;
        }

        public void SetTarget(CameraTarget cameraTarget)
        {
            _cameraTargetsList.Add(cameraTarget);
            if (_cameraTargetsList.Count == 1)
            {
                _nowTarget = cameraTarget;
                return;
            }

            if (cameraTarget.Priority > _nowTarget.Priority)
            {
                _nowTarget = cameraTarget;
            }
        }

        public void DeleteTarget(CameraTarget cameraTarget)
        {
            _cameraTargetsList.Remove(cameraTarget);
            foreach (var target in _cameraTargetsList)
            {
                if (cameraTarget.Priority > _nowTarget.Priority)
                {
                    _nowTarget = cameraTarget;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_cameraTargetsList.Count == 0) return;

            Vector3 desiredPosition = new Vector3(_nowTarget.transform.position.x, _nowTarget.transform.position.y, _z);
            Vector3 smoothedPosition =
                Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.fixedDeltaTime);

            camera.orthographicSize = _size + sizeSmoothSpeed * (transform.position - smoothedPosition).magnitude;

            transform.position = smoothedPosition;
        }
    }
}