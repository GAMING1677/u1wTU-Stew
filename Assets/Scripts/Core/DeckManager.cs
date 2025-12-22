using UnityEngine;
using System.Collections.Generic;
using ApprovalMonster.Data;
using NaughtyAttributes;

namespace ApprovalMonster.Core
{
    public class DeckManager : MonoBehaviour
    {
        [Header("Decks")]
        [ReadOnly] public List<CardData> drawPile = new List<CardData>();
        [ReadOnly] public List<CardData> hand = new List<CardData>();
        [ReadOnly] public List<CardData> discardPile = new List<CardData>();

        [Header("Settings")]
        [Expandable]
        public GameSettings settings;

        public System.Action<CardData> OnCardDrawn;
        public System.Action<CardData> OnCardDiscarded;
        public System.Action OnDeckShuffled;

        public void InitializeDeck(List<CardData> initialDeck, GameSettings gameSettings)
        {
            settings = gameSettings;
            drawPile = new List<CardData>(initialDeck);
            hand.Clear();
            discardPile.Clear();
            ShuffleDrawPile();
        }

        public void ShuffleDrawPile()
        {
            // Fisher-Yates shuffle
            for (int i = 0; i < drawPile.Count; i++)
            {
                CardData temp = drawPile[i];
                int randomIndex = Random.Range(i, drawPile.Count);
                drawPile[i] = drawPile[randomIndex];
                drawPile[randomIndex] = temp;
            }
            OnDeckShuffled?.Invoke();
        }

        public void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count > 0)
                    {
                        ReshuffleDiscardToDraw();
                    }
                    else
                    {
                        // No cards left
                        break;
                    }
                }

                CardData card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                OnCardDrawn?.Invoke(card);
            }
        }

        private void ReshuffleDiscardToDraw()
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDrawPile();
        }

        public void DiscardHand()
        {
            foreach (var card in hand)
            {
                discardPile.Add(card);
                OnCardDiscarded?.Invoke(card);
            }
            hand.Clear();
        }

        public void PlayCard(CardData card)
        {
            if (hand.Contains(card))
            {
                hand.Remove(card);
                discardPile.Add(card);
                OnCardDiscarded?.Invoke(card); // Or specific OnCardPlayed event
            }
        }
        
        public void AddCardToDiscard(CardData newCard)
        {
             discardPile.Add(newCard);
        }
    }
}
