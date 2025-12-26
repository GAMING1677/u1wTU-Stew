using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// キャラクターのアニメーションとリアクション画像の設定
    /// 通常時とモンスターモード時で切り替えて使用する
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "ApprovalMonster/CharacterProfile")]
    public class CharacterProfile : ScriptableObject
    {
        [Header("Idle Animation")]
        [Tooltip("待機アニメーションのコマリスト")]
        public List<Sprite> idleFrames;
        
        [Tooltip("1コマあたりの表示時間（秒）")]
        [MinValue(0.01f)]
        public float frameRate = 0.2f;
        
        [Tooltip("アニメーション1ループ後の待機時間（最小）")]
        public float minIdleWait = 0.5f;
        
        [Tooltip("アニメーション1ループ後の待機時間（最大）")]
        public float maxIdleWait = 2.0f;
        
        [Header("Reactions")]
        [Tooltip("喜び（軽）")]
        [ShowAssetPreview]
        public Sprite reactionHappy_1;
        
        [Tooltip("喜び（中）")]
        [ShowAssetPreview]
        public Sprite reactionHappy_2;
        
        [Tooltip("喜び（大）")]
        [ShowAssetPreview]
        public Sprite reactionHappy_3;
        
        [Tooltip("悲しみ（軽）")]
        [ShowAssetPreview]
        public Sprite reactionSad_1;
        
        [Tooltip("悲しみ（大）")]
        [ShowAssetPreview]
        public Sprite reactionSad_2;
        
        [Tooltip("リアクションの表示時間（秒）")]
        public float reactionDuration = 1.0f;
    }
}
