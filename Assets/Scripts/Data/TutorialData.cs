using UnityEngine;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// チュートリアル用の画像アニメーションデータ
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorial", menuName = "ApprovalMonster/Tutorial Data")]
    public class TutorialData : ScriptableObject
    {
        [Header("基本情報")]
        [Tooltip("チュートリアルのタイトル")]
        public string tutorialTitle;
        
        [Tooltip("説明文")]
        [TextArea(2, 4)]
        public string description;
        
        [Header("アニメーション設定")]
        [Tooltip("アニメーションのフレーム画像")]
        public Sprite[] frames;
        
        [Tooltip("1フレームあたりの表示秒数")]
        [Range(0.01f, 1f)]
        public float frameInterval = 0.05f; // 20fps相当
        
        [Tooltip("ループ再生するか")]
        public bool loop = true;
        
        /// <summary>
        /// 総再生時間（秒）
        /// </summary>
        public float TotalDuration => frames != null ? frames.Length * frameInterval : 0f;
        
        /// <summary>
        /// フレーム数
        /// </summary>
        public int FrameCount => frames != null ? frames.Length : 0;
    }
}
