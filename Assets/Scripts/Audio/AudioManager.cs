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

            var source = new GameObject($"SFX_{name}").AddComponent<AudioSource>();
            source.clip = entry.clip;
            source.volume = entry.volume;
            source.pitch = entry.pitch + UnityEngine.Random.Range(-entry.pitch, entry.pitch) *entry.pitchFactor;
            source.outputAudioMixerGroup = channel == AudioChannel.UI ? uiMixer : vfxMixer;
            source.transform.position = position;
            source.spatialBlend = 1f; // 3D
            source.minDistance = 1f;
            source.maxDistance = 15f;
            source.Play();
            Destroy(source.gameObject, entry.clip.length + 0.1f);
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

            var source = new GameObject($"UI_{name}").AddComponent<AudioSource>();
            source.clip = entry.clip;
            source.volume = entry.volume;
            source.pitch = entry.pitch;
            source.outputAudioMixerGroup = uiMixer;
            source.spatialBlend = 0f; // 2D
            source.transform.SetParent(transform, false);
            source.Play();
            Destroy(source.gameObject, entry.clip.length + 0.1f);
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
            var param = channel == AudioChannel.UI ? "UIVolume" : channel == AudioChannel.VFX ? "VFXVolume" : "MusicVolume";
            AudioMixer mixer = (channel == AudioChannel.UI) ? uiMixer.audioMixer :
                (channel == AudioChannel.VFX) ? vfxMixer.audioMixer : musicMixer.audioMixer;
            mixer.SetFloat(param, decibels);
        }
    }
}