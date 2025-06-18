using System;
using System.Collections.Generic;
using Constants;
using EditorAttributes;
using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class SoundEntry
        {
            private static string[] collection = AudioConst.AllSounds;
            [Dropdown("collection")]public string name;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
            [Range(0f, 1f)] public float pitchFactor;
        }

        public List<SoundEntry> uiSounds = new List<SoundEntry>();
        public List<SoundEntry> vfxSounds = new List<SoundEntry>();
        public List<SoundEntry> musicTracks = new List<SoundEntry>();
    }

    public enum AudioChannel
    {
        UI,
        VFX,
        Music
    }
}