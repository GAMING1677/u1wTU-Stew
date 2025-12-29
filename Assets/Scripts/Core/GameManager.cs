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
        [SerializeField] private bool shouldTriggerMonsterModeAfterCutIn = false; // Defer monster mode
        [SerializeField] private bool isMonsterModeFromTurnEnd = false; // Track if monster mode was from turn end
        
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
                
                // Setup clear goal UI after stage is set
                var uiManager = FindObjectOfType<UI.UIManager>();
                if (uiManager != null)
                {
                    uiManager.SetupClearGoal();
                }
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
            
            
            // REMOVED: Monster mode event registration
            // Event fires immediately, preventing deferred trigger
            // OnMonsterModeTriggered will be called manually when needed
            // resourceManager.onMonsterModeTriggered.AddListener(OnMonsterModeTriggered);
            Debug.Log("[GameManager] Monster mode will be triggered manually (not via event)");
            
            // Prevent duplicate listeners - only remove GameManager's own listeners
            turnManager.OnTurnStart.RemoveListener(OnTurnStart);
            turnManager.OnTurnEnd.RemoveListener(OnTurnEnd);
            // REMOVED: OnDraftStart - handled entirely by UIManager
            // turnManager.OnDraftStart.RemoveListener(OnDraftStart);
            
            // Remove GameManager's resource listeners (not all listeners!)
            resourceManager.onMentalChanged.RemoveListener(OnMentalChanged);
            resourceManager.onImpressionsChanged.RemoveListener(OnImpressionsChanged);

            // Hook up events
            turnManager.OnTurnStart.AddListener(OnTurnStart);
            turnManager.OnTurnEnd.AddListener(OnTurnEnd);
            // REMOVED: OnDraftStart - handled entirely by UIManager
            // turnManager.OnDraftStart.AddListener(OnDraftStart);
            
            resourceManager.onMentalChanged.AddListener(OnMentalChanged);
            resourceManager.onImpressionsChanged.AddListener(OnImpressionsChanged);
            
            // ★ NEW: ステージ開始カットインを表示してから実際にゲームを開始
            ShowStageStartCutIn();
        }
        
        /// <summary>
        /// ステージ開始カットインを表示し、クリック後にゲームを開始
        /// </summary>
        private void ShowStageStartCutIn()
        {
            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[GameManager] UIManager not found, starting game directly");
                turnManager.StartGame();
                return;
            }
            
            // ステージ開始SEを再生
            AudioManager.Instance?.PlaySE(Data.SEType.StageStart);
            
            // プリセットが設定されている場合はプリセットを使用
            if (currentStage?.stageStartPreset != null)
            {
                Debug.Log($"[GameManager] Showing stage start cut-in with preset");
                uiManager.ShowCutInPreset(currentStage.stageStartPreset, () =>
                {
                    Debug.Log("[GameManager] Stage start cut-in dismissed, starting game");
                    turnManager.StartGame();
                });
            }
            else
            {
                // プリセットが設定されていない場合は直接開始
                Debug.Log("[GameManager] No stage start cut-in configured, starting game directly");
                turnManager.StartGame();
            }
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
                SceneNavigator.Instance.WasStageCleared = false; // Game over = failed
                SceneNavigator.Instance.IsScoreAttackMode = (currentStage == null || currentStage.clearCondition == null || !currentStage.clearCondition.hasScoreGoal);
                Debug.Log($"[GameManager] Saved game over score: {score}, cleared=false");
            }
            
            // Navigate directly to result scene
            if (SceneNavigator.Instance != null)
            {
                Debug.Log("[GameManager] Navigating to Result scene after game over");
                SceneNavigator.Instance.GoToResult();
            }
            else
            {
                Debug.LogError("[GameManager] SceneNavigator.Instance is null!");
            }
        }
        
        /// <summary>
        /// ステージクリア（スコア達成時）
        /// </summary>
        private void ClearStage()
        {
            Debug.Log("[GameManager] ClearStage() called (Score goal achieved)!");
            isGameActive = false;
            
            // Save clear status
            if (SaveDataManager.Instance != null && currentStage != null)
            {
                SaveDataManager.Instance.SaveStageClear(currentStage.stageName);
                Debug.Log($"[GameManager] Stage '{currentStage.stageName}' marked as cleared");
            }
            
            // Save Score
            long score = resourceManager.totalImpressions;
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.LastGameScore = score;
                SceneNavigator.Instance.WasStageCleared = true; // Score clear = success
                SceneNavigator.Instance.IsScoreAttackMode = false; // Score goal exists
                Debug.Log($"[GameManager] Saved clear score: {score}, cleared=true");
            }
            
            // Navigate to result scene
            if (SceneNavigator.Instance != null)
            {
                Debug.Log("[GameManager] Navigating to Result scene after score clear");
                SceneNavigator.Instance.GoToResult();
            }
            else
            {
                Debug.LogError("[GameManager] SceneNavigator.Instance is null!");
            }
        }
        
        public void FinishStage()
        {
            Debug.Log("[GameManager] FinishStage() called (Turn limit reached)!");
            isGameActive = false;
            
            // スコアゴールが設定されている場合は達成チェック
            bool hasScoreGoal = (currentStage != null && 
                                currentStage.clearCondition != null && 
                                currentStage.clearCondition.hasScoreGoal);
            bool scoreAchieved = false;
            
            if (hasScoreGoal)
            {
                scoreAchieved = resourceManager.totalImpressions >= currentStage.clearCondition.targetScore;
                Debug.Log($"[GameManager] Score goal check: {resourceManager.totalImpressions} >= {currentStage.clearCondition.targetScore} = {scoreAchieved}");
            }
            
            // クリア判定：スコアゴールがない、またはスコアゴール達成した場合のみクリア
            bool wasCleared = !hasScoreGoal || scoreAchieved;
            
            // セーブ：クリアした場合のみ記録
            if (wasCleared && SaveDataManager.Instance != null && currentStage != null)
            {
                SaveDataManager.Instance.SaveStageClear(currentStage.stageName);
                Debug.Log($"[GameManager] Stage '{currentStage.stageName}' marked as cleared (turn limit, score achieved)");
            }
            else if (hasScoreGoal && !scoreAchieved)
            {
                Debug.Log($"[GameManager] Stage '{currentStage.stageName}' NOT cleared (turn limit, score NOT achieved)");
            }
            
            // Save Score
            long score = resourceManager.totalImpressions;
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.LastGameScore = score;
                SceneNavigator.Instance.WasStageCleared = wasCleared;
                SceneNavigator.Instance.IsScoreAttackMode = !hasScoreGoal;
                Debug.Log($"[GameManager] Saved finish score: {score}, cleared={wasCleared}, isScoreAttack={!hasScoreGoal}");
            }
            
            // Navigate directly to result scene
            if (SceneNavigator.Instance != null)
            {
                Debug.Log("[GameManager] Navigating to Result scene after stage completion");
                SceneNavigator.Instance.GoToResult();
            }
            else
            {
                Debug.LogError("[GameManager] SceneNavigator.Instance is null!");
            }
        }

        /// <summary>
        /// スコアベースのクリア条件をチェック
        /// </summary>
        private void CheckScoreClear()
        {
            // ゲーム非アクティブ時はチェックしない
            if (!isGameActive)
            {
                Debug.Log("[GameManager] CheckScoreClear: Game not active, skipping");
                return;
            }
            
            // Debug: Current stage info
            Debug.Log($"[GameManager] CheckScoreClear: currentStage={(currentStage != null ? currentStage.stageName : "NULL")}");
            
            // クリア条件がnullまたはhasScoreGoal=falseなら無制限プレイ
            if (currentStage == null)
            {
                Debug.LogWarning("[GameManager] CheckScoreClear: currentStage is NULL!");
                return;
            }
            
            if (currentStage.clearCondition == null)
            {
                Debug.Log($"[GameManager] CheckScoreClear: Stage '{currentStage.stageName}' has no clearCondition (unlimited play)");
                return;
            }
            
            if (!currentStage.clearCondition.hasScoreGoal)
            {
                Debug.Log($"[GameManager] CheckScoreClear: Stage '{currentStage.stageName}' hasScoreGoal=false (unlimited play)");
                return;
            }
            
            // Score check
            long currentScore = resourceManager.totalImpressions;
            long targetScore = currentStage.clearCondition.targetScore;
            Debug.Log($"[GameManager] CheckScoreClear: Stage '{currentStage.stageName}' score check: {currentScore} / {targetScore} (hasScoreGoal={currentStage.clearCondition.hasScoreGoal})");
            
            if (currentScore >= targetScore)
            {
                Debug.Log($"[GameManager] Score clear! {currentScore} >= {targetScore}");
                ClearStage();
            }
        }


        public void ResetGame()
        {
            Debug.Log("[GameManager] ResetGame called - full state reset");
            
            // 0. Reset game active flag
            isGameActive = false;
            
            // 1. Explicitly clear deck first
            deckManager.ClearAll();

            // 2. Reset Resources (also resets monster mode flags)
            resourceManager.Initialize(gameSettings);
            extraTurnDraws = 0;
            
            // Clear debug statistics
            turnStatsList.Clear();
            
            // 3. Reset Deck (if stage is set)
            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }
            
            // 4. Reset Quota
            turnStartImpressions = 0;
            turnStartFollowers = 0;
            turnStartMental = 0;
            lastTurnGainedFollowers = 0;
            currentTurnQuota = 0;
            
            // 5. Reset Draft Manager
            if (draftManager != null)
            {
                draftManager.ResetSelectedCards();
            }
            
            // 6. Reset ALL game state flags
            hasPerformedMonsterDraft = false;
            isWaitingForMonsterDraft = false;
            isMonsterModeFromTurnEnd = false;
            shouldTriggerMonsterModeAfterCutIn = false;
            
            // 7. Remove event listeners to prevent duplicates on next StartGame
            if (turnManager != null)
            {
                turnManager.OnTurnStart.RemoveListener(OnTurnStart);
                turnManager.OnTurnEnd.RemoveListener(OnTurnEnd);
                turnManager.ResetState(); // Reset turn count and phase
            }
            if (resourceManager != null)
            {
                resourceManager.onMentalChanged.RemoveListener(OnMentalChanged);
                resourceManager.onImpressionsChanged.RemoveListener(OnImpressionsChanged);
            }
            
            // 8. Reset character profile to normal and UI state
            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager != null)
            {
                if (currentStage != null && currentStage.normalProfile != null)
                {
                    uiManager.SetupCharacter(currentStage.normalProfile);
                }
                uiManager.StopCharacterReaction(); // Stop any playing animations
                uiManager.ClearTimeline(); // Clear timeline posts
                uiManager.ResetEndTurnButtonPulse(); // Reset pulse animation
                // NOTE: SetupClearGoal is called in StartGame after currentStage is updated
                Debug.Log("[GameManager] Reset UI state (character, timeline, pulse)");
            }
            
            Debug.Log("[GameManager] ResetGame complete");
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
            
            // Reset flaming state for new turn (seeds persist)
            resourceManager.ResetFlamingTurn();
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
            
            // ========== Penalty + Flaming Damage ==========
            int penalty = quotaMet ? 0 : CalculateQuotaPenalty(turnManager.CurrentTurnCount);
            int flamingDamage = resourceManager.ConsumeFlamingLevel();
            int totalDamage = penalty + flamingDamage;
            
            if (totalDamage > 0)
            {
                Debug.Log($"[GameManager] Turn End Damage: penalty={penalty}, flaming={flamingDamage}, total={totalDamage}");
                
                // Check mental before damage
                int mentalBefore = resourceManager.currentMental;
                int threshold = gameSettings != null ? gameSettings.monsterThreshold : 90;
                
                resourceManager.DamageMental(totalDamage);
                
                // Check if monster mode was just triggered
                if (mentalBefore > threshold && 
                    resourceManager.currentMental <= threshold &&
                    !hasPerformedMonsterDraft)
                {
                    Debug.Log("[GameManager] Monster mode triggered - will be deferred until after turn result cut-in");
                    shouldTriggerMonsterModeAfterCutIn = true;
                }
                
                // Check if GameOver was triggered by DamageMental
                if (!isGameActive)
                {
                    Debug.Log("[GameManager] OnTurnEnd - GameOver was triggered during penalty, stopping");
                    return;
                }
                
                // Update mental change after penalty
                mentalChange = resourceManager.currentMental - turnStartMental;
            }
            
            if (quotaMet)
            {
                Debug.Log("[GameManager] Quota Met!");
            }
            else
            {
                Debug.Log($"[GameManager] Quota Failed! Penalty: {penalty}");
            }
            
            // Record turn statistics for debug (use ended turn number, not next turn)
            var stats = new TurnStats(endedTurn, followerGained, impGained, mentalChange, currentTurnQuota, quotaMet);
            turnStatsList.Add(stats);
            Debug.Log($"[GameManager] Turn Stats: {stats}");
            
            deckManager.DiscardHand();
            
            // Only start next turn if game is still active and not ending
            Debug.Log($"[GameManager] OnTurnEnd - After processing, Phase: {turnManager.CurrentPhase}, isGameActive: {isGameActive}");
            
            // Only start next turn if game is still active and not ending
            Debug.Log($"[GameManager] OnTurnEnd - After processing, Phase: {turnManager.CurrentPhase}, isGameActive: {isGameActive}");
            
            if (isGameActive && 
                turnManager.CurrentPhase != TurnManager.TurnPhase.Result && 
                turnManager.CurrentPhase != TurnManager.TurnPhase.GameOver)
            {
                // Show Turn Result Cut-in
                string title = quotaMet ? "ノルマ達成！" : "ノルマ未達...";
                string message = quotaMet 
                    ? $"インプレッション: +{impGained:N0}\nフォロワー: +{followerGained:N0}" 
                    : $"ペナルティ: メンタル -{CalculateQuotaPenalty(endedTurn)}";
                
                // Color override (Green for success, Red/Purple for fail?) - Optional
                // For now just standard cut-in
                
                Debug.Log("[GameManager] Showing Turn Result CutIn");
                
                // Determine Character Reaction
                double ratio = (double)impGained / currentTurnQuota;
                UI.CharacterAnimator.ReactionType reactionType = UI.CharacterAnimator.ReactionType.Sad_1;
                
                if (ratio >= 5.0) reactionType = UI.CharacterAnimator.ReactionType.Happy_3;
                else if (ratio >= 1.0) reactionType = UI.CharacterAnimator.ReactionType.Happy_2;
                else if (ratio < 0.5) reactionType = UI.CharacterAnimator.ReactionType.Sad_2;
                else reactionType = UI.CharacterAnimator.ReactionType.Sad_1; // 0.5 <= ratio < 1.0
                
                Debug.Log($"[GameManager] Quota Ratio: {ratio:F2} (Gained: {impGained} / Quota: {currentTurnQuota}) -> Reaction: {reactionType}");
                
                var ui = FindObjectOfType<UI.UIManager>();
                if (ui != null)
                {
                    ui.ShowCharacterReaction(reactionType, loop: true);
                }
                else
                {
                    Debug.LogError("[GameManager] UIManager not found when trying to show reaction!");
                }
                
                FindObjectOfType<UI.UIManager>()?.ShowCutIn(title, message, () => {
                    Debug.Log("[GameManager] CutIn dismissed");
                    
                    // Stop looping reaction
                    FindObjectOfType<UI.UIManager>()?.StopCharacterReaction();
                    
                    // Check if monster mode should be triggered now
                    if (shouldTriggerMonsterModeAfterCutIn)
                    {
                        Debug.Log("[GameManager] Triggering deferred monster mode");
                        shouldTriggerMonsterModeAfterCutIn = false;
                        
                        // Mark that this monster mode is from turn end (not mid-turn card play)
                        isMonsterModeFromTurnEnd = true;
                        isWaitingForMonsterDraft = true;
                        OnMonsterModeTriggered();
                        // Note: StartTurn will be called after monster draft completes
                    }
                    else
                    {
                        turnManager.StartTurn();
                    }
                });
            }
            else
            {
                Debug.Log("[ GameManager] OnTurnEnd - Game is ending, not starting next turn");
            }
        }
        
        /// <summary>
        /// ノルマ未達成時のペナルティ計算
        /// round(ターン数 / 10) × 5
        /// 例: 10ターン以下なら5, 11-20ターンなら10, 21-30ターンなら15
        /// </summary>
        public int CalculateQuotaPenalty(int currentTurn)
        {
            int multiplier = Mathf.CeilToInt(currentTurn / 10f);
            if (multiplier < 1) multiplier = 1; // 最低でも1（つまり5ダメージ）
            return multiplier * 5;
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
            
            // Notify UI (penalty is now calculated by UI itself)
            onQuotaUpdate?.Invoke(gained, currentTurnQuota);
        }

        // REMOVED: OnDraftStart method
        // Draft handling is now entirely managed by UIManager
        // Monster draft is triggered manually from turn result cut-in callback
        
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
                
                // Show cut-in for insufficient motivation
                var uiManager = FindObjectOfType<UI.UIManager>();
                if (uiManager != null)
                {
                    // やる気不足SE再生
                    AudioManager.Instance?.PlaySE(Data.SEType.MotivationLow);
                    
                    // モチベ不足カットインを表示
                    uiManager.ShowMotivationLowCutIn();
                }
                
                return;
            }

            // ========== Flaming System ==========
            bool flamingTriggeredThisCard = false;
            
            // 1. 特殊カード①: 種×インプ率でインプレッション獲得、種消費
            if (card.seedToFollowerMultiplier > 0 && !card.healMentalBySeeds)
            {
                int seeds = resourceManager.flamingSeeds;
                if (seeds > 0)
                {
                    // ②パターン: 炎上率がある場合はギャンブル
                    if (card.flamingRate > 0 && !resourceManager.isOnFire)
                    {
                        if (Random.value <= card.flamingRate)
                        {
                            // 炎上発動 → スコアなし、種消費して炎上度へ
                            resourceManager.TryTriggerFlaming(1.0f);
                            flamingTriggeredThisCard = true;
                            Debug.Log("[GameManager] Gamble card: FLAMING TRIGGERED! No score.");
                        }
                        else
                        {
                            // 成功 → インプレッション獲得（フォロワー×種×倍率）、種維持
                            float impRate = seeds * card.seedToFollowerMultiplier;
                            long impGained = resourceManager.AddImpression(impRate);
                            Debug.Log($"[GameManager] Gamble card: SUCCESS! {seeds} seeds × {card.seedToFollowerMultiplier} rate = {impGained} impressions");
                        }
                    }
                    else
                    {
                        // ①パターン: 確実にインプレッション獲得、種消費
                        float impRate = seeds * card.seedToFollowerMultiplier;
                        long impGained = resourceManager.AddImpression(impRate);
                        resourceManager.flamingSeeds = 0;
                        resourceManager.onFlamingChanged?.Invoke(0, resourceManager.flamingLevel, resourceManager.isOnFire);
                        Debug.Log($"[GameManager] Seed to Impression: {seeds} seeds × {card.seedToFollowerMultiplier} rate = {impGained} impressions, seeds consumed");
                    }
                }
            }
            // 2. 特殊カード③: 種でメンタル回復（種の1/2、切り上げ）
            else if (card.healMentalBySeeds)
            {
                int seeds = resourceManager.flamingSeeds;
                if (seeds > 0)
                {
                    int healAmount = Mathf.CeilToInt(seeds / 2f);
                    resourceManager.HealMental(healAmount);
                    resourceManager.flamingSeeds = 0;
                    resourceManager.onFlamingChanged?.Invoke(0, resourceManager.flamingLevel, resourceManager.isOnFire);
                    Debug.Log($"[GameManager] Heal by seeds: {seeds} seeds → {healAmount} mental healed, seeds consumed");
                }
            }
            // 3. 通常の炎上処理（種加算）
            else if (card.flamingSeedCount > 0)
            {
                resourceManager.AddFlamingSeeds(card.flamingSeedCount);
                
                // 炎上率判定（炎上中でなければ）
                if (card.flamingRate > 0 && !resourceManager.isOnFire)
                {
                    if (resourceManager.TryTriggerFlaming(card.flamingRate))
                    {
                        flamingTriggeredThisCard = true;
                        // TODO: 炎上カットイン表示
                        FindObjectOfType<UI.UIManager>()?.ShowCutIn("炎上！", $"ターン終了時に {resourceManager.flamingLevel} ダメージ");
                    }
                }
            }
            // ========== Flaming System End ==========

            // Mental cost (positive = hurt)
            if (card.mentalCost > 0)
            {
                int mentalBefore = resourceManager.currentMental;
                int threshold = gameSettings != null ? gameSettings.monsterThreshold : 90;
                
                resourceManager.DamageMental(card.mentalCost);
                
                // Check if monster mode was triggered during card play
                if (mentalBefore > threshold && 
                    resourceManager.currentMental <= threshold &&
                    !hasPerformedMonsterDraft)
                {
                    Debug.Log("[GameManager] Monster mode triggered during card play");
                    // Mark that this is from card play, not turn end
                    isMonsterModeFromTurnEnd = false;
                    // Trigger immediately (no StartTurn after draft)
                    OnMonsterModeTriggered();
                }
            }
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
                            deckManager.AddCardToMiddleOfDraw(gen.card);
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
            
            // Play card play SE (モンスターカードか通常カードか判定)
            if (resourceManager.isMonsterMode && currentStage != null && currentStage.monsterDeck != null && currentStage.monsterDeck.Contains(card))
            {
                AudioManager.Instance?.PlaySE(Data.SEType.MonsterCardPlay);
            }
            else
            {
                AudioManager.Instance?.PlaySE(Data.SEType.CardPlay);
            }
            
            // Show Happy reaction on card play
            FindObjectOfType<UI.UIManager>()?.ShowCharacterReaction(UI.CharacterAnimator.ReactionType.Happy_1);
            
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
            
            // スコアクリア条件をチェック（カードプレイ後）
            CheckScoreClear();
            
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
            
            // ★ Bug fix: 即座にフラグを設定して再発動を防止
            hasPerformedMonsterDraft = true;
            
            // Set monster mode state (since ResourceManager no longer does this)
            resourceManager.isMonsterMode = true;
            
            // Heal mental: currentMental / 2 (rounded up)
            int healAmount = Mathf.CeilToInt(resourceManager.currentMental / 2f);
            resourceManager.HealMental(healAmount);
            Debug.Log($"[GameManager] Monster Mode Activated! Healed {healAmount} mental to {resourceManager.currentMental}");
            
            // Switch BGM to monster mode BGM
            if (currentStage != null && currentStage.monsterModePreset != null && currentStage.monsterModePreset.monsterBGM != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBGM(currentStage.monsterModePreset.monsterBGM);
                    Debug.Log($"[GameManager] Switched to monster BGM: {currentStage.monsterModePreset.monsterBGM.name}");
                }
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
            
            // Only set this flag if from turn end - mid-turn doesn't need StartTurn
            if (isMonsterModeFromTurnEnd)
            {
                isWaitingForMonsterDraft = true;
            }
            
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
            
            // Add to draw pile (1 copy) - 山札の真ん中に追加
            deckManager.AddCardToMiddleOfDraw(selectedCard);
            
            // Add to discard pile (1 copy)
            deckManager.AddCardToDiscard(selectedCard);
            
            Debug.Log($"[GameManager] Monster card '{selectedCard.cardName}' added: 1 to hand, 1 to draw pile, 1 to discard pile");
            
            // Switch to monster profile
            var uiManager = FindObjectOfType<UI.UIManager>();
            if (uiManager != null)
            {
                uiManager.SwitchToMonsterProfile();
                Debug.Log("[GameManager] Switched to monster profile");
            }
            
            // Check flag BEFORE clearing it
            bool wasFromTurnEnd = isMonsterModeFromTurnEnd;
            
            // Clear flags
            isWaitingForMonsterDraft = false;
            isMonsterModeFromTurnEnd = false;
            hasPerformedMonsterDraft = true;
            
            // If from turn end, start next turn now
            // Otherwise (from card play), just return to normal game flow (continue in PlayerAction)
            if (wasFromTurnEnd)
            {
                Debug.Log("[GameManager] Monster draft complete - starting next turn (from turn end)");
                turnManager.StartTurn();
            }
            else
            {
                Debug.Log("[GameManager] Monster draft complete - continuing card play (mid-turn)");
            }
        }
        


        // Duplicate GameOver removed. Using the one defined earlier.

        
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
    public class QuotaUpdateEvent : UnityEngine.Events.UnityEvent<long, long> { }
    
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
