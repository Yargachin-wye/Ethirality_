using System;
using System.Collections.Generic;
using Bootstrapper;
using CharacterComponents;
using CharacterComponents.CharacterStat;
using CharacterComponents.Moving;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Improvement;
using UnityEngine;
using Utilities;

namespace Leaderboard
{
    // Records a ranked run: a checkpoint every ~2s of unpaused game time,
    // MessageBroker/Stats events, and a per-chunk input histogram fed by
    // PlayerControllable through RecordInput. Chunks are sealed with the
    // SHA-256 chain + HMAC and handed to the LeaderboardClient WAL.
    [DefaultExecutionOrder(-19900)]
    public sealed class RunRecorder : MonoBehaviour
    {
        public const float CheckpointSeconds = 2f;
        public const long ChunkTicks = 600; // ~12s at the default fixedDeltaTime

        public static RunRecorder Active { get; private set; }
        public static bool IsRecording => Active != null && Active._recording;

        // Input histogram hook from PlayerControllable; mirrors the probe's
        // TryQueue shape but never swallows the input.
        public static void RecordInput(PlayerControllable source, RunInput input)
        {
            var recorder = Active;
            if (recorder == null || !recorder._recording || recorder._control != source) return;
            if (GameplayPause.IsPaused) return;
            recorder._current.inputHistogram.Add(input.kind);
        }

        private Character _player;
        private PlayerControllable _control;
        private LumpMeatMovable _movement;
        private Stats _stats;
        private RankedRunSession _session;
        private byte[] _sessionKeyBytes;
        private float _startTime;
        private float _unpausedSeconds;
        private long _chunkStartTick;
        private int _seq;
        private string _prevHash;
        private string _lastHash;
        private int _lastHp;
        private bool _recording;
        private RunChunk _current;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active = null;

        public long CurrentTick =>
            (long)Math.Floor((Time.time - _startTime) / _session.fixedDeltaTime);
        public string LastHash => _lastHash ?? _prevHash;

        public void Begin(Character player, RankedRunSession session)
        {
            if (session == null || string.IsNullOrEmpty(session.sessionKey))
                throw new ArgumentException("A ranked session is required.", nameof(session));
            _player = player;
            _session = session;
            _control = player.GetComponent<PlayerControllable>();
            _movement = player.GetComponent<LumpMeatMovable>();
            _stats = player.Stats;
            _sessionKeyBytes = RunHashChain.HexToBytes(session.sessionKey);
            _prevHash = RunHashChain.GenesisHash(session.runId, session.sessionKey);
            _lastHash = null;
            _startTime = Time.time;
            _unpausedSeconds = 0;
            _chunkStartTick = 0;
            _seq = 0;
            _lastHp = _stats != null ? _stats.CurrentHealth : 0;
            _current = NewChunk();

            if (_stats != null)
            {
                _stats.OnDmgAction += OnDamaged;
                _stats.OnCureAction += OnCured;
                _stats.OnDeadAction += OnDead;
            }
            MessageBroker.Default.Receive<AddNewImprovementEvent>()
                .Subscribe(e => AddEvent(new RunEventRecord
                {
                    type = RunEventTypes.Upgrade,
                    tick = CurrentTick,
                    upgradeId = e.Definition != null ? e.Definition.ResId : (int?)null
                }))
                .AddTo(_subscriptions);
            MessageBroker.Default.Receive<GameplayPauseChangedEvent>()
                .Subscribe(e => AddEvent(new RunEventRecord
                {
                    type = e.IsPaused ? RunEventTypes.PauseStart : RunEventTypes.PauseEnd,
                    tick = CurrentTick
                }))
                .AddTo(_subscriptions);

            _recording = true;
            Active = this;
        }

        private void Update()
        {
            if (!_recording || GameplayPause.IsPaused || _player == null) return;
            _unpausedSeconds += Time.deltaTime;
            if (_unpausedSeconds < CheckpointSeconds) return;
            _unpausedSeconds = 0;
            _current.checkpoints.Add(new RunCheckpointEntry
            {
                tick = CurrentTick,
                state = Capture()
            });
            if (CurrentTick - _chunkStartTick >= ChunkTicks) SealCurrent();
        }

