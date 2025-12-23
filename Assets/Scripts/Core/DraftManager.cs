using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ApprovalMonster.Data;

namespace ApprovalMonster.Core
{
    /// <summary>
    /// カードドラフトシステムを管理するクラス
    /// インプレッション値に基づいてレアリティの重み付けを行い、候補カードを生成
    /// </summary>
    public class DraftManager : MonoBehaviour
    {
        [Header("Draft Settings")]
        [Tooltip("ドラフトで提示するカード枚数")]
        public int draftCardCount = 3;

        [Header("Rarity Weights by Impression Tier")]
        [Tooltip("インプレッション 0-10000")]
        public RarityWeights tier1Weights = new RarityWeights(70f, 25f, 5f);
        
        [Tooltip("インプレッション 10001-50000")]
        public RarityWeights tier2Weights = new RarityWeights(50f, 35f, 15f);
        
        [Tooltip("インプレッション 50001+")]
        public RarityWeights tier3Weights = new RarityWeights(30f, 40f, 30f);

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
        /// </summary>
        /// <param name="pool">ドラフトプール（StageDataから取得）</param>
        /// <param name="currentImpressions">現在のインプレッション値</param>
        /// <returns>ドラフト候補カードのリスト</returns>
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
            var weights = GetWeightsForImpressions(currentImpressions);

            for (int i = 0; i < draftCardCount && availablePool.Count > 0; i++)
            {
                var card = SelectWeightedCard(availablePool, weights);
                if (card != null)
                {
                    options.Add(card);
                    // 同じカードが複数回選ばれないように一時的に除外
                    availablePool.Remove(card);
                }
            }

            Debug.Log($"[DraftManager] Generated {options.Count} draft options from {availablePool.Count + selectedCards.Count} total cards");
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
        /// インプレッション値に応じた重み設定を取得
        /// </summary>
        private RarityWeights GetWeightsForImpressions(long impressions)
        {
            if (impressions <= 10000)
                return tier1Weights;
            else if (impressions <= 50000)
                return tier2Weights;
            else
                return tier3Weights;
        }

        /// <summary>
        /// 重み付けに基づいてカードを選択
        /// </summary>
        private CardData SelectWeightedCard(List<CardData> pool, RarityWeights weights)
        {
            // レアリティごとにカードを分類
            var commonCards = pool.Where(c => c.rarity == CardRarity.Common).ToList();
            var rareCards = pool.Where(c => c.rarity == CardRarity.Rare).ToList();
            var epicCards = pool.Where(c => c.rarity == CardRarity.Epic).ToList();

            // 重み付け抽選
            float totalWeight = weights.commonWeight + weights.rareWeight + weights.epicWeight;
            float randomValue = Random.Range(0f, totalWeight);

            if (randomValue < weights.commonWeight && commonCards.Count > 0)
            {
                return commonCards[Random.Range(0, commonCards.Count)];
            }
            else if (randomValue < weights.commonWeight + weights.rareWeight && rareCards.Count > 0)
            {
                return rareCards[Random.Range(0, rareCards.Count)];
            }
            else if (epicCards.Count > 0)
            {
                return epicCards[Random.Range(0, epicCards.Count)];
            }

            // フォールバック: どのレアリティも該当しない場合はプール全体からランダム
            return pool[Random.Range(0, pool.Count)];
        }
    }

    /// <summary>
    /// レアリティごとの重み設定
    /// </summary>
    [System.Serializable]
    public class RarityWeights
    {
        public float commonWeight;
        public float rareWeight;
        public float epicWeight;

        public RarityWeights(float common, float rare, float epic)
        {
            commonWeight = common;
            rareWeight = rare;
            epicWeight = epic;
        }
    }
}
