using UnityEngine;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// モンスターモードカットイン用のプリセット
    /// ステージごとに異なる画像・テキスト・サイズを設定可能
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterModePreset", menuName = "ApprovalMonster/MonsterModePreset")]
    public class MonsterModePreset : ScriptableObject
    {
        [Header("Visuals")]
        [Tooltip("背景画像")]
        public Sprite backgroundImage;
        
        [Tooltip("キャラクター画像")]
        public Sprite characterImage;
        
        [Tooltip("キャラクター画像のサイズ")]
        public Vector2 characterSize = new Vector2(400, 600);
        
        [Header("Text")]
        [Tooltip("タイトルテキスト")]
        public string titleText = "MONSTER MODE";
        
        [Tooltip("メッセージテキスト")]
        [TextArea(2, 4)]
        public string messageText = "承認欲求が暴走を始めた...";
        
        [Header("Audio")]
        [Tooltip("カットイン表示時のサウンド")]
        public AudioClip showSound;
        
        [Tooltip("クリック時のサウンド")]
        public AudioClip clickSound;
        
        [Tooltip("モンスターモード中のBGM")]
        public AudioClip monsterBGM;
    }
}