        // A dead or removed player still yields a final checkpoint.
        private RunCheckpoint Capture()
        {
            var body = _player != null ? _player.rb2D : null;
            int hp = Mathf.Max(0, _stats != null ? _stats.CurrentHealth : _lastHp);
            return new RunCheckpoint
            {
                x = body != null ? body.position.x : 0,
                y = body != null ? body.position.y : 0,
                vx = body != null ? body.velocity.x : 0,
                vy = body != null ? body.velocity.y : 0,
                rotation = body != null ? body.rotation : 0,
                angularVelocity = body != null ? body.angularVelocity : 0,
                hp = hp,
                alive = _player != null && _player.gameObject.activeInHierarchy
                    && _stats != null && _stats.CurrentHealth > 0,
                frozen = _movement != null && _movement.IsFreeze,
                dashing = _movement != null && _movement.IsDashing,
                dashCooldown = _movement != null ? _movement.DashDelayTimer : 0,
                upgrades = GameSession.Instance != null
                    ? GameSession.Instance.State.UpgradeIds.ToArray()
                    : new int[0]
            };
        }

        private void OnDamaged(int ignored)
        {
            AddEvent(new RunEventRecord
            {
                type = RunEventTypes.Damage,
                tick = CurrentTick,
                amount = Mathf.Max(0, _lastHp - _stats.CurrentHealth)
            });
            _lastHp = _stats.CurrentHealth;
        }

        private void OnCured(int ignored)
        {
            AddEvent(new RunEventRecord
            {
                type = RunEventTypes.Heal,
                tick = CurrentTick,
                amount = Mathf.Max(0, _stats.CurrentHealth - _lastHp)
            });
            _lastHp = _stats.CurrentHealth;
        }

        private void OnDead()
        {
            if (!_recording) return;
            // Stats.Dead swallows the lethal OnDmgAction, so the killing blow
            // is reported here; the death event stays last (P4).
            if (_lastHp > 0)
                AddEvent(new RunEventRecord
                {
                    type = RunEventTypes.Damage, tick = CurrentTick, amount = _lastHp
                });
            _lastHp = 0;
            _current.checkpoints.Add(new RunCheckpointEntry
            {
                tick = CurrentTick,
                state = Capture()
            });
            AddEvent(new RunEventRecord { type = RunEventTypes.Death, tick = CurrentTick });
            SealCurrent();
        }

        private void AddEvent(RunEventRecord record)
        {
            if (!_recording) return;
            _current.events.Add(record);
        }

        private RunChunk NewChunk() => new RunChunk
        {
            seq = _seq,
            prevHash = _prevHash
        };

        // Seals the buffered chunk if it carries anything and queues it for
        // delivery; the client WAL resends it until the session expires.
        public void SealCurrent()
        {
            if (_current.checkpoints.Count == 0 && _current.events.Count == 0
                && _current.inputHistogram.look == 0 && _current.inputHistogram.dash == 0
                && _current.inputHistogram.freeze == 0 && _current.inputHistogram.shot == 0)
                return;
            _current.tick = CurrentTick;
            _lastHash = RunHashChain.Seal(_current, _sessionKeyBytes);
            var client = LeaderboardClient.Instance;
            if (client != null)
                client.Enqueue($"runs/{_session.runId}/chunks",
                    RunChunkJson.ChunkToCanonical(_current), _session.expiresAtUtc);
            _seq++;
            _chunkStartTick = _current.tick;
            _prevHash = _lastHash;
            _current = NewChunk();
        }

        // Called by RankedRunService before the finish request is queued.
        public void StopRecording()
        {
            if (!_recording) return;
            _recording = false;
            SealCurrent();
            _subscriptions.Dispose();
            if (_stats != null)
            {
                _stats.OnDmgAction -= OnDamaged;
                _stats.OnCureAction -= OnCured;
                _stats.OnDeadAction -= OnDead;
            }
        }

        private void OnDestroy()
        {
            StopRecording();
            if (Active == this) Active = null;
        }
    }
}
