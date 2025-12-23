using UnityEngine;
using UnityEngine.Events;

namespace ApprovalMonster.Core
{
    public class TurnManager : MonoBehaviour
    {
        public enum TurnPhase
        {
            StartStep,
            PlayerAction,
            EndStep,
            Result,
            GameOver
        }

        [SerializeField] private TurnPhase currentPhase;
        public TurnPhase CurrentPhase => currentPhase;

        public UnityEvent OnTurnStart;
        public UnityEvent OnTurnEnd;
        public UnityEvent<int> OnTurnChanged;

        private int turnCount;

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

        private void SetPhase(TurnPhase nextPhase)
        {
            currentPhase = nextPhase;
            
            switch (currentPhase)
            {
                case TurnPhase.StartStep:
                    OnTurnStart?.Invoke();
                    OnTurnChanged?.Invoke(turnCount);
                    // Proceed to action automatically or after animation
                    SetPhase(TurnPhase.PlayerAction);
                    break;
                case TurnPhase.PlayerAction:
                    // Enable Input
                    break;
                case TurnPhase.EndStep:
                    OnTurnEnd?.Invoke();
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
                        StartTurn(); 
                    }
                    break;
                case TurnPhase.Result:
                case TurnPhase.GameOver:
                    break;
            }
        }
    }
}
