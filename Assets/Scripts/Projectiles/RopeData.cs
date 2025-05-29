using System;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    [Serializable]
    public class RopeData
    {
        [SerializeField] private int segmentCount = 20; // Количество сегментов веревки
        [SerializeField] private float segmentLength = 0.2f; // Длина сегмента
        [SerializeField] private Vector2 gravity = new Vector2(0, -10f); // Гравитация, направленная вниз
        [SerializeField] private int verletIterations = 50; // Количество итераций Верле для стабильности

        private List<Vector2> _currentPos = new();
        private List<Vector2> _previousPos = new();
        private bool _isFirstNailed = false;
        private bool _isLastNailed = false;
        private Vector2 _firstPos;
        private Vector2 _lastPos;

        public int SegmentCount => segmentCount;

        public void UnpinLastPos()
        {
            _isLastNailed = false;
        }
        public void UnpinFirstPos()
        {
            _isFirstNailed = false;
        }

        private void StartFromFirst(Vector2 position)
        {
            _currentPos = new();
            _previousPos = new();
            _isFirstNailed = true;

            for (int i = segmentCount - 1; i > 0; i--)
            {
                _currentPos.Add(position);
                _previousPos.Add(position);
                position.y -= segmentLength;
            }
        }

        private void StartFromLast(Vector2 position)
        {
            _currentPos = new();
            _previousPos = new();
            _isLastNailed = true;

            for (int i = 0; i < segmentCount; i++)
            {
                _currentPos.Add(position);
                _previousPos.Add(position);
                position.y -= segmentLength;
            }
        }

        public void StartBetween2Positions(Vector2 firstPosition, Vector2 lastPosition, int segments)
        {
            segmentCount = segments;
            SetLikeLine(firstPosition, lastPosition);
        }

        private void SetLikeLine(Vector2 firstPosition, Vector2 lastPosition)
        {
            _currentPos = new();
            _previousPos = new();
            Vector2 direction = (lastPosition - firstPosition).normalized;
            for (int i = 0; i < segmentCount; i++)
            {
                _currentPos.Add(firstPosition + segmentLength * i * direction);
                _previousPos.Add(firstPosition + segmentLength * i * direction);
            }
        }

        private void Update()
        {
            for (int i = 0; i < _currentPos.Count; i++)
            {
                Vector2 velocity = _currentPos[i] - _previousPos[i];
                _previousPos[i] = _currentPos[i];

                velocity += new Vector2(
                    gravity.x * Time.fixedDeltaTime * Time.fixedDeltaTime,
                    gravity.y * Time.fixedDeltaTime * Time.fixedDeltaTime);

                _currentPos[i] += velocity;
            }
        }

        private void ApplyConstraints()
        {
            for (int i = 0; i < _currentPos.Count - 1; i++)
            {
                Vector2 delta = _currentPos[i + 1] - _currentPos[i];
                float distance = delta.magnitude;

                if (_isFirstNailed && i == 0)
                {
                    float error = (distance - segmentLength) / distance;

                    _currentPos[0] = _firstPos;
                    Vector2 correction = delta * error;
                    _currentPos[i + 1] -= correction;
                }
                else if (_isLastNailed && i + 1 == _currentPos.Count - 1)
                {
                    float error = (distance - segmentLength) / distance;

                    _currentPos[_currentPos.Count - 1] = _lastPos;
                    Vector2 correction = delta * error;
                    _currentPos[i] += correction;
                }

                if (distance > segmentLength)
                {
                    float error = (distance - segmentLength) / distance;
                    Vector2 correction = delta * error * 0.5f;
                    _currentPos[i] += correction;
                    _currentPos[i + 1] -= correction;
                }
            }
        }


        public List<Vector2> UpdateRopePhysics()
        {
            Update();

            for (int i = 0; i < verletIterations; i++)
            {
                ApplyConstraints();
            }

            return _currentPos;
        }

        public void SetFirstSectionPos(Vector2 position)
        {
            _isFirstNailed = true;
            _firstPos = position;
        }

        public void SetLastSectionPos(Vector2 position)
        {
            _isLastNailed = true;
            _lastPos = position;
        }

        public void DrawRope()
        {
            for (int i = 0; i < _currentPos.Count - 1; i++)
            {
                Debug.DrawLine(_currentPos[i], _currentPos[i + 1], Color.blue);
            }
        }
    }
}