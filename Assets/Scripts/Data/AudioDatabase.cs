using UnityEngine;
using System.Collections.Generic;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// 音声データベース
    /// BGMとSEの全AudioClipを一元管理
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "ApprovalMonster/AudioDatabase")]
    public class AudioDatabase : ScriptableObject
    {
        [Header("BGM")]
        [Tooltip("メインテーマBGM（起動時・タイトル画面）")]
        public AudioClip mainThemeBGM;
        
        [Header("SE")]
        [Tooltip("SE種別ごとのAudioClip。Inspectorで設定")]
        public SEAudioClip[] seClips;
        
        private Dictionary<SEType, AudioClip> seDict;
        
        /// <summary>
        /// SE種別からAudioClipを取得
        /// </summary>
        public AudioClip GetSE(SEType type)
        {
            if (seDict == null)
            {
                BuildDictionary();
            }
            
            if (seDict.TryGetValue(type, out AudioClip clip))
            {
                return clip;
            }
            
            Debug.LogWarning($"[AudioDatabase] SE '{type}' is not assigned!");
            return null;
        }
        
        private void BuildDictionary()
        {
            seDict = new Dictionary<SEType, AudioClip>();
            
            if (seClips != null)
            {
                foreach (var se in seClips)
                {
                    if (se.clip != null)
                    {
                        seDict[se.type] = se.clip;
                    }
                }
            }
        }
        
        /// <summary>
        /// Inspector用のSE設定クラス
        /// </summary>
        [System.Serializable]
        public class SEAudioClip
        {
            public SEType type;
            public AudioClip clip;
        }
    }
}
