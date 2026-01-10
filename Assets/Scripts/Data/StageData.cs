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
        
        [Header("Cut-in Presets")]
        [Tooltip("ステージ開始時のカットインプリセット")]
        public CutInPreset stageStartPreset;
        
        [Tooltip("モチベーション不足時のカットインプリセット")]
        public CutInPreset motivationLowPreset;
        
        [Tooltip("ゲーム中のターン毎目標スコア（ペナルティ用、クリア条件ではない）")]
        public long quotaScore;
        
        [Tooltip("ゲームの最大ターン数")]
        public int maxTurnCount = 20;

        [Header("Decks")]
        public List<CardData> initialDeck;
        public List<CardData> draftPool;
        public List<CardData> monsterDeck;
        
        [Header("Flaming System")]
        [Tooltip("このステージでフレーミング（炎上）システムを有効にするか")]
        public bool enableFlaming = false;
        
        [Header("Infection System")]
        [Tooltip("このステージで感染システム（ゾンビデッキ）を有効にするか")]
        public bool enableInfection = false;
        
        [Tooltip("デッキリシャッフル時に感染度を減少させる割合（0-100%）\n100%=完全リセット、50%=半減")]
        [Range(0f, 100f)]
        public float infectionResetRate = 100f;
        
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
        
        [Header("Tracked Card UI")]
        [Tooltip("特定カードの枚数表示UIを使用するか")]
        public bool showTrackedCardUI = false;
        
        [Tooltip("追跡対象のカード")]
        public CardData trackedCard;
    }
}
