using UnityEngine;
using UnityEngine.UI;
using ApprovalMonster.UI; // Explicitly add namespace
using TMPro;
using ApprovalMonster.Core;
using ApprovalMonster.Data;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardView cardPrefab;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI followersText;
        [SerializeField] private TextMeshProUGUI mentalText;
        [SerializeField] private TextMeshProUGUI motivationText;
        [SerializeField] private TextMeshProUGUI impressionText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI drawPileCountText;
        [SerializeField] private TextMeshProUGUI discardPileCountText;
        [SerializeField] private Image deckPileImage;
        [SerializeField] private Image discardPileImage;
        
        [Header("Deck Viewer")]
        [SerializeField] private DeckViewerUI deckViewerUI;
        [SerializeField] private Button deckPileButton;
        [SerializeField] private Button discardPileButton;

        private int prevDeckCount = -1; // -1 to force initial set without animation if needed, or handle in setup
        private int prevDiscardCount = -1;

        [Header("Gain Effects - Static")]
        [SerializeField] private GainEffectUI followerGainUI;
        [SerializeField] private GainEffectUI impressionGainUI;
        
        [Header("Fill Images")]
        [SerializeField] private Image mentalFillImage;
        [SerializeField] private Image motivationFillImage;
        [Tooltip("モチベ不足時に振動させるオブジェクト")]
        [SerializeField] private RectTransform motivationContainer;
        
        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        
        [Header("Draft")]
        [SerializeField] private DraftUI draftUI;
        [SerializeField] public Transform handContainer; // Public for DraftUI access
        
        [Header("Card Layout")]
        [SerializeField] private float defaultCardSpacing = 20f;
        [SerializeField] private float minCardSpacing = -100f; // Negative for overlap
        
        [Header("Fan Layout")]
        [SerializeField] private float maxRotationAngle = 10f; // Max rotation at edges (degrees)
        [SerializeField] private float arcHeight = 50f; // How much lower the edges are
        
        [Header("Quota")]
        [SerializeField] private TextMeshProUGUI quotaText; // Displays "Remaining: XXX"
        [SerializeField] private GameObject penaltyRiskContainer; // Container for penalty risk (includes background)
        [SerializeField] private TextMeshProUGUI penaltyRiskText; // Displays "Penalty Risk: XX Mental"
        
        [Header("Clear Goal")]
        [SerializeField] private TextMeshProUGUI clearGoalText; // Displays "目標：xxx,xxx" if hasScoreGoal
        
        [Header("Timeline")]
        [SerializeField] private GameObject postPrefab;
        [SerializeField] private Transform timelineContainer;
        [SerializeField] private int maxPosts = 5;
        

        
        [Header("Settings")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private SoundSettingsUI soundSettingsUI;

        [Header("Cut-In")]
        [SerializeField] private CutInUI cutInUI;

        [Header("Character")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("End Turn Button Pulse")]
        [SerializeField] private ButtonPulse endTurnButtonPulse;
        
        [Header("Flaming UI")]
        [Tooltip("種の数を表示するテキスト（オプショナル）")]
        [SerializeField] private TextMeshProUGUI flamingSeedText;
        [Tooltip("炎上ダメージを表示するテキスト（オプショナル）")]
        [SerializeField] private TextMeshProUGUI flamingLevelText;
        [Tooltip("炎上中のみ表示するコンテナ（オプショナル）")]
        [SerializeField] private GameObject flamingContainer;
        
        [Header("Tutorial")]
        [Tooltip("チュートリアルプレイヤー")]
        [SerializeField] private TutorialPlayer tutorialPlayer;
        
        [Header("Tracked Card Count")]
        [Tooltip("山札内の枚数を表示するテキスト")]
        [SerializeField] private TextMeshProUGUI trackedCardDrawPileText;
        [Tooltip("捨て札内の枚数を表示するテキスト")]
        [SerializeField] private TextMeshProUGUI trackedCardDiscardPileText;
        [Tooltip("追跡カードUIのコンテナ（表示/非表示制御用）")]
        [SerializeField] private GameObject trackedCardUIContainer;
        
        // 追跡対象カード（ステージから設定）
        private CardData trackedCard;
        
        private bool isSetup = false;

        private List<CardView> activeCards = new List<CardView>();
        
        // カウントアニメーション用の現在表示値
        private float displayedFollowers = 0;
        private float displayedImpression = 0;
        private float displayedMentalCurrent = 0;
        private float displayedQuotaRemaining = 0;
        private float displayedClearGoalRemaining = 0;
        
        // アニメーション時間
        private const float COUNT_ANIM_DURATION = 0.5f;

        private void Awake()
        {
            Debug.Log("[UIManager] Awake called.");
        }

        public void SetupCharacter(CharacterProfile profile)
        {
            Debug.Log($"[UIManager] SetupCharacter called with profile: {(profile != null ? profile.name : "NULL")}");
            
            if (characterAnimator != null)
            {
                Debug.Log("[UIManager] Calling characterAnimator.SetProfile...");
                characterAnimator.SetProfile(profile);
                Debug.Log("[UIManager] characterAnimator.SetProfile completed");
            }
            else
            {
                Debug.LogError("[UIManager] characterAnimator is NULL! Cannot set profile.");
            }
        }
        
        public void ShowCharacterReaction(CharacterAnimator.ReactionType type)
        {
            ShowCharacterReaction(type, loop: false);
        }
        
        public void ShowCharacterReaction(CharacterAnimator.ReactionType type, bool loop)
        {
            if (characterAnimator != null)
            {
                Debug.Log($"[UIManager] ShowCharacterReaction called with {type}, loop: {loop}. Delegating to CharacterAnimator.");
                characterAnimator.PlayReaction(type, loop);
            }
            else
            {
                Debug.LogError("[UIManager] ShowCharacterReaction called but characterAnimator is NULL!");
            }
        }
        
        public void StopCharacterReaction()
        {
            if (characterAnimator != null)
            {
                Debug.Log("[UIManager] StopCharacterReaction called.");
                characterAnimator.StopCurrentReaction();
            }
        }
        
        /// <summary>
        /// タイムラインの投稿をすべてクリアする（ゲームリセット時用）
        /// </summary>
        public void ClearTimeline()
        {
            if (timelineContainer != null)
            {
                foreach (Transform child in timelineContainer)
                {
                    Destroy(child.gameObject);
                }
                Debug.Log("[UIManager] Timeline cleared.");
            }
        }
        
        /// <summary>
        /// エンドターンボタンのパルスアニメーションをリセットする（ゲームリセット時用）
        /// </summary>
        public void ResetEndTurnButtonPulse()
        {
            if (endTurnButtonPulse != null)
            {
                endTurnButtonPulse.StopPulse();
                Debug.Log("[UIManager] End turn button pulse reset.");
            }
        }

        private void OnEnable()
        {
            Debug.Log("[UIManager] OnEnable called.");
            // Subscribe to managers
            var gm = GameManager.Instance;
            if (gm != null)
            {
                Debug.Log("[UIManager] GameManager instance found. Subscribing to events.");
                // Remove first to avoid duplicates
                gm.resourceManager.onFollowersChanged.RemoveListener(UpdateFollowers);
                gm.resourceManager.onMentalChanged.RemoveListener(UpdateMental);
                gm.resourceManager.onMotivationChanged.RemoveListener(UpdateMotivation);
                gm.resourceManager.onImpressionsChanged.RemoveListener(UpdateImpression);
                gm.deckManager.OnCardDrawn -= OnCardDrawn;
                gm.deckManager.OnCardDiscarded -= OnCardDiscarded;
                gm.deckManager.OnReset -= OnReset;
                gm.onQuotaUpdate.RemoveListener(UpdateQuotaUI);

                gm.resourceManager.onFollowersChanged.AddListener(UpdateFollowers);
                gm.resourceManager.onMentalChanged.AddListener(UpdateMental);
                gm.resourceManager.onMotivationChanged.AddListener(UpdateMotivation);
                gm.resourceManager.onImpressionsChanged.AddListener(UpdateImpression);
                
                gm.onQuotaUpdate.AddListener(UpdateQuotaUI);
                
                // Gain Effects
                gm.resourceManager.onFollowerGained.AddListener(ShowFollowerGain);
                gm.resourceManager.onImpressionGained.AddListener(ShowImpressionGain);
                
                gm.deckManager.OnCardDrawn += OnCardDrawn;
                gm.deckManager.OnCardDiscarded += OnCardDiscarded;
                gm.deckManager.OnReset += OnReset;
                gm.deckManager.OnDeckCountChanged += UpdateDeckCounts;
                gm.deckManager.OnDeckShuffled += OnDeckShuffled;
                
                // Initialize quota display
                UpdateQuotaUI(gm.resourceManager.totalImpressions, gm.currentStage.quotaScore);
                
                // Subscribe to turn events
                gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
                gm.turnManager.OnTurnChanged.AddListener(UpdateTurnDisplay);
                
                // Subscribe to draft events
                gm.turnManager.OnDraftStart.RemoveListener(OnDraftStart);
                gm.turnManager.OnDraftStart.AddListener(OnDraftStart);
                
                // Monster mode profile switch is now called manually from GameManager.OnMonsterDraftComplete
                // (Event subscription removed to fix timing issue - profile should switch AFTER draft, not immediately)
                
                // Flaming event
                gm.resourceManager.onFlamingChanged -= UpdateFlamingDisplay;
                gm.resourceManager.onFlamingChanged += UpdateFlamingDisplay;
            }
            else
            {
                 Debug.LogError("[UIManager] GameManager Instance is NULL in OnEnable! Cannot subscribe.");
            }
        }

        private void Start()
        {
             Debug.Log("[UIManager] Start called.");
             
             // Initial Setup for Character
             if (StageManager.Instance != null && StageManager.Instance.SelectedStage != null)
             {
                 SetupCharacter(StageManager.Instance.SelectedStage.normalProfile);
                 SetupClearGoal(); // クリア目標の表示を設定
                 SetupFlamingUI(); // Flaming UIの表示/非表示を設定
                 SetupTrackedCardUI(); // 追跡カードUIの設定
             }
             
             // Setup button listener
             if (endTurnButton != null)
             {
                 endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
             }
             
             if (settingsButton != null)
             {
                 settingsButton.onClick.AddListener(OnSettingsButtonClicked);
             }
             
             // Deck Viewer buttons
             if (deckPileButton != null)
             {
                 deckPileButton.onClick.AddListener(OnDeckPileButtonClicked);
             }
             if (discardPileButton != null)
             {
                 discardPileButton.onClick.AddListener(OnDiscardPileButtonClicked);
             }
             
             // EndTurnボタンを常にパルス
             if (endTurnButtonPulse != null)
             {
                 endTurnButtonPulse.StartPulse();
             }
        }
        
        private void OnSettingsButtonClicked()
        {
            if (soundSettingsUI != null)
            {
                soundSettingsUI.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] SoundSettingsUI is not assigned!");
            }
        }
        
        private void OnDeckPileButtonClicked()
        {
            if (deckViewerUI != null)
            {
                deckViewerUI.ShowDrawPile();
            }
        }
        
        private void OnDiscardPileButtonClicked()
        {
            if (deckViewerUI != null)
            {
                deckViewerUI.ShowDiscardPile();
            }
        }

        private void OnDisable()
        {
             Debug.Log("[UIManager] OnDisable called.");
             var gm = GameManager.Instance;
             if (gm != null)
             {
                 gm.resourceManager.onFollowersChanged.RemoveListener(UpdateFollowers);
                 gm.resourceManager.onMentalChanged.RemoveListener(UpdateMental);
                 gm.resourceManager.onMotivationChanged.RemoveListener(UpdateMotivation);
                 gm.resourceManager.onImpressionsChanged.RemoveListener(UpdateImpression);
                 
                 // Gain Effects removal
                 gm.resourceManager.onFollowerGained.RemoveListener(ShowFollowerGain);
                 gm.resourceManager.onImpressionGained.RemoveListener(ShowImpressionGain);
                 
                 gm.deckManager.OnCardDrawn -= OnCardDrawn;
                 gm.deckManager.OnCardDiscarded -= OnCardDiscarded;
                 gm.deckManager.OnReset -= OnReset;
                 gm.deckManager.OnDeckCountChanged -= UpdateDeckCounts;
                 gm.deckManager.OnDeckShuffled -= OnDeckShuffled;
                 gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
                 
                 // Flaming event
                 gm.resourceManager.onFlamingChanged -= UpdateFlamingDisplay;
             }
        }
        
        /// <summary>
        /// モンスタープロフィールに切り替え（モンスタードラフト完了後に手動で呼ばれる）
        /// </summary>
        public void SwitchToMonsterProfile()
        {
            Debug.Log("[UIManager] SwitchToMonsterProfile called");
            
            if (GameManager.Instance?.currentStage?.monsterProfile != null && characterAnimator != null)
            {
                // フラッシュエフェクト付きでプロフィール切り替え（durationはCharacterAnimatorのInspector設定を使用）
                characterAnimator.SetProfileWithFlash(GameManager.Instance.currentStage.monsterProfile);
                Debug.Log("[UIManager] SetProfileWithFlash initiated");
            }
            else
            {
                Debug.LogError("[UIManager] Cannot switch to monster profile - missing references");
            }
        }
                
        private void OnReset()
        {
            Debug.Log("[UIManager] OnReset called - clearing cards and resetting character");
            
            // Clear all active cards
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            activeCards.Clear();
            
            // キャラクターを初期状態に戻す（アニメーション再開のため重要）
            if (GameManager.Instance?.currentStage?.normalProfile != null && characterAnimator != null)
            {
                Debug.Log("[UIManager] Resetting character to normal profile");
                SetupCharacter(GameManager.Instance.currentStage.normalProfile);
            }
        }
        
        private void LayoutCards()
        {
            if (activeCards.Count == 0) return;
            
            // Get container and card dimensions
            RectTransform containerRect = handContainer.GetComponent<RectTransform>();
            float containerWidth = containerRect.rect.width;
            
            // Assume all cards have the same width (get from prefab)
            float cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
            
            // Calculate total width if cards were placed with default spacing
            float totalDefaultWidth = (activeCards.Count * cardWidth) + ((activeCards.Count - 1) * defaultCardSpacing);
            
            float spacing;
            if (totalDefaultWidth > containerWidth && activeCards.Count > 1)
            {
                // Overlap mode: calculate spacing to fit in container
                spacing = (containerWidth - cardWidth) / (activeCards.Count - 1);
                spacing = Mathf.Max(spacing, minCardSpacing); // Respect minimum spacing
            }
            else
            {
                // Normal mode: use default spacing
                spacing = cardWidth + defaultCardSpacing;
            }
            
            // Calculate starting X position (center the hand)
            float totalWidth = (activeCards.Count - 1) * spacing + cardWidth;
            float startX = -totalWidth / 2f + cardWidth / 2f;
            
            // Get default Y position from prefab
            float baseYPos = cardPrefab.GetComponent<RectTransform>().anchoredPosition.y;
            
            // Position each card
            for (int i = 0; i < activeCards.Count; i++)
            {
                CardView card = activeCards[i];
                
                // Skip layout for selected cards to prevent animation override
                if (card.IsSelected)
                {
                    Debug.Log($"[UIManager] Skipping layout for selected card: {card.CardName}");
                    continue;
                }
                
                float xPos = startX + (i * spacing);
                
                // Calculate fan effect
                float centerIndex = (activeCards.Count - 1) / 2f;
                float relativeIndex = i - centerIndex;
                
                // Rotation: edges rotate outward
                float rotationZ = 0f;
                if (activeCards.Count > 1 && centerIndex > 0)
                {
                    float normalizedPos = relativeIndex / centerIndex; // -1 to 1
                    rotationZ = normalizedPos * maxRotationAngle;
                }
                
                // Y offset: parabolic curve (center high, edges low)
                // Using absolute value makes edges lower than center
                float arcOffset = 0f;
                if (centerIndex > 0)
                {
                    arcOffset = -Mathf.Abs(relativeIndex / centerIndex) * arcHeight;
                }
                float yPos = baseYPos + arcOffset;
                
                RectTransform cardRect = card.GetComponent<RectTransform>();
                
                // Ensure scale is 1 (in case scale animation gets killed)
                cardRect.localScale = Vector3.one;
                
                cardRect.DOKill(); // Kill any existing position tweens
                cardRect.DOAnchorPos(new Vector2(xPos, yPos), 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
                    // Update the original position after layout is complete
                    card.UpdateOriginalPosition();
                });
                cardRect.DORotate(new Vector3(0, 0, rotationZ), 0.3f).SetEase(Ease.OutQuad);
            }
        }

        private void UpdateFollowers(int val)
        {
            // 既存のアニメーションを停止
            DOTween.Kill("followersCount");
            
            // カウントアニメーション
            DOTween.To(() => displayedFollowers, x => {
                displayedFollowers = x;
                followersText.text = $"{(int)displayedFollowers:N0} ";
            }, val, COUNT_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetId("followersCount")
            .OnComplete(() => {
                displayedFollowers = val;
                followersText.text = $"{val:N0} ";
            });
            
            // パンチスケール
            followersText.transform.DOKill();
            followersText.transform.localScale = Vector3.one;
            followersText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        private void UpdateMental(int current, int max)
        {
            // 既存のアニメーションを停止
            DOTween.Kill("mentalCount");
            
            // カウントアニメーション
            int targetCurrent = current;
            int targetMax = max;
            DOTween.To(() => displayedMentalCurrent, x => {
                displayedMentalCurrent = x;
                mentalText.text = $"{(int)displayedMentalCurrent}/{targetMax}";
            }, targetCurrent, COUNT_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetId("mentalCount")
            .OnComplete(() => {
                displayedMentalCurrent = targetCurrent;
                mentalText.text = $"{targetCurrent}/{targetMax}";
            });
            
            if (mentalFillImage != null)
            {
                float fillAmount = max > 0 ? (float)current / max : 0f;
                mentalFillImage.DOKill();
                mentalFillImage.DOFillAmount(fillAmount, 0.5f);
            }
        }

        private void UpdateMotivation(int current, int max)
        {
            motivationText.text = $"{current}/{max}";
            if (motivationFillImage != null)
            {
                float fillAmount = max > 0 ? (float)current / max : 0f;
                motivationFillImage.DOKill();
                motivationFillImage.DOFillAmount(fillAmount, 0.3f);
            }
            
            // パルス条件は削除 - 常にパルスさせる設定はStart時に行う
        }

        private void UpdateImpression(long val)
        {
            // 既存のアニメーションを停止
            DOTween.Kill("impressionCount");
            
            // カウントアニメーション
            long targetVal = val;
            DOTween.To(() => displayedImpression, x => {
                displayedImpression = x;
                impressionText.text = ((long)displayedImpression).ToString("N0");
            }, (float)targetVal, COUNT_ANIM_DURATION)
            .SetEase(Ease.OutQuad)
            .SetId("impressionCount")
            .OnComplete(() => {
                displayedImpression = targetVal;
                impressionText.text = targetVal.ToString("N0");
            });
            
            // パンチスケール（軽め）
            impressionText.transform.DOKill();
            impressionText.transform.localScale = Vector3.one;
            impressionText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            
            UpdateClearGoalText(val);
        }

        private void ShowFollowerGain(int amount)
        {
            if (followerGainUI != null)
            {
                followerGainUI.PlayEffect($"+{FormatNumber(amount)}"); 
            }
        }

        private void UpdateFlamingDisplay(int seeds, int level, bool isOnFire)
        {
            // Update seed count text
            if (flamingSeedText != null)
            {
                flamingSeedText.text = seeds > 0 ? seeds.ToString() : "";
            }
            
            // Update flaming level text
            if (flamingLevelText != null)
            {
                flamingLevelText.text = isOnFire ? $"-{level}" : "";
            }
            
            // flamingContainer visibility is controlled by SetupFlamingUI
            
            Debug.Log($"[UIManager] Flaming UI Updated: seeds={seeds}, level={level}, isOnFire={isOnFire}");
        }
        
        /// <summary>
        /// ステージ設定に基づいてFlaming UIの表示/非表示を設定
        /// </summary>
        private void SetupFlamingUI()
        {
            if (flamingContainer == null) return;
            
            var stage = StageManager.Instance?.SelectedStage;
            bool showFlaming = stage != null && stage.enableFlaming;
            
            flamingContainer.SetActive(showFlaming);
            Debug.Log($"[UIManager] Flaming UI visibility set to: {showFlaming} (stage: {stage?.stageName ?? "null"})");
        }
        
        /// <summary>
        /// ステージに基づいて追跡カードUIを設定
        /// </summary>
        private void SetupTrackedCardUI()
        {
            var stage = StageManager.Instance?.SelectedStage;
            
            if (stage != null && stage.showTrackedCardUI && stage.trackedCard != null)
            {
                // ステージの設定を使用
                trackedCard = stage.trackedCard;
                if (trackedCardUIContainer != null)
                {
                    trackedCardUIContainer.SetActive(true);
                }
                Debug.Log($"[UIManager] Tracked card UI enabled for: {trackedCard.cardName}");
            }
            else
            {
                // 無効化
                trackedCard = null;
                if (trackedCardUIContainer != null)
                {
                    trackedCardUIContainer.SetActive(false);
                }
                Debug.Log("[UIManager] Tracked card UI disabled");
            }
        }
        
        /// <summary>
        /// 外部から追跡カードUIを再初期化する（ゲームリセット時など）
        /// </summary>
        public void RefreshTrackedCardUI()
        {
            SetupTrackedCardUI();
        }

        private void ShowImpressionGain(long amount, float rate)
        {
            if (impressionGainUI != null)
            {
                string mainText = $"+{FormatNumber(amount)}";
                
                // %表示に変換（1000%以上はカンマ区切り）
                int ratePercent = Mathf.RoundToInt(rate * 100);
                string subText = ratePercent >= 1000 
                    ? $"{ratePercent:N0}%" 
                    : $"{ratePercent}%";
                
                impressionGainUI.PlayEffect(mainText, subText);
            }
        }

        /// <summary>
        /// Formats a number with K/M suffixes.
        /// < 1000: Raw number (e.g. 999)
        /// 1000 - 999999: 1.2K
        /// >= 1000000: 1.2M
        /// </summary>
        private string FormatNumber(long num)
        {
            if (num >= 1000000)
            {
                return (num / 1000000f).ToString("0.##") + "M";
            }
            if (num >= 1000)
            {
                return (num / 1000f).ToString("0.##") + "K";
            }
            return num.ToString("N0");
        }
        
        private void UpdateTurnDisplay(int turn)
        {
            if (turnText != null)
            {
                int maxTurn = GameManager.Instance?.currentStage?.maxTurnCount ?? 20;
                turnText.text = $"{turn}/{maxTurn}ターン";
                turnText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }

        public void UpdateDeckCounts(int deckCount, int discardCount)
        {
            if (drawPileCountText != null) 
            {
                // 前回と値が違う場合のみロールアニメーション
                if (deckCount != prevDeckCount && prevDeckCount != -1)
                {
                    int startVal = prevDeckCount;
                    // 数値のロールアニメーション (2秒)
                    DOTween.To(() => startVal, x => {
                        startVal = x;
                        drawPileCountText.text = startVal.ToString();
                    }, deckCount, 2.0f).SetEase(Ease.OutQuad).OnComplete(() => {
                        drawPileCountText.text = deckCount.ToString();
                    });
                }
                else
                {
                    drawPileCountText.text = deckCount.ToString();
                }
            }
            
            if (discardPileCountText != null) 
            {
                // 前回と値が違う場合のみロールアニメーション
                if (discardCount != prevDiscardCount && prevDiscardCount != -1)
                {
                    int startVal = prevDiscardCount;
                    // 数値のロールアニメーション (2秒)
                    DOTween.To(() => startVal, x => {
                        startVal = x;
                        discardPileCountText.text = startVal.ToString();
                    }, discardCount, 2.0f).SetEase(Ease.OutQuad).OnComplete(() => {
                        discardPileCountText.text = discardCount.ToString();
                    });
                }
                else
                {
                    discardPileCountText.text = discardCount.ToString();
                }
            }
            
            // 追跡カードの枚数を更新
            UpdateTrackedCardCounts();
            
            prevDeckCount = deckCount;
            prevDiscardCount = discardCount;
        }

        private void OnDeckShuffled()
        {
            // リシャッフル時のパルスアニメーション (2秒間繰り返す)
            if (deckPileImage != null)
            {
                deckPileImage.transform.DOKill();
                deckPileImage.transform.localScale = Vector3.one;
                // Vibrato=5程度で2秒間揺らす
                deckPileImage.transform.DOPunchScale(Vector3.one * 0.3f, 2.0f, 5, 1);
            }
            if (discardPileImage != null)
            {
                discardPileImage.transform.DOKill();
                discardPileImage.transform.localScale = Vector3.one;
                // Vibrato=5程度で2秒間揺らす
                discardPileImage.transform.DOPunchScale(Vector3.one * 0.3f, 2.0f, 5, 1);
            }
        }
        
        /// <summary>
        /// 追跡対象カードの山札・捨て札内の枚数を更新
        /// </summary>
        private void UpdateTrackedCardCounts()
        {
            if (trackedCard == null) return;
            
            var gm = Core.GameManager.Instance;
            if (gm == null || gm.deckManager == null) return;
            
            // 山札内の枚数をカウント
            int drawPileCount = gm.deckManager.drawPile.Count(c => c == trackedCard);
            // 捨て札内の枚数をカウント
            int discardPileCount = gm.deckManager.discardPile.Count(c => c == trackedCard);
            
            if (trackedCardDrawPileText != null)
            {
                trackedCardDrawPileText.text = drawPileCount.ToString();
            }
            
            if (trackedCardDiscardPileText != null)
            {
                trackedCardDiscardPileText.text = discardPileCount.ToString();
            }
        }
        


        public void UpdateQuotaUI(long currentImpression, long quota)
        {
            long remaining = quota - currentImpression;
            
            if (quotaText != null)
            {
                if (remaining > 0)
                {
                    // 未達時 - カウントアニメーション
                    DOTween.Kill("quotaCount");
                    
                    long targetRemaining = remaining;
                    DOTween.To(() => displayedQuotaRemaining, x => {
                        displayedQuotaRemaining = x;
                        quotaText.text = ((long)displayedQuotaRemaining).ToString("N0");
                    }, (float)targetRemaining, COUNT_ANIM_DURATION)
                    .SetEase(Ease.OutQuad)
                    .SetId("quotaCount")
                    .OnComplete(() => {
                        displayedQuotaRemaining = targetRemaining;
                        quotaText.text = targetRemaining.ToString("N0");
                    });
                    
                    
                    // パンチスケール
                    quotaText.transform.DOKill(complete: true);
                    quotaText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1);
                }
                else
                {
                    // 完了時
                    DOTween.Kill("quotaCount");
                    if (quotaText.text != "OK") // 完了になった瞬間
                    {
                        quotaText.text = "OK";
                        
                        // 完了時のアニメーション (強め)
                        quotaText.transform.DOKill();
                        quotaText.transform.localScale = Vector3.one;
                        quotaText.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 10, 1);
                    }
                }
            }

            if (penaltyRiskText != null)
            {
                if (remaining > 0)
                {
                    // 未達時（待機中）
                    int penalty = 5;
                    if (GameManager.Instance != null && GameManager.Instance.turnManager != null)
                    {
                        penalty = GameManager.Instance.CalculateQuotaPenalty(GameManager.Instance.turnManager.CurrentTurnCount);
                    }
                    
                    penaltyRiskText.text = penalty.ToString(); // テキスト更新
                    
                    if (penaltyRiskContainer != null)
                    {
                        // 表示の際のアニメーション
                        bool wasActive = penaltyRiskContainer.activeSelf;
                        if (!wasActive)
                        {
                            penaltyRiskContainer.SetActive(true);
                            penaltyRiskContainer.transform.DOKill();
                            penaltyRiskContainer.transform.localScale = Vector3.one;
                            penaltyRiskContainer.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 10, 1);
                        }
                        
                        // 待機中のループアニメーション（びよーん）
                        // Tweenしていない場合のみ開始（表示アニメーション中は待つ挙動になるが、ループ再開される）
                        if (!DOTween.IsTweening(penaltyRiskContainer.transform))
                        {
                             Sequence seq = DOTween.Sequence();
                             seq.Append(penaltyRiskContainer.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 0.5f)); // 弱めのびよーん
                             seq.AppendInterval(0.5f); // 間隔（短めに）
                             seq.SetLoops(-1, LoopType.Restart); // ループ
                             seq.SetTarget(penaltyRiskContainer.transform);
                        }
                    }
                }
                else
                {
                    // 完了時
                    if (penaltyRiskContainer != null && penaltyRiskContainer.activeSelf)
                    {
                        // 完了になった瞬間（まだアクティブな場合）
                        
                        // テキストを空に（見栄えのため）
                        if (penaltyRiskText != null) penaltyRiskText.text = "";
                        
                        // ループ停止
                        penaltyRiskContainer.transform.DOKill();
                        
                        // 完了アニメーション後に非表示
                        // パンチしてから消える演出
                        // 1. スケールを1にリセット（DOKill直後なので）
                        penaltyRiskContainer.transform.localScale = Vector3.one;
                        
                        // 2. パンチ演出 → 縮小して消滅
                        Sequence seq = DOTween.Sequence();
                        seq.Append(penaltyRiskContainer.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 10, 1)); // パンチ
                        seq.Append(penaltyRiskContainer.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)); // 縮小
                        seq.OnComplete(() => {
                            if (penaltyRiskContainer != null)
                                penaltyRiskContainer.SetActive(false); // オブジェクトごと非表示
                        });
                        seq.SetTarget(penaltyRiskContainer.transform); // DOKill用
                    }
                }
            }
        }
        
        public void AddPost(string text, long impressionCount, Sprite icon = null)
        {
            if (postPrefab == null || timelineContainer == null) return;

            GameObject postObj = Instantiate(postPrefab, timelineContainer);
            ApprovalMonster.UI.PostView view = postObj.GetComponent<ApprovalMonster.UI.PostView>();
            if (view != null)
            {
                view.SetContent(text, impressionCount, icon);
            }
            
            // 投稿SE再生
            Core.AudioManager.Instance?.PlaySE(Data.SEType.TimelinePost);
            
            // Add to top
            postObj.transform.SetAsFirstSibling();

            // Limit number of posts
            if (timelineContainer.childCount > maxPosts)
            {
                // Destroy oldest (last child)
                Destroy(timelineContainer.GetChild(timelineContainer.childCount - 1).gameObject);
            }
        }
        
        private void OnEndTurnButtonClicked()
        {
            // SE removed per user request (other buttons still have SE)
            Debug.Log("[UIManager] End Turn button clicked.");
            var gm = GameManager.Instance;
            if (gm != null && gm.turnManager != null)
            {
                gm.turnManager.EndPlayerAction();
            }
        }
        
        private void OnDraftStart()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            
            // Check if we're past the last draft turn
            int currentTurn = gm.turnManager.CurrentTurnCount;
            int lastDraftTurn = gm.gameSettings != null ? gm.gameSettings.lastDraftTurn : 10;
            
            if (currentTurn > lastDraftTurn)
            {
                Debug.Log($"[UIManager] OnDraftStart - Turn {currentTurn} > lastDraftTurn {lastDraftTurn}, skipping draft");
                // Complete draft immediately to proceed to PlayerAction
                gm.turnManager.CompleteDraft();
                return;
            }
            
            Debug.Log("[UIManager] OnDraftStart received. Showing draft UI.");
            if (gm.draftManager != null && gm.currentStage != null)
            {
                var options = gm.draftManager.GenerateDraftOptions(
                    gm.currentStage.draftPool,
                    gm.resourceManager.totalImpressions
                );
                
                // Check if options is empty
                if (options == null || options.Count == 0)
                {
                    Debug.LogWarning("[UIManager] No draft options available, completing draft immediately");
                    gm.turnManager.CompleteDraft();
                    return;
                }
                
                if (draftUI != null)
                {
                    draftUI.ShowDraftOptions(options);
                }
                else
                {
                    Debug.LogError("[UIManager] DraftUI is not assigned!");
                }
            }
        }
        
        public void ShowMonsterDraft(List<CardData> options)
        {
            Debug.Log("[UIManager] Showing Monster Draft");
            
            if (draftUI != null)
            {
                // 既存のDraftUIを再利用
                // タイトルを変更して表示
                draftUI.ShowDraftOptions(options, isMonsterDraft: true);
            }
            else
            {
                Debug.LogError("[UIManager] DraftUI is not assigned!");
            }
        }
        
        /// <summary>
        /// 通常ドラフトを表示
        /// </summary>
        public void ShowNormalDraft(List<CardData> options)
        {
            Debug.Log("[UIManager] Showing Normal Draft");
            
            if (draftUI != null)
            {
                draftUI.ShowDraftOptions(options, isMonsterDraft: false);
            }
            else
            {
                Debug.LogError("[UIManager] DraftUI is not assigned!");
            }
        }
              public void OnCardDrawn(CardData data)
        {
            // Play card draw SE
            Core.AudioManager.Instance?.PlaySE(Data.SEType.CardDraw);
            
            StartCoroutine(DrawCardAnimated(data));
        }

        private System.Collections.IEnumerator DrawCardAnimated(CardData data)
        {
            Debug.Log($"[UIManager] OnCardDrawn called for {data.cardName}");
            var card = Instantiate(cardPrefab, handContainer);
            if (card == null) Debug.LogError("[UIManager] Failed to instantiate cardPrefab!");
            
            card.gameObject.SetActive(true);
            card.Setup(data);
            activeCards.Add(card);
            
            // Animation
            card.transform.localScale = Vector3.zero;
            card.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            
            // Update card layout
            LayoutCards();
            
            yield return null;
        }

        private void OnCardDiscarded(CardData data)
        {
            Debug.Log($"[UIManager] OnCardDiscarded called for {data.cardName}");
            
            // Find the first card view with matching CardData reference
            CardView target = activeCards.Find(c => c.CardData == data);
            
            if (target != null)
            {
                Debug.Log($"[UIManager] Found CardView to remove: {target.CardName}");
                activeCards.Remove(target);
                
                // Kill any active tweens on this object
                target.transform.DOKill();
                
                // Immediate destroy (animation can be added back later)
                Destroy(target.gameObject);
                Debug.Log($"[UIManager] Destroyed {target.CardName} immediately");
                
                // Update layout for remaining cards
                LayoutCards();
            }
            else
            {
                Debug.LogWarning($"[UIManager] Could not find CardView for {data.cardName}");
            }
        }
        
        /// <summary>
        /// ゲームオーバーカットインを表示（プリセット対応）
        /// </summary>
        public void ShowGameOverCutIn(System.Action onComplete)
        {
            if (cutInUI != null)
            {
                cutInUI.ShowGameOver(onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned! Proceeding directly.");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// ステージクリアカットインを表示（プリセット対応）
        /// </summary>
        public void ShowStageClearCutIn(System.Action onComplete)
        {
            if (cutInUI != null)
            {
                cutInUI.ShowStageClear(onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned! Proceeding directly.");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// モチベーション不足カットインを表示（プリセット対応）
        /// </summary>
        public void ShowMotivationLowCutIn(System.Action onComplete = null)
        {
            // モチベゲージを振動させる
            ShakeMotivationUI();
            
            if (cutInUI != null)
            {
                cutInUI.ShowMotivationLow(onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned! Proceeding directly.");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// モチベーションUIを振動させる
        /// </summary>
        private void ShakeMotivationUI()
        {
            if (motivationContainer == null) return;
            
            // 既存のアニメーションをキャンセル
            motivationContainer.DOKill();
            
            // 振動アニメーション
            motivationContainer.DOShakePosition(0.4f, new Vector3(10f, 5f, 0), 20, 90, false, true);
        }
        
        /// <summary>
        /// 手札条件不足カットインを表示
        /// </summary>
        public void ShowHandConditionNotMetCutIn(System.Action onComplete = null)
        {
            if (cutInUI != null)
            {
                cutInUI.ShowHandConditionNotMet(onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned! Proceeding directly.");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// カードプレイ不可カットインを表示
        /// </summary>
        public void ShowCardUnplayableCutIn(System.Action onComplete = null)
        {
            if (cutInUI != null)
            {
                cutInUI.ShowCardUnplayable(onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned! Proceeding directly.");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// 汎用カットインを表示
        /// </summary>
        public void ShowCutIn(string title, string message, System.Action onComplete = null)
        {
            if (cutInUI != null)
            {
                cutInUI.Show(title, message, onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned!");
                onComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// プリセットを使用してカットインを表示
        /// </summary>
        public void ShowCutInPreset(CutInPreset preset, System.Action onComplete = null)
        {
            if (preset == null)
            {
                Debug.LogWarning("[UIManager] CutInPreset is null!");
                onComplete?.Invoke();
                return;
            }
            
            if (cutInUI != null)
            {
                cutInUI.ShowPreset(preset, onComplete);
            }
            else
            {
                Debug.LogWarning("[UIManager] CutInUI is not assigned!");
                onComplete?.Invoke();
            }
        }
        
        private void UpdateClearGoalText(long currentScore)
        {
            if (clearGoalText == null) return;
            
            var currentStage = GameManager.Instance?.currentStage;
            // Check if goal exists
            if (currentStage != null && 
                currentStage.clearCondition != null && 
                currentStage.clearCondition.hasScoreGoal)
            {
                long target = currentStage.clearCondition.targetScore;
                long remaining = target - currentScore;
                
                if (remaining > 0)
                {
                    // カウントアニメーション
                    DOTween.Kill("clearGoalCount");
                    
                    long targetRemaining = remaining;
                    DOTween.To(() => displayedClearGoalRemaining, x => {
                        displayedClearGoalRemaining = x;
                        clearGoalText.text = $"クリアまで… {(long)displayedClearGoalRemaining:N0}";
                    }, (float)targetRemaining, COUNT_ANIM_DURATION)
                    .SetEase(Ease.OutQuad)
                    .SetId("clearGoalCount")
                    .OnComplete(() => {
                        displayedClearGoalRemaining = targetRemaining;
                        clearGoalText.text = $"クリアまで… {targetRemaining:N0}";
                    });
                }
                else
                {
                    DOTween.Kill("clearGoalCount");
                    clearGoalText.text = "目標スコア達成完了！";
                }
            }
        }

        /// <summary>
        /// クリア目標の表示を設定
        /// スコアゴールがある場合のみ表示
        /// </summary>
        public void SetupClearGoal()
        {
            if (clearGoalText == null)
            {
                Debug.LogWarning("[UIManager] clearGoalText is not assigned");
                return;
            }
            
            var currentStage = GameManager.Instance?.currentStage;
            if (currentStage == null)
            {
                Debug.LogWarning("[UIManager] currentStage is null");
                clearGoalText.gameObject.SetActive(false);
                return;
            }
            
            // clearConditionがあり、hasScoreGoalがtrueの場合のみ表示
            if (currentStage.clearCondition != null && currentStage.clearCondition.hasScoreGoal)
            {
                clearGoalText.gameObject.SetActive(true);
                // Initial update
                UpdateClearGoalText(GameManager.Instance.resourceManager.totalImpressions);
                Debug.Log($"[UIManager] Clear goal displayed. Target: {currentStage.clearCondition.targetScore:N0}");
            }
            else
            {
                // スコアアタックモードまたは条件なし
                clearGoalText.gameObject.SetActive(false);
                Debug.Log("[UIManager] Clear goal hidden (score attack mode or no clear condition)");
            }
        }
        
        /// <summary>
        /// チュートリアルを表示する（外部からの呼び出し用）
        /// ボタンのOnClickイベントから直接呼び出し可能
        /// </summary>
        public void ShowTutorial()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            if (tutorialPlayer != null)
            {
                tutorialPlayer.Show();
                Debug.Log("[UIManager] Tutorial opened.");
            }
            else
            {
                Debug.LogWarning("[UIManager] TutorialPlayer is not assigned!");
            }
        }
    }
}
