using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ApprovalMonster.Core;
using ApprovalMonster.Data;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ドラフトランクテーブルの1行を表すコンポーネント
    /// </summary>
    public class DraftRankRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankNameText;
        [SerializeField] private TextMeshProUGUI requiredImpText;
        [SerializeField] private TextMeshProUGUI commonText;
        [SerializeField] private TextMeshProUGUI rareText;
        [SerializeField] private TextMeshProUGUI epicText;
        
        [Header("Colors")]
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color rareColor = new Color(0f, 0.75f, 1f); // 水色
        [SerializeField] private Color epicColor = new Color(1f, 0.84f, 0f); // 金色
        
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        /// <summary>
        /// 行のデータを設定
        /// </summary>
        /// <param name="rankName">ランク名（例: "Rank 1", "MAX"）</param>
        /// <param name="requiredImpressions">必要インプレッション</param>
        /// <param name="commonPercent">Common排出率</param>
        /// <param name="rarePercent">Rare排出率</param>
        /// <param name="epicPercent">Epic排出率</param>
        public void SetData(string rankName, long requiredImpressions, int commonPercent, int rarePercent, int epicPercent)
        {
            if (rankNameText != null)
                rankNameText.text = rankName;
            
            if (requiredImpText != null)
                requiredImpText.text = FormatNumber(requiredImpressions);
            
            if (commonText != null)
            {
                commonText.text = commonPercent > 0 ? $"{commonPercent}" : "-";
                commonText.color = commonColor;
            }
            
            if (rareText != null)
            {
                rareText.text = rarePercent > 0 ? $"{rarePercent}" : "-";
                rareText.color = rareColor;
            }
            
            if (epicText != null)
            {
                epicText.text = epicPercent > 0 ? $"{epicPercent}" : "-";
                epicText.color = epicColor;
            }
            
            // 表示状態にする
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }
        
        /// <summary>
        /// 空行として設定（端の場合）
        /// </summary>
        public void SetEmpty()
        {
            if (rankNameText != null) rankNameText.text = "";
            if (requiredImpText != null) requiredImpText.text = "";
            if (commonText != null) commonText.text = "";
            if (rareText != null) rareText.text = "";
            if (epicText != null) epicText.text = "";
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.3f; // 薄く表示
            }
        }
        
        private string FormatNumber(long value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:0.#}M";
            if (value >= 1000)
                return $"{value / 1000f:0.#}K";
            return value.ToString();
        }
    }
}
