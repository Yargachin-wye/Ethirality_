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
            public int MobilityRolled;
            public int MobilityPlaced;

            public int Rolled => ThreatRolled + FoodRolled + MobilityRolled;
            public int Placed => ThreatPlaced + FoodPlaced + MobilityPlaced;
        }

        public static Result Populate(LayerContext layer, PlacementRules placement, int attempts)
        {
            var result = new Result();
            if (layer.SpawnTable == null) return result;

            placement.Reset();

            var threats = layer.SpawnTable.Roll(layer.Random, layer.Difficulty, layer.ThreatBudget,
                layer.Definition.Composition);
            var supports = layer.SpawnTable.RollMobility(layer.Random, layer.Difficulty, layer.MobilityBudget);
            var food = layer.SpawnTable.RollFood(layer.Random, layer.Difficulty, layer.FoodBudget);
            result.ThreatRolled = threats.Count;
            result.MobilityRolled = supports.Count;
            result.FoodRolled = food.Count;

            int chainPlaced = SupportChain.Build(layer, placement, supports, threats);
            int chainThreats = CountChainThreats(layer);
            result.MobilityPlaced += chainPlaced - chainThreats;
            result.ThreatPlaced += chainThreats;

            var decorated = LayoutDecorator.Apply(layer, placement, threats, food);
            result.ThreatPlaced += decorated.ThreatsPlaced;
            result.FoodPlaced += decorated.FoodPlaced;

            foreach (var def in supports)
                if (placement.TryPlace(layer, def, attempts)) result.MobilityPlaced++;

            var profile = layer.Layout != null ? layer.Layout.Profile : LayoutProfile.Fallback;
            result.ThreatPlaced += PackPlacer.Place(layer, placement, threats, profile.ThreatPackSize,
                profile.ThreatPackRadius, attempts);
            result.FoodPlaced += PackPlacer.Place(layer, placement, food, profile.FoodPackSize,
                profile.FoodPackRadius, attempts);

            return result;
        }

        private static int CountChainThreats(LayerContext layer)
        {
            int count = 0;
            foreach (var node in layer.Chain)
                if (node.Harmful) count++;
            return count;
        }
    }
}
