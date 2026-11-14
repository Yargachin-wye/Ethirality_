using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Constants;
using UnityEditor;
using UnityEngine;

namespace Audio.Editor
{
    public static class AudioLibraryTools
    {
        [MenuItem("Tools/Audio/Autofill Library")]
        private static void Autofill()
        {
            var library = FindLibrary();
            if (library == null) return;

            Undo.RecordObject(library, "Autofill Audio Library");

            int set = 0;
            var created = new List<string>();
            var missing = new List<string>();

            foreach (string name in AudioConst.AllSounds)
            {
                AudioClip clip = FindClip(name);
                var entry = FindEntry(library, name);
                if (entry == null)
                {
                    if (clip == null)
                    {
                        missing.Add(name);
                        continue;
                    }
                    entry = new AudioLibrary.SoundEntry { name = name, pitchFactor = 0.1f };
                    bool isUi = name.StartsWith("Ui", StringComparison.Ordinal);
                    (isUi ? library.uiSounds : library.vfxSounds).Add(entry);
                    created.Add($"{name} -> {(isUi ? "uiSounds" : "vfxSounds")}");
                }
                if (clip == null)
                {
                    if (entry.clip == null) missing.Add(name);
                    continue;
                }
                if (entry.clip != clip)
                {
                    entry.clip = clip;
                    set++;
                }
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            var sb = new StringBuilder($"[AudioLibraryTools] Автозаполнение: проставлено клипов {set}, создано записей {created.Count}.");
            if (created.Count > 0) sb.Append($"\n  новые записи: {string.Join(", ", created)}");
            if (missing.Count > 0) sb.Append($"\n  без клипа на диске: {string.Join(", ", missing)}");
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Audio/Report Missing")]
        private static void ReportMissing()
        {
            var library = FindLibrary();
            if (library == null) return;

            var entryByName = new Dictionary<string, AudioLibrary.SoundEntry>();
            foreach (var e in library.uiSounds.Concat(library.vfxSounds).Concat(library.musicTracks))
                if (!entryByName.ContainsKey(e.name))
                    entryByName[e.name] = e;

            var noEntry = new List<string>();
            var noClip = new List<string>();
            foreach (string name in AudioConst.AllSounds)
            {
                if (!entryByName.TryGetValue(name, out var entry)) noEntry.Add(name);
                else if (entry.clip == null) noClip.Add(name);
            }

            var prefabSounds = CollectPrefabSounds();
            var prefabMissing = prefabSounds
                .Where(kv => !entryByName.TryGetValue(kv.Key, out var e) || e.clip == null)
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToList();

            var unknownEntries = entryByName.Keys
                .Except(AudioConst.AllSounds)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder("[AudioLibraryTools] Отчёт по звукам:");
            AppendList(sb, "в AllSounds нет записи в библиотеке", noEntry);
            AppendList(sb, "записи в библиотеке без клипа", noClip);
            sb.Append("\n  звуки на префабах без клипа в библиотеке:");
            if (prefabMissing.Count == 0) sb.Append(" нет");
            foreach (var kv in prefabMissing)
                sb.Append($"\n    {kv.Key} (префабы: {string.Join(", ", kv.Value)})");
            AppendList(sb, "записи в библиотеке вне AllSounds", unknownEntries);
            Debug.Log(sb.ToString());
        }

        private static AudioLibrary FindLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioLibrary");
            if (guids.Length == 0)
            {
                Debug.LogError("[AudioLibraryTools] Ассет AudioLibrary не найден в проекте.");
                return null;
            }
            if (guids.Length > 1)
                Debug.LogWarning($"[AudioLibraryTools] Найдено несколько AudioLibrary, используется {AssetDatabase.GUIDToAssetPath(guids[0])}");
            return AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static AudioLibrary.SoundEntry FindEntry(AudioLibrary library, string name)
            => library.uiSounds.Concat(library.vfxSounds).Concat(library.musicTracks)
                .FirstOrDefault(e => e.name == name);

        private static AudioClip FindClip(string name)
        {
            var exact = new List<string>();
            var loose = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (file == name) exact.Add(path);
                else if (string.Equals(file, name, StringComparison.OrdinalIgnoreCase)) loose.Add(path);
            }
            if (exact.Count == 0 && loose.Count > 0)
                Debug.LogWarning($"[AudioLibraryTools] '{name}' нашёлся только с другим регистром имени файла: {string.Join(", ", loose)}");
            var candidates = exact.Count > 0 ? exact : loose;
            if (candidates.Count == 0) return null;
            string chosen = candidates
                .OrderByDescending(p => p.Replace('\\', '/').Contains("/SFX/"))
                .ThenBy(p => p, StringComparer.Ordinal)
                .First();
            if (candidates.Count > 1)
                Debug.LogWarning($"[AudioLibraryTools] Несколько клипов '{name}', выбран {chosen} из:\n  {string.Join("\n  ", candidates)}");
            return AssetDatabase.LoadAssetAtPath<AudioClip>(chosen);
        }

        private static Dictionary<string, HashSet<string>> CollectPrefabSounds()
        {
            var map = new Dictionary<string, HashSet<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) continue;
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    foreach (string sound in SerializedSoundFields(new SerializedObject(component)))
                    {
                        if (!map.TryGetValue(sound, out var prefabs))
                            map[sound] = prefabs = new HashSet<string>();
                        prefabs.Add(Path.GetFileNameWithoutExtension(path));
                    }
                }
            }
            return map;
        }

        // Top-level serialized string fields ending in "Sound" — the codebase convention
        // for library sound names (deadSound, dmgSound, hitSound, explosionSound, ...).
        private static IEnumerable<string> SerializedSoundFields(SerializedObject so)
        {
            var prop = so.GetIterator();
            bool enter = true;
            while (prop.NextVisible(enter))
            {
                enter = false;
                if (prop.propertyType == SerializedPropertyType.String
                    && prop.name.EndsWith("Sound", StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(prop.stringValue))
                    yield return prop.stringValue;
            }
        }

        private static void AppendList(StringBuilder sb, string title, List<string> items)
        {
            sb.Append($"\n  {title}: ");
            sb.Append(items.Count == 0 ? "нет" : string.Join(", ", items));
        }
    }
}
