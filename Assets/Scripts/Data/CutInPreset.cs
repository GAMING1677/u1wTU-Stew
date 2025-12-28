using UnityEngine;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// カットインのプリセット設定
    /// インスペクターから事前に設定しておくことで、コードからはプリセット名で呼び出せる
    /// </summary>
    [CreateAssetMenu(fileName = "CutInPreset", menuName = "ApprovalMonster/CutInPreset")]
    public class CutInPreset : ScriptableObject
    {
        [Header("テキスト")]
        [Tooltip("タイトルテキスト")]
        public string title = "TITLE";
        
        [Tooltip("メッセージテキスト")]
        [TextArea(2, 4)]
        public string message = "";
        
        [Header("ビジュアル")]
        [Tooltip("背景画像（nullの場合は単色表示）")]
        public Sprite backgroundImage;
        
        [Tooltip("背景色")]
        public Color backgroundColor = new Color(0, 0, 0, 0.8f);
        
        [Tooltip("アイコンを表示するか")]
        public bool showIcon = false;
        
        [Tooltip("アイコン画像（showIconがtrueの場合のみ表示）")]
        public Sprite iconImage;
        
        [Tooltip("タイトルの色")]
        public Color titleColor = Color.white;
        
        [Tooltip("メッセージの色")]
        public Color messageColor = Color.white;
        
        [Header("アニメーション")]
        [Tooltip("フェードイン時間")]
        public float fadeInDuration = 0.3f;
        
        [Tooltip("フェードアウト時間")]
        public float fadeOutDuration = 0.2f;
        
        [Header("オーディオ")]
        [Tooltip("表示時の効果音")]
        public AudioClip showSound;
        
        [Tooltip("クリック時の効果音")]
        public AudioClip clickSound;
    }
}
