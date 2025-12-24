using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private Transform handContainer;
        [SerializeField] private CardView cardPrefab;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI followersText;
        [SerializeField] private TextMeshProUGUI mentalText;
        [SerializeField] private TextMeshProUGUI motivationText;
        [SerializeField] private TextMeshProUGUI impressionText;
        [SerializeField] private TextMeshProUGUI turnText;
        
        [Header("Fill Images")]
        [SerializeField] private Image mentalFillImage;
        [SerializeField] private Image motivationFillImage;
        
        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        
        [Header("Draft")]
        [SerializeField] private DraftUI draftUI;
        
        [Header("Card Layout")]
        [SerializeField] private float defaultCardSpacing = 20f;
        [SerializeField] private float minCardSpacing = -100f; // Negative for overlap
        
        [Header("Fan Layout")]
        [SerializeField] private float maxRotationAngle = 10f; // Max rotation at edges (degrees)
        [SerializeField] private float arcHeight = 50f; // How much lower the edges are
        
        [Header("Quota")]
        [SerializeField] private TextMeshProUGUI quotaText; // Displays "Remaining: XXX"
        [SerializeField] private GameObject penaltyRiskContainer; // Parent object containing text and image
        [SerializeField] private TextMeshProUGUI penaltyRiskText; // Displays "Risk: YYY"

        private List<CardView> activeCards = new List<CardView>();

        private void Awake()
        {
            Debug.Log("[UIManager] Awake called.");
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
                gm.resourceManager.onImpressionsChanged.RemoveListener(UpdateImpressions);
                gm.deckManager.OnCardDrawn -= OnCardDrawn;
                gm.deckManager.OnCardDiscarded -= OnCardDiscarded;
                gm.deckManager.OnReset -= OnReset;
                gm.onQuotaUpdate.RemoveListener(UpdateQuota);

                gm.resourceManager.onFollowersChanged.AddListener(UpdateFollowers);
                gm.resourceManager.onMentalChanged.AddListener(UpdateMental);
                gm.resourceManager.onMotivationChanged.AddListener(UpdateMotivation);
                gm.resourceManager.onImpressionsChanged.AddListener(UpdateImpressions);
                gm.onQuotaUpdate.AddListener(UpdateQuota);
                
                gm.deckManager.OnCardDrawn += OnCardDrawn;
                gm.deckManager.OnCardDiscarded += OnCardDiscarded;
                gm.deckManager.OnReset += OnReset;
                
                // Subscribe to turn events
                gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
                gm.turnManager.OnTurnChanged.AddListener(UpdateTurnDisplay);
                
                // Subscribe to draft events
                gm.turnManager.OnDraftStart.RemoveListener(OnDraftStart);
                gm.turnManager.OnDraftStart.AddListener(OnDraftStart);
            }
            else
            {
                 Debug.LogError("[UIManager] GameManager Instance is NULL in OnEnable! Cannot subscribe.");
            }
        }

        private void Start()
        {
             Debug.Log("[UIManager] Start called.");
             
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
                 gm.resourceManager.onImpressionsChanged.RemoveListener(UpdateImpressions);
                 
                 gm.deckManager.OnCardDrawn -= OnCardDrawn;
                 gm.deckManager.OnCardDiscarded -= OnCardDiscarded;
                 gm.deckManager.OnReset -= OnReset;
                 gm.turnManager.OnTurnChanged.RemoveListener(UpdateTurnDisplay);
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
                float xPos = startX + (i * spacing);
                
                // Calculate fan effect
                float centerIndex = (activeCards.Count - 1) / 2f;
                float relativeIndex = i - centerIndex;
                
                // Rotation: edges rotate outward
                float rotationZ = 0f;
                if (activeCards.Count > 1)
                {
                    float normalizedPos = relativeIndex / centerIndex; // -1 to 1
                    rotationZ = normalizedPos * maxRotationAngle;
                }
                
                // Y offset: parabolic curve (center high, edges low)
                // Using absolute value makes edges lower than center
                float arcOffset = -Mathf.Abs(relativeIndex / centerIndex) * arcHeight;
                float yPos = baseYPos + arcOffset;
                
                RectTransform cardRect = card.GetComponent<RectTransform>();
                
                // Ensure scale is 1 (in case scale animation gets killed)
                cardRect.localScale = Vector3.one;
                
                cardRect.DOKill(); // Kill any existing position tweens
                cardRect.DOAnchorPos(new Vector2(xPos, yPos), 0.3f).SetEase(Ease.OutQuad);
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

        private void UpdateImpressions(long val)
        {
            impressionText.text = $"{val:N0}インプ";
            impressionText.transform.DOKill();
            impressionText.transform.localScale = Vector3.one;
            impressionText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }
        
        private void UpdateTurnDisplay(int turn)
        {
            if (turnText != null)
            {
                turnText.text = $"{turn}ターン";
                turnText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }

        private void UpdateQuota(long gained, long target, int penalty)
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
                    // "未達だと…{penalty}病む"
                    penaltyRiskText.text = $"未達だと…\n{penalty} ポイント病む";
                    
                    if (penaltyRiskContainer != null)
                    {
                        penaltyRiskContainer.SetActive(true);
                    }
                    else
                    {
                        penaltyRiskText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (penaltyRiskContainer != null)
                    {
                        penaltyRiskContainer.SetActive(false);
                    }
                    else
                    {
                        penaltyRiskText.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        private void OnEndTurnButtonClicked()
        {
            Debug.Log("[UIManager] End Turn button clicked.");
            var gm = GameManager.Instance;
            if (gm != null && gm.turnManager != null)
            {
                gm.turnManager.EndPlayerAction();
            }
        }
        
        private void OnDraftStart()
        {
            Debug.Log("[UIManager] OnDraftStart received. Showing draft UI.");
            var gm = GameManager.Instance;
            if (gm != null && gm.draftManager != null && gm.currentStage != null)
            {
                var options = gm.draftManager.GenerateDraftOptions(
                    gm.currentStage.draftPool,
                    gm.resourceManager.totalImpressions
                );
                
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



        public void OnCardDrawn(CardData data)
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
    }
}
