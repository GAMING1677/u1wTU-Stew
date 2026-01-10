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
        [SerializeField] private Color quotaClearColor = new Color(0f, 0.75f, 1f); // Rare水色（達成時）
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
        
        [Header("Discard Animation")]
        [Tooltip("カードが捨て札に吸い込まれるアニメーションの時間")]
        [SerializeField] private float discardAnimDuration = 0.4f;
        [Tooltip("各カード間のアニメーション開始遅延")]
        [SerializeField] private float discardAnimStagger = 0.05f;
        
        // 捨て札アニメーション中かどうか
        private bool isDiscardingHand = false;
        
        [Header("Draw Animation")]
        [Tooltip("山札からカードが手札に移動するアニメーションの時間")]
        [SerializeField] private float drawAnimDuration = 0.35f;
        [Tooltip("各カード間のドローアニメーション開始遅延")]
        [SerializeField] private float drawAnimStagger = 0.08f;
        
        // ドローアニメーション中のカード数（LayoutCardsでスキップ用）
        private HashSet<CardView> animatingCards = new HashSet<CardView>();
        
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
        
        [Header("Infection UI")]
        [Tooltip("感染度ゲージのFill Image（0-1でfillAmount設定）")]
        [SerializeField] private Image infectionFillImage;
        [Tooltip("感染度のテキスト表示（例: 25%）")]
        [SerializeField] private TextMeshProUGUI infectionText;
        [Tooltip("予測されるフォロワー減少数のテキスト")]
        [SerializeField] private TextMeshProUGUI infectionPenaltyText;
        [Tooltip("感染度UIのコンテナ（表示/非表示制御用）")]
        [SerializeField] private GameObject infectionContainer;
        [Tooltip("感染度リセット時のエフェクトUI")]
        [SerializeField] private GainEffectUI infectionResetEffectUI;
        
        [Header("Notification Banner")]
        [Tooltip("通知バナーのパネル（ターン制限・カンスト通知用）")]
        [SerializeField] private GameObject notificationPanel;
        [Tooltip("通知バナーのテキスト")]
        [SerializeField] private TextMeshProUGUI notificationText;
        
        [Header("Max Score Indicator")]
        [Tooltip("カンスト時に常時表示するUI")]
        [SerializeField] private GameObject maxScoreIndicatorPanel;
        [Tooltip("カンスト常時表示のテキスト")]
        [SerializeField] private TextMeshProUGUI maxScoreIndicatorText;
        
        private bool isSetup = false;

        private List<CardView> activeCards = new List<CardView>();
        
        // カウントアニメーション用の現在表示値
        private float displayedFollowers = 0;
        private float displayedImpression = 0;
        private float displayedMentalCurrent = 0;
        private float displayedQuotaRemaining = 0;
        private float displayedClearGoalRemaining = 0;
        
        // ノルマテキストの初期色（エディタで設定した色）
        private Color _quotaOriginalColor;
        private bool _quotaIsCleared = false; // ノルマ達成状態を保持
        
        // アニメーション時間
        private const float COUNT_ANIM_DURATION = 0.5f;

        private void Awake()
        {
            Debug.Log("[UIManager] Awake called.");
            
            // FlamingUIをデフォルトで非表示（ステージ設定で有効な場合のみ表示）
            if (flamingContainer != null)
            {
                flamingContainer.SetActive(false);
                Debug.Log("[UIManager] Awake: flamingContainer set to false");
            }
            else
            {
                Debug.LogWarning("[UIManager] Awake: flamingContainer is NULL!");
            }
            
            // ノルマテキストの初期色を保存
            if (quotaText != null)
            {
                _quotaOriginalColor = quotaText.color;
            }
        }

        public void SetupCharacter(CharacterProfile profile)
        {
            
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

            }
        }

        private void OnEnable()
        {
            // Subscribe to managers
            var gm = GameManager.Instance;
            if (gm != null)
            {

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
                
                // Infection event
                gm.resourceManager.onInfectionChanged -= UpdateInfectionDisplay;
                gm.resourceManager.onInfectionChanged += UpdateInfectionDisplay;
                
                // Infection reset event (for reshuffle effect)
                gm.resourceManager.onInfectionReset -= ShowInfectionResetEffect;
                gm.resourceManager.onInfectionReset += ShowInfectionResetEffect;
                
                // Flaming UIの表示設定（ここで確実にステージ情報が取れる）
                SetupFlamingUI();
                Debug.Log("[UIManager] OnEnable: About to call SetupInfectionUI()");
                SetupInfectionUI();
                Debug.Log("[UIManager] OnEnable: SetupInfectionUI() completed");
            }
            else
            {
                 Debug.LogError("[UIManager] GameManager Instance is NULL in OnEnable! Cannot subscribe.");
            }
        }

        private void Start()
        {

             
             // Initial Setup for Character
             if (StageManager.Instance != null && StageManager.Instance.SelectedStage != null)
             {
                 SetupCharacter(StageManager.Instance.SelectedStage.normalProfile);
                 SetupClearGoal(); // クリア目標の表示を設定
                 SetupFlamingUI(); // Flaming UIの表示/非表示を設定
                 SetupInfectionUI(); // Infection UIの表示/非表示を設定
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
            Debug.Log("[UIManager] OnSettingsButtonClicked called!");
            if (soundSettingsUI != null)
            {
                soundSettingsUI.Show();
                Debug.Log("[UIManager] soundSettingsUI.Show() called");
            }
            else
            {
                Debug.LogWarning("[UIManager] SoundSettingsUI is not assigned!");
            }
        }
        
        private void OnDeckPileButtonClicked()
        {
            Debug.Log("[UIManager] OnDeckPileButtonClicked called!");
            if (deckViewerUI != null)
            {
                deckViewerUI.ShowDrawPile();
                Debug.Log("[UIManager] deckViewerUI.ShowDrawPile() called");
            }
        }
        
        private void OnDiscardPileButtonClicked()
        {
            Debug.Log("[UIManager] OnDiscardPileButtonClicked called!");
            if (deckViewerUI != null)
            {
                deckViewerUI.ShowDiscardPile();
                Debug.Log("[UIManager] deckViewerUI.ShowDiscardPile() called");
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
                 
                 // Infection event
                 gm.resourceManager.onInfectionChanged -= UpdateInfectionDisplay;
                 
                 // Infection reset event
                 gm.resourceManager.onInfectionReset -= ShowInfectionResetEffect;
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
                
                // Skip layout for cards currently animating from draw pile
                if (animatingCards.Contains(card))
                {
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
        public void SetupFlamingUI()
        {
            if (flamingContainer == null) return;
            
            var stage = StageManager.Instance?.SelectedStage;
            bool showFlaming = stage != null && stage.enableFlaming;
            
            flamingContainer.SetActive(showFlaming);
            Debug.Log($"[UIManager] Flaming UI visibility set to: {showFlaming} (stage: {stage?.stageName ?? "null"})");
        }
        
        // ========== Infection UI ==========
        
        /// <summary>
        /// 感染度の表示を更新
        /// </summary>
        private void UpdateInfectionDisplay(float infectionRate)
        {
            // Fill image (0-100% -> 0-1)
            if (infectionFillImage != null)
            {
                float fillAmount = infectionRate / 100f;
                infectionFillImage.DOKill();
                infectionFillImage.DOFillAmount(fillAmount, 0.3f);
            }
            
            // Text display
            if (infectionText != null)
            {
                infectionText.text = $"{infectionRate:F0}%";
            }
            
            // Penalty prediction
            if (infectionPenaltyText != null)
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.resourceManager != null)
                {
                    int penalty = gm.resourceManager.CalculateInfectionPenalty();
                    infectionPenaltyText.text = penalty > 0 ? $"-{penalty:N0}" : "";
                }
            }
            
            Debug.Log($"[UIManager] Infection UI Updated: {infectionRate}%");
        }
        
        /// <summary>
        /// ステージ設定に基づいて感染度UIの表示/非表示を設定
        /// </summary>
        public void SetupInfectionUI()
        {
            Debug.Log($"[UIManager] SetupInfectionUI() CALLED. infectionContainer null? {infectionContainer == null}");
            
            if (infectionContainer == null)
            {
                Debug.LogWarning("[UIManager] SetupInfectionUI: infectionContainer is NULL, returning early");
                return;
            }
            
            var stage = StageManager.Instance?.SelectedStage;
            Debug.Log($"[UIManager] SetupInfectionUI: StageManager.Instance null? {StageManager.Instance == null}, stage null? {stage == null}");
            
            if (stage != null)
            {
                Debug.Log($"[UIManager] SetupInfectionUI: stage.enableInfection = {stage.enableInfection}");
            }
            
            bool showInfection = stage != null && stage.enableInfection;
            
            infectionContainer.SetActive(showInfection);
            Debug.Log($"[UIManager] Infection UI visibility set to: {showInfection} (stage: {stage?.stageName ?? "null"})");
            
            // 初期値を表示
            if (showInfection)
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.resourceManager != null)
                {
                    UpdateInfectionDisplay(gm.resourceManager.infectionRate);
                }
            }
        }
        
        /// <summary>
        /// 感染度リセット時のエフェクトを表示
        /// </summary>
        /// <param name="previousRate">リセット前の感染度</param>
        /// <param name="reducedAmount">減少した量</param>
        private void ShowInfectionResetEffect(float previousRate, float reducedAmount)
        {
            if (infectionResetEffectUI == null)
            {
                Debug.Log($"[UIManager] Infection reset: -{reducedAmount:F0}% (no effect UI assigned)");
                return;
            }
            
            // フォロワー増減と同様のエフェクト表示
            string mainText = $"-{reducedAmount:F0}%";
            string subText = "感染度リセット";
            
            infectionResetEffectUI.PlayEffect(mainText, subText);
            Debug.Log($"[UIManager] Infection reset effect: {mainText} ({subText})");
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
                    _quotaIsCleared = false;
                    DOTween.Kill("quotaCount");
                    DOTween.Kill("quotaColorReset");
                    
                    // 通常カラーに戻す
                    quotaText.color = _quotaOriginalColor;
                    
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
                    
                    if (!_quotaIsCleared) // 完了になった瞬間（1回だけ実行）
                    {
                        _quotaIsCleared = true;
                        
                        // クリアカラー（Epic色）で「OK」を表示
                        quotaText.text = "OK";
                        quotaText.color = quotaClearColor;
                        
                        // 完了時のアニメーション (強め)
                        quotaText.transform.DOKill();
                        quotaText.transform.localScale = Vector3.one;
                        quotaText.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 10, 1);
                        
                        // 色はターン開始時にResetQuotaColorで戻す
                    }
                    // 既に達成済みの場合は何もしない（色を維持）
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
        
        /// <summary>
        /// ノルマテキストの色を元に戻す（ターン開始時に呼び出す）
        /// </summary>
        public void ResetQuotaColor()
        {
            if (quotaText != null)
            {
                quotaText.color = _quotaOriginalColor;
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
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
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
            if (card == null)
            {
                Debug.LogError("[UIManager] Failed to instantiate cardPrefab!");
                yield break;
            }
            
            card.gameObject.SetActive(true);
            card.Setup(data);
            
            // アニメーション中リストに追加（LayoutCardsでスキップされる）
            animatingCards.Add(card);
            activeCards.Add(card);
            
            RectTransform cardRect = card.GetComponent<RectTransform>();
            
            // 山札の位置を取得
            Vector3 deckWorldPos = deckPileImage != null 
                ? deckPileImage.transform.position 
                : Vector3.zero;
            
            // 山札の位置にカードを配置（小さく）
            card.transform.position = deckWorldPos;
            card.transform.localScale = Vector3.zero;
            card.transform.localRotation = Quaternion.Euler(0, 0, 15f); // 少し傾ける
            
            // 目標位置を計算（現在のカード枚数に基づく）
            Vector2 targetPos = CalculateCardTargetPosition(activeCards.Count - 1, activeCards.Count);
            float targetRotation = CalculateCardTargetRotation(activeCards.Count - 1, activeCards.Count);
            
            // 既存のカードのレイアウトを更新（新しいカードはスキップされる）
            LayoutCards();
            
            // カード間の遅延を適用（複数枚同時ドロー時）
            int drawIndex = animatingCards.Count - 1;
            float delay = drawIndex * drawAnimStagger;
            
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // 山札パイルのパルスアニメーション
            if (deckPileImage != null && drawIndex == 0)
            {
                deckPileImage.transform.DOKill();
                deckPileImage.transform.localScale = Vector3.one;
                deckPileImage.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 1);
            }
            
            // アニメーション: 山札から目標位置へ移動しながら拡大
            Sequence seq = DOTween.Sequence();
            seq.Append(cardRect.DOAnchorPos(targetPos, drawAnimDuration).SetEase(Ease.OutQuad));
            seq.Join(card.transform.DOScale(1f, drawAnimDuration).SetEase(Ease.OutBack));
            seq.Join(card.transform.DORotate(new Vector3(0, 0, targetRotation), drawAnimDuration).SetEase(Ease.OutQuad));
            seq.OnComplete(() =>
            {
                // アニメーション完了、通常のレイアウト対象に戻す
                animatingCards.Remove(card);
                card.UpdateOriginalPosition();
                
                // 全てのアニメーションが完了したらレイアウトを更新
                if (animatingCards.Count == 0)
                {
                    LayoutCards();
                }
            });
            
            yield return null;
        }
        
        /// <summary>
        /// カードの目標位置を計算（LayoutCardsのロジックを流用）
        /// </summary>
        private Vector2 CalculateCardTargetPosition(int cardIndex, int totalCards)
        {
            if (totalCards == 0) return Vector2.zero;
            
            RectTransform containerRect = handContainer.GetComponent<RectTransform>();
            float containerWidth = containerRect.rect.width;
            float cardWidth = cardPrefab.GetComponent<RectTransform>().sizeDelta.x;
            
            float totalDefaultWidth = (totalCards * cardWidth) + ((totalCards - 1) * defaultCardSpacing);
            
            float spacing;
            if (totalDefaultWidth > containerWidth && totalCards > 1)
            {
                spacing = (containerWidth - cardWidth) / (totalCards - 1);
                spacing = Mathf.Max(spacing, minCardSpacing);
            }
            else
            {
                spacing = cardWidth + defaultCardSpacing;
            }
            
            float totalWidth = (totalCards - 1) * spacing + cardWidth;
            float startX = -totalWidth / 2f + cardWidth / 2f;
            
            float baseYPos = cardPrefab.GetComponent<RectTransform>().anchoredPosition.y;
            
            float xPos = startX + (cardIndex * spacing);
            
            // Arc calculation
            float centerIndex = (totalCards - 1) / 2f;
            float relativeIndex = cardIndex - centerIndex;
            float arcOffset = 0f;
            if (centerIndex > 0)
            {
                arcOffset = -Mathf.Abs(relativeIndex / centerIndex) * arcHeight;
            }
            float yPos = baseYPos + arcOffset;
            
            return new Vector2(xPos, yPos);
        }
        
        /// <summary>
        /// カードの目標回転を計算
        /// </summary>
        private float CalculateCardTargetRotation(int cardIndex, int totalCards)
        {
            if (totalCards <= 1) return 0f;
            
            float centerIndex = (totalCards - 1) / 2f;
            float relativeIndex = cardIndex - centerIndex;
            
            if (centerIndex > 0)
            {
                float normalizedPos = relativeIndex / centerIndex;
                return normalizedPos * maxRotationAngle;
            }
            return 0f;
        }

        private void OnCardDiscarded(CardData data)
        {
            // 一括アニメーション中はイベントをスキップ（データ処理はGameManagerが行う）
            if (isDiscardingHand)
            {
                Debug.Log($"[UIManager] OnCardDiscarded skipped during bulk discard: {data.cardName}");
                return;
            }
            
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
        /// 手札を一括で捨て札へアニメーション付きで移動させる
        /// 各カードが順番に捨て札へ吸い込まれていく演出
        /// </summary>
        /// <param name="onComplete">アニメーション完了後のコールバック</param>
        public void AnimateDiscardHand(System.Action onComplete)
        {
            // 手札がない場合は即座に完了
            if (activeCards.Count == 0)
            {
                Debug.Log("[UIManager] AnimateDiscardHand: No cards to animate");
                onComplete?.Invoke();
                return;
            }
            
            Debug.Log($"[UIManager] AnimateDiscardHand: Animating {activeCards.Count} cards");
            isDiscardingHand = true;
            
            // 捨て札の位置を取得
            Vector3 discardPos = discardPileImage != null 
                ? discardPileImage.transform.position 
                : Vector3.zero;
            
            // アニメーション対象のカードをコピー（activeCardsはクリアするため）
            List<CardView> cardsToAnimate = new List<CardView>(activeCards);
            activeCards.Clear();
            
            int animatingCount = cardsToAnimate.Count;
            int completedCount = 0;
            
            for (int i = 0; i < cardsToAnimate.Count; i++)
            {
                CardView card = cardsToAnimate[i];
                float delay = i * discardAnimStagger;
                
                // 既存のTweenをキャンセル
                card.transform.DOKill();
                
                // パルスアニメーションを停止
                card.StopPulse();
                
                // 親から切り離して最前面に配置（他のUIの上を移動するため）
                card.transform.SetParent(handContainer.parent);
                
                // シーケンスアニメーション
                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Append(card.transform.DOMove(discardPos, discardAnimDuration).SetEase(Ease.InQuad));
                seq.Join(card.transform.DOScale(0f, discardAnimDuration).SetEase(Ease.InQuad));
                seq.Join(card.transform.DORotate(new Vector3(0, 0, -30f), discardAnimDuration, RotateMode.Fast));
                seq.OnComplete(() =>
                {
                    Destroy(card.gameObject);
                    completedCount++;
                    
                    // 全てのアニメーションが完了したらコールバック
                    if (completedCount >= animatingCount)
                    {
                        Debug.Log("[UIManager] AnimateDiscardHand: All animations completed");
                        isDiscardingHand = false;
                        onComplete?.Invoke();
                    }
                });
            }
            
            // 捨て札パイルのパルスアニメーション
            if (discardPileImage != null)
            {
                discardPileImage.transform.DOKill();
                discardPileImage.transform.localScale = Vector3.one;
                
                // 少し遅延してから開始（カードが到着し始める頃）
                float pulseDelay = cardsToAnimate.Count * discardAnimStagger * 0.5f;
                DOTween.Sequence()
                    .AppendInterval(pulseDelay)
                    .Append(discardPileImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 1));
            }
        }
        
        /// <summary>
        /// 手札を即座にクリアする（アニメーションなし、緊急時用）
        /// </summary>
        public void ClearHandImmediate()
        {
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    card.transform.DOKill();
                    Destroy(card.gameObject);
                }
            }
            activeCards.Clear();
            isDiscardingHand = false;
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
        
        // ========== 通知バナーシステム ==========
        
        /// <summary>
        /// 通知バナーを表示（自動非表示）
        /// </summary>
        public void ShowNotification(string message, float duration = 2.5f)
        {
            if (notificationPanel == null || notificationText == null)
            {
                Debug.LogWarning("[UIManager] Notification UI is not assigned!");
                return;
            }
            
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            // 指定時間後に非表示
            DOVirtual.DelayedCall(duration, () => {
                if (notificationPanel != null)
                {
                    notificationPanel.SetActive(false);
                }
            });
        }
        
        // ========== カンスト常時表示UI ==========
        
        /// <summary>
        /// カンスト達成後に常時表示するUI
        /// </summary>
        public void ShowMaxScoreIndicator(int totalCardsPlayed)
        {
            if (maxScoreIndicatorPanel == null) return;
            
            if (maxScoreIndicatorText != null)
            {
                maxScoreIndicatorText.text = $"カンスト！";
            }
            maxScoreIndicatorPanel.SetActive(true);
            Debug.Log($"[UIManager] Max score indicator shown. Cards: {totalCardsPlayed}");
        }
        
        /// <summary>
        /// カンスト表示UIを非表示（ゲームリセット時）
        /// </summary>
        public void HideMaxScoreIndicator()
        {
            if (maxScoreIndicatorPanel != null)
            {
                maxScoreIndicatorPanel.SetActive(false);
            }
        }
    }
}
