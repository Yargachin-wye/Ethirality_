using System.Collections.Generic;
using Balance;
using Balance.Editor;
using NUnit.Framework;
using UnityEditor;

namespace BalanceTests
{
    public static class BalanceTestContext
    {
        public const string ScenarioPath = "Assets/Infos/Balance/OpenWorld.asset";

        private static BalanceScenario _scenario;
        private static List<LayerReport> _layers;
        private static IReadOnlyList<PlayerBuild> _builds;

        public static BalanceScenario Scenario
        {
            get
            {
                Ensure();
                return _scenario;
            }
        }

        public static List<LayerReport> Layers
        {
            get
            {
                Ensure();
                return _layers;
            }
        }

        public static IReadOnlyList<PlayerBuild> Builds
        {
            get
            {
                Ensure();
                return _builds;
            }
        }

        public static void Warn(string message)
        {
            TestContext.Out.WriteLine(message);
            UnityEngine.Debug.LogWarning("[Balance.Target] " + message);
            Assert.Inconclusive(message);
        }

        private static void Ensure()
        {
            if (_scenario == null)
            {
                _scenario = AssetDatabase.LoadAssetAtPath<BalanceScenario>(ScenarioPath);
                if (_scenario == null)
                {
                    BalanceScenarioFactory.CreateDefault();
                    _scenario = AssetDatabase.LoadAssetAtPath<BalanceScenario>(ScenarioPath);
                }
            }

            if (_scenario != null && _layers == null)
            {
                _layers = LayerSimulator.SimulateLayers(_scenario, _scenario.SimulatedLayers);
                _builds = _scenario.Builds();
            }
        }
    }
}
