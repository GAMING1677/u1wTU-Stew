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
        [SerializeField] private Slider mentalSlider;
        
        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        
        [Header("Draft")]
        [SerializeField] private DraftUI draftUI;

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

                gm.resourceManager.onFollowersChanged.AddListener(UpdateFollowers);
                gm.resourceManager.onMentalChanged.AddListener(UpdateMental);
                gm.resourceManager.onMotivationChanged.AddListener(UpdateMotivation);
                gm.resourceManager.onImpressionsChanged.AddListener(UpdateImpressions);
                
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

        private void UpdateFollowers(int val)
        {
            followersText.text = $"{val:N0} Followers";
            followersText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        private void UpdateMental(int current, int max)
        {
            mentalText.text = $"{current}/{max}";
            mentalSlider.maxValue = max;
            mentalSlider.DOValue(current, 0.5f);
        }

        private void UpdateMotivation(int current, int max)
        {
            motivationText.text = $"Motivation: {current}/{max}";
        }

        private void UpdateImpressions(long val)
        {
            impressionText.text = $"{val:N0} Impressions";
            impressionText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }
        
        private void UpdateTurnDisplay(int turn)
        {
            if (turnText != null)
            {
                turnText.text = $"Turn {turn}/5";
                turnText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
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

        private void OnCardDrawn(CardData data)
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
        }

        private void OnCardDiscarded(CardData data)
        {
            // Find the card view corresponding to this data
            // This is a simple implementation; ideally map data->view
            var view = activeCards.Find(c => c.CardName == data.cardName); // Hacky identification
            if (view != null)
            {
                activeCards.Remove(view);
                view.transform.DOScale(0f, 0.2f).OnComplete(() => Destroy(view.gameObject));
            }
            else
            {
                // Just remove the first one matching? simple list management needed
                // For MVP, just refresh all could be safer but slower. 
                // Let's implement destroy first one found
                 CardView target = null;
                 // Ideally CardView holds ref to Data and we compare refs
                 // Assuming Setup stores _data, but CardData is shared SO.
                 // We need a unique ID or rely on object equality if DeckManager passes specific instance from list?
                 // DeckManager uses List<CardData>, so same SO can be there multiple times.
                 // We need to just pop one instance.
                 foreach(var c in activeCards)
                 {
                    // For now, destroy the first generic match
                    // In real game, map Hand Index to Card View
                     target = c; 
                     break; 
                 }
                 
                 if(target != null)
                 {
                     activeCards.Remove(target);
                     Destroy(target.gameObject);
                 }
            }
        }
    }
}
