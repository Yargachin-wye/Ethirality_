using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Camera
{
    public class ParallaxEffect : MonoBehaviour
    {
        [SerializeField] private new Transform camera;
        [SerializeField] private List<ParallaxPack> parallaxs;
        private List<TransformPack> _parallaxs = new List<TransformPack>();
        private Vector3 _cameraTransformLasPos;

        void Start()
        {
            _cameraTransformLasPos = camera.position;
            foreach (var parallax in parallaxs)
            {
                _parallaxs.Add(new TransformPack(
                    parallax.sprite.transform,
                    parallax.sprite.bounds.size,
                    parallax.effect));
            }
        }

        void Update()
        {
            Vector2 moveTo = camera.position - _cameraTransformLasPos;
            _cameraTransformLasPos = camera.position;
            
            foreach (var p in _parallaxs)
            {
                Vector2 nextPos = new Vector2(p.Transform.position.x - moveTo.x * p.Effect.x,
                    p.Transform.position.y - moveTo.y * p.Effect.y);

                if (p.Transform.position.y > camera.position.y + p.Size.y)
                    nextPos = new Vector2(nextPos.x, camera.position.y - p.Size.y);

                if (p.Transform.position.y < camera.position.y - p.Size.y)
                    nextPos = new Vector2(nextPos.x, camera.position.y + p.Size.y);

                if (p.Transform.position.x > camera.position.x + p.Size.x)
                    nextPos = new Vector2(camera.position.x - p.Size.x, nextPos.y);

                if (p.Transform.position.x < camera.position.x - p.Size.x)
                    nextPos = new Vector2(camera.position.x + p.Size.x, nextPos.y);

                p.Transform.position = new Vector3(nextPos.x, nextPos.y, p.Transform.position.z);
            }
        }

        public struct TransformPack
        {
            public Transform Transform;
            public Vector2 Size;
            public Vector2 Effect;

            public TransformPack(Transform transform, Vector2 size, Vector2 effect)
            {
                Transform = transform;
                Size = size;
                Effect = effect;
            }
        }

        [Serializable]
        public struct ParallaxPack
        {
            public SpriteRenderer sprite;
            public Vector2 effect;
        }
    }
}