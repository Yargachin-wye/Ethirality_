using System;
using System.IO;
using UnityEngine;

namespace Leaderboard
{
    public static class LocalRunStore
    {
        private static LocalRunHistory _history;
        public static LocalRunResult LastResult { get; private set; }
        public static bool LastWasPersonalBest { get; private set; }
        public static string FilePath => Path.Combine(Application.persistentDataPath, "local-run-results.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _history = null;
            LastResult = null;
            LastWasPersonalBest = false;
        }

        public static void ClearLastResult()
        {
            LastResult = null;
            LastWasPersonalBest = false;
        }

        public static long BestHeight(LocalRunResult table)
        {
            EnsureLoaded();
            return _history.BestHeight(table);
        }

        public static void Finish(LocalRunResult result)
        {
            EnsureLoaded();
            LastResult = result.Copy();
            LastWasPersonalBest = _history.Record(result);
            if (!LastWasPersonalBest) return;

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(_history, true));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is NotSupportedException)
            {
                Debug.LogWarning($"Could not save local run results: {e.Message}");
            }
        }

        private static void EnsureLoaded()
        {
            if (_history != null) return;
            _history = Read(FilePath) ?? Read(FilePath + ".bak") ?? new LocalRunHistory();
        }

        private static LocalRunHistory Read(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024) return null;
                var history = JsonUtility.FromJson<LocalRunHistory>(File.ReadAllText(path));
                if (history == null || history.schemaVersion != 1 || history.bestResults == null
                    || history.bestResults.Count > 64) return null;
                return history;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is ArgumentException)
            {
                Debug.LogWarning($"Could not read local run results: {e.Message}");
                return null;
            }
        }
    }
}
