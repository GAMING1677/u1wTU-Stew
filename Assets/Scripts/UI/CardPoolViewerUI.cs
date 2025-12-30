using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using ApprovalMonster.Data;
using ApprovalMonster.Core;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// カードプール（全カード一覧）を表示するパネル
    /// </summary>
    public class CardPoolViewerUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Content")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private DeckViewerItemUI itemPrefab;
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;
        
        private CanvasGroup _canvasGroup;
        private List<DeckViewerItemUI> _activeItems = new List<DeckViewerItemUI>();
        
        private void Awake()
        {
            _canvasGroup = panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            
            // 初期状態で非表示
            panel.SetActive(false);
        }
        
        /// <summary>
        /// カードプールを表示
        /// </summary>
        public void ShowCardPool()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.deckManager == null) return;
            
            var stageManager = StageManager.Instance;
            if (stageManager == null || stageManager.SelectedStage == null) return;
            
            var stage = stageManager.SelectedStage;
            
            // 全カードプールを取得（初期デッキ + ドラフトプール + モンスターデッキ）
            var allCards = new List<CardData>();
            
            // 初期デッキを追加
            if (stage.initialDeck != null)
            {
                allCards.AddRange(stage.initialDeck);
            }
            
            // ドラフトプールを追加
            if (stage.draftPool != null)
            {
                allCards.AddRange(stage.draftPool);
            }
            
            // モンスターデッキを追加
            if (stage.monsterDeck != null)
            {
                allCards.AddRange(stage.monsterDeck);
            }
            
            if (allCards.Count == 0)
            {
                Debug.LogWarning("[CardPoolViewerUI] No cards available in this stage!");
                return;
            }
            
            // 現在のデッキに含まれるカードを取得
            var ownedCards = GetOwnedCards();
            
            Show(allCards, ownedCards);
        }
        
        /// <summary>
        /// 現在のデッキに含まれるカードリストを取得
        /// </summary>
        private HashSet<CardData> GetOwnedCards()
        {
            var gm = GameManager.Instance;
            var owned = new HashSet<CardData>();
            
            if (gm != null && gm.deckManager != null)
            {
                // 山札、捨て札、手札の全カードを収集
                owned.UnionWith(gm.deckManager.drawPile);
                owned.UnionWith(gm.deckManager.discardPile);
                owned.UnionWith(gm.deckManager.hand);
            }
            
            return owned;
        }
        
        /// <summary>
        /// カードリストを表示
        /// </summary>
        private void Show(List<CardData> allCards, HashSet<CardData> ownedCards)
        {
            // タイトル設定
            if (titleText != null)
            {
                int ownedCount = allCards.Count(c => ownedCards.Contains(c));
                titleText.text = $"◆ カードプール ({ownedCount}/{allCards.Count} 入手済み)";
            }
            
            // 既存アイテムをクリア
            ClearItems();
            
            // カードをグループ化してソート
            var groupedCards = allCards
                .GroupBy(c => c)
                .OrderBy(g => !ownedCards.Contains(g.Key))  // 入手済み → 未入手
                .ThenBy(g => g.Key.rarity)                   // レアリティ順
                .ThenBy(g => g.Key.cardName);                // 名前順
            
            // アイテム生成
            foreach (var group in groupedCards)
            {
                var item = Instantiate(itemPrefab, contentContainer);
                bool isOwned = ownedCards.Contains(group.Key);
                int count = group.Count();
                
                // 拡張版Setupを使用（レアリティ表示ON、入手状態を指定）
                item.Setup(group.Key, count, showRarity: true, isOwned: isOwned);
                _activeItems.Add(item);
            }
            
            // パネル表示
            panel.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, fadeDuration);
        }
        
        /// <summary>
        /// パネルを非表示
        /// </summary>
        public void Hide()
        {
            _canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                panel.SetActive(false);
                ClearItems();
            });
        }
        
        private void ClearItems()
        {
            foreach (var item in _activeItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _activeItems.Clear();
        }
    }
}
