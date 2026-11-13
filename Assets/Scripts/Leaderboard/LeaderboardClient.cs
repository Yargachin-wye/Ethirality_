using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Leaderboard
{
    // UnityWebRequest + Supabase email-OTP auth + a WAL outbox in
    // persistentDataPath so chunks cut off by a crash or network loss are
    // re-sent until the session expires. Nothing here blocks the UI.
    public sealed class LeaderboardClient : MonoBehaviour
    {
        public static LeaderboardClient Instance { get; private set; }
        public bool IsReady => !string.IsNullOrEmpty(_accessToken);
        public int PendingCount => _pending.Count;

        public bool IsSignedIn => !string.IsNullOrEmpty(_refreshToken);
        public string Email { get; private set; }
        public event Action OnAuthChanged;

        private LeaderboardConfig _config;
        private string _accessToken;
        private string _refreshToken;
        private DateTime _accessExpiresUtc;
        private bool _refreshing;
        private readonly List<WalEntry> _pending = new List<WalEntry>();
        private Coroutine _pump;

        private static string WalPath =>
            Path.Combine(Application.persistentDataPath, "ranked-run-wal.json");

        [Serializable] private sealed class WalEntry
        {
            public string path;
            public string body;
            public string expiresAtUtc;
        }

        [Serializable] private sealed class WalFile
        {
            public List<WalEntry> entries = new List<WalEntry>();
        }

        [Serializable] private sealed class AuthUser
        {
            public string email;
        }

        [Serializable] private sealed class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public AuthUser user;
        }

        [Serializable] private sealed class OtpRequest
        {
            public string email;
            public bool create_user;
        }

        [Serializable] private sealed class VerifyRequest
        {
            public string type;
            public string email;
            public string token;
        }

        [Serializable] private sealed class RefreshRequest
        {
            public string refresh_token;
        }

        [Serializable] private sealed class ErrorResponse
        {
            public string error;
            public string message;
        }

        public sealed class Result
        {
            public bool ok;
            public long status;
            public string body;
            public string error; // transport-level error, not HTTP status
            public bool Conflict => status == 409;
            public bool Gone => status == 410;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // Self-spawning persistent service; a scene may also place it manually.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
                new GameObject(nameof(LeaderboardClient), typeof(LeaderboardClient));
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("More than one LeaderboardClient.");
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _config = LeaderboardConfig.Resolve();
            var saved = AuthSessionStore.Load();
            if (saved != null) { _refreshToken = saved.refreshToken; Email = saved.email; }
            LoadWal();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Configured => _config != null;

        // ---------- auth

        // Passwordless sign-in: RequestCode mails a 6-digit code, VerifyCode
        // exchanges it for a session. Only a refresh token is persisted.
        public IEnumerator RequestCode(string email, Action<Result> done)
        {
            if (_config == null) { done(new Result { error = "not configured" }); yield break; }
            var req = Request("POST", _config.SupabaseUrl + "/auth/v1/otp",
                JsonUtility.ToJson(new OtpRequest { email = email, create_user = true }));
            yield return req.SendWebRequest();
            done(Collect(req));
        }

        public IEnumerator VerifyCode(string email, string code, Action<Result> done)
        {
            if (_config == null) { done(new Result { error = "not configured" }); yield break; }
            var req = Request("POST", _config.SupabaseUrl + "/auth/v1/verify",
                JsonUtility.ToJson(new VerifyRequest { type = "email", email = email, token = code }));
            yield return req.SendWebRequest();
            var result = Collect(req);
            if (result.ok) ApplySession(JsonUtility.FromJson<AuthResponse>(result.body), email);
            done(result);
        }

        public IEnumerator SignOut()
        {
            if (_config != null && IsReady)
            {
                // scope=local ends only this device's session.
                var req = Request("POST", _config.SupabaseUrl + "/auth/v1/logout?scope=local", null, true);
                yield return req.SendWebRequest();
                req.Dispose();
            }
            ClearSession();
        }

        private bool HasFreshAccessToken =>
            IsReady && DateTime.UtcNow < _accessExpiresUtc.AddSeconds(-30);

        private void ApplySession(AuthResponse auth, string fallbackEmail)
        {
            if (auth == null || string.IsNullOrEmpty(auth.access_token)
                || string.IsNullOrEmpty(auth.refresh_token)) return;
            _accessToken = auth.access_token;
            _refreshToken = auth.refresh_token;
            _accessExpiresUtc = DateTime.UtcNow.AddSeconds(auth.expires_in > 0 ? auth.expires_in : 3600);
            string email = auth.user != null && !string.IsNullOrEmpty(auth.user.email)
                ? auth.user.email : fallbackEmail ?? Email;
            bool changed = email != Email;
            Email = email;
            AuthSessionStore.Save(_refreshToken, Email);
            if (changed) OnAuthChanged?.Invoke();
        }

        private void ClearSession()
        {
            _accessToken = null;
            _refreshToken = null;
            Email = null;
            AuthSessionStore.Clear();
            OnAuthChanged?.Invoke();
        }

        private IEnumerator EnsureSignedIn(Action<bool> done)
        {
            if (_config == null) { done(false); yield break; }
            if (HasFreshAccessToken) { done(true); yield break; }
            if (!IsSignedIn) { done(false); yield break; }
            yield return Refresh(done);
        }

        // Serialized so parallel calls never spend a rotating refresh token twice.
        private IEnumerator Refresh(Action<bool> done)
        {
            if (_refreshing)
            {
                while (_refreshing) yield return null;
                done(HasFreshAccessToken);
                yield break;
            }
            _refreshing = true;
            var req = Request("POST", _config.SupabaseUrl + "/auth/v1/token?grant_type=refresh_token",
                JsonUtility.ToJson(new RefreshRequest { refresh_token = _refreshToken }));
            yield return req.SendWebRequest();
            var result = Collect(req);
            if (result.ok) ApplySession(JsonUtility.FromJson<AuthResponse>(result.body), null);
            // A refused token (revoked, expired, reused) cannot recover: sign out.
            else if (result.status >= 400 && result.status < 500 && result.status != 429)
                ClearSession();
            _accessToken = result.ok ? _accessToken : null;
            _refreshing = false;
            done(result.ok && HasFreshAccessToken);
        }

        // ---------- public calls (yielded from service code)

        public IEnumerator SignedPost(string path, string body, Action<Result> done)
        {
            bool authed = false;
            yield return EnsureSignedIn(v => authed = v);
            if (!authed)
            {
                done(new Result { error = "sign-in failed" });
                yield break;
            }
            var req = Request("POST", FunctionsUrl(path), body, true);
            yield return req.SendWebRequest();
            var result = Collect(req);
            if (result.status == 401 && !IsPayloadRejection(result))
            {
                _accessToken = null; // token expired; re-auth once
                yield return EnsureSignedIn(v => authed = v);
                if (!authed) { done(result); yield break; }
                req = Request("POST", FunctionsUrl(path), body, true);
                yield return req.SendWebRequest();
                result = Collect(req);
            }
            done(result);
        }

        public IEnumerator SignedGet(string path, Action<Result> done)
        {
            bool authed = false;
            yield return EnsureSignedIn(v => authed = v);
            if (!authed)
            {
                done(new Result { error = "sign-in failed" });
                yield break;
            }
            var req = Request("GET", FunctionsUrl(path), null, true);
            yield return req.SendWebRequest();
            var result = Collect(req);
            if (result.status == 401 && !IsPayloadRejection(result))
            {
                _accessToken = null;
                yield return EnsureSignedIn(v => authed = v);
                if (!authed) { done(result); yield break; }
                req = Request("GET", FunctionsUrl(path), null, true);
                yield return req.SendWebRequest();
                result = Collect(req);
            }
            done(result);
        }

        // The server also answers 401 "bad_hmac" for a chunk it refuses; that
        // is not an expired session and must not trigger a new anonymous user.
        private static bool IsPayloadRejection(Result result) =>
            result.body != null && result.body.Contains("bad_hmac");

        // Queues a request body into the WAL and lets the pump deliver it.
        // expiresAtUtc bounds how long resends stay meaningful.
        public void Enqueue(string path, string body, DateTime expiresAtUtc)
        {
            _pending.Add(new WalEntry
            {
                path = path, body = body, expiresAtUtc = expiresAtUtc.ToString("O")
            });
            SaveWal();
            if (_pump == null) _pump = StartCoroutine(Pump());
        }

        private IEnumerator Pump()
        {
            var wait = new WaitForSecondsRealtime(5f);
            while (_pending.Count > 0)
            {
                var entry = _pending[0];
                if (DateTime.TryParse(entry.expiresAtUtc, out var expiry) && DateTime.UtcNow > expiry)
                {
                    _pending.RemoveAt(0);
                    SaveWal();
                    continue;
                }
                Result result = null;
                yield return SignedPost(entry.path, entry.body, r => result = r);
                if (result == null) yield break;
                if (result.ok || result.status == 204 || result.Conflict || result.status == 401
                    || result.status == 403)
                {
                    // Delivered, idempotent replay, or permanently refused.
                    if (!result.ok)
                        Debug.LogWarning($"LeaderboardClient dropped {entry.path}: {result.status} {result.body}");
                    _pending.RemoveAt(0);
                    SaveWal();
                }
                else yield return wait;
            }
            _pump = null;
        }

        // ---------- WAL persistence

        private void LoadWal()
        {
            try
            {
                if (!File.Exists(WalPath)) return;
                var file = JsonUtility.FromJson<WalFile>(File.ReadAllText(WalPath));
                if (file?.entries != null) _pending.AddRange(file.entries);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is ArgumentException)
            {
                Debug.LogWarning($"LeaderboardClient could not read WAL: {e.Message}");
            }
        }

        private void SaveWal()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = WalPath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(
                    new WalFile { entries = new List<WalEntry>(_pending) }));
                if (File.Exists(WalPath)) File.Replace(temporary, WalPath, WalPath + ".bak");
                else File.Move(temporary, WalPath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException
                || e is NotSupportedException)
            {
                Debug.LogWarning($"LeaderboardClient could not write WAL: {e.Message}");
            }
        }

        // ---------- HTTP plumbing

        private string FunctionsUrl(string path) =>
            _config.SupabaseUrl + "/functions/v1/" + path;

        private UnityWebRequest Request(string method, string url, string body, bool authed = false)
        {
            var req = new UnityWebRequest(url, method);
            if (body != null)
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", _config.AnonKey);
            req.SetRequestHeader("Authorization",
                "Bearer " + (authed && IsReady ? _accessToken : _config.AnonKey));
            return req;
        }

        private static Result Collect(UnityWebRequest req)
        {
            var result = new Result { status = req.responseCode };
            if (req.result == UnityWebRequest.Result.Success)
            {
                result.ok = true;
                result.body = req.downloadHandler?.text;
            }
            else if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                result.body = req.downloadHandler?.text;
            }
            else
            {
                result.error = req.error;
            }
            req.Dispose();
            return result;
        }
    }
}
