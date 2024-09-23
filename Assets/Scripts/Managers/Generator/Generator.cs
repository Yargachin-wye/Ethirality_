using System.Collections;
using UnityEngine;

namespace Managers.Generator
{
    public abstract class Generator : MonoBehaviour
    {
        private Vector2 _position;
        private System.Random _random;
        public System.Random Random => _random;
        public Vector2 Position => _position;

        public virtual IEnumerator Init(System.Random random, Vector2 position)
        {
            _random = random;
            _position = position;
            
            yield return null;
        }
    }
}