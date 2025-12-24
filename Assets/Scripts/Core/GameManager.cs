using UnityEngine;
using ApprovalMonster.Data;
using NaughtyAttributes;

namespace ApprovalMonster.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Components")]
        public ResourceManager resourceManager;
        public DeckManager deckManager;
        public TurnManager turnManager;
        public DraftManager draftManager;

        [Header("Data")]
        [Expandable]
        public GameSettings gameSettings;
        [Expandable]
        public StageData currentStage;

        [Header("State")]
        [SerializeField] private bool isGameActive = false;
        [SerializeField] private bool isWaitingForMonsterDraft = false;
        [SerializeField] private bool hasPerformedMonsterDraft = false;
        
        // Persistent modifiers
        // Persistent modifiers
        private int extraTurnDraws = 0;
        
        // Quota System
        private long turnStartImpressions;
        private int turnStartFollowers; // New field
        private int lastTurnGainedFollowers; // New field
        private long currentTurnQuota;
        public QuotaUpdateEvent onQuotaUpdate = new QuotaUpdateEvent();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // For prototyping, start immediately
            if (gameSettings != null && currentStage != null)
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            Debug.Log("[GameManager] StartGame called.");
            isGameActive = true;
            hasPerformedMonsterDraft = false;
            extraTurnDraws = 0;
            
            // Initial setup if not already done via Reset
            if (resourceManager.currentMental <= 0 && gameSettings != null)
            {
                 resourceManager.Initialize(gameSettings);
            }

            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }
            
            // Prevent duplicate listeners
            turnManager.OnTurnStart.RemoveListener(OnTurnStart);
            turnManager.OnTurnEnd.RemoveListener(OnTurnEnd);
            turnManager.OnDraftStart.RemoveListener(OnDraftStart);
            
            // Resource listeners
            resourceManager.onMentalChanged.RemoveListener(OnMentalChanged);
            resourceManager.onImpressionsChanged.RemoveListener(OnImpressionsChanged);

            // Hook up events
            turnManager.OnTurnStart.AddListener(OnTurnStart);
            turnManager.OnTurnEnd.AddListener(OnTurnEnd);
            turnManager.OnDraftStart.AddListener(OnDraftStart);
            
            resourceManager.onMentalChanged.AddListener(OnMentalChanged);
            resourceManager.onImpressionsChanged.AddListener(OnImpressionsChanged);
            
            turnManager.StartGame();
        }
        
        private void OnImpressionsChanged(long total)
        {
            UpdateQuotaDisplay();
        }

        private void OnMentalChanged(int current, int max)
        {
            if (isGameActive && current <= 0)
            {
                GameOver();
            }
        }

        public void CheckGameOver()
        {
            if (resourceManager.currentMental <= 0)
            {
                GameOver();
            }
        }

        private void GameOver()
        {
            Debug.Log("Game Over!");
            isGameActive = false;
            turnManager.SetPhase(TurnManager.TurnPhase.GameOver);
            // Show Game Over UI
        }

        public void ResetGame()
        {
            // 0. Explicitly clear everything first
            deckManager.ClearAll();

            // 1. Reset Resources
            resourceManager.Initialize(gameSettings);
            extraTurnDraws = 0;
            
            // 2. Reset Deck
            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }
            
            // 3. Reset Quota
            turnStartImpressions = 0;
            currentTurnQuota = 0;
            
            // 4. Reset Draft Manager
            if (draftManager != null)
            {
                draftManager.ResetSelectedCards();
            }
            
            // 5. Reset Turn
            turnManager.StartGame();
        }

        private void OnTurnStart()
        {
            Debug.Log($"[GameManager] OnTurnStart Turn {turnManager.CurrentTurnCount}");
            resourceManager.ResetMotivation();
            
            // Apply persistent Draw Bonus
            int drawCount = gameSettings != null ? gameSettings.initialHandSize + extraTurnDraws : 5 + extraTurnDraws;
            
            deckManager.DrawCards(drawCount);
            
            // Setup Quota for this turn
            turnStartImpressions = resourceManager.totalImpressions;
            turnStartFollowers = resourceManager.currentFollowers; // Record start followers
            currentTurnQuota = CalculateTurnQuota();
            UpdateQuotaDisplay();
        }

        private void OnTurnEnd()
        {
            Debug.Log("[GameManager] OnTurnEnd");
            
            // Check Quota
            long gained = resourceManager.totalImpressions - turnStartImpressions;
            
            // Record follower gain for next turn's quota
            lastTurnGainedFollowers = resourceManager.currentFollowers - turnStartFollowers;
            if (lastTurnGainedFollowers < 0) lastTurnGainedFollowers = 0; // Should not trigger, but safe guard
            
            if (gained < currentTurnQuota)
            {
                int penalty = CalculatePenalty();
                Debug.Log($"[GameManager] Quota Failed! Penalty: {penalty} Mental Damage");
                resourceManager.DamageMental(penalty);
            }
            else
            {
                Debug.Log("[GameManager] Quota Met!");
            }
            
            deckManager.DiscardHand();
            turnManager.StartTurn();
        }
        
        public long CalculateTurnQuota()
        {
            int turn = turnManager.CurrentTurnCount;
            
            // Turn 1 Fixed Quota
            if (turn <= 1) return 1;

            // Updated Formula:
            // 1: LastTurnGainedFollowers
            // 2: Turn
            // 3: 0.3 * Round(Turn / 3) [Float division]
            // Quota = (1 * 2) * 2 / 10 * 3
            
            float factor3 = 0.3f * Mathf.Round(turn / 3.0f);
            
            // Calculate
            // (LastGain * Turn) * Turn / 10
            float baseValue = (float)(lastTurnGainedFollowers * turn) * turn / 10.0f;
            
            long quota = (long)(baseValue * factor3);
            
            // Ensure quota doesn't drop to 0 unexpectedly if calculation results in small number
            if (quota < 1) quota = 1;

            return quota;
        }
        
        public int CalculatePenalty()
        {
            // Monster Mode: Penalty = Max Mental (Instant Death potential)
            if (resourceManager.isMonsterMode)
            {
                return resourceManager.MaxMental;
            }

            // Normal Mode: Round(MaxMental * Turn / 10)
            float penalty = resourceManager.MaxMental * turnManager.CurrentTurnCount / 10.0f;
            return Mathf.RoundToInt(penalty);
        }
        
        private void UpdateQuotaDisplay()
        {
            long gained = resourceManager.totalImpressions - turnStartImpressions;
            int penalty = CalculatePenalty();
            
            // Notify UI
            onQuotaUpdate?.Invoke(gained, currentTurnQuota, penalty);
        }

        private void OnDraftStart()
        {
            Debug.Log("[GameManager] OnDraftStart");
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft)
            {
                // Monster draft logic
                StartMonsterDraft();
            }
            else
            {
                // Regular draft or skip
                turnManager.CompleteDraft();
            }
        }
        
        public void OnDraftComplete(CardData selectedCard)
        {
            Debug.Log($"[GameManager] Draft complete. Selected: {selectedCard.cardName}");
            draftManager.SelectCard(selectedCard);
            turnManager.CompleteDraft();
        }

        public void TryPlayCard(CardData card)
        {
            if (!isGameActive || deckManager.isDrawing || isWaitingForMonsterDraft) return;
            if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerAction) return;

            // Check motivation
            if (!resourceManager.UseMotivation(card.motivationCost))
            {
                Debug.Log("Not enough motivation!");
                // Show UI feedback
                return;
            }

            // Mental cost (positive = hurt)
            if (card.mentalCost > 0)
                resourceManager.DamageMental(card.mentalCost);
            else if (card.mentalCost < 0)
                resourceManager.HealMental(-card.mentalCost);

            long gainedImpressions = 0;
            // Execute Effects
            resourceManager.AddFollowers(card.followerGain);
            if (card.impressionRate > 0)
            {
                gainedImpressions = resourceManager.AddImpression(card.impressionRate);
            }

            // New Effects: Draw & AP Logic
            if (card.drawCount > 0)
            {
                deckManager.DrawCards(card.drawCount);
            }

            if (card.motivationRecovery != 0)
            {
                resourceManager.AddMotivation(card.motivationRecovery);
            }

            // New Effects: Persistent
            if (card.turnDrawBonus > 0)
            {
                extraTurnDraws += card.turnDrawBonus;
                Debug.Log($"[GameManager] Extra turn draws increased by {card.turnDrawBonus}. Total extra: {extraTurnDraws}");
            }
            
            if (card.maxMotivationBonus != 0)
            {
                resourceManager.IncreaseMaxMotivation(card.maxMotivationBonus);
                Debug.Log($"[GameManager] Max Motivation increased by {card.maxMotivationBonus}.");
            }

            // Risk Logic
            if (card.HasRisk())
            {
               // Implement risk logic e.g. probability check
               if (Random.value < card.riskProbability)
               {
                   // ApplyRisk(card.riskType, card.riskValue); 
                   // Simplified risk for now
                   if (card.riskType == RiskType.Flaming)
                   {
                        resourceManager.DamageMental(card.mentalCost);
                   }
               }
            }
            


            // Move card
            deckManager.PlayCard(card);
            
            // カードプレイ完了後、モンスタードラフトチェック
            // まだドラフトを行っておらず、かつモンスターモードになった場合のみ実行
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft && !isWaitingForMonsterDraft)
            {
                StartMonsterDraft();
                return; // ドラフト待機へ
            }
            
            
            // 6. Post Comment (Timeline)
            if (card.postComments != null && card.postComments.Count > 0 && gainedImpressions > 0)
            {
                string comment = card.postComments[Random.Range(0, card.postComments.Count)];
                FindObjectOfType<UI.UIManager>()?.AddPost(comment, gainedImpressions);
            }
            
            // Check turn end condition
            if (resourceManager.currentMotivation <= 0)
            {
                turnManager.EndPlayerAction();
            }


        }

        private void StartMonsterDraft()
        {
            Debug.Log("[GameManager] Starting Monster Draft");
            isWaitingForMonsterDraft = true;
            
            var options = draftManager.GenerateMonsterDraftOptions(
                currentStage.monsterDeck,
                gameSettings.monsterDraftCardCount
            );
            
            FindObjectOfType<UI.UIManager>()?.ShowMonsterDraft(options);
        }
        
        public void OnMonsterDraftComplete(CardData selectedCard)
        {
            Debug.Log($"[GameManager] Monster Draft complete. Selected: {selectedCard.cardName}");
            
            deckManager.hand.Add(selectedCard);
            FindObjectOfType<UI.UIManager>()?.OnCardDrawn(selectedCard);
            
            isWaitingForMonsterDraft = false;
            hasPerformedMonsterDraft = true;
            
            // Check turn end again as drafting might have happened at 0 motivation
            if (resourceManager.currentMotivation <= 0)
            {
                turnManager.EndPlayerAction();
            }
        }
        


        // Duplicate GameOver removed. Using the one defined earlier.

        public void FinishStage()
        {
            // Save Score
            long score = resourceManager.totalImpressions;
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.LastGameScore = score;
                SaveDataManager.Instance.SaveHighScore(score); // Auto save high score
                SceneNavigator.Instance.GoToResult();
            }
            else
            {
                Debug.LogWarning("SceneNavigator not found, cannot go to Result.");
            }
        }
        
        private void ApplyRisk(RiskType risk, int value)
        {
            switch (risk)
            {
                case RiskType.Flaming:
                    resourceManager.DamageMental(value);
                    break;
                case RiskType.LoseFollower:
                    resourceManager.AddFollowers(-value); // Reduce followers
                    break;
                case RiskType.Ban:
                    // Game Over
                    break;
                 // Freeze logic etc.
            }
        }
    }
    
    [System.Serializable]
    public class QuotaUpdateEvent : UnityEngine.Events.UnityEvent<long, long, int> { }
}
