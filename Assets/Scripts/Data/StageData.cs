using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    [CreateAssetMenu(fileName = "NewStage", menuName = "ApprovalMonster/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("Stage Settings")]
        public string stageName;
        
        [Tooltip("Target Impression Score to clear the stage")]
        public long quotaScore;

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
