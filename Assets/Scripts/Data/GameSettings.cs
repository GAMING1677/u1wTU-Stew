using UnityEngine;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "ApprovalMonster/GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Player Defaults")]
        public int initialFollowers = 100;
        public int maxMental = 10;
        public int maxMotivation = 3;
        public int initialHandSize = 3;
        
        [Tooltip("Number of cards to draw at the start of each turn (after turn 1)")]
        public int turnDrawCount = 2;

        [Header("Monster Mode")]
        [Tooltip("Mental threshold to trigger Monster Mode")]
        public int monsterThreshold = 3;

        [Tooltip("Number of cards to show in monster draft")]
        [Range(2, 3)]
        public int monsterDraftCardCount = 3;
        


        [Header("Monster Mode Effects")]
        [Tooltip("Impression multiplier when in Monster Mode (1.0 = disabled)")]
        public float monsterModeMultiplier = 1.0f;
        
        [Tooltip("ノルマ未達時のペナルティ倍率（モンスターモード時）")]
        [Range(1f, 5f)]
        public float monsterPenaltyMultiplier = 2.0f;
        
        [Tooltip("Follower penalty per turn in Monster Mode (0 = disabled)")]
        public int monsterFollowerPenalty = 0;

        [Header("Score Attack")]
        public long scoreCap = 9999999999;
    }
}
