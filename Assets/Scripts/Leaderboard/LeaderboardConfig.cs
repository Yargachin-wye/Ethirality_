using System;
using UnityEngine;

namespace Leaderboard
{
    // Project endpoint + anon key. Create one asset under
    // Assets/Resources/LeaderboardConfig.asset; on desktop builds the env
    // variables below take precedence so keys never live in code.
    [CreateAssetMenu(menuName = "Leaderboard/LeaderboardConfig")]
    public sealed class LeaderboardConfig : ScriptableObject
    {
        public const string EnvUrl = "LEADERBOARD_SUPABASE_URL";
        public const string EnvAnonKey = "LEADERBOARD_SUPABASE_ANON_KEY";

        [SerializeField] private string supabaseUrl;
        [SerializeField] private string anonKey;

        public string SupabaseUrl => supabaseUrl;
        public string AnonKey => anonKey;

        public bool IsValid =>
            !string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(anonKey);

        public static LeaderboardConfig Resolve()
        {
            string url = Environment.GetEnvironmentVariable(EnvUrl);
            string anon = Environment.GetEnvironmentVariable(EnvAnonKey);
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(anon))
            {
                var env = CreateInstance<LeaderboardConfig>();
                env.supabaseUrl = url.TrimEnd('/');
                env.anonKey = anon;
                return env;
            }
            var asset = Resources.Load<LeaderboardConfig>("LeaderboardConfig");
            return asset != null && asset.IsValid ? asset : null;
        }
    }
}
