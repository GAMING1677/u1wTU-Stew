using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ApprovalMonster.Data;
using ApprovalMonster.Core;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ドラフトUIを管理するクラス
    /// カード候補を表示し、プレイヤーの選択を受け付ける
    /// </summary>
    public class DraftUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI remainingDraftsText;
        [SerializeField] private TextMeshProUGUI currentRankText;
        [SerializeField] private Button openRankTableButton;
        [SerializeField] private DraftRankTableUI draftRankTableUI;
        [SerializeField] private Transform cardOptionsContainer;
        [SerializeField] private CardView cardViewPrefab;
        
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite normalBackground;
        [SerializeField] private Sprite monsterBackground;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        private List<CardData> _currentDraftPool; // ランクテーブル用に保持

        private List<CardView> activeCardViews = new List<CardView>();
        private CardData selectedCard;
        private bool _isMonsterDraft = false;  // ★ モンスタードラフトかどうかを追跡
        
        /// <summary>
        /// DraftPanel (the actual UI GameObject referenced by canvasGroup)
        /// </summary>
        public GameObject DraftPanel => canvasGroup != null ? canvasGroup.gameObject : null;

        // Removed: Awake() - visibility is now controlled by UIManager

        /// <summary>
        /// ドラフト候補カードを表示
        /// </summary>
        public void ShowDraftOptions(List<CardData> options, bool isMonsterDraft = false)
        {
            Debug.Log($"[DraftUI] ===== ShowDraftOptions CALLED ===== isMonsterDraft={isMonsterDraft}, gameObject.activeSelf={gameObject.activeSelf}");
            
            // ★ フラグを保存（コールバック時に使用）
            _isMonsterDraft = isMonsterDraft;
            
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("[DraftUI] No draft options provided!");
                return;
            }

            // モンスタードラフトならタイトルと背景を変更
            if (isMonsterDraft)
            {
                titleText.text = "選んだカードを3枚入手し、山札・手札・捨て札に加える";
                titleText.color = Color.red;
                
                // モンスタードラフト時は残りドラフト表示を非表示
                if (remainingDraftsText != null)
                {
                    remainingDraftsText.gameObject.SetActive(false);
                }
                
                // モンスタードラフト時はランク表示とボタンを非表示
                if (currentRankText != null)
                {
                    currentRankText.gameObject.SetActive(false);
                }
                if (openRankTableButton != null)
                {
                    openRankTableButton.gameObject.SetActive(false);
                }
                
                // モンスタードラフト時はランクテーブルを非表示
                if (draftRankTableUI != null)
                {
                    draftRankTableUI.Hide();
                }
                
                // 背景をモンスター用に変更
                if (backgroundImage != null && monsterBackground != null)
                {
                    backgroundImage.sprite = monsterBackground;
                }
            }
            else
            {
                // ドラフトプールを保持（ボタンクリック時用）
                _currentDraftPool = GameManager.Instance?.currentStage?.draftPool;
                
                // 残りドラフト回数を計算
                var gm = GameManager.Instance;
                int currentTurn = gm?.turnManager?.CurrentTurnCount ?? 1;
                int lastDraftTurn = gm?.gameSettings?.lastDraftTurn ?? 10;
                int totalDrafts = lastDraftTurn; // 合計ドラフト回数
                int currentDraftNumber = currentTurn; // 現在のドラフト回数（＝現在のターン）
                
                titleText.text = "選択したカードが山札の一番上にセットされます";
                titleText.color = Color.white;
                
                // 別テキストに現在のドラフト回数を表示
                if (remainingDraftsText != null)
                {
                    remainingDraftsText.text = $"{currentDraftNumber}/{totalDrafts}回目";
                    remainingDraftsText.gameObject.SetActive(true);
                }
                
                // 現在のランクを表示
                UpdateCurrentRankText();
                if (currentRankText != null)
                {
                    currentRankText.gameObject.SetActive(true);
                }
                
                // ランクテーブルを開くボタンを表示
                if (openRankTableButton != null)
                {
                    openRankTableButton.gameObject.SetActive(true);
                }
                
                // テーブルはボタンで開くので、最初は非表示
                if (draftRankTableUI != null)
                {
                    draftRankTableUI.gameObject.SetActive(false);
                }
                
                // 背景を通常用に変更
                if (backgroundImage != null && normalBackground != null)
                {
                    backgroundImage.sprite = normalBackground;
                }
            }
            
            Debug.Log($"[DraftUI] ShowDraftOptions called. Count: {options?.Count ?? 0}, isMonsterDraft: {isMonsterDraft}");
            
            // Defensive check: if no options, complete draft immediately
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("[DraftUI] No draft options provided! Completing draft immediately.");
                // Call CompleteDraft to prevent freeze
                GameManager.Instance?.turnManager?.CompleteDraft();
                return;
            }
            
            if (cardViewPrefab == null)
            {
                Debug.LogError("[DraftUI] CardView Prefab is not assigned! Please assign it in the Inspector.");
                return;
            }




            // 既存のカードビューをクリア
            ClearCardViews();

            // Show DraftPanel and fade in
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[DraftUI] DraftPanel shown, alpha reset to 0, starting fade in");
            canvasGroup.DOFade(1f, fadeInDuration);

            // ドラフト表示SE再生
            AudioManager.Instance?.PlaySE(SEType.CardDraftPanelShow);

            // カードビューを生成
            Debug.Log($"[DraftUI] Starting card generation. cardOptionsContainer null? {cardOptionsContainer == null}");
            Debug.Log($"[DraftUI] cardViewPrefab null? {cardViewPrefab == null}");
            Debug.Log($"[DraftUI] Options count: {options.Count}");
            
            foreach (var cardData in options)
            {
                Debug.Log($"[DraftUI] Instantiating card: {cardData.cardName}");
                var cardView = Instantiate(cardViewPrefab, cardOptionsContainer);
                Debug.Log($"[DraftUI] Card instantiated: {cardView != null}, active: {cardView?.gameObject.activeSelf}");
                
                cardView.Setup(cardData, showTag: true); // Show tag/rarity in draft
                
                
                // CardViewのクリックイベントを無効化（ドラフト中はプレイさせない）
                cardView.enabled = false;
                
                // クリックイベントを設定
                var button = cardView.GetComponent<Button>();
                if (button == null)
                {
                    button = cardView.gameObject.AddComponent<Button>();
                }
                
                // ラムダでキャプチャ
                var capturedCard = cardData;
                button.onClick.AddListener(() => OnCardSelected(capturedCard));
                
                activeCardViews.Add(cardView);

                // 出現アニメーション
                cardView.transform.localScale = Vector3.zero;
                cardView.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(0.1f * activeCardViews.Count);
            }

            Debug.Log($"[DraftUI] Showing {options.Count} draft options, gameObject.activeSelf={gameObject.activeSelf}, activeCardViews.Count={activeCardViews.Count}");
        }

        /// <summary>
        /// カードが選択された時の処理
        /// </summary>
        private void OnCardSelected(CardData card)
        {
            if (selectedCard != null)
            {
                // 既に選択済みの場合は無視
                return;
            }

            selectedCard = card;
            Debug.Log($"[DraftUI] Card selected: {card.cardName}");
            
            // ★ NEW: ドラフト選択時のSE再生（フィールドを使用）
            if (_isMonsterDraft)
            {
                // モンスタードラフトの場合、MonsterModePresetのclickSoundを使用
                var preset = GameManager.Instance?.currentStage?.monsterModePreset;
                if (preset != null && preset.clickSound != null)
                {
                    AudioManager.Instance?.PlaySE(preset.clickSound);
                    Debug.Log($"[DraftUI] Playing monster draft click sound: {preset.clickSound.name}");
                }
                else
                {
                    // フォールバック：通常のボタンSE
                    AudioManager.Instance?.PlaySE(SEType.ButtonClick);
                }
            }
            else
            {
                // 通常ドラフトの場合
                AudioManager.Instance?.PlaySE(SEType.ButtonClick);
            }

            // 選択されたカード以外をフェードアウト
            foreach (var cardView in activeCardViews)
            {
                if (cardView.CardData != card)
                {
                    cardView.GetComponent<CanvasGroup>()?.DOFade(0.3f, 0.2f);
                }
                else
                {
                    // 選択されたカードを強調
                    cardView.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                }
            }

            // 少し待ってからUIを閉じる
            Debug.Log($"[DraftUI] Starting delayed callback for card: {card.cardName}");
            DOVirtual.DelayedCall(0.5f, () =>
            {
                Debug.Log($"[DraftUI] Delayed callback executed. selectedCard: {selectedCard?.cardName ?? "NULL"}");
                // Fade out animation
                HideDraftUI(() =>
                {
                    Debug.Log("[DraftUI] HideDraftUI callback executed");
                    // After fade complete, hide DraftPanel (not this script's empty gameObject)
                    canvasGroup.gameObject.SetActive(false);
                    
                    // Restore hand container
                    var uiManager = FindObjectOfType<UIManager>();
                    if (uiManager != null && uiManager.handContainer != null)
                    {
                        uiManager.handContainer.gameObject.SetActive(true);
                        Debug.Log("[DraftUI] Hand container restored");
                    }
                    
                    // モンスタードラフトか通常ドラフトかで分岐（★ フィールドを使用）
                    Debug.Log($"[DraftUI] Calling GameManager. isMonsterDraft: {_isMonsterDraft}");
                    if (_isMonsterDraft)
                    {
                        Debug.Log($"[DraftUI] Calling OnMonsterDraftComplete with {selectedCard?.cardName}");
                        Core.GameManager.Instance.OnMonsterDraftComplete(selectedCard);
                    }
                    else
                    {
                        Debug.Log($"[DraftUI] Calling OnDraftComplete with {selectedCard?.cardName}");
                        Core.GameManager.Instance.OnDraftComplete(selectedCard);
                    }
                });
            });
        }


        /// <summary>
        /// ドラフトUIを非表示
        /// </summary>
        public void HideDraftUI(System.Action onComplete = null)
        {
            Debug.Log("[DraftUI] ===== HideDraftUI CALLED =====");
            
            // ランクテーブルも非表示にする
            if (draftRankTableUI != null)
            {
                draftRankTableUI.Hide();
            }
            
            canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                Debug.Log("[DraftUI] Fade complete, disabling raycasts");
                canvasGroup.blocksRaycasts = false;
                ClearCardViews();
                
                // IMPORTANT: Invoke callback BEFORE clearing selectedCard
                // Otherwise the callback receives null
                onComplete?.Invoke();
                
                selectedCard = null;
            });
        }

        /// <summary>
        /// カードビューをクリア
        /// </summary>
        private void ClearCardViews()
        {
            foreach (var cardView in activeCardViews)
            {
                if (cardView != null)
                {
                    Destroy(cardView.gameObject);
                }
            }
            activeCardViews.Clear();
        }
        
        private void Awake()
        {
            // ボタンのクリックイベントを設定
            if (openRankTableButton != null)
            {
                openRankTableButton.onClick.AddListener(OnOpenRankTableButtonClicked);
            }
        }
        
        /// <summary>
        /// 現在のランクテキストを更新
        /// </summary>
        private void UpdateCurrentRankText()
        {
            if (currentRankText == null) return;
            
            var gm = GameManager.Instance;
            if (gm == null || gm.draftManager == null)
            {
                currentRankText.text = "ランク: -";
                return;
            }
            
            long currentImpressions = gm.resourceManager?.totalImpressions ?? 0;
            int rankIndex = gm.draftManager.GetCurrentRankIndex(currentImpressions);
            int maxRank = gm.draftManager.probabilityTable?.Count ?? 0;
            
            // 最高ランクならMAX表示
            if (maxRank > 0 && rankIndex == maxRank - 1)
            {
                currentRankText.text = "ランク: MAX";
            }
            else
            {
                currentRankText.text = $"ランク: {rankIndex + 1}";
            }
        }
        
        /// <summary>
        /// ランクテーブルを開くボタンがクリックされた
        /// </summary>
        private void OnOpenRankTableButtonClicked()
        {
            if (draftRankTableUI != null && _currentDraftPool != null)
            {
                draftRankTableUI.Show(_currentDraftPool);
            }
        }
    }
}
