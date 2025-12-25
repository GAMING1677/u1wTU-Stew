using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ApprovalMonster.Data;

namespace ApprovalMonster.Core
{
    /// <summary>
    /// カードドラフトシステムを管理するクラス
    /// Tier1=Common, Tier2=Rare, Tier3=Epicからカードを選択
    /// </summary>
    public class DraftManager : MonoBehaviour
    {
        [Header("Draft Settings")]
        [Tooltip("ドラフトで提示するカード枚数")]
        public int draftCardCount = 3;

        [Header("Impression Tier Thresholds")]
        [Tooltip("Tier1(Common) の上限インプレッション")]
        public long tier1MaxImpressions = 10000;
        
        [Tooltip("Tier2(Rare) の上限インプレッション（超えたらTier3=Epic）")]
        public long tier2MaxImpressions = 50000;

        private List<CardData> selectedCards = new List<CardData>();

        /// <summary>
        /// ゲーム開始時に選択履歴をリセット
        /// </summary>
        public void ResetSelectedCards()
        {
            selectedCards.Clear();
            Debug.Log("[DraftManager] Selected cards history cleared");
        }

        /// <summary>
        /// ドラフト候補カードを生成
        /// Tier1=Common, Tier2=Rare, Tier3=Epicから選択
        /// カードが足りない場合は1ランク下のTierにフォールバック
        /// </summary>
        public List<CardData> GenerateDraftOptions(List<CardData> pool, long currentImpressions)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[DraftManager] Draft pool is empty!");
                return new List<CardData>();
            }

            // 既に選択されたカードを除外
            var availablePool = pool.Where(card => !selectedCards.Contains(card)).ToList();
            
            if (availablePool.Count == 0)
            {
                Debug.LogWarning("[DraftManager] All cards have been selected! Resetting pool.");
                selectedCards.Clear();
                availablePool = new List<CardData>(pool);
            }

            var options = new List<CardData>();
            int tier = GetTierForImpressions(currentImpressions);

            for (int i = 0; i < draftCardCount && availablePool.Count > 0; i++)
            {
                var card = SelectCardByTier(availablePool, tier);
                if (card != null)
                {
                    options.Add(card);
                    availablePool.Remove(card);
                }
                // カードが見つからない場合はスキップ
            }

            Debug.Log($"[DraftManager] Generated {options.Count} draft options (Tier {tier}) from {availablePool.Count + options.Count} available cards");
            return options;
        }

        /// <summary>
        /// モンスタードラフト用の候補カードを生成
        /// MonsterDeck の全カードをドラフト対象として返す
        /// </summary>
        public List<CardData> GenerateMonsterDraftOptions(List<CardData> monsterDeck, int count)
        {
            if (monsterDeck == null || monsterDeck.Count == 0)
            {
                Debug.LogWarning("[DraftManager] Monster deck is empty!");
                return new List<CardData>();
            }
            
            // MonsterDeckの全カードをドラフト対象として返す
            var options = new List<CardData>(monsterDeck);
            
            Debug.Log($"[DraftManager] Generated {options.Count} monster draft options (all cards from MonsterDeck)");
            return options;
        }

        /// <summary>
        /// プレイヤーが選択したカードを山札の一番上に追加
        /// </summary>
        /// <param name="selectedCard">選択されたカード</param>
        public void SelectCard(CardData selectedCard)
        {
            if (selectedCard == null)
            {
                Debug.LogError("[DraftManager] Selected card is null!");
                return;
            }

            var deckManager = GameManager.Instance.deckManager;
            
            // 山札の一番上に追加
            deckManager.drawPile.Insert(0, selectedCard);
            
            // 選択履歴に追加
            selectedCards.Add(selectedCard);
            
            Debug.Log($"[DraftManager] Added '{selectedCard.cardName}' to top of draw pile. Total selected: {selectedCards.Count}");
        }

        /// <summary>
        /// インプレッション値に応じたTierを取得 (1, 2, 3)
        /// </summary>
        private int GetTierForImpressions(long impressions)
        {
            if (impressions <= tier1MaxImpressions)
                return 1;
            else if (impressions <= tier2MaxImpressions)
                return 2;
            else
                return 3;
        }

        /// <summary>
        /// Tierに基づいてカードを選択
        /// Tier1=Common, Tier2=Rare, Tier3=Epic
        /// カードが足りない場合は1ランク下にフォールバック
        /// </summary>
        private CardData SelectCardByTier(List<CardData> pool, int tier)
        {
            // レアリティごとにカードを分類
            var commonCards = pool.Where(c => c.rarity == CardRarity.Common).ToList();
            var rareCards = pool.Where(c => c.rarity == CardRarity.Rare).ToList();
            var epicCards = pool.Where(c => c.rarity == CardRarity.Epic).ToList();

            // Tierに応じたプールを選択
            List<CardData> targetPool = null;
            
            switch (tier)
            {
                case 3: // Epic
                    if (epicCards.Count > 0)
                        targetPool = epicCards;
                    else if (rareCards.Count > 0)
                        targetPool = rareCards; // フォールバック: Rare
                    else if (commonCards.Count > 0)
                        targetPool = commonCards; // フォールバック: Common
                    break;
                    
                case 2: // Rare
                    if (rareCards.Count > 0)
                        targetPool = rareCards;
                    else if (commonCards.Count > 0)
                        targetPool = commonCards; // フォールバック: Common
                    break;
                    
                case 1: // Common
                default:
                    if (commonCards.Count > 0)
                        targetPool = commonCards;
                    break;
            }
            
            // ターゲットプールが見つからない場合はnullを返す（スキップ）
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.Log($"[DraftManager] No cards available for Tier {tier} or fallback");
                return null;
            }
            
            // ランダムに1枚選択
            return targetPool[Random.Range(0, targetPool.Count)];
        }
    }
}
