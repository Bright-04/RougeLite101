using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RougeLite.Managers
{
    /// <summary>
    /// Audio Manager handles all sound effects, music, and audio settings
    /// Provides centralized audio control with volume mixing and spatial audio support
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Audio Settings

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup masterMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup voiceMixerGroup;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float voiceVolume = 1f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private List<AudioSource> sfxSources = new List<AudioSource>();

        #endregion

        #region Audio Clips

        [Header("Music Tracks")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip gameOverMusic;

        [Header("Ambient Sounds")]
        [SerializeField] private AudioClip ambientLoop;

        [Header("UI Sound Effects")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioClip menuCloseSound;

        [Header("Gameplay Sound Effects")]
        [SerializeField] private AudioClip[] playerAttackSounds;
        [SerializeField] private AudioClip[] playerHurtSounds;
        [SerializeField] private AudioClip[] enemyHurtSounds;
        [SerializeField] private AudioClip[] projectileLaunchSounds;
        [SerializeField] private AudioClip[] projectileHitSounds;

        #endregion

        #region Audio Pools

        private Queue<AudioSource> availableSfxSources;
        private Dictionary<string, AudioClip> audioClipCache;
        private int maxSfxSources = 10;

        #endregion

        #region Properties

        public float MasterVolume 
        { 
            get => masterVolume; 
            set 
            { 
                masterVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
            } 
        }

        public float MusicVolume 
        { 
            get => musicVolume; 
            set 
            { 
                musicVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
            } 
        }

        public float SfxVolume 
        { 
            get => sfxVolume; 
            set 
            { 
                sfxVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
            } 
        }

        public float VoiceVolume 
        { 
            get => voiceVolume; 
            set 
            { 
                voiceVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
            } 
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadAudioSettings();
            ApplyVolumeSettings();
        }

        #endregion

        #region Initialization

        private void InitializeAudioManager()
        {
            Debug.Log("AudioManager: Initializing...");

            // Initialize audio clip cache
            audioClipCache = new Dictionary<string, AudioClip>();

            // Create audio sources if they don't exist
            CreateAudioSources();

            // Initialize SFX source pool
            InitializeSfxSourcePool();

            Debug.Log("AudioManager: Initialization complete");
        }

        private void CreateAudioSources()
        {
            // Create music source
            if (musicSource == null)
            {
                GameObject musicGO = new GameObject("Music Source");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.outputAudioMixerGroup = musicMixerGroup;
            }

            // Create ambient source
            if (ambientSource == null)
            {
                GameObject ambientGO = new GameObject("Ambient Source");
                ambientGO.transform.SetParent(transform);
                ambientSource = ambientGO.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        private void InitializeSfxSourcePool()
        {
            availableSfxSources = new Queue<AudioSource>();

            // Create initial pool of SFX sources
            for (int i = 0; i < maxSfxSources; i++)
            {
                CreateSfxSource();
            }
        }

        private AudioSource CreateSfxSource()
        {
            GameObject sfxGO = new GameObject($"SFX Source {sfxSources.Count + 1}");
            sfxGO.transform.SetParent(transform);
            AudioSource source = sfxGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = sfxMixerGroup;
            
            sfxSources.Add(source);
            availableSfxSources.Enqueue(source);
            
            return source;
        }

        #endregion

        #region Music Control

        public void PlayMusic(AudioClip musicClip, bool loop = true, float fadeInTime = 1f)
        {
            if (musicClip == null) return;

            StartCoroutine(FadeMusic(musicClip, loop, fadeInTime));
        }

        public void PlayMainMenuMusic()
        {
            PlayMusic(mainMenuMusic);
        }

        public void PlayGameplayMusic()
        {
            PlayMusic(gameplayMusic);
        }

        public void PlayVictoryMusic()
        {
            PlayMusic(victoryMusic, false);
        }

        public void PlayGameOverMusic()
        {
            PlayMusic(gameOverMusic, false);
        }

        public void StopMusic(float fadeOutTime = 1f)
        {
            StartCoroutine(FadeOutMusic(fadeOutTime));
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        private System.Collections.IEnumerator FadeMusic(AudioClip newClip, bool loop, float fadeTime)
        {
            // Fade out current music
            if (musicSource.isPlaying)
            {
                float startVolume = musicSource.volume;
                while (musicSource.volume > 0)
                {
                    musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
                    yield return null;
                }
                musicSource.Stop();
            }

            // Set new clip and fade in
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.volume = 0f;
            musicSource.Play();

            while (musicSource.volume < musicVolume * masterVolume)
            {
                musicSource.volume += musicVolume * masterVolume * Time.unscaledDeltaTime / fadeTime;
                yield return null;
            }
            musicSource.volume = musicVolume * masterVolume;
        }

        private System.Collections.IEnumerator FadeOutMusic(float fadeTime)
        {
            if (!musicSource.isPlaying) yield break;

            float startVolume = musicSource.volume;
            while (musicSource.volume > 0)
            {
                musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = startVolume;
        }

        #endregion

        #region Sound Effects

        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSfxSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.pitch = pitch;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1f; // 3D sound
            }
            else
            {
                source.spatialBlend = 0f; // 2D sound
            }

            source.Play();

            // Return source to pool after clip finishes
            StartCoroutine(ReturnSfxSourceToPool(source, clip.length / pitch));
        }

        public void PlaySFX(string clipName, float volume = 1f, float pitch = 1f, Vector3? position = null)
        {
            AudioClip clip = GetAudioClip(clipName);
            PlaySFX(clip, volume, pitch, position);
        }

        public void PlayRandomSFX(AudioClip[] clips, float volume = 1f, float pitch = 1f, Vector3? position = null)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            PlaySFX(randomClip, volume, pitch, position);
        }

        // Convenience methods for specific sound types
        public void PlayButtonClick() => PlaySFX(buttonClickSound);
        public void PlayButtonHover() => PlaySFX(buttonHoverSound);
        public void PlayMenuOpen() => PlaySFX(menuOpenSound);
        public void PlayMenuClose() => PlaySFX(menuCloseSound);
        
        public void PlayPlayerAttack() => PlayRandomSFX(playerAttackSounds);
        public void PlayPlayerHurt() => PlayRandomSFX(playerHurtSounds);
        public void PlayEnemyHurt() => PlayRandomSFX(enemyHurtSounds);
        public void PlayProjectileLaunch() => PlayRandomSFX(projectileLaunchSounds);
        public void PlayProjectileHit() => PlayRandomSFX(projectileHitSounds);

        #endregion

        #region Ambient Audio

        public void PlayAmbient(AudioClip ambientClip, float volume = 0.5f)
        {
            if (ambientClip == null || ambientSource == null) return;

            ambientSource.clip = ambientClip;
            ambientSource.volume = volume * sfxVolume * masterVolume;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.Stop();
            }
        }

        #endregion

        #region Source Pool Management

        private AudioSource GetAvailableSfxSource()
        {
            if (availableSfxSources.Count > 0)
            {
                return availableSfxSources.Dequeue();
            }

            // If no sources available, create a new one if under limit
            if (sfxSources.Count < maxSfxSources * 2)
            {
                return CreateSfxSource();
            }

            // Otherwise, find a source that's not playing
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null; // All sources are busy
        }

        private System.Collections.IEnumerator ReturnSfxSourceToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (source != null && !source.isPlaying)
            {
                availableSfxSources.Enqueue(source);
            }
        }

        #endregion

        #region Volume Control

        private void ApplyVolumeSettings()
        {
            // Apply master volume through mixer or directly
            if (masterMixerGroup != null)
            {
                float masterDB = masterVolume > 0 ? 20f * Mathf.Log10(masterVolume) : -80f;
                masterMixerGroup.audioMixer.SetFloat("MasterVolume", masterDB);
            }

            if (musicMixerGroup != null)
            {
                float musicDB = musicVolume > 0 ? 20f * Mathf.Log10(musicVolume) : -80f;
                musicMixerGroup.audioMixer.SetFloat("MusicVolume", musicDB);
            }

            if (sfxMixerGroup != null)
            {
                float sfxDB = sfxVolume > 0 ? 20f * Mathf.Log10(sfxVolume) : -80f;
                sfxMixerGroup.audioMixer.SetFloat("SFXVolume", sfxDB);
            }

            if (voiceMixerGroup != null)
            {
                float voiceDB = voiceVolume > 0 ? 20f * Mathf.Log10(voiceVolume) : -80f;
                voiceMixerGroup.audioMixer.SetFloat("VoiceVolume", voiceDB);
            }

            // Update music source volume directly if no mixer
            if (musicSource != null && musicMixerGroup == null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = volume;
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = volume;
        }

        public void SetVoiceVolume(float volume)
        {
            VoiceVolume = volume;
        }

        #endregion

        #region Audio Clip Management

        private AudioClip GetAudioClip(string clipName)
        {
            if (audioClipCache.ContainsKey(clipName))
            {
                return audioClipCache[clipName];
            }

            // Try to load from Resources
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                audioClipCache[clipName] = clip;
            }

            return clip;
        }

        public void PreloadAudioClip(string clipName)
        {
            GetAudioClip(clipName);
        }

        #endregion

        #region Settings Persistence

        private void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
        }

        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
            PlayerPrefs.Save();
        }

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Box(new Rect(10, 320, 250, 140), "Audio Manager Debug");
            GUI.Label(new Rect(20, 340, 230, 20), $"Master Volume: {masterVolume:F2}");
            GUI.Label(new Rect(20, 360, 230, 20), $"Music: {(musicSource?.isPlaying == true ? "Playing" : "Stopped")}");
            GUI.Label(new Rect(20, 380, 230, 20), $"SFX Sources: {sfxSources.Count} ({availableSfxSources.Count} available)");
            GUI.Label(new Rect(20, 400, 230, 20), $"Audio Clips Cached: {audioClipCache.Count}");

            if (GUI.Button(new Rect(20, 420, 100, 20), "Test SFX"))
            {
                PlayButtonClick();
            }

            if (GUI.Button(new Rect(130, 420, 100, 20), "Stop Music"))
            {
                StopMusic();
            }
        }

        #endregion
    }
}