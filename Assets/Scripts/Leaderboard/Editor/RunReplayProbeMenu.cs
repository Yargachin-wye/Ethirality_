using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Leaderboard.Editor
{
    public static class RunReplayProbeMenu
    {
        [MenuItem("Tools/Leaderboard Probe/Record Next OpenWorld")]
        private static void Record() => TryArm(false);

        [MenuItem("Tools/Leaderboard Probe/Replay Last Trace In Next OpenWorld")]
        private static void Replay() => TryArm(true);

        private static void TryArm(bool replay)
        {
            try
            {
                if (!Application.isPlaying)
                    throw new InvalidOperationException("Enter Play from Boot, then select this command in the game menu.");
                string fingerprint = ContentFingerprint();
                RunTrace trace = null;
                if (replay)
                {
                    var info = new FileInfo(RunReplayProbe.TracePath);
                    if (!info.Exists || info.Length > 16 * 1024 * 1024)
                        throw new InvalidDataException("Trace is missing or exceeds 16 MB.");
                    trace = JsonUtility.FromJson<RunTrace>(File.ReadAllText(info.FullName));
                    if (trace == null) throw new InvalidDataException("Trace is empty.");
                }
                RunReplayProbe.Arm(fingerprint, trace);
                Debug.Log($"Run probe armed for {(replay ? "replay" : "recording")}. Start OpenWorld from the game menu.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not arm run probe: {e.Message}");
            }
        }

        [MenuItem("Tools/Leaderboard Probe/Stop Recording")]
        private static void StopRecording() => RunReplayProbe.Active?.EndSegment("manual boundary");

        [MenuItem("Tools/Leaderboard Probe/Cancel Pending Probe")]
        private static void Cancel() => RunReplayProbe.CancelArmed();

        [MenuItem("Tools/Leaderboard Probe/Open Output Folder")]
        private static void OpenFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        // Conservative local compatibility check, not an attestation of a genuine client.
        // Includes runtime scripts, gameplay assets and physics/package settings.
        private static string ContentFingerprint()
        {
            string[] roots = { "Assets/Scripts", "Assets/Infos", "Assets/Prefabs", "Assets/Scenes",
                "Assets/Data", "ProjectSettings", "Packages" };
            using var hash = SHA256.Create();
            foreach (string path in roots.SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                byte[] name = Encoding.UTF8.GetBytes(path.Replace('\\', '/') + "\n");
                hash.TransformBlock(name, 0, name.Length, null, 0);
                byte[] content = File.ReadAllBytes(path);
                hash.TransformBlock(content, 0, content.Length, null, 0);
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(hash.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
