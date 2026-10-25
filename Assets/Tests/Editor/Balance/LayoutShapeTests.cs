using System.Collections.Generic;
using Generation;
using NUnit.Framework;
using UnityEngine;

namespace BalanceTests
{
    public class LayoutShapeTests
    {
        private const int Layers = 60;

        private static List<LayerContext> Build()
        {
            var s = BalanceTestContext.Scenario;
            var list = new List<LayerContext>();
            for (int i = 0; i < Layers; i++)
            {
                var layer = LayerPlanner.Plan(s.Seed, i, i * 100f, s.DifficultyProfile, s.LayerDefinitions, out _);
                if (layer == null) continue;
                LayerPopulator.Populate(layer, s.Placement, s.AttemptsPerEnemy);
                list.Add(layer);
            }

            return list;
        }

        [Test]
        public void ShapesAreDiverseAndNotRepeated()
        {
            var layers = Build();
            var shapes = new HashSet<LayoutShape>();
            for (int i = 0; i < layers.Count; i++)
            {
                shapes.Add(layers[i].Layout.Shape);
                if (i > 0)
                    Assert.AreNotEqual(layers[i - 1].Layout.Shape, layers[i].Layout.Shape, $"Слой {i}: форма повторяется");
            }

            Assert.GreaterOrEqual(shapes.Count, 4, "Ожидается минимум 4 разных формы на 60 слоях");
            Assert.IsFalse(shapes.Contains(LayoutShape.Scatter));
        }

        [Test]
        public void ChainStaysTraversable()
        {
            foreach (var layer in Build())
            {
                var chain = layer.Chain;
                for (int i = 1; i < chain.Count; i++)
                    Assert.LessOrEqual(Vector2.Distance(chain[i - 1].Position, chain[i].Position),
                        layer.Climb.StepCap + 1e-3f, $"Слой {layer.Index} ({layer.Layout.Shape}): шаг цепочки {i}");
            }
        }

        [Test]
        public void ShapedPathActuallyMoves()
        {
            var spans = new List<float>();
            foreach (var layer in Build())
            {
                if (layer.Layout.Shape != LayoutShape.Zigzag && layer.Layout.Shape != LayoutShape.Serpentine) continue;
                float min = float.MaxValue, max = float.MinValue;
                foreach (var node in layer.Chain)
                {
                    min = Mathf.Min(min, node.Position.x);
                    max = Mathf.Max(max, node.Position.x);
                }

                spans.Add(max - min);
            }

            Assert.IsNotEmpty(spans);
            float mean = 0f;
            foreach (float v in spans) mean += v;
            mean /= spans.Count;
            Assert.GreaterOrEqual(mean, 10f, "Zigzag/Serpentine должны заметно уходить вбок");
        }

        [Test]
        public void SpawnsStayInsideBounds()
        {
            foreach (var layer in Build())
                foreach (var spawn in layer.Spawns)
                    Assert.IsTrue(layer.Bounds.Contains(spawn.Position), $"Слой {layer.Index}: {spawn.Definition.name} вне границ");
        }

        [Test]
        public void LayoutIsDeterministic()
        {
            var a = Build();
            var b = Build();
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Layout.Shape, b[i].Layout.Shape);
                Assert.AreEqual(a[i].Spawns.Count, b[i].Spawns.Count);
                for (int k = 0; k < a[i].Spawns.Count; k++)
                    Assert.AreEqual(a[i].Spawns[k].Position, b[i].Spawns[k].Position);
            }
        }
    }
}
