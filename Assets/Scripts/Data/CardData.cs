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
        [Tooltip("カード効果の説明文")]
        [TextArea(3, 5)]
        public string description;
        
        [BoxGroup("Basic Info")]
        [Tooltip("カードのタグ（モンスターカード用：初心者向け、上級者向けなど）")]
        public string cardTag = "";
     
        [BoxGroup("Basic Info")]
        [TextArea(2, 4)]
        public string flavorText;

        [BoxGroup("Basic Info")]
        [TextArea(2, 4)]
        public System.Collections.Generic.List<string> postComments;
        
        [BoxGroup("Basic Info")]
        [Tooltip("ポスト表示時のアイコン（未設定時はデフォルト）")]
        [ShowAssetPreview]
        public Sprite postIcon;

        [BoxGroup("Costs")]
        [MinValue(0)]
        public int motivationCost;

        [BoxGroup("Costs")]
        [Tooltip("Positive value decreases mental. Negative value heals.")]
        public int mentalCost;

        [BoxGroup("Effects")]
        public int followerGain;

        [BoxGroup("Effects")]
        [Tooltip("If true, gain (CurrentFollowers * CurrentTurn) followers instead of followerGain.")]
        public bool isTurnMultiplierEffect = false;
        
        [BoxGroup("Effects")]
        public int drawCount = 0; // Number of cards to draw immediately
        
        [BoxGroup("Effects")]
        public int motivationRecovery = 0; // AP to recover (can be negative to drain)

        [BoxGroup("Effects")]
        public int turnDrawBonus = 0; // Increase cards drawn per turn (Persistent)
        
        [BoxGroup("Effects")]
        public int maxMotivationBonus = 0; // Increase Max AP (Persistent)
        
        [BoxGroup("Effects")]
        [Tooltip("Multiplier for impression gain based on current followers.")]
        public float impressionRate = 1.0f;

        [BoxGroup("Effects")]
        [Tooltip("If true, gain impressions = CurrentFollowers * (CurrentTurn / 10) instead of using impressionRate.")]
        public bool isTurnImpressionEffect = false;

        [BoxGroup("Type & Risk")]
        public CardType cardType;

        [BoxGroup("Type & Risk")]
        [Tooltip("If true, this card is removed from the game after use (not sent to discard pile).")]
        public bool isExhaust = false;
        
        [BoxGroup("Type & Risk")]
        public CardRarity rarity = CardRarity.Common;
        
        [BoxGroup("Type & Risk")]
        [Tooltip("カードのプレイ条件（特定条件下でのみプレイ可能）")]
        public CardPlayCondition playCondition = CardPlayCondition.None;
        
        [BoxGroup("Type & Risk")]
        public RiskType riskType;

        [BoxGroup("Type & Risk")]
        [ShowIf("HasRisk")]
        public float riskProbability = 1.0f; // 1.0 = 100%

        [BoxGroup("Type & Risk")]
        [ShowIf("HasRisk")]
        public int riskValue; // Damage or specific value

        [BoxGroup("Card Generation")]
        [Tooltip("プレイ時に生成するカードのリスト")]
        public System.Collections.Generic.List<GeneratedCard> generatedCards;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("この効果の対象となるカード")]
        public CardData handEffectTargetCard;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("① 手札の対象カードをすべて除外する")]
        public bool exhaustAllTargetCards = false;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("② 手札枚数×この値でインプレッション獲得（0=使用しない）")]
        public float handCountImpressionRate = 0f;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("③ 手札枚数分ドローする（false=使用しない）")]
        public bool drawByHandCount = false;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("④ 手札枚数×この値でフォロワー獲得（0=使用しない）")]
        public int handCountFollowerRate = 0;

        [BoxGroup("Hand-Based Effects")]
        [Tooltip("②③④の最低必要枚数（コスト）")]
        public int handEffectMinCount = 1;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("山札内の対象カード枚数×この値でインプレッション獲得（0=使用しない）")]
        public float drawPileCountImpressionRate = 0f;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("捨て札内の対象カード枚数×この値でインプレッション獲得（0=使用しない）")]
        public float discardPileCountImpressionRate = 0f;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("山札内の対象カード枚数×この値でフォロワー獲得（0=使用しない）")]
        public int drawPileCountFollowerRate = 0;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("捨て札内の対象カード枚数×この値でフォロワー獲得（0=使用しない）")]
        public int discardPileCountFollowerRate = 0;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("山札と捨て札の少ない方の枚数×この値でインプレッション獲得（0=使用しない）")]
        public float minPileCountImpressionRate = 0f;
        
        [BoxGroup("Pile-Based Effects")]
        [Tooltip("山札と捨て札の少ない方の枚数×この値でフォロワー獲得（0=使用しない）")]
        public int minPileCountFollowerRate = 0;

        [BoxGroup("Flaming")]
        [Tooltip("炎上の種の個数（プレイ時に加算）")]
        public int flamingSeedCount = 0;

        [BoxGroup("Flaming")]
        [Tooltip("炎上率（0～1）。0=抽選しない")]
        [Range(0f, 1f)]
        public float flamingRate = 0f;

        [BoxGroup("Flaming Special")]
        [Tooltip("種×この値でフォロワー獲得（0=使用しない）")]
        public int seedToFollowerMultiplier = 0;

        [BoxGroup("Flaming Special")]
        [Tooltip("種でメンタル回復、種消費")]
        public bool healMentalBySeeds = false;
        
        [BoxGroup("Infection")]
        [Tooltip("プレイ時の感染度増減（%）正で増加、負で減少")]
        public float infectionChange = 0f;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("モンスターカードA: 感染度をリセットして換金")]
        public bool zombieResetInfection = false;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("感染度1%あたりのインプ率（例: 100 = 感染度20%で2000%インプ）")]
        public float zombieImpressionPerInfection = 0f;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("感染度1%あたりのフォロワー増加数（例: 10 = 感染度20%で200人増加）")]
        public int zombieFollowerPerInfection = 0;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("感染度を減少させる割合（0-100%）100%=完全リセット、50%=半減")]
        [Range(0f, 100f)]
        public float zombieInfectionResetRate = 100f;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("モンスターカードB: 感染度%の確率で山札に複製")]
        public bool zombieDuplicateOnPlay = false;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("モンスターカードC: 感染度%の確率で手札のカードを変質")]
        public bool zombieTransformOnPlay = false;
        
        [BoxGroup("Zombie Monster Card")]
        [Tooltip("累積効果: プレイ回数×この値でインプ率獲得")]
        public float zombieStackingImpressionRate = 0f;

        public bool HasRisk() => cardType == CardType.Risk || riskType != RiskType.None;
    }

    [System.Serializable]
    public class GeneratedCard
    {
        [Tooltip("生成するカード")]
        public CardData card;
        
        [Tooltip("カードの追加先")]
        public CardDestination destination;
    }

    public enum CardDestination
    {
        Discard,    // 捨て札
        Hand,       // 手札
        DrawPile    // 山札の一番上
    }

    public enum CardType
    {
        Normal,
        Risk,
        Monster,
        Special,
        Passive
    }
    
    public enum CardRarity
    {
        Basic,    // 初期デッキ用カード
        Common,   // 基本カード
        Rare,     // レアカード
        Epic      // 超レアカード
    }

    public enum RiskType
    {
        None,
        Flaming,      // 炎上 (Decrease Mental or Follower)
        Freeze,     // 凍結 (Skip Turn)
        Ban,        // BAN (Game Over)
        LoseFollower // フォロワー減少
    }
}
