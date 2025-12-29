using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ApprovalMonster.Data;

namespace ApprovalMonster.Core
{
    /// <summary>
    /// 音声管理システム（Singleton）
    /// BGMとSEの再生を一元管理
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Database")]
        [SerializeField] private AudioDatabase audioDatabase;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource[] seSources; // 複数同時再生用
        
        [Header("Volume Settings")]
        [SerializeField] private float defaultMasterVolume = 1.0f;
        [SerializeField] private float defaultBGMVolume = 0.25f;
        [SerializeField] private float defaultSEVolume = 0.25f;
        
        private float masterVolume;
        private float bgmVolume;
        private float seVolume;
        
        private const string KEY_MASTER_VOLUME = "Master_Volume";
        private const string KEY_BGM_VOLUME = "BGM_Volume";
        private const string KEY_SE_VOLUME = "SE_Volume";
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            // Load volumes from EasySave (fallback to defaults)
            masterVolume = ES3.Load(KEY_MASTER_VOLUME, defaultMasterVolume);
            bgmVolume = ES3.Load(KEY_BGM_VOLUME, defaultBGMVolume);
            seVolume = ES3.Load(KEY_SE_VOLUME, defaultSEVolume);
            
            ApplyVolumes();
            
            // Preload all audio clips to prevent lag on first play
            PreloadAudioClips();
            
            Debug.Log($"[AudioManager] Initialized. Master: {masterVolume}, BGM: {bgmVolume}, SE: {seVolume}");
        }
        
        // ... (省略: PlayBGM, PlaySE等は変更なし) ...

        /// <summary>
        /// マスター音量を設定
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveVolumes();
        }

        /// <summary>
        /// BGM音量を設定
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveVolumes();
        }
        
        /// <summary>
        /// SE音量を設定
        /// </summary>
        public void SetSEVolume(float volume)
        {
            seVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            SaveVolumes();
        }
        
        public float GetMasterVolume() => masterVolume;
        public float GetBGMVolume() => bgmVolume;
        public float GetSEVolume() => seVolume;
        
        private void ApplyVolumes()
        {
            // リニア（スライダー値をそのまま適用）
            float effectiveMaster = masterVolume;
            float effectiveBGM = bgmVolume;
            float effectiveSE = seVolume;
            
            if (bgmSource != null)
            {
                bgmSource.volume = effectiveBGM * effectiveMaster;
            }
            
            foreach (var source in seSources)
            {
                if (source != null)
                {
                    source.volume = effectiveSE * effectiveMaster;
                }
            }
        }
        
        // ... (省略: PreloadAudioClips) ...
        

        
        /// <summary>
        /// BGMを再生
        /// </summary>
        public void PlayBGM(AudioClip bgm, bool loop = true)
        {
            if (bgmSource == null || bgm == null) return;
            
            if (bgmSource.clip == bgm && bgmSource.isPlaying)
            {
                Debug.Log($"[AudioManager] BGM '{bgm.name}' is already playing");
                return;
            }
            
            bgmSource.clip = bgm;
            bgmSource.loop = loop;
            bgmSource.Play();
            
            Debug.Log($"[AudioManager] Playing BGM: {bgm.name}");
        }
        
        /// <summary>
        /// メインテーマBGMを再生
        /// </summary>
        public void PlayMainTheme()
        {
            if (audioDatabase != null && audioDatabase.mainThemeBGM != null)
            {
                PlayBGM(audioDatabase.mainThemeBGM);
            }
            else
            {
                Debug.LogWarning("[AudioManager] Main theme BGM is not assigned!");
            }
        }
        
        /// <summary>
        /// BGMを停止
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }
        
        /// <summary>
        /// SEを再生（SEType指定）
        /// </summary>
        public void PlaySE(SEType type)
        {
            if (audioDatabase == null)
            {
                Debug.LogWarning("[AudioManager] AudioDatabase is not assigned!");
                return;
            }
            
            AudioClip clip = audioDatabase.GetSE(type);
            if (clip == null) return;
            
            PlaySEClip(clip);
        }
        
        /// <summary>
        /// SEを再生（AudioClip直接指定）
        /// MonsterModePresetのclickSound等、カスタム音声の再生用
        /// </summary>
        public void PlaySE(AudioClip clip)
        {
            if (clip == null) return;
            PlaySEClip(clip);
        }
        
        /// <summary>
        /// SE再生の共通処理
        /// </summary>
        private void PlaySEClip(AudioClip clip)
        {
            // Find available SE source
            foreach (var source in seSources)
            {
                if (!source.isPlaying)
                {
                    source.PlayOneShot(clip);
                    return;
                }
            }
            
            // All sources busy, use first one
            if (seSources.Length > 0)
            {
                seSources[0].PlayOneShot(clip);
            }
        }
        

        
        /// <summary>
        /// 全AudioClipを事前ロードして初回再生時のラグを防止
        /// </summary>
        private void PreloadAudioClips()
        {
            if (audioDatabase == null)
            {
                Debug.LogWarning("[AudioManager] Cannot preload: AudioDatabase is null");
                return;
            }
            
            int preloadCount = 0;
            
            // Preload BGM
            if (audioDatabase.mainThemeBGM != null)
            {
                audioDatabase.mainThemeBGM.LoadAudioData();
                preloadCount++;
            }
            
            // Preload all SE
            if (audioDatabase.seClips != null)
            {
                foreach (var se in audioDatabase.seClips)
                {
                    if (se.clip != null)
                    {
                        se.clip.LoadAudioData();
                        preloadCount++;
                    }
                }
            }
            
            Debug.Log($"[AudioManager] Preloaded {preloadCount} audio clips");
        }
        
        private void SaveVolumes()
        {
            ES3.Save(KEY_MASTER_VOLUME, masterVolume);
            ES3.Save(KEY_BGM_VOLUME, bgmVolume);
            ES3.Save(KEY_SE_VOLUME, seVolume);
        }
    }
}

