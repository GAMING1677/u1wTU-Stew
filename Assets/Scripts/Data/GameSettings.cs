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

        [Header("Game Rules")]
        [Tooltip("Mental threshold to trigger Monster Mode")]
        public int monsterThreshold = 3;

        [Tooltip("Number of cards to show in monster draft")]
        [Range(2, 3)]
        public int monsterDraftCardCount = 3;
        
        [Tooltip("Multiplier applied when in Monster Mode")]
        public float monsterModeMultiplier = 3.0f;

        [Header("Score Attack")]
        public long scoreCap = 9999999999;
    }
}
