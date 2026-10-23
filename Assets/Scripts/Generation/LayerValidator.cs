using System.Diagnostics;
using Definitions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Generation
{
    public static class LayerValidator
    {
        [Conditional("UNITY_EDITOR")]
        public static void Validate(LayerContext layer, PlacementRules rules = null)
        {
            if (layer == null || layer.Spawns.Count == 0) return;

            EnemySpawnTable table = layer.SpawnTable;
            Rect bounds = layer.Bounds;
            Vector2 entry = PlacementRules.EntryPoint(layer);
            float safeTop = bounds.yMin + (rules != null ? rules.SafeZoneHeight : 0f);
            float threatSpent = 0f;
            float foodSpent = 0f;
            float mobilitySpent = 0f;

            foreach (SpawnRequest spawn in layer.Spawns)
            {
                if (spawn.Definition == null) continue;
                Vector2 p = spawn.Position;
                string enemyName = spawn.Definition.name;

                if (!bounds.Contains(p))
                    Debug.LogWarning(
                        $"[LayerValidator] Слой {layer.Index}: {enemyName} вне Bounds {p} / {bounds}",
                        spawn.Definition);

                if (rules != null)
                {
                    if (p.y < safeTop)
                        Debug.LogWarning(
                            $"[LayerValidator] Слой {layer.Index}: {enemyName} в безопасной зоне входа " +
                            $"(y={p.y:0.##} < {safeTop:0.##})", spawn.Definition);

                    float range = PlacementRules.GetAttackRange(spawn.Definition);
                    if (rules.IsRanged(spawn.Definition) && Vector2.Distance(p, entry) < range)
                        Debug.LogWarning(
                            $"[LayerValidator] Слой {layer.Index}: {enemyName} держит точку входа " +
                            $"в зоне поражения (dist {Vector2.Distance(p, entry):0.#} < range {range:0.#})",
                            spawn.Definition);
                }

                if (table != null && table.TryGetEntry(spawn.Definition, out var spawnEntry))
                {
                    if (spawnEntry.kind == EnemySpawnTable.SpawnKind.Food) foodSpent += spawnEntry.FoodValue;
                    else if (spawnEntry.kind == EnemySpawnTable.SpawnKind.Mobility) mobilitySpent += spawnEntry.MobilityValue;
                    else threatSpent += spawnEntry.ThreatValue;
                }
            }

            if (threatSpent > layer.ThreatBudget + 0.001f)
                Debug.LogWarning(
                    $"[LayerValidator] Слой {layer.Index}: перерасход бюджета угроз " +
                    $"{threatSpent:0.#} > {layer.ThreatBudget:0.#}");

            if (foodSpent > layer.FoodBudget + 0.001f)
                Debug.LogWarning(
                    $"[LayerValidator] Слой {layer.Index}: перерасход бюджета еды " +
                    $"{foodSpent:0.#} > {layer.FoodBudget:0.#}");

            if (mobilitySpent > layer.MobilityBudget + 0.001f)
                Debug.LogWarning(
                    $"[LayerValidator] Слой {layer.Index}: перерасход бюджета мобильности " +
                    $"{mobilitySpent:0.#} > {layer.MobilityBudget:0.#}");

            if (layer.Climb != null && layer.Climb.ChainEnabled)
                for (int i = 1; i < layer.Chain.Count; i++)
                {
                    float step = Vector2.Distance(layer.Chain[i - 1].Position, layer.Chain[i].Position);
                    if (step > layer.Climb.StepCap + 0.001f)
                        Debug.LogWarning(
                            $"[LayerValidator] Слой {layer.Index}: шаг цепочки опор {step:0.#} > {layer.Climb.StepCap:0.#}");
                }
        }
    }
}
