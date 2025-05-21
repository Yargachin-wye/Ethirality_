using UnityEngine;

namespace Camera
{
    public class ParallaxEffect : MonoBehaviour
    {
        [SerializeField] private Vector2 effect;
        [SerializeField] private Material material;

        [SerializeField] private Transform cam;
        private Vector2 offset;

        void LateUpdate()
        {
            transform.position = new Vector3(cam.position.x, cam.position.y, transform.position.z);
            offset = new Vector2(cam.position.x * effect.x , cam.position.y * effect.y);
            material.mainTextureOffset = offset;
        }
    }
}