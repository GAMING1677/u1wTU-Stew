using UnityEngine;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "ApprovalMonster/CardData")]
    public class CardData : ScriptableObject
    {
        [BoxGroup("Basic Info")]
        public string cardName;
        
        [BoxGroup("Basic Info")]
        [ShowAssetPreview]
        public Sprite cardImage;
        
        [BoxGroup("Basic Info")]
        [TextArea(3, 5)]
        public string flavorText;

        [BoxGroup("Costs")]
        [MinValue(0)]
        public int motivationCost;

        [BoxGroup("Costs")]
        [Tooltip("Positive value decreases mental. Negative value heals.")]
        public int mentalCost;

        [BoxGroup("Effects")]
        public int followerGain;
        
        [BoxGroup("Effects")]
        public float impressionRate = 1.0f;

        [BoxGroup("Type & Risk")]
        public CardType cardType;
        
        [BoxGroup("Type & Risk")]
        public RiskType riskType;

        [BoxGroup("Type & Risk")]
        [ShowIf("HasRisk")]
        public float riskProbability = 1.0f; // 1.0 = 100%

        [BoxGroup("Type & Risk")]
        [ShowIf("HasRisk")]
        public int riskValue; // Damage or specific value

        public bool HasRisk() => cardType == CardType.Risk || riskType != RiskType.None;
    }

    public enum CardType
    {
        Normal,
        Risk,
        Monster,
        Special,
        Passive
    }

    public enum RiskType
    {
        None,
        Flame,      // 炎上 (Decrease Mental or Follower)
        Freeze,     // 凍結 (Skip Turn)
        Ban,        // BAN (Game Over)
        LoseFollower // フォロワー減少
    }
}
