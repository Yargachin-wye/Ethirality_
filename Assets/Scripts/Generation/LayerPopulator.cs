namespace Generation
{
    public static class LayerPopulator
    {
        public struct Result
        {
            public int ThreatRolled;
            public int ThreatPlaced;
            public int FoodRolled;
            public int FoodPlaced;

            public int Rolled => ThreatRolled + FoodRolled;
            public int Placed => ThreatPlaced + FoodPlaced;
        }

        public static Result Populate(LayerContext layer, PlacementRules placement, int attempts)
        {
            var result = new Result();
            if (layer.SpawnTable == null) return result;

            placement.Reset();

            var threats = layer.SpawnTable.Roll(layer.Random, layer.Difficulty, layer.ThreatBudget,
                layer.Definition.Composition);
            result.ThreatRolled = threats.Count;
            foreach (var def in threats)
                if (placement.TryPlace(layer, def, attempts)) result.ThreatPlaced++;

            var food = layer.SpawnTable.RollFood(layer.Random, layer.Difficulty, layer.FoodBudget);
            result.FoodRolled = food.Count;
            foreach (var def in food)
                if (placement.TryPlace(layer, def, attempts)) result.FoodPlaced++;

            return result;
        }
    }
}
