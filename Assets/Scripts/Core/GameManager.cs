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
            
            // Initial setup if not already done via Reset
            if (resourceManager.currentMental <= 0 && gameSettings != null)
            {
                 resourceManager.Initialize(gameSettings);
            }
            
            // Prevent duplicate listeners
            turnManager.OnTurnStart.RemoveListener(OnTurnStart);
            turnManager.OnTurnEnd.RemoveListener(OnTurnEnd);
            turnManager.OnDraftStart.RemoveListener(OnDraftStart);

            // Hook up events
            turnManager.OnTurnStart.AddListener(OnTurnStart);
            turnManager.OnTurnEnd.AddListener(OnTurnEnd);
            turnManager.OnDraftStart.AddListener(OnDraftStart);
            
            // Resource listeners
            resourceManager.onMentalChanged.RemoveListener(OnMentalChanged);
            resourceManager.onMentalChanged.AddListener(OnMentalChanged);

            turnManager.StartGame();
        }
        
        private void OnMentalChanged(int current, int max)
        {
            if (isGameActive && current <= 0)
            {
                GameOver();
            }
        }

        public void ResetGame()
        {
            // 0. Explicitly clear everything first
            deckManager.ClearAll();

            // 1. Reset Resources
            resourceManager.Initialize(gameSettings);
            
            // 2. Reset Deck
            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }
            
            // 3. Reset Draft Manager
            if (draftManager != null)
            {
                draftManager.ResetSelectedCards();
            }
        }

        private void OnTurnStart()
        {
            Debug.Log("[GameManager] OnTurnStart received. Drawing cards.");
            resourceManager.ResetMotivation();
            deckManager.DrawCards(gameSettings.initialHandSize);
        }

        private void OnTurnEnd()
        {
            deckManager.DiscardHand();
            // Check Quota or Monster Mode consistency here
        }
        
        private void OnDraftStart()
        {
            Debug.Log("[GameManager] OnDraftStart received. Generating draft options.");
            // Draft options will be generated and shown by UIManager
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

            Debug.Log($"Playing Card: {card.cardName}");
            // Execute Card Effect
            resourceManager.AddImpression(card.impressionRate);
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
            
            // Mental cost (can be positive for damage or negative for heal)
            if (card.mentalCost > 0) resourceManager.DamageMental(card.mentalCost);
            else if (card.mentalCost < 0) resourceManager.HealMental(-card.mentalCost);

            // Move card
            deckManager.PlayCard(card);
            
            // カードプレイ完了後、モンスタードラフトチェック
            // まだドラフトを行っておらず、かつモンスターモードになった場合のみ実行
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft && !isWaitingForMonsterDraft)
            {
                StartMonsterDraft();
                return; // ドラフト待機へ
            }
            
            // Check turn end condition
            if (resourceManager.currentMotivation <= 0)
            {
                turnManager.EndPlayerAction();
            }

            // Monster Mode Trigger Check
            // Triggered if we are in monster mode, haven't drafted yet, and aren't waiting
            if (resourceManager.isMonsterMode && !hasPerformedMonsterDraft && !isWaitingForMonsterDraft)
            {
                StartMonsterDraft();
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
        
        private void StartMonsterDraft()
        {
            Debug.Log("[GameManager] Starting Monster Draft");
            isWaitingForMonsterDraft = true;
            
            // UIManagerにモンスタードラフト開始を通知
            var options = draftManager.GenerateMonsterDraftOptions(
                currentStage.monsterDeck,
                gameSettings.monsterDraftCardCount
            );
            
            // UIManagerのメソッドを呼び出し
            FindObjectOfType<UI.UIManager>()?.ShowMonsterDraft(options);
        }
        
        public void OnMonsterDraftComplete(CardData selectedCard)
        {
            Debug.Log($"[GameManager] Monster Draft complete. Selected: {selectedCard.cardName}");
            
            // 選択したカードを手札に直接追加
            deckManager.hand.Add(selectedCard);
            
            // UIManagerに手札更新を通知
            FindObjectOfType<UI.UIManager>()?.OnCardDrawn(selectedCard);
            
            isWaitingForMonsterDraft = false;
            hasPerformedMonsterDraft = true; // Mark as completed
            
            // ターン終了チェックをここでも行う
            if (resourceManager.currentMotivation <= 0)
            {
                turnManager.EndPlayerAction();
            }
        }

        public void CheckGameEndCondition()
        {
             // ターン数制限などをここでチェック
             // 例: 5ターン終了でクリア
             // if (turnManager.CurrentTurn > 5) FinishStage();
        }

        private void GameOver()
        {
            Debug.Log("[GameManager] Game Over! Mental depleted.");
            isGameActive = false;
            
            // ゲームオーバー時の処理
            // 現状はスコアを保存してリザルトへ
            // TODO: ゲームオーバー演出（「活動停止...」など）を追加
            
            FinishStage();
        }

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
}
