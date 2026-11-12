#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Bootstrapper;
using CharacterComponents;
using CharacterComponents.Moving;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Leaderboard
{
    [Serializable]
    public sealed class RunTraceFrame
    {
        public RunCheckpoint beforeInput;
        public List<RunInput> inputs = new List<RunInput>();
    }

    [Serializable]
    public sealed class RunTrace
    {
        public int protocolVersion = 1;
        public string gameVersion, rulesetId, unityVersion, contentFingerprint;
        public float fixedDeltaTime;
        public int seed;
        public Random.State initialRandomState;
        public int initialHp;
        public string stopReason;
        public List<RunTraceFrame> frames = new List<RunTraceFrame>();

        public void Validate(string fingerprint)
        {
            if (protocolVersion != 1 || gameVersion != Application.version
                || rulesetId != RunScore.RulesetId || unityVersion != Application.unityVersion
                || contentFingerprint != fingerprint || fixedDeltaTime != Time.fixedDeltaTime)
                throw new InvalidDataException("Trace version, content or physics step does not match this project.");
            if (GameSession.Instance == null || initialHp != GameSession.Instance.Session.StartingHp
                || frames == null || frames.Count == 0 || frames.Count > RunReplayProbe.MaxFrames)
                throw new InvalidDataException("Invalid initial state or trace length.");
            foreach (var frame in frames)
            {
                if (frame == null || frame.beforeInput == null || !frame.beforeInput.IsValid
                    || frame.inputs == null || frame.inputs.Count > RunReplayProbe.MaxInputsPerFrame)
                    throw new InvalidDataException("Invalid checkpoint or input count.");
                foreach (var input in frame.inputs)
                    if (!input.IsValid) throw new InvalidDataException("Invalid input.");
            }
        }
    }

    [Serializable]
    public sealed class RunReplayReport
    {
        public string outcome;
        public string detail;
        public int checkedFrames;
        public int recordedFrames;
        public RunCheckpoint expected;
        public RunCheckpoint actual;
    }

    // An Editor experiment, NOT a server verifier. Ordinary gameplay never queues its inputs here.
    [DefaultExecutionOrder(-20000)]
    public sealed class RunReplayProbe : MonoBehaviour
    {
        public const int MaxFrames = 3000;
        public const int MaxInputsPerFrame = 128;
        public static RunReplayProbe Active { get; private set; }
        public static string TracePath => Path.Combine(Application.persistentDataPath, "run-probe.json");
        public static string ReportPath => Path.Combine(Application.persistentDataPath, "run-probe-report.json");
        public static bool IsArmed => _armed;
        private static bool _armed;
        private static RunTrace _replay;
        private static string _fingerprint;
        private static bool _usedThisPlaySession;
        private readonly List<RunInput> _pending = new List<RunInput>();
        private RunTrace _trace;
        private bool _playingBack, _finished, _started;
        private int _frame;
        private Character _player;
        private PlayerControllable _control;
        private LumpMeatMovable _movement;
        private IDisposable _pauseSubscription;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            _armed = false;
            _replay = null;
            _fingerprint = null;
            _usedThisPlaySession = false;
        }

        public static void Arm(string fingerprint, RunTrace replay = null)
        {
            if (!Application.isPlaying || Active != null || OpenWorldRun.Active != null
                || _usedThisPlaySession)
                throw new InvalidOperationException("Start a fresh Play session and arm the probe in the menu.");
            replay?.Validate(fingerprint);
            _fingerprint = fingerprint;
            _replay = replay;
            _armed = true;
        }

        public static void CancelArmed()
        {
            _armed = false;
            _replay = null;
        }

        public static RunReplayProbe Prepare(GameObject host, ref int seed)
        {
            if (!_armed) return null;
            _armed = false;
            if (GameSession.Instance == null || GameSession.Instance.State.Mode != GameMode.OpenWorld)
            {
                Debug.LogError("Run probe needs a fresh OpenWorld run started from Boot.");
                return null;
            }
            _usedThisPlaySession = true;
            var probe = host.AddComponent<RunReplayProbe>();
            Active = probe;
            probe._playingBack = _replay != null;
            probe._trace = _replay ?? new RunTrace
            {
                gameVersion = Application.version,
                rulesetId = RunScore.RulesetId,
                unityVersion = Application.unityVersion,
                contentFingerprint = _fingerprint,
                fixedDeltaTime = Time.fixedDeltaTime,
                seed = seed,
                initialRandomState = Random.state,
                initialHp = GameSession.Instance.State.Hp
            };
            _replay = null;
            seed = probe._trace.seed;
            // Restore once, before world generation. Never correct a replay from later snapshots.
            Random.state = probe._trace.initialRandomState;
            return probe;
        }

        public void Begin(Character player)
        {
            _player = player;
            _control = player.GetComponent<PlayerControllable>();
            _movement = player.GetComponent<LumpMeatMovable>();
            if (_control == null || _movement == null)
            {
                EndSegment("missing player components");
                return;
            }
            _started = true;
            _pauseSubscription = MessageBroker.Default.Receive<GameplayPauseChangedEvent>()
                .Subscribe(e =>
                {
                    if (e.IsPaused) EndSegment("pause/selection boundary");
                });
        }

        public static bool TryQueue(PlayerControllable source, RunInput input)
        {
            var probe = Active;
            if (probe == null || probe._control != source) return false;
            // Block live player input throughout a diagnostic replay, including after its report.
            if (probe._playingBack) return true;
            if (!probe._started || probe._finished) return false;
            if (probe._pending.Count >= MaxInputsPerFrame)
            {
                probe.EndSegment("input limit");
                return true;
            }
            probe._pending.Add(input);
            return true;
        }

        private void FixedUpdate()
        {
            if (!_started || _finished || GameplayPause.IsPaused) return;
            if (_playingBack)
            {
                if (_frame == _trace.frames.Count)
                {
                    Report("sampled-states-match", "Only the recorded player checkpoints were compared.");
                    return;
                }
                var frame = _trace.frames[_frame];
                var actual = Capture();
                string difference = frame.beforeInput.Difference(actual);
                if (difference != null)
                {
                    Report("diverged", difference, frame.beforeInput, actual);
                    return;
                }
                _frame++;
                foreach (var input in frame.inputs) _control.ApplyRunInput(input);
            }
            else
            {
                if (_trace.frames.Count == MaxFrames)
                {
                    EndSegment("frame limit");
                    return;
                }
                var frame = new RunTraceFrame { beforeInput = Capture(), inputs = new List<RunInput>(_pending) };
                _pending.Clear();
                _trace.frames.Add(frame);
                foreach (var input in frame.inputs) _control.ApplyRunInput(input);
            }
        }

        private RunCheckpoint Capture()
        {
            var body = _player.rb2D;
            return new RunCheckpoint
            {
                x = body.position.x, y = body.position.y,
                vx = body.velocity.x, vy = body.velocity.y,
                rotation = body.rotation, angularVelocity = body.angularVelocity,
                hp = _player.Stats.CurrentHealth, alive = _player.gameObject.activeInHierarchy,
                frozen = _movement.IsFreeze, dashing = _movement.IsDashing,
                dashCooldown = _movement.DashDelayTimer,
                upgrades = GameSession.Instance.State.UpgradeIds.ToArray()
            };
        }

        public void EndSegment(string reason)
        {
            if (_finished) return;
            if (_playingBack)
            {
                Report(_frame == _trace.frames.Count ? "sampled-states-match" : "ended-early",
                    $"{reason}; compared {_frame} of {_trace.frames.Count} checkpoints.");
                return;
            }
            _finished = true;
            _pending.Clear();
            _trace.stopReason = reason;
            WriteJson(TracePath, _trace);
            Debug.Log($"Run probe: recorded {_trace.frames.Count} ticks ({reason}). {TracePath}");
        }

        private void Report(string outcome, string detail, RunCheckpoint expected = null, RunCheckpoint actual = null)
        {
            _finished = true;
            WriteJson(ReportPath, new RunReplayReport
            {
                outcome = outcome, detail = detail, checkedFrames = _frame,
                recordedFrames = _trace.frames.Count, expected = expected, actual = actual
            });
            Debug.Log($"Run probe: {outcome} at tick {_frame}. {detail} Report: {ReportPath}");
            UnityEditor.EditorApplication.isPaused = true;
        }

        private static void WriteJson(string path, object value)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(path, JsonUtility.ToJson(value, true));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"Run probe could not write file: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            _pauseSubscription?.Dispose();
            if (!_finished && _trace != null && !_playingBack) EndSegment("scene exit");
            if (Active == this) Active = null;
        }
    }
}
#endif
