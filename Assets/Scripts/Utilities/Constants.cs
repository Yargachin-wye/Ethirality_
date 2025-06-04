namespace Utilities
{
    public static class UiConst
    {
        public const string LevelUp = "LevelUpPanel";
        public const string MainMenu = "MainMenuPanel";
        public const string GamePlay = "GamePlayPanel";
        public const string Settings = "SettingsPanel";
        public const string GameOver = "GameOverPanel";
        public const string ChoosingNextLevel = "ChoosingNextLevelPanel";

        public static readonly string[] Panels = { LevelUp, MainMenu, GamePlay, Settings, GameOver, ChoosingNextLevel };
    }

    public static class ImprovementsConst
    {
        public const string DashUp = "DashUp";
        public const string ArrowUp = "ArrowUp";
        public const string JawUp = "JawUp";

        public const string Shield = "Shield";
        public const string Spike = "Spike";

        public static readonly string[] AllImps =
        {
            DashUp, ArrowUp, JawUp,
            Shield, Spike
        };
    }
}