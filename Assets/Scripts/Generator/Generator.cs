using System.Collections;
using UnityEngine;

namespace Managers.Generator
{
    public abstract class Generator : MonoBehaviour
    {
        protected System.Random Random;
        protected Vector2 Position;

        public virtual IEnumerator Init(System.Random random, Vector2 position)
        {
            Random = random;
            Position = position;
            
            yield return null;
        }
    }
}