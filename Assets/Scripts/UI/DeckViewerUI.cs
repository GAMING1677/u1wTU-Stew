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
    /// 山札・捨て札の内容を一覧表示するパネル
    /// シングルトンパターン - 複数インスタンス対応
    /// </summary>
    public class DeckViewerUI : MonoBehaviour
    {
        public static DeckViewerUI Instance { get; private set; }
        
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
        
        public enum ViewType
        {
            DrawPile,
            DiscardPile
        }
        
        private void Awake()
        {
            // シングルトンパターン
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"[DeckViewerUI] Instance set to: {gameObject.name}");
            }
            else if (Instance != this)
            {
                Debug.Log($"[DeckViewerUI] Duplicate found, disabling: {gameObject.name}");
                enabled = false;
                return;
            }
            
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
        /// 山札を表示
        /// </summary>
        public void ShowDrawPile()
        {
            Debug.Log("[DeckViewerUI] ShowDrawPile() called");
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[DeckViewerUI] GameManager.Instance is null!");
                return;
            }
            if (gm.deckManager == null)
            {
                Debug.LogError("[DeckViewerUI] deckManager is null!");
                return;
            }
            
            Debug.Log($"[DeckViewerUI] Showing draw pile with {gm.deckManager.drawPile.Count} cards");
            Show(gm.deckManager.drawPile, ViewType.DrawPile);
        }
        
        /// <summary>
        /// 捨て札を表示
        /// </summary>
        public void ShowDiscardPile()
        {
            Debug.Log("[DeckViewerUI] ShowDiscardPile() called");
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[DeckViewerUI] GameManager.Instance is null!");
                return;
            }
            if (gm.deckManager == null)
            {
                Debug.LogError("[DeckViewerUI] deckManager is null!");
                return;
            }
            
            Debug.Log($"[DeckViewerUI] Showing discard pile with {gm.deckManager.discardPile.Count} cards");
            Show(gm.deckManager.discardPile, ViewType.DiscardPile);
        }
        
        /// <summary>
        /// カードリストを表示
        /// </summary>
        private void Show(List<CardData> cards, ViewType viewType)
        {
            // タイトル設定
            if (titleText != null)
            {
                string typeName = viewType == ViewType.DrawPile ? "山札" : "捨て札";
                titleText.text = $"◆ {typeName} ({cards.Count}枚)";
            }
            
            // 既存アイテムをクリア
            ClearItems();
            
            // カードをグループ化
            var groupedCards = cards
                .GroupBy(c => c)
                .OrderBy(g => g.Key.rarity)
                .ThenBy(g => g.Key.cardName);
            
            // アイテム生成
            foreach (var group in groupedCards)
            {
                var item = Instantiate(itemPrefab, contentContainer);
                item.Setup(group.Key, group.Count());
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
