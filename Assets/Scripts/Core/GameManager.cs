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
        
        [Header("UI")]
        public UI.MonsterModeCutInUI monsterModeCutInUI;

        [Header("Data")]
        [Expandable]
        public GameSettings gameSettings;
        [Expandable]
        public StageData currentStage;
        [Expandable]
        [Tooltip("ターンごとのノルマ設定")]
        public QuotaSettings quotaSettings;

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
        private int turnStartMental; // For tracking mental changes
        private int lastTurnGainedFollowers; // New field
        private long currentTurnQuota;
        public QuotaUpdateEvent onQuotaUpdate = new QuotaUpdateEvent();
        
        // Debug Statistics - ターンごとの統計
        [Header("Debug - Turn Statistics")]
        [SerializeField, ReorderableList] 
        private System.Collections.Generic.List<TurnStats> turnStatsList = new System.Collections.Generic.List<TurnStats>();
        
        [TextArea(5, 15)]
        [SerializeField] private string statsTextOutput = "";
        
        [Button("統計をテキスト出力")]
        private void ExportStatsToText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Turn\tFollowers\tImpressions\tMental\tQuota\tMet");
            sb.AppendLine("----\t---------\t-----------\t------\t-----\t---");
            
            foreach (var s in turnStatsList)
            {
                string met = s.quotaMet ? "Y" : "N";
                sb.AppendLine($"{s.turnNumber}\t{s.followersGained}\t{s.impressionsGained}\t{s.mentalChange}\t{s.quota}\t{met}");
            }
            
            statsTextOutput = sb.ToString();
            GUIUtility.systemCopyBuffer = statsTextOutput;
            Debug.Log("[GameManager] Stats copied to clipboard!");
        }

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

        private void Update()
        {
            // Press R to reload scene (using new Input System)
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.Log("[GameManager] R key pressed - Reloading scene");
                SceneNavigator.Instance?.ReloadScene();
            }
        }

        private void Start()
        {
            // Game will be started by SceneNavigator button click
            // Automatic start removed to prevent duplicate initialization
        }

        public void StartGame()
        {
            Debug.Log("[GameManager] StartGame called.");
            Debug.Log($"[GameManager] StartGame StackTrace:\n{System.Environment.StackTrace}");
            isGameActive = true;
            hasPerformedMonsterDraft = false;
            extraTurnDraws = 0;
            
            // Get selected stage from StageManager
            if (StageManager.Instance != null && StageManager.Instance.SelectedStage != null)
            {
                currentStage = StageManager.Instance.SelectedStage;
                Debug.Log($"[GameManager] Using selected stage from StageManager: {currentStage.stageName}");
            }
            else
            {
                Debug.LogError("[GameManager] No stage selected in StageManager! Cannot start game.");
                return;
            }
            
            // Initial setup if not already done via Reset
            if (resourceManager.currentMental <= 0 && gameSettings != null)
            {
                 resourceManager.Initialize(gameSettings);
            }

            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }
            
            
            // Subscribe to monster mode event (モンスターモード発動時のイベントリスナー)
            resourceManager.onMonsterModeTriggered.AddListener(OnMonsterModeTriggered);
            Debug.Log("[GameManager] Monster mode event listener registered");
            
            // Prevent duplicate listeners - only remove GameManager's own listeners
            turnManager.OnTurnStart.RemoveListener(OnTurnStart);
            turnManager.OnTurnEnd.RemoveListener(OnTurnEnd);
            turnManager.OnDraftStart.RemoveListener(OnDraftStart);
            
            // Remove GameManager's resource listeners (not all listeners!)
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
            Debug.Log($"[GameManager] OnMentalChanged called: current={current}, max={max}, isGameActive={isGameActive}");
            if (isGameActive && current <= 0)
            {
                Debug.Log("[GameManager] Mental <= 0, triggering GameOver!");
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
            Debug.Log("[GameManager] GameOver() called!");
            isGameActive = false;
            turnManager.SetPhase(TurnManager.TurnPhase.GameOver);
            
            // Save Score
            long score = resourceManager.totalImpressions;
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.LastGameScore = score;
                Debug.Log($"[GameManager] Saved game over score: {score}");
            }
            
            // Show game over cut-in, then navigate to result
            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowGameOverCutIn(() =>
                {
                    // This runs after user clicks the cut-in
                    SceneNavigator.Instance?.GoToResult();
                });
            }
            else
            {
                // Fallback: go directly to result
                SceneNavigator.Instance?.GoToResult();
            }
        }

        public void ResetGame()
        {
            // 0. Explicitly clear everything first
            deckManager.ClearAll();

            // 1. Reset Resources
            resourceManager.Initialize(gameSettings);
            extraTurnDraws = 0;
            
            // Clear debug statistics
            turnStatsList.Clear();
            
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
            
            // 5. Reset flags
            hasPerformedMonsterDraft = false;
            isWaitingForMonsterDraft = false;
            
            // NOTE: Don't start turn here - StartGame() will call turnManager.StartGame()
        }

        private void OnTurnStart()
        {
            Debug.Log($"[GameManager] OnTurnStart Turn {turnManager.CurrentTurnCount}");
            Debug.Log($"[GameManager] OnTurnStart StackTrace:\n{System.Environment.StackTrace}");
            resourceManager.ResetMotivation();
            
            // Draw cards
            int drawCount;
            if (turnManager.CurrentTurnCount == 1)
            {
                // First turn: use initial hand size
                drawCount = gameSettings != null ? gameSettings.initialHandSize : 3;
                Debug.Log($"[GameManager] Turn 1: Drawing initial hand of {drawCount} cards");
            }
            else
            {
                // Subsequent turns: use turn draw count from settings + bonuses
                int baseTurnDraw = gameSettings != null ? gameSettings.turnDrawCount : 2;
                drawCount = baseTurnDraw + extraTurnDraws;
                Debug.Log($"[GameManager] Turn {turnManager.CurrentTurnCount}: Drawing {drawCount} cards (base: {baseTurnDraw}, bonus: {extraTurnDraws})");
            }
            
            deckManager.DrawCards(drawCount);
            
            // Setup Quota for this turn
            turnStartImpressions = resourceManager.totalImpressions;
            turnStartFollowers = resourceManager.currentFollowers;
            turnStartMental = resourceManager.currentMental; // Record start mental
            currentTurnQuota = CalculateTurnQuota();
            UpdateQuotaDisplay();
        }

        private void OnTurnEnd()
        {
            // Note: TurnManager already incremented turnCount before calling this
            // So CurrentTurnCount is actually the NEXT turn number
            int endedTurn = turnManager.CurrentTurnCount - 1;
            
            Debug.Log($"[GameManager] OnTurnEnd - Ended Turn: {endedTurn}, Current Phase: {turnManager.CurrentPhase}");
            
            // Calculate gains/changes
            long impGained = resourceManager.totalImpressions - turnStartImpressions;
            int followerGained = resourceManager.currentFollowers - turnStartFollowers;
            int mentalChange = resourceManager.currentMental - turnStartMental;
            
            // Record follower gain for next turn's quota
            lastTurnGainedFollowers = followerGained;
            if (lastTurnGainedFollowers < 0) lastTurnGainedFollowers = 0;
            
            bool quotaMet = impGained >= currentTurnQuota;
            
            if (!quotaMet)
            {
                int penalty = 5; // Fixed penalty for quota failure
                Debug.Log($"[GameManager] Quota Failed! Penalty: {penalty} Mental Damage");
                resourceManager.DamageMental(penalty);
                
                // Check if GameOver was triggered by DamageMental
                if (!isGameActive)
                {
                    Debug.Log("[GameManager] OnTurnEnd - GameOver was triggered during penalty, stopping");
                    return;
                }
                
                // Update mental change after penalty
                mentalChange = resourceManager.currentMental - turnStartMental;
            }
            else
            {
                Debug.Log("[GameManager] Quota Met!");
            }
            
            // Record turn statistics for debug (use ended turn number, not next turn)
            var stats = new TurnStats(endedTurn, followerGained, impGained, mentalChange, currentTurnQuota, quotaMet);
            turnStatsList.Add(stats);
            Debug.Log($"[GameManager] Turn Stats: {stats}");
            
            deckManager.DiscardHand();
            
            // Only start next turn if game is still active and not ending
            Debug.Log($"[GameManager] OnTurnEnd - After processing, Phase: {turnManager.CurrentPhase}, isGameActive: {isGameActive}");
            
            if (isGameActive && 
                turnManager.CurrentPhase != TurnManager.TurnPhase.Result && 
                turnManager.CurrentPhase != TurnManager.TurnPhase.GameOver)
            {
                Debug.Log("[GameManager] OnTurnEnd - Starting next turn");
                turnManager.StartTurn();
            }
            else
            {
                Debug.Log("[GameManager] OnTurnEnd - Game is ending, not starting next turn");
            }
        }
        
        public long CalculateTurnQuota()
        {
            int turn = turnManager.CurrentTurnCount;
            
            // Use QuotaSettings if available
            if (quotaSettings != null)
            {
                return quotaSettings.GetQuotaForTurn(turn);
            }
            
            // Fallback: simple scaling
            return turn * 100;
        }
        
        public int CalculatePenalty()
        {
            int turn = turnManager.CurrentTurnCount;
            
            // New formula: ceil(turn / 4) * 5
            // Turn 1-4 = 5, Turn 5-8 = 10, Turn 9-12 = 15, etc.
            int basePenalty = Mathf.CeilToInt(turn / 4.0f) * 5;
            
            // Monster Mode: Apply multiplier
            if (resourceManager.isMonsterMode)
            {
                float multiplier = gameSettings != null ? gameSettings.monsterPenaltyMultiplier : 2.0f;
                return Mathf.Max(1, Mathf.RoundToInt(basePenalty * multiplier));
            }

            return basePenalty;
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
            int currentTurn = turnManager.CurrentTurnCount;
            int lastDraftTurn = gameSettings != null ? gameSettings.lastDraftTurn : 10;
            
            Debug.Log($"[GameManager] OnDraftStart - Turn={currentTurn}, lastDraftTurn={lastDraftTurn}, isMonsterMode={resourceManager.isMonsterMode}, hasPerformedMonsterDraft={hasPerformedMonsterDraft}");
            
            // Skip ALL drafts after lastDraftTurn
            if (currentTurn > lastDraftTurn)
            {
                Debug.Log($"[GameManager] OnDraftStart -> Turn {currentTurn} > lastDraftTurn {lastDraftTurn}, skipping draft");
                turnManager.CompleteDraft();
                return;
            }
            
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft)
            {
                Debug.Log("[GameManager] OnDraftStart -> Starting Monster Draft");
                // Monster draft logic
                StartMonsterDraft();
            }
            else
            {
                Debug.Log("[GameManager] OnDraftStart -> Calling CompleteDraft (skipping regular draft)");
                // Regular draft or skip
                turnManager.CompleteDraft();
                Debug.Log($"[GameManager] OnDraftStart -> After CompleteDraft, Phase is now: {turnManager.CurrentPhase}");
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
            // Detailed debug logging
            Debug.Log($"[GameManager] TryPlayCard called for: {card?.cardName ?? "NULL"}");
            Debug.Log($"[GameManager] State: isGameActive={isGameActive}, isDrawing={deckManager.isDrawing}, isWaitingForMonsterDraft={isWaitingForMonsterDraft}");
            Debug.Log($"[GameManager] Phase: {turnManager.CurrentPhase}, isMonsterMode={resourceManager.isMonsterMode}, hasPerformedMonsterDraft={hasPerformedMonsterDraft}");
            
            if (!isGameActive)
            {
                Debug.Log("[GameManager] BLOCKED: Game is not active");
                return;
            }
            if (deckManager.isDrawing)
            {
                Debug.Log("[GameManager] BLOCKED: Deck is drawing");
                return;
            }
            if (isWaitingForMonsterDraft)
            {
                Debug.Log("[GameManager] BLOCKED: Waiting for monster draft");
                return;
            }
            if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerAction)
            {
                Debug.Log($"[GameManager] BLOCKED: Wrong phase ({turnManager.CurrentPhase} != PlayerAction)");
                return;
            }

            // Hand-Based Effect Cost Check
            if (card.handEffectTargetCard != null)
            {
                int handCount = deckManager.CountCardInHand(card.handEffectTargetCard, card);
                
                // ① Exhaust all cost check (need at least 1)
                if (card.exhaustAllTargetCards && handCount < 1)
                {
                    Debug.Log($"[GameManager] No {card.handEffectTargetCard.cardName} in hand to exhaust!");
                    return;
                }
                
                // ②③④ Min count check
                if ((card.handCountImpressionRate > 0 || card.drawByHandCount || card.handCountFollowerRate > 0) 
                    && handCount < card.handEffectMinCount)
                {
                    Debug.Log($"[GameManager] Need at least {card.handEffectMinCount} {card.handEffectTargetCard.cardName} in hand!");
                    return;
                }
            }

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
            
            // Turn Multiplier Effect
            if (card.isTurnMultiplierEffect)
            {
                int currentFollowers = resourceManager.currentFollowers;
                int currentTurn = turnManager.CurrentTurnCount;
                int multipliedGain = currentFollowers * currentTurn;
                resourceManager.AddFollowers(multipliedGain);
                Debug.Log($"[GameManager] Turn Multiplier Effect: {currentFollowers} × {currentTurn} = {multipliedGain} followers gained!");
            }
            else
            {
                resourceManager.AddFollowers(card.followerGain);
            }
            
            
            // Impression Gain
            if (card.isTurnImpressionEffect)
            {
                int currentFollowers = resourceManager.currentFollowers;
                int currentTurn = turnManager.CurrentTurnCount;
                float turnMultiplier = currentTurn / 10.0f;
                float calculatedRate = currentFollowers * turnMultiplier;
                gainedImpressions = resourceManager.AddImpression(calculatedRate);
                Debug.Log($"[GameManager] Turn Impression Effect: {currentFollowers} × ({currentTurn}/10) = {calculatedRate:F2} rate, {gainedImpressions} impressions gained!");
            }
            else if (card.impressionRate > 0)
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

            // Card Generation Effect
            if (card.generatedCards != null && card.generatedCards.Count > 0)
            {
                foreach (var gen in card.generatedCards)
                {
                    if (gen.card == null) continue;
                    
                    switch (gen.destination)
                    {
                        case CardDestination.Discard:
                            deckManager.AddCardToDiscard(gen.card);
                            Debug.Log($"[GameManager] Generated card '{gen.card.cardName}' added to discard.");
                            break;
                        case CardDestination.Hand:
                            deckManager.AddCardToHand(gen.card);
                            Debug.Log($"[GameManager] Generated card '{gen.card.cardName}' added to hand.");
                            break;
                        case CardDestination.DrawPile:
                            deckManager.AddCardToTopOfDraw(gen.card);
                            Debug.Log($"[GameManager] Generated card '{gen.card.cardName}' added to draw pile.");
                            break;
                    }
                }
            }

            // Hand-Based Effects Execution
            if (card.handEffectTargetCard != null)
            {
                int handCount = deckManager.CountCardInHand(card.handEffectTargetCard, card);
                
                // ① Exhaust ALL target cards in hand
                if (card.exhaustAllTargetCards && handCount > 0)
                {
                    deckManager.ExhaustCardsOfType(card.handEffectTargetCard, handCount, card);
                    Debug.Log($"[GameManager] Exhausted all {handCount} {card.handEffectTargetCard.cardName} cards.");
                }
                
                // ② Count-based impressions
                if (card.handCountImpressionRate > 0 && handCount > 0)
                {
                    float rate = handCount * card.handCountImpressionRate;
                    long gained = resourceManager.AddImpression(rate);
                    Debug.Log($"[GameManager] Hand count effect: {handCount} cards × {card.handCountImpressionRate} rate = {gained} impressions.");
                }
                
                // ③ Count-based draw
                if (card.drawByHandCount && handCount > 0)
                {
                    deckManager.DrawCards(handCount);
                    Debug.Log($"[GameManager] Drew {handCount} cards based on hand count.");
                }
                
                // ④ Count-based followers
                if (card.handCountFollowerRate > 0 && handCount > 0)
                {
                    int followers = handCount * card.handCountFollowerRate;
                    resourceManager.AddFollowers(followers);
                    Debug.Log($"[GameManager] Hand count effect: {handCount} cards × {card.handCountFollowerRate} = {followers} followers.");
                }
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
            

            // Move card (Exhaust or Discard)
            if (card.isExhaust)
            {
                deckManager.ExhaustCard(card);
            }
            else
            {
                deckManager.PlayCard(card);
            }
            
            // カードプレイ完了後、モンスタードラフトチェック
            // まだドラフトを行っておらず、かつモンスターモードになった場合のみ実行
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft && !isWaitingForMonsterDraft)
            {
                StartMonsterDraft();
                return; // ドラフト待機へ
            }
            
            
            // 6. Post Comment (Timeline)
            // ポストは常に表示（インプレッション獲得条件を削除）
            if (card.postComments != null && card.postComments.Count > 0)
            {
                string comment = card.postComments[Random.Range(0, card.postComments.Count)];
                // gainedImpressionsが0の場合、フォロワー数×1%を使用
                long displayImpressions = gainedImpressions > 0 ? gainedImpressions : (long)(resourceManager.currentFollowers * 0.01f);
                FindObjectOfType<UI.UIManager>()?.AddPost(comment, displayImpressions);
            }
            
            // Player must click End Turn button to proceed (no automatic turn end)


        }

        
        /// <summary>
        /// モンスターモード発動時の処理
        /// 1. カットイン表示 → 2. クリック待ち → 3. モンスタードラフト開始
        /// </summary>
        private void OnMonsterModeTriggered()
        {
            Debug.Log("[GameManager] ===== OnMonsterModeTriggered() CALLED =====");
            Debug.Log($"[GameManager] monsterModeCutInUI null? {monsterModeCutInUI == null}");
            Debug.Log($"[GameManager] currentStage null? {currentStage == null}");
            
            if (currentStage != null)
            {
                Debug.Log($"[GameManager] currentStage.monsterModePreset null? {currentStage.monsterModePreset == null}");
            }
            
            if (monsterModeCutInUI != null && currentStage != null)
            {
                // Show cut-in with stage-specific preset
                Debug.Log($"[GameManager] Calling monsterModeCutInUI.Show() with preset: {(currentStage.monsterModePreset != null ? currentStage.monsterModePreset.name : "null")}");
                monsterModeCutInUI.Show(currentStage.monsterModePreset, () => {
                    // After user clicks cut-in, start monster draft
                    Debug.Log("[GameManager] Cut-in complete. Starting monster draft...");
                    StartMonsterDraft();
                });
            }
            else
            {
                // Fallback: directly start monster draft without cut-in
                Debug.LogWarning($"[GameManager] Skipping cut-in. monsterModeCutInUI null? {monsterModeCutInUI == null}, currentStage null? {currentStage == null}");
                StartMonsterDraft();
            }
        }
        
        private void StartMonsterDraft()
        {
            Debug.Log("[GameManager] Starting Monster Draft");
            
            // Check if monster deck exists and has cards
            if (currentStage == null || currentStage.monsterDeck == null || currentStage.monsterDeck.Count == 0)
            {
                Debug.LogWarning("[GameManager] Monster deck is empty! Skipping monster draft.");
                hasPerformedMonsterDraft = true; // Mark as done so it doesn't try again
                turnManager.CompleteDraft(); // IMPORTANT: Complete draft to transition to PlayerAction
                return;
            }
            
            isWaitingForMonsterDraft = true;
            
            var options = draftManager.GenerateMonsterDraftOptions(
                currentStage.monsterDeck,
                gameSettings.monsterDraftCardCount
            );
            
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("[GameManager] Failed to generate monster draft options!");
                isWaitingForMonsterDraft = false;
                hasPerformedMonsterDraft = true;
                turnManager.CompleteDraft(); // IMPORTANT: Complete draft to transition to PlayerAction
                return;
            }
            
            FindObjectOfType<UI.UIManager>()?.ShowMonsterDraft(options);
        }
        
        public void OnMonsterDraftComplete(CardData selectedCard)
        {
            Debug.Log($"[GameManager] Monster Draft complete. Selected: {selectedCard.cardName}");
            
            // Add to hand (1 copy)
            deckManager.hand.Add(selectedCard);
            FindObjectOfType<UI.UIManager>()?.OnCardDrawn(selectedCard);
            
            // Add to draw pile (1 copy)
            deckManager.AddCardToTopOfDraw(selectedCard);
            
            // Add to discard pile (1 copy)
            deckManager.AddCardToDiscard(selectedCard);
            
            Debug.Log($"[GameManager] Monster card '{selectedCard.cardName}' added: 1 to hand, 1 to draw pile, 1 to discard pile");
            
            isWaitingForMonsterDraft = false;
            hasPerformedMonsterDraft = true;
            
            // IMPORTANT: Complete draft to transition to PlayerAction phase
            turnManager.CompleteDraft();
            Debug.Log($"[GameManager] OnMonsterDraftComplete - Phase is now: {turnManager.CurrentPhase}");
            
            // Player must click End Turn button to proceed (no automatic turn end)
        }
        


        // Duplicate GameOver removed. Using the one defined earlier.

        public void FinishStage()
        {
            Debug.Log("[GameManager] FinishStage() called!");
            
            // Save Score
            long score = resourceManager.totalImpressions;
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.LastGameScore = score;
                SaveDataManager.Instance.SaveHighScore(score); // Auto save high score
            }
            
            // Show stage clear cut-in, then navigate to result
            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowStageClearCutIn(() =>
                {
                    // This runs after user clicks the cut-in
                    SceneNavigator.Instance?.GoToResult();
                });
            }
            else
            {
                // Fallback: go directly to result
                Debug.LogWarning("UIManager not found, going directly to Result.");
                SceneNavigator.Instance?.GoToResult();
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
    
    /// <summary>
    /// ターンごとの統計情報（デバッグ用）
    /// </summary>
    [System.Serializable]
    public class TurnStats
    {
        public int turnNumber;
        public int followersGained;
        public long impressionsGained;
        public int mentalChange;
        public long quota;
        public bool quotaMet;
        
        public TurnStats(int turn, int followers, long imps, int mental, long quota, bool met)
        {
            turnNumber = turn;
            followersGained = followers;
            impressionsGained = imps;
            mentalChange = mental;
            this.quota = quota;
            quotaMet = met;
        }
        
        public override string ToString()
        {
            string status = quotaMet ? "✓" : "✗";
            return $"T{turnNumber}: F+{followersGained}, I+{impressionsGained}, M{mentalChange:+#;-#;0} [{status}]";
        }
    }
}
