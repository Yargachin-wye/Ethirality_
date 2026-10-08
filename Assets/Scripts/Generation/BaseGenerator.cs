using System.Collections;
using UnityEngine;

namespace Generation
{
    public abstract class BaseGenerator : MonoBehaviour
    {
        public abstract IEnumerator Generate(LayerContext layer);
    }
}
