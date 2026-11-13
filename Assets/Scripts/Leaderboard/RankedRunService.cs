using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace Leaderboard
{
    public enum RankedRunStatus
    {
        Inactive,
        Starting,
        Recording,
        Finishing,
        Verified,
        Rejected,
        Review,
        Failed
    }

    // A session granted by POST /runs/start; held only in memory.
    public sealed class RankedRunSession
    {
        public string runId;
        public string sessionKey;
        public int seed;
        public DateTime expiresAtUtc;
        public float fixedDeltaTime;
        public string leaderboardId;
    }

    // Orchestrates the ranked flow for OpenWorld runs: StartAsync before world
    // generation (seed/limits come from the server), FinishAsync after
    // OpenWorldRun.Finish. Static so both UI and GameplayManager can drive it.
    public static class RankedRunService
    {
        public static RankedRunSession Current { get; private set; }
        public static RankedRunStatus Status { get; private set; } = RankedRunStatus.Inactive;
        public static string LastRejectReason { get; private set; }
        public static long? VerifiedHeight { get; private set; }
        // runId of the most recently finished ranked run; lets panels show its
        // verdict after the session itself is consumed.
        public static string LastFinishedRunId { get; private set; }
        // Board of the most recent ranked session; still useful after finish.
        public static string LastLeaderboardId { get; private set; }
        public static bool HasActive => Current != null;

        public static event Action<RankedRunStatus> OnStatusChanged;

        [Serializable] private sealed class StartResponse
        {
            public string runId;
            public string leaderboardId;
            public int seed;
            public string sessionKey;
            public string expiresAt;
            public float fixedDeltaTime;
        }

        [Serializable] private sealed class FinishResponse
        {
            public string verdict;
            public string rejectReason;
            public long verifiedHeight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Status = RankedRunStatus.Inactive;
            LastRejectReason = null;
            VerifiedHeight = null;
            LastFinishedRunId = null;
            LastLeaderboardId = null;
            OnStatusChanged = null;
        }

        public static string DeviceHash() =>
            RunHashChain.Sha256Hex(SystemInfo.deviceUniqueIdentifier ?? "");

        // Application.buildGUID is empty in the editor; the seed row
        // 'dev-local' keeps editor play sessions working.
        private static string BuildId()
        {
#if UNITY_EDITOR
            return "dev-local";
#else
            return Application.buildGUID;
#endif
        }

        // Yield from a persistent MonoBehaviour (LeaderboardClient).
        // On success Current is set and must be passed to OpenWorldRun.Begin.
        public static IEnumerator StartAsync(string leaderboardId, string nickname,
            Action<bool> done)
        {
            var client = LeaderboardClient.Instance;
            if (client == null || !client.Configured || !client.IsSignedIn)
            {
                SetStatus(RankedRunStatus.Failed);
                done(false);
                yield break;
            }
            SetStatus(RankedRunStatus.Starting);
            var body = new StringBuilder(160);
            body.Append("{\"buildId\":\"").Append(BuildId()).Append('"');
            if (!string.IsNullOrEmpty(leaderboardId))
                body.Append(",\"leaderboardId\":\"").Append(leaderboardId).Append('"');
            if (!string.IsNullOrEmpty(nickname))
                body.Append(",\"nickname\":").Append(JsonEscape(nickname));
            body.Append(",\"deviceHash\":\"").Append(DeviceHash()).Append("\"}");

            LeaderboardClient.Result result = null;
            yield return client.SignedPost("runs/start", body.ToString(), r => result = r);
            if (result == null || !result.ok)
            {
                SetStatus(RankedRunStatus.Failed);
                done(false);
                yield break;
            }
            var parsed = JsonUtility.FromJson<StartResponse>(result.body);
            if (parsed == null || string.IsNullOrEmpty(parsed.runId))
            {
                SetStatus(RankedRunStatus.Failed);
                done(false);
                yield break;
            }
            LastLeaderboardId = parsed.leaderboardId;
            Current = new RankedRunSession
            {
                runId = parsed.runId,
                sessionKey = parsed.sessionKey,
                seed = parsed.seed,
                expiresAtUtc = DateTime.TryParse(parsed.expiresAt,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal, out var exp)
                    ? exp : DateTime.UtcNow.AddMinutes(30),
                fixedDeltaTime = parsed.fixedDeltaTime > 0 ? parsed.fixedDeltaTime : 0.02f,
                leaderboardId = parsed.leaderboardId
            };
            SetStatus(RankedRunStatus.Recording);
            done(true);
        }

        // Called after OpenWorldRun.Finish; queues the sealed tail + finish
        // body into the WAL so they survive the pending scene unload.
        public static IEnumerator FinishAsync(RunEndReason reason, Action done = null)
        {
            var session = Current;
            var client = LeaderboardClient.Instance;
            if (session == null || client == null)
            {
                done?.Invoke();
                yield break;
            }
            SetStatus(RankedRunStatus.Finishing);
            var recorder = RunRecorder.Active;
            recorder?.StopRecording();

            long heightUnits = LocalRunStore.LastResult != null
                && LocalRunStore.LastResult.runId == session.runId
                ? LocalRunStore.LastResult.heightUnits : 0;
            var finishBody = BuildFinishBody(reason, heightUnits,
                recorder != null ? recorder.CurrentTick : 0,
                recorder != null ? recorder.LastHash
                    : RunHashChain.GenesisHash(session.runId, session.sessionKey));
            string path = $"runs/{session.runId}/finish";
            client.Enqueue(path, finishBody, session.expiresAtUtc);

            // The WAL delivers chunks strictly before finish; posting finish
            // directly would overtake them and be verified without any chunks.
            float waited = 0f;
            while (client.PendingCount > 0 && waited < 20f)
            {
                waited += 0.25f;
                yield return new WaitForSecondsRealtime(0.25f);
            }

            LeaderboardClient.Result result = null;
            if (client.PendingCount == 0)
                yield return client.SignedGet($"runs/{session.runId}", r => result = r);
            if (result != null && result.ok)
            {
                var parsed = JsonUtility.FromJson<FinishResponse>(result.body);
                ApplyVerdict(parsed);
            }
            else if (result != null && (result.Gone || result.Conflict))
            {
                SetStatus(RankedRunStatus.Failed);
            }
            if (result == null || !result.ok)
                Debug.LogWarning($"Ranked finish not accepted: {result?.status} {result?.error ?? result?.body}");
            // Transport failure: the WAL keeps retrying until expiresAtUtc.
            LastFinishedRunId = session.runId;
            Current = null;
            done?.Invoke();
        }

        public static void Abandon()
        {
            Current = null;
            SetStatus(RankedRunStatus.Inactive);
        }

        private static void ApplyVerdict(FinishResponse parsed)
        {
            if (parsed == null) { SetStatus(RankedRunStatus.Failed); return; }
            LastRejectReason = parsed.rejectReason;
            VerifiedHeight = parsed.verifiedHeight > 0 ? parsed.verifiedHeight : (long?)null;
            switch (parsed.verdict)
            {
                case "verified": SetStatus(RankedRunStatus.Verified); break;
                case "rejected": SetStatus(RankedRunStatus.Rejected); break;
                case "review": SetStatus(RankedRunStatus.Review); break;
                case "pending": break;
                default: SetStatus(RankedRunStatus.Failed); break;
            }
        }

        private static string BuildFinishBody(RunEndReason reason,
            long heightUnits, long tick, string lastHash)
        {
            string endReason;
            switch (reason)
            {
                case RunEndReason.Death: endReason = "death"; break;
                case RunEndReason.SceneExit: endReason = "sceneExit"; break;
                default: endReason = "applicationQuit"; break;
            }
            var sb = new StringBuilder(160);
            sb.Append("{\"endReason\":\"").Append(endReason).Append('"');
            sb.Append(",\"heightUnits\":").Append(heightUnits);
            sb.Append(",\"lastHash\":\"").Append(lastHash).Append('"');
            sb.Append(",\"tick\":").Append(tick).Append('}');
            return sb.ToString();
        }

        private static void SetStatus(RankedRunStatus status)
        {
            Status = status;
            OnStatusChanged?.Invoke(status);
        }

        private static string JsonEscape(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char ch in value)
            {
                if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
                else if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4"));
                else sb.Append(ch);
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
