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

        [Header("Data")]
        [Expandable]
        public GameSettings gameSettings;
        [Expandable]
        public StageData currentStage;

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
            resourceManager.Initialize(gameSettings);
            
            // Should pass currentStage.initialDeck
            if (currentStage != null)
            {
                deckManager.InitializeDeck(currentStage.initialDeck, gameSettings);
            }

            // Hook up events
            turnManager.OnTurnStart.AddListener(OnTurnStart);
            turnManager.OnTurnEnd.AddListener(OnTurnEnd);

            turnManager.StartGame();
        }

        private void OnTurnStart()
        {
            resourceManager.ResetMotivation();
            deckManager.DrawCards(gameSettings.initialHandSize);
        }

        private void OnTurnEnd()
        {
            deckManager.DiscardHand();
            // Check Quota or Monster Mode consistency here
        }

        public void TryPlayCard(CardData card)
        {
            // Check costs
            if (resourceManager.currentMotivation < card.motivationCost)
            {
                Debug.Log("Not enough motivation!");
                return;
            }

            // Pay costs
            resourceManager.UseMotivation(card.motivationCost);
            // Mental cost (positive = hurt)
            if (card.mentalCost > 0)
                resourceManager.DamageMental(card.mentalCost);
            else if (card.mentalCost < 0)
                resourceManager.HealMental(-card.mentalCost);

            // Execute Effects
            resourceManager.AddFollowers(card.followerGain);
            if (card.impressionRate > 0)
            {
                resourceManager.AddImpression(card.impressionRate);
            }

            // Risk Logic
            if (card.HasRisk())
            {
               // Implement risk logic e.g. probability check
               if (Random.value < card.riskProbability)
               {
                   ApplyRisk(card.riskType, card.riskValue);
               }
            }

            // Move card
            deckManager.PlayCard(card);
            
            // Check turn end condition
            if (resourceManager.currentMotivation <= 0)
            {
                turnManager.EndPlayerAction();
            }
        }
        
        private void ApplyRisk(RiskType risk, int value)
        {
            switch (risk)
            {
                case RiskType.Flame:
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
