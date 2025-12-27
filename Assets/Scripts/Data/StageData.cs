using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    [CreateAssetMenu(fileName = "NewStage", menuName = "ApprovalMonster/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("Clear Conditions")]
        [Tooltip("このステージのクリア条件（nullの場合は無制限プレイ）")]
        public ClearCondition clearCondition;
        
        [Tooltip("このステージをアンロックするために必要なステージのリスト（全てクリア必須）")]
        [ReorderableList]
        public List<StageData> requiredStages = new List<StageData>();
        
        [Header("Stage Settings")]
        public string stageName;
        
        [Tooltip("ステージの説明文（ステージセレクト画面で表示）")]
        [TextArea(2, 4)]
        public string stageDescription = "がんばれ！";
        
        [Tooltip("ゲーム中のターン毎目標スコア（ペナルティ用、クリア条件ではない）")]
        public long quotaScore;
        
        [Tooltip("ゲームの最大ターン数")]
        public int maxTurnCount = 20;

        [Header("Decks")]
        public List<CardData> initialDeck;
        public List<CardData> draftPool;
        public List<CardData> monsterDeck;
        
        [Header("Monster Mode")]
        [Tooltip("このステージ専用のモンスターモードカットイン設定")]
        public MonsterModePreset monsterModePreset;
        
        [Header("Visuals")]
        public Sprite background;
        public Color themeColor = Color.white;
        
        [Header("Character")]
        [Tooltip("通常時のキャラクター設定")]
        public CharacterProfile normalProfile;
        
        [Tooltip("モンスターモード時のキャラクター設定")]
        public CharacterProfile monsterProfile;
    }
}
