namespace Generation
{
    public static class RewardPlanner
    {
        private const int RetryMultiplier = 5;

        public static int Generate(LayerContext layer, RewardSettings settings, PlacementRules placement, int attempts)
        {
            if (layer == null || settings == null || !settings.IsRewardLayer(layer.Index)) return 0;

            placement.Reset();
            foreach (var spawn in layer.Spawns)
                placement.Register(spawn.Definition, spawn.Position);

            int count = layer.Random.Next(settings.MinRewards, settings.MaxRewards + 1);
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                if (placement.TryPlace(layer, settings.RewardDefinition, attempts) ||
                    placement.TryPlace(layer, settings.RewardDefinition, attempts * RetryMultiplier))
                    placed++;
            }

            return placed;
        }
    }
}
