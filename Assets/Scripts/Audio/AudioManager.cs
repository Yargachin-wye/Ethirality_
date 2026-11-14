using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer Groups")]
        public AudioMixerGroup uiMixer;
        public AudioMixerGroup vfxMixer;
        public AudioMixerGroup musicMixer;

        [Header("Library")]
        public AudioLibrary library;

        [Header("Ambient")]
        public float ambientFadeTime = 1.5f;

        private Dictionary<string, AudioLibrary.SoundEntry> _sounds = new Dictionary<string, AudioLibrary.SoundEntry>();
        private AudioSource _musicSource;
        private class AmbientTrack
        {
            public AudioSource Source;
            public AudioLibrary.SoundEntry Entry;
            public bool Paused;
        }

        private readonly Dictionary<string, AmbientTrack> _ambients = new Dictionary<string, AmbientTrack>();
        private readonly HashSet<string> _warnedAmbients = new HashSet<string>();
        private string _ambientWanted;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private string _musicName;
        private const int MaxPoolSize = 32;
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private int _nextSteal;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cache all sounds
            foreach (var s in library.uiSounds) _sounds[s.name] = s;
            foreach (var s in library.vfxSounds) _sounds[s.name] = s;
            foreach (var s in library.musicTracks) _sounds[s.name] = s;

            // Setup music source
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.outputAudioMixerGroup = musicMixer;
            _musicSource.loop = true;

            _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

            gameObject.AddComponent<AmbientController>();
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
                if (_musicSource != null && _musicSource.isPlaying && _sounds.TryGetValue(_musicName ?? "", out var e))
                    _musicSource.volume = e.volume * _musicVolume;
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
            }
        }

        /// <summary>
        /// Play a one-shot sound on a given channel at world position
        /// </summary>
        public void PlaySound(string name, AudioChannel channel, Vector3 position)
        {
            if (!_sounds.TryGetValue(name, out var entry))
            {
                Debug.LogWarning($"Sound '{name}' not found in library.");
                return;
            }

            var source = GetPooledSource();
            source.clip = entry.clip;
            source.volume = entry.volume * _sfxVolume;
            source.pitch = entry.pitch + Random.Range(-entry.pitch, entry.pitch) * entry.pitchFactor;
            source.outputAudioMixerGroup = channel == AudioChannel.UI ? uiMixer : vfxMixer;
            source.transform.position = position;
            source.spatialBlend = 1f; // 3D
            source.minDistance = 1f;
            source.maxDistance = 15f;
            source.Play();
        }

        private AudioSource GetPooledSource()
        {
            foreach (var s in _pool)
                if (!s.isPlaying) return s;

            if (_pool.Count >= MaxPoolSize)
            {
                var oldest = _pool[_nextSteal++ % _pool.Count];
                oldest.Stop();
                return oldest;
            }

            var source = new GameObject("PooledSFX").AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            _pool.Add(source);
            return source;
        }

        /// <summary>
        /// Play a UI sound that follows the listener (2D)
        /// </summary>
        public void PlayUISound(string name)
        {
            if (!_sounds.TryGetValue(name, out var entry))
            {
                Debug.LogWarning($"UI Sound '{name}' not found in library.");
                return;
            }

            var source = GetPooledSource();
            source.clip = entry.clip;
            source.volume = entry.volume * _sfxVolume;
            source.pitch = entry.pitch + Random.Range(-entry.pitch, entry.pitch) * entry.pitchFactor;
            source.outputAudioMixerGroup = uiMixer;
            source.spatialBlend = 0f; // 2D
            source.transform.localPosition = Vector3.zero;
            source.Play();
        }

        /// <summary>
        /// Start background music by name
        /// </summary>
        public void PlayMusic(string name)
        {
            if (!_sounds.TryGetValue(name, out var entry))
            {
                Debug.LogWarning($"Music '{name}' not found in library.");
                return;
            }

            _musicName = name;
            _musicSource.clip = entry.clip;
            _musicSource.volume = entry.volume * _musicVolume;
            _musicSource.pitch = entry.pitch;
            _musicSource.Play();
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic() => _musicSource.Stop();

        /// <summary>
        /// Select which ambience should be audible; others fade out and pause, keeping their position
        /// </summary>
        public void PlayAmbient(string name) => _ambientWanted = name;

        /// <summary>
        /// Start the given ambience over from the beginning
        /// </summary>
        public void RestartAmbient(string name)
        {
            var track = GetAmbient(name);
            if (track == null) return;
            track.Source.Stop();
            track.Source.volume = 0f;
            track.Paused = false;
        }

        private AmbientTrack GetAmbient(string name)
        {
            if (_ambients.TryGetValue(name, out var track)) return track;
            if (!_sounds.TryGetValue(name, out var entry) || entry.clip == null)
            {
                if (_warnedAmbients.Add(name)) Debug.LogWarning($"Ambient '{name}' not found in library or has no clip.");
                return null;
            }

            if (entry.clip.loadState == AudioDataLoadState.Unloaded) entry.clip.LoadAudioData();
            var source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = musicMixer;
            source.clip = entry.clip;
            source.pitch = entry.pitch >= 0.1f ? entry.pitch : 1f;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            track = new AmbientTrack { Source = source, Entry = entry };
            _ambients[name] = track;
            return track;
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_ambientWanted)) GetAmbient(_ambientWanted);
            float step = Time.unscaledDeltaTime / Mathf.Max(ambientFadeTime, 0.01f);
            foreach (var kv in _ambients)
            {
                var track = kv.Value;
                var source = track.Source;
                bool wanted = kv.Key == _ambientWanted;
                float target = wanted ? track.Entry.volume * _musicVolume : 0f;

                if (wanted && !source.isPlaying)
                {
                    if (track.Paused) source.UnPause();
                    else source.Play();
                    track.Paused = false;
                }

                source.volume = Mathf.MoveTowards(source.volume, target, step);
                if (!wanted && source.isPlaying && source.volume <= 0f)
                {
                    source.Pause();
                    track.Paused = true;
                }
            }
        }

        /// <summary>
        /// Set volume on mixer
        /// </summary>
        public void SetChannelVolume(AudioChannel channel, float decibels)
        {
            var param = channel == AudioChannel.UI ? "UIVolume" :
                channel == AudioChannel.VFX ? "VFXVolume" : "MusicVolume";
            AudioMixer mixer = (channel == AudioChannel.UI) ? uiMixer.audioMixer :
                (channel == AudioChannel.VFX) ? vfxMixer.audioMixer : musicMixer.audioMixer;
            mixer.SetFloat(param, decibels);
        }
    }
}