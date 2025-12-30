using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ApprovalMonster.Data;

namespace ApprovalMonster.Core
{
    [System.Serializable]
    public class DraftTierProbability
    {
        [Tooltip("この確率テーブルを適用する最小インプレッション")]
        public long minImpressions;
        
        [Header("Weights (0-100)")]
        [Range(0, 100)] public int commonWeight;
        [Range(0, 100)] public int rareWeight;
        [Range(0, 100)] public int epicWeight;

        public int GetTotalWeight() => commonWeight + rareWeight + epicWeight;
    }

    /// <summary>
    /// カードドラフトシステムを管理するクラス
    /// Tier1=Common, Tier2=Rare, Tier3=Epicからカードを選択
    /// </summary>
    public class DraftManager : MonoBehaviour
    {
        [Header("Draft Settings")]
        [Tooltip("ドラフトで提示するカード枚数")]
        public int draftCardCount = 3;

        [Header("Probability Settings")]
        [Tooltip("インプレッションスコアに応じたTier排出確率テーブル（昇順でソート推奨）")]
        public List<DraftTierProbability> probabilityTable = new List<DraftTierProbability>();

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
        /// 確率テーブルに基づいてTierを抽選し、カードを選択
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
            var probability = GetProbabilityForImpressions(currentImpressions);

            for (int i = 0; i < draftCardCount && availablePool.Count > 0; i++)
            {
                // 各スロットごとにTierを抽選
                int tier = RollTier(probability);
                
                var card = SelectCardByTier(availablePool, tier);
                if (card != null)
                {
                    options.Add(card);
                    availablePool.Remove(card);
                }
                // カードが見つからない場合はスキップ
            }

            Debug.Log($"[DraftManager] Generated {options.Count} draft options (Score: {currentImpressions}) from {availablePool.Count + options.Count} available cards");
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
        /// 現在のインプレッションに対応する確率設定を取得
        /// </summary>
        public DraftTierProbability GetProbabilityForImpressions(long impressions)
        {
            // テーブルが空ならデフォルト（Common 100%）を返す
            if (probabilityTable == null || probabilityTable.Count == 0)
            {
                return new DraftTierProbability { minImpressions = 0, commonWeight = 100, rareWeight = 0, epicWeight = 0 };
            }

            // 該当する範囲で最も条件の厳しい（インプレッションが高い）ものを探す
            // probabilityTableは昇順であると仮定して、条件を満たす最後の要素を取得
            var match = probabilityTable
                .Where(p => impressions >= p.minImpressions)
                .OrderByDescending(p => p.minImpressions)
                .FirstOrDefault();

            if (match == null)
            {
                return probabilityTable[0]; // 最低設定を使用
            }

            return match;
        }
        
        /// <summary>
        /// 現在のインプレッションに対応するランクインデックス (0-indexed) を取得
        /// </summary>
        public int GetCurrentRankIndex(long impressions)
        {
            if (probabilityTable == null || probabilityTable.Count == 0)
                return 0;
            
            // 昇順ソート済みと仮定し、条件を満たす最大のインデックスを返す
            int rankIndex = 0;
            for (int i = 0; i < probabilityTable.Count; i++)
            {
                if (impressions >= probabilityTable[i].minImpressions)
                {
                    rankIndex = i;
                }
            }
            return rankIndex;
        }
        
        /// <summary>
        /// ドラフトプール内の残りカード枚数をレアリティ別に取得
        /// </summary>
        public (int common, int rare, int epic) GetRemainingCardCounts(List<CardData> pool)
        {
            if (pool == null)
                return (0, 0, 0);
            
            // 選択済みカードを除外
            var available = pool.Where(card => !selectedCards.Contains(card)).ToList();
            
            int common = available.Count(c => c.rarity == CardRarity.Common);
            int rare = available.Count(c => c.rarity == CardRarity.Rare);
            int epic = available.Count(c => c.rarity == CardRarity.Epic);
            
            return (common, rare, epic);
        }

        /// <summary>
        /// 確率に基づいてTier (1-3) を抽選する
        /// </summary>
        private int RollTier(DraftTierProbability prob)
        {
            int totalWeight = prob.GetTotalWeight();
            if (totalWeight <= 0) return 1; // フォールバック

            int roll = Random.Range(0, totalWeight);

            if (roll < prob.commonWeight)
            {
                return 1; // Common
            }
            roll -= prob.commonWeight;

            if (roll < prob.rareWeight)
            {
                return 2; // Rare
            }
            
            return 3; // Epic
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

            // Tierに応じたプールを選択（フォールバック付き）
            List<CardData> targetPool = null;
            
            // 再帰的にフォールバックするよりも、優先順位を決めて探す
            if (tier == 3) // Epic狙い
            {
                if (epicCards.Count > 0) targetPool = epicCards;
                else if (rareCards.Count > 0) targetPool = rareCards;
                else if (commonCards.Count > 0) targetPool = commonCards;
            }
            else if (tier == 2) // Rare狙い
            {
                if (rareCards.Count > 0) targetPool = rareCards;
                else if (commonCards.Count > 0) targetPool = commonCards;
            }
            else // Common狙い
            {
                if (commonCards.Count > 0) targetPool = commonCards;
                else if (rareCards.Count > 0) targetPool = rareCards; // Common枯渇なら上位へ？いや仕様では下位へ。Commonの下はないので、上位へ行くべきか？
                // 元の仕様: Commonがない場合のフォールバックは記述なし（null）。
                // しかし「カード切れ」の場合はリセットされる前提。
                // 特定レアリティだけ枯渇するケース:
                // Common枯渇 -> Rare -> Epic と逆フォールバックする方が親切だが、元の仕様通りにするならnull。
                // ユーザー要望「下位ランクにフォールバック」に従うならCommonのフォールバックはない。
                // しかし、何も出ないよりは何か出たほうがいいので、Common枯渇時は上位へ行くロジックを追加してもいいが、
                // 今回は元のロジック「Commonがないならnull」を維持する（プール枯渇は別判定でリセットされるので）
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
