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

        private Dictionary<string, AudioLibrary.SoundEntry> _sounds = new Dictionary<string, AudioLibrary.SoundEntry>();
        private AudioSource _musicSource;
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
            source.volume = entry.volume;
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
            source.volume = entry.volume;
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

            _musicSource.clip = entry.clip;
            _musicSource.volume = entry.volume;
            _musicSource.pitch = entry.pitch;
            _musicSource.Play();
        }

        /// <summary>
        /// Stop background music
        /// </summary>
        public void StopMusic() => _musicSource.Stop();

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