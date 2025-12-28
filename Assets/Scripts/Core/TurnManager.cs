using UnityEngine;
using UnityEngine.Events;

namespace ApprovalMonster.Core
{
    public class TurnManager : MonoBehaviour
    {
        public enum TurnPhase
        {
            StartStep,
            DraftPhase,
            PlayerAction,
            EndStep,
            Result,
            GameOver
        }

        [SerializeField] private TurnPhase currentPhase;
        public TurnPhase CurrentPhase => currentPhase;

        public UnityEvent OnTurnStart;
        public UnityEvent OnDraftStart;
        public UnityEvent OnTurnEnd;
        public UnityEvent<int> OnTurnChanged;

        private int turnCount;
        public int CurrentTurnCount => turnCount;

        /// <summary>
        /// ゲーム状態をリセット（再プレイ時に呼び出す）
        /// </summary>
        public void ResetState()
        {
            Debug.Log("[TurnManager] ResetState called");
            turnCount = 0;
            currentPhase = TurnPhase.StartStep;
        }

        public void StartGame()
        {
            Debug.Log($"[TurnManager] StartGame called. StackTrace:\n{System.Environment.StackTrace}");
            turnCount = 1;
            SetPhase(TurnPhase.StartStep);
        }

        public void StartTurn()
        {
            Debug.Log($"[TurnManager] StartTurn called. StackTrace:\n{System.Environment.StackTrace}");
            SetPhase(TurnPhase.StartStep);
        }

        public void EndPlayerAction()
        {
            if (currentPhase == TurnPhase.PlayerAction)
            {
                SetPhase(TurnPhase.EndStep);
            }
        }
        
        public void CompleteDraft()
        {
            if (currentPhase == TurnPhase.DraftPhase)
            {
                SetPhase(TurnPhase.PlayerAction);
            }
        }

        public void SetPhase(TurnPhase nextPhase)
        {
            currentPhase = nextPhase;
            
            switch (currentPhase)
            {
                case TurnPhase.StartStep:
                    OnTurnStart?.Invoke();
                    OnTurnChanged?.Invoke(turnCount);
                    // Proceed to draft phase
                    SetPhase(TurnPhase.DraftPhase);
                    break;
                case TurnPhase.DraftPhase:
                    OnDraftStart?.Invoke();
                    // Wait for player to select a card (CompleteDraft will be called)
                    break;
                case TurnPhase.PlayerAction:
                    // Enable Input
                    break;
                case TurnPhase.EndStep:
                    // Increment turn count BEFORE firing OnTurnEnd
                    // This ensures the next turn starts with correct count
                    turnCount++;
                    
                    // Get max turn count from settings
                    int maxTurns = GameManager.Instance?.currentStage?.maxTurnCount ?? 20;
                    
                    // Turn limit checked
                    if (turnCount > maxTurns)
                    {
                        OnTurnEnd?.Invoke();
                        SetPhase(TurnPhase.Result);
                        GameManager.Instance.FinishStage();
                    }
                    else
                    {
                        OnTurnEnd?.Invoke();
                        // GameManager.OnTurnEnd will call StartTurn()
                    }
                    break;
                case TurnPhase.Result:
                case TurnPhase.GameOver:
                    break;
            }
        }
    }
}
