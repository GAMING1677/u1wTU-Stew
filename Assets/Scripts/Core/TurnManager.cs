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

        public void StartGame()
        {
            turnCount = 1;
            SetPhase(TurnPhase.StartStep);
        }

        public void StartTurn()
        {
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
                    OnTurnEnd?.Invoke();
                    turnCount++;
                    
                    // PROTOTYPE: End game after 5 turns
                    if (turnCount > 5)
                    {
                        SetPhase(TurnPhase.Result);
                        GameManager.Instance.FinishStage();
                    }
                    else
                    {
                        // Wait for GameManager to trigger next turn
                    }
                    break;
                case TurnPhase.Result:
                case TurnPhase.GameOver:
                    break;
            }
        }
    }
}
