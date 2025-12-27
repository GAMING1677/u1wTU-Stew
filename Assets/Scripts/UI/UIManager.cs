using UnityEngine;
using UnityEngine.UI;
using ApprovalMonster.UI; // Explicitly add namespace
using TMPro;
using ApprovalMonster.Core;
using ApprovalMonster.Data;
using System.Collections.Generic;
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

        [Header("Gain Effects - Static")]
        [SerializeField] private GainEffectUI followerGainUI;
        [SerializeField] private GainEffectUI impressionGainUI;
        
        [Header("Fill Images")]
        [SerializeField] private Image mentalFillImage;
        [SerializeField] private Image motivationFillImage;
        
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
        
        [Header("Cut-In")]
        [SerializeField] private CutInUI cutInUI;

        [Header("Character")]
        [SerializeField] private CharacterAnimator characterAnimator;

        private List<CardView> activeCards = new List<CardView>();

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
                gm.onQuotaUpdate.RemoveListener(UpdateQuota);

                gm.resourceManager.onFollowersChanged.AddListener(UpdateFollowers);
                gm.resourceManager.onMentalChanged.AddListener(UpdateMental);
                gm.resourceManager.onMotivationChanged.AddListener(UpdateMotivation);
                gm.resourceManager.onImpressionsChanged.AddListener(UpdateImpression);
                gm.onQuotaUpdate.AddListener(UpdateQuota);
                
                // Gain Effects
                gm.resourceManager.onFollowerGained.AddListener(ShowFollowerGain);
                gm.resourceManager.onImpressionGained.AddListener(ShowImpressionGain);
                
                gm.deckManager.OnCardDrawn += OnCardDrawn;
                gm.deckManager.OnCardDiscarded += OnCardDiscarded;
                gm.deckManager.OnReset += OnReset;
                gm.deckManager.OnDeckCountChanged += UpdateDeckCounts;
                
                // Subscribe to turn events
                gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
                gm.turnManager.OnTurnChanged.AddListener(UpdateTurnDisplay);
                
                // Subscribe to draft events
                gm.turnManager.OnDraftStart.RemoveListener(OnDraftStart);
                gm.turnManager.OnDraftStart.AddListener(OnDraftStart);
                
                // Monster mode profile switch is now called manually from GameManager.OnMonsterDraftComplete
                // (Event subscription removed to fix timing issue - profile should switch AFTER draft, not immediately)
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
             if (GameManager.Instance != null && GameManager.Instance.currentStage != null)
             {
                 SetupCharacter(GameManager.Instance.currentStage.normalProfile);
                 SetupClearGoal(); // クリア目標の表示を設定
             }
             
             // Setup button listener
             if (endTurnButton != null)
             {
                 endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
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
                 gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
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
            foreach(var card in activeCards)
            {
                if(card != null) Destroy(card.gameObject);
            }
            activeCards.Clear();
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
            followersText.text = $"{val:N0} ";
            followersText.transform.DOKill();
            followersText.transform.localScale = Vector3.one;
            followersText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        private void UpdateMental(int current, int max)
        {
            mentalText.text = $"{current}/{max}";
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
        }

        private void UpdateImpression(long val)
        {
            impressionText.text = $"{val:N0}";
            // Slightly smaller punch for frequent updates
            impressionText.transform.DOKill();
            impressionText.transform.localScale = Vector3.one;
            impressionText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }

        private void ShowFollowerGain(int amount)
        {
            if (followerGainUI != null)
            {
                // Green for gain
                followerGainUI.PlayEffect($"+{FormatNumber(amount)}", new Color(0.2f, 1f, 0.2f)); 
            }
        }

        private void ShowImpressionGain(long amount, float rate)
        {
            if (impressionGainUI != null)
            {
                // Yellow/Orange for impressions
                // Display: Amount (Main), Rate (Sub)
                string mainText = $"+{FormatNumber(amount)}";
                string subText = $"{rate*100:F0}%"; // No parentheses
                impressionGainUI.PlayEffect(mainText, new Color(1f, 0.8f, 0.2f), subText);
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
        
        /// <summary>
        /// Update draw pile and discard pile count display with animation
        /// </summary>
        private void UpdateDeckCounts(int drawCount, int discardCount)
        {
            if (drawPileCountText != null)
            {
                drawPileCountText.text = drawCount.ToString();
                drawPileCountText.transform.DOKill();
                drawPileCountText.transform.localScale = Vector3.one;
                drawPileCountText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
            }
            
            if (discardPileCountText != null)
            {
                discardPileCountText.text = discardCount.ToString();
                discardPileCountText.transform.DOKill();
                discardPileCountText.transform.localScale = Vector3.one;
                discardPileCountText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
            }
        }

        private void UpdateQuota(long gained, long target)
        {
            long remaining = System.Math.Max(0, target - gained);

            if (quotaText != null)
            {
                if (remaining > 0)
                {
                    // "{xxx}"
                    quotaText.text = $" <size=50%>ノルマまで…</size>\n<align=right>{remaining:N0}</align>";
                    quotaText.color = Color.white; 
                }
                else
                {
                    quotaText.text = "ノルマ達成！";
                    quotaText.color = Color.green;
                }
            }

            if (penaltyRiskText != null)
            {
                if (remaining > 0)
                {
                    // Calculate penalty dynamically from current turn
                    int penalty = 5; // Default fallback
                    if (GameManager.Instance != null && GameManager.Instance.turnManager != null)
                    {
                        penalty = GameManager.Instance.CalculateQuotaPenalty(GameManager.Instance.turnManager.CurrentTurnCount);
                    }
                    
                    // "未達だと…{penalty}病む"
                    penaltyRiskText.text = $"<size=50%>足りないと…</size>\n{penalty} <size=50%>病む</size>";
                    
                    // Show entire container (includes background)
                    if (penaltyRiskContainer != null)
                    {
                        penaltyRiskContainer.SetActive(true);
                    }
                }
                else
                {
                    // Hide entire container (includes background)
                    if (penaltyRiskContainer != null)
                    {
                        penaltyRiskContainer.SetActive(false);
                    }
                }
            }
        }
        
        public void AddPost(string text, long impressionCount)
        {
            if (postPrefab == null || timelineContainer == null) return;

            GameObject postObj = Instantiate(postPrefab, timelineContainer);
            ApprovalMonster.UI.PostView view = postObj.GetComponent<ApprovalMonster.UI.PostView>();
            if (view != null)
            {
                view.SetContent(text, impressionCount);
            }
            
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
        /// クリア目標の表示を設定
        /// スコアゴールがある場合のみ表示
        /// </summary>
        private void SetupClearGoal()
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
                clearGoalText.text = $"目標：{currentStage.clearCondition.targetScore:N0}";
                Debug.Log($"[UIManager] Clear goal displayed: {currentStage.clearCondition.targetScore:N0}");
            }
            else
            {
                // スコアアタックモードまたは条件なし
                clearGoalText.gameObject.SetActive(false);
                Debug.Log("[UIManager] Clear goal hidden (score attack mode or no clear condition)");
            }
        }
    }
}
