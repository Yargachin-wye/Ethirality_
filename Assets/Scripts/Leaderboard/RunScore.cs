using System;
using System.Collections.Generic;

namespace Leaderboard
{
    // A local measurement, never evidence for a server leaderboard.
    public sealed class RunScore
    {
        public const string RulesetId = "openworld-height-v1";
        public const int UnitsPerHeight = 100;
        private readonly double _startY;
        private double _maxY;

        public long HeightUnits { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsValid { get; private set; } = true;

        public RunScore(double startY)
        {
            if (!IsFinite(startY)) throw new ArgumentOutOfRangeException(nameof(startY));
            _startY = _maxY = startY;
        }

        public void Observe(double y)
        {
            if (IsFinished || !IsValid) return;
            if (!IsFinite(y))
            {
                IsValid = false;
                return;
            }

            _maxY = Math.Max(_maxY, y);
            double units = Math.Floor((_maxY - _startY) * UnitsPerHeight);
            if (!IsFinite(units) || units >= long.MaxValue)
            {
                IsValid = false;
                return;
            }
            HeightUnits = (long)units;
        }

        public bool Finish(double y)
        {
            if (IsFinished) return false;
            Observe(y);
            IsFinished = true;
            return true;
        }

        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public enum RunEndReason
    {
        Death,
        SceneExit,
        ApplicationQuit
    }

    [Serializable]
    public sealed class LocalRunResult
    {
        public string runId;
        public string gameVersion;
        public string buildId;
        public string rulesetId;
        public int seed;
        public long heightUnits;
        public string finishedUtc;
        public RunEndReason endReason;
        public bool valid;
        public bool diagnostic;

        public bool CanBeLocalBest => valid && !diagnostic && endReason == RunEndReason.Death
            && heightUnits > 0 && !string.IsNullOrEmpty(runId)
            && !string.IsNullOrEmpty(gameVersion) && !string.IsNullOrEmpty(rulesetId);

        public bool SameTable(LocalRunResult other) => other != null
            && gameVersion == other.gameVersion && rulesetId == other.rulesetId && seed == other.seed;

        public LocalRunResult Copy() => (LocalRunResult)MemberwiseClone();
    }

    [Serializable]
    public sealed class LocalRunHistory
    {
        public int schemaVersion = 1;
        public List<LocalRunResult> bestResults = new List<LocalRunResult>();

        public long BestHeight(LocalRunResult table)
        {
            long best = 0;
            if (table == null || bestResults == null) return best;
            foreach (var result in bestResults)
                if (result != null && result.CanBeLocalBest && result.SameTable(table))
                    best = Math.Max(best, result.heightUnits);
            return best;
        }

        public bool Record(LocalRunResult result)
        {
            if (result == null || !result.CanBeLocalBest) return false;
            bestResults ??= new List<LocalRunResult>();
            if (bestResults.Exists(r => r != null && r.CanBeLocalBest && r.SameTable(result))
                && result.heightUnits <= BestHeight(result)) return false;

            bestResults.RemoveAll(r => r == null || !r.CanBeLocalBest || r.SameTable(result));
            bestResults.Add(result.Copy());
            // Bound local storage; this is not the public leaderboard archive.
            if (bestResults.Count > 64) bestResults.RemoveRange(0, bestResults.Count - 64);
            return true;
        }
    }
}
