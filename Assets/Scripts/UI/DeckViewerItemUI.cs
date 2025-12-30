using UnityEngine;
using TMPro;
using ApprovalMonster.Data;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// デッキビューアーの1アイテム（2行表示）
    /// </summary>
    public class DeckViewerItemUI : MonoBehaviour
    {
        [Header("Line 1: Card Name + Count")]
        [SerializeField] private TextMeshProUGUI line1Text;
        
        [Header("Line 2: Costs and Effects")]
        [SerializeField] private TextMeshProUGUI line2Text;
        
        [Header("Line 3: Description")]
        [SerializeField] private TextMeshProUGUI line3Text;
        
        [Header("Ownership Indicator")]
        [SerializeField] private UnityEngine.UI.Image ownershipIcon;
        [SerializeField] private Color ownedColor = Color.green;
        [SerializeField] private Color unownedColor = Color.gray;
        
        [Header("Rarity Colors")]
        [SerializeField] private Color basicColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color rareColor = new Color(0f, 0.75f, 1f);
        [SerializeField] private Color epicColor = new Color(1f, 0.84f, 0f);
        
        /// <summary>
        /// カード情報をセットアップ（通常版：山札・捨て札用）
        /// </summary>
        public void Setup(CardData card, int count)
        {
            Setup(card, count, showRarity: false, isOwned: true);
        }
        
        /// <summary>
        /// カード情報をセットアップ（拡張版：カードプール用）
        /// </summary>
        public void Setup(CardData card, int count, bool showRarity, bool isOwned)
        {
            if (card == null) return;
            
            // Line 1: [R]【カード名】×3枚 (レアリティタグはオプション)
            string rarityTag = showRarity ? GetRarityTag(card.rarity) + " " : "";
            string countStr = count > 1 ? $"×{count}枚" : "";
            if (line1Text != null)
            {
                line1Text.text = $"{rarityTag}【{card.cardName}】{countStr}";
                // レアリティ色を適用
                if (showRarity)
                {
                    line1Text.color = GetRarityColor(card.rarity);
                }
            }
            
            // 入手・未入手アイコンの設定
            if (ownershipIcon != null)
            {
                ownershipIcon.gameObject.SetActive(true);
                ownershipIcon.color = isOwned ? ownedColor : unownedColor;
            }
            
            // Line 2: モチベ:x メンタル:x フォロワー:+xxx インプ率:+xxx
            string motivCost = card.motivationCost > 0 ? $"モチベ:{card.motivationCost}" : "";
            string mentalCost = card.mentalCost > 0 ? $"メンタル:{card.mentalCost}" : "";
            string follower = card.followerGain != 0 ? $"フォロワー:{FormatNumber(card.followerGain)}" : "";
            string impRate = card.impressionRate > 0 ? $"インプ率:+{card.impressionRate * 100:F0}%" : "";
            
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(motivCost)) parts.Add(motivCost);
            if (!string.IsNullOrEmpty(mentalCost)) parts.Add(mentalCost);
            if (!string.IsNullOrEmpty(follower)) parts.Add(follower);
            if (!string.IsNullOrEmpty(impRate)) parts.Add(impRate);
            
            if (line2Text != null)
            {
                line2Text.text = parts.Count > 0 ? "  " + string.Join(" ", parts) : "";
            }
            
            // Line 3: Description (改行をスペースに置換)
            if (line3Text != null)
            {
                string desc = card.description ?? "";
                desc = desc.Replace("\n", " ").Replace("\r", "");
                line3Text.text = !string.IsNullOrEmpty(desc) ? $"  {desc}" : "";
            }
        }
        
        private string GetRarityTag(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Basic: return "[B]";
                case CardRarity.Common: return "[C]";
                case CardRarity.Rare: return "[R]";
                case CardRarity.Epic: return "[E]";
                default: return "[?]";
            }
        }
        
        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Basic: return basicColor;
                case CardRarity.Rare: return rareColor;
                case CardRarity.Epic: return epicColor;
                case CardRarity.Common:
                default: return commonColor;
            }
        }
        
        private string FormatNumber(long value)
        {
            if (value >= 0)
            {
                return $"+{value}";
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
