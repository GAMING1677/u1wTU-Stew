using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
        public System.Action OnReset; // New event
        
        // Deck count events
        public System.Action<int, int> OnDeckCountChanged; // (drawPileCount, discardPileCount)

        public bool isDrawing { get; set; } = false;

        public void InitializeDeck(List<CardData> initialDeck, GameSettings gameSettings)
        {
            Debug.Log($"[DeckManager] InitializeDeck called. Incoming deck count: {initialDeck?.Count ?? 0}");
            settings = gameSettings;
            if (initialDeck == null)
            {
                Debug.LogError("[DeckManager] InitialDeck is NULL!");
                drawPile = new List<CardData>();
            }
            else
            {
                drawPile = new List<CardData>(initialDeck);
                Debug.Log($"[DeckManager] DrawPile initialized with {drawPile.Count} cards.");
            }
            ShuffleDrawPile();
        }
        
        public void ClearAll()
        {
            hand.Clear();
            discardPile.Clear();
            drawPile.Clear();
            OnReset?.Invoke();
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
            
            // Play shuffle SE
            AudioManager.Instance?.PlaySE(Data.SEType.DeckShuffle);
            
            OnDeckShuffled?.Invoke();
        }

        public void DrawCards(int count)
        {
            Debug.Log($"[DeckManager] DrawCards called. Requesting {count} cards. DrawPile: {drawPile.Count}, DiscardPile: {discardPile.Count}");
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
                Debug.Log($"[DeckManager] Drawing card: {card.cardName}. Invoking OnCardDrawn.");
                OnCardDrawn?.Invoke(card);
            }
            
            // Notify deck count change
            OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }

        private void ReshuffleDiscardToDraw()
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDrawPile();
            
            // Notify deck count change
            OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }

        public void DiscardHand()
        {
            foreach (var card in hand)
            {
                discardPile.Add(card);
                OnCardDiscarded?.Invoke(card);
            }
            hand.Clear();
            
            // Notify deck count change
            OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }

        public void PlayCard(CardData card)
        {
            if (hand.Contains(card))
            {
                hand.Remove(card);
                discardPile.Add(card);
                OnCardDiscarded?.Invoke(card); // Or specific OnCardPlayed event
                
                // Notify deck count change
                OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
            }
        }
        
        public void AddCardToDiscard(CardData newCard)
        {
             discardPile.Add(newCard);
             
             // Notify deck count change
             OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }

        public void AddCardToHand(CardData card)
        {
            hand.Add(card);
            Debug.Log($"[DeckManager] Card added to hand: {card.cardName}");
            OnCardDrawn?.Invoke(card);
        }

        public void AddCardToTopOfDraw(CardData card)
        {
            drawPile.Insert(0, card);
            Debug.Log($"[DeckManager] Card added to top of draw pile: {card.cardName}");
            
            // Notify deck count change
            OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }
        
        /// <summary>
        /// カードを山札の真ん中に追加（切り上げ）
        /// </summary>
        public void AddCardToMiddleOfDraw(CardData card)
        {
            // 真ん中の位置を計算（切り上げ）
            // 0枚: 0, 1枚: 1, 2枚: 1, 3枚: 2, 4枚: 2, ...
            int middleIndex = Mathf.CeilToInt(drawPile.Count / 2f);
            drawPile.Insert(middleIndex, card);
            Debug.Log($"[DeckManager] Card added to middle of draw pile (index {middleIndex}): {card.cardName}");
            
            // Notify deck count change
            OnDeckCountChanged?.Invoke(drawPile.Count, discardPile.Count);
        }

        public void ExhaustCard(CardData card)
        {
            if (hand.Contains(card))
            {
                hand.Remove(card);
                Debug.Log($"[DeckManager] Card exhausted (removed from game): {card.cardName}");
                // Trigger event so UI updates
                OnCardDiscarded?.Invoke(card);
                // Card is not added to discard pile - it's removed from the game
            }
        }

        /// <summary>
        /// 手札内の特定カードの枚数をカウント（自分自身を除外可能）
        /// </summary>
        public int CountCardInHand(CardData targetCard, CardData excludeSelf = null)
        {
            int count = 0;
            foreach (var c in hand)
            {
                if (c == targetCard && c != excludeSelf)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 手札から特定カードをn枚除外（Exhaust）
        /// </summary>
        public int ExhaustCardsOfType(CardData targetCard, int count, CardData excludeSelf = null)
        {
            int exhausted = 0;
            for (int i = hand.Count - 1; i >= 0 && exhausted < count; i--)
            {
                if (hand[i] == targetCard && hand[i] != excludeSelf)
                {
                    CardData card = hand[i];
                    hand.RemoveAt(i);
                    Debug.Log($"[DeckManager] Card exhausted by effect: {card.cardName}");
                    OnCardDiscarded?.Invoke(card);
                    exhausted++;
                }
            }
            return exhausted;
        }
        
        /// <summary>
        /// 山札内の特定カードの枚数をカウント
        /// </summary>
        public int CountCardInDrawPile(CardData targetCard)
        {
            return drawPile.Count(c => c == targetCard);
        }
        
        /// <summary>
        /// 捨て札内の特定カードの枚数をカウント
        /// </summary>
        public int CountCardInDiscardPile(CardData targetCard)
        {
            return discardPile.Count(c => c == targetCard);
        }
    }
}
