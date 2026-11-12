using System;
using Bootstrapper;
using CharacterComponents;
using UnityEngine;
using Utilities;

namespace Leaderboard
{
    // Created by the generator after the player's spawn position is known.
    public sealed class OpenWorldRun : MonoBehaviour
    {
        public static OpenWorldRun Active { get; private set; }
        public long HeightUnits => _score?.HeightUnits ?? 0;
        public string RunId => _result?.runId;
        public string GameVersion => _result?.gameVersion;
        public bool IsDiagnostic => _result != null && _result.diagnostic;
        private Character _player;
        private RunScore _score;
        private LocalRunResult _result;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active = null;

        public void Begin(Character player, int seed, bool diagnostic,
            RankedRunSession session = null)
        {
            if (GameSession.Instance == null || GameSession.Instance.State.Mode != GameMode.OpenWorld)
                throw new InvalidOperationException("Only OpenWorld runs can produce a leaderboard result.");
            if (_score != null) throw new InvalidOperationException("A run can only start once.");
            if (Active != null) Active.Finish(RunEndReason.SceneExit);
            _player = player;
            _score = new RunScore(player.rb2D.position.y);
            _result = new LocalRunResult
            {
                runId = session != null ? session.runId : Guid.NewGuid().ToString("N"),
                gameVersion = Application.version,
                buildId = Application.buildGUID,
                rulesetId = RunScore.RulesetId,
                seed = seed,
                diagnostic = diagnostic
            };
            LocalRunStore.ClearLastResult();
            Active = this;
            // Ranked runs stream evidence to the server; diagnostic runs never do.
            if (session != null && !diagnostic)
                player.gameObject.AddComponent<RunRecorder>().Begin(player, session);
        }

        private void FixedUpdate() => Observe();
        private void LateUpdate() => Observe();

        private void Observe()
        {
            if (_score == null || _score.IsFinished || _player == null || GameplayPause.IsPaused) return;
            // Rigidbody position avoids measuring an interpolated render position.
            _score.Observe(_player.rb2D.position.y);
        }

        public void Finish(RunEndReason reason)
        {
            if (_score == null || !_score.Finish(_player != null ? _player.rb2D.position.y : double.NaN))
                return;
            _result.heightUnits = _score.HeightUnits;
            _result.valid = _score.IsValid;
            _result.endReason = reason;
            _result.finishedUtc = DateTime.UtcNow.ToString("O");
            LocalRunStore.Finish(_result);
            if (Active == this) Active = null;
        }

        private void OnApplicationQuit() => Finish(RunEndReason.ApplicationQuit);
        private void OnDestroy() => Finish(RunEndReason.SceneExit);
    }
}
