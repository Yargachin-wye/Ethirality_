using System;
using System.Collections;
using System.Collections.Generic;
using Managers.Pools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Generator
{
    public class GeneratorsManager : MonoBehaviour
    {
        [SerializeField] private List<GeneratorPack> unityEventlist;
        [FormerlySerializedAs("pointsContainer")] [SerializeField] private PointsContainerGenerator pointsContainerGenerator;
        [SerializeField] private int seed;
        private Random _random;

        private void Start()
        {
            GenerateWorld();
        }

        public void GenerateWorld()
        {
            StartCoroutine(Generate());
        }

        IEnumerator Generate()
        {
            yield return StartCoroutine(pointsContainerGenerator.Clear());

            _random = new Random(seed);
            foreach (var uEvent in unityEventlist)
            {
                if (uEvent.generate)
                {
                    yield return StartCoroutine(uEvent.baseGenerator.Init(_random, new Vector2(0, 0)));
                }
            }

            yield return null;
        }

        [Serializable]
        public struct GeneratorPack
        {
            [FormerlySerializedAs("generator")] public BaseGenerator baseGenerator;
            public bool generate;
        }
    }
}