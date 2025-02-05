using System;
using System.Collections;
using System.Collections.Generic;
using Managers.Pools;
using UnityEngine;
using Random = System.Random;

namespace Managers.Generator
{
    public class GeneratorsManager : MonoBehaviour
    {
        [SerializeField] private List<GeneratorPack> unityEventlist;
        [SerializeField] private PointsContainer pointsContainer;
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
            yield return StartCoroutine(pointsContainer.Clear());

            _random = new Random(seed);
            foreach (var uEvent in unityEventlist)
            {
                if (uEvent.generate)
                {
                    yield return StartCoroutine(uEvent.generator.Init(_random, new Vector2(0, 0)));
                }
            }

            yield return null;
        }

        [Serializable]
        public struct GeneratorPack
        {
            public Generator generator;
            public bool generate;
        }
    }
}