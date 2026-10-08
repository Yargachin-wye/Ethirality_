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
            float spent = 0f;

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

                spent += ThreatCost(table, spawn.Definition);
            }

            if (spent > layer.ThreatBudget + 0.001f)
                Debug.LogWarning(
                    $"[LayerValidator] Слой {layer.Index}: перерасход бюджета угроз " +
                    $"{spent:0.#} > {layer.ThreatBudget:0.#}");
        }

        private static float ThreatCost(EnemySpawnTable table, CharacterDefinition definition)
        {
            if (table?.Entries == null) return 0f;
            foreach (EnemySpawnTable.Entry e in table.Entries)
                if (e.definition == definition) return e.threatCost;
            return 0f;
        }
    }
}
