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
    /// ドラフトランクテーブルUI
    /// 現在のランクを中心に上下1行ずつ表示し、スクロールアニメーションで切り替える
    /// シングルトンパターン - 複数インスタンス対応
    /// </summary>
    public class DraftRankTableUI : MonoBehaviour
    {
        public static DraftRankTableUI Instance { get; private set; }
        
        [Header("Container References")]
        [Tooltip("スクロールするコンテンツのコンテナ（全ランク行を含む）")]
        [SerializeField] private RectTransform contentContainer;
        
        [Tooltip("行プレハブ")]
        [SerializeField] private DraftRankRowUI rowPrefab;
        
        [Header("Layout Settings")]
        [Tooltip("1行の高さ")]
        [SerializeField] private float rowHeight = 40f;
        
        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        
        [Header("Remaining Count")]
        [SerializeField] private TextMeshProUGUI remainingCommonText;
        [SerializeField] private TextMeshProUGUI remainingRareText;
        [SerializeField] private TextMeshProUGUI remainingEpicText;
        
        [Header("Next Rank Info")]
        [SerializeField] private TextMeshProUGUI nextRankImpText;
        
        [Header("Animation")]
        [SerializeField] private float scrollDuration = 0.3f;
        [SerializeField] private Ease scrollEase = Ease.OutCubic;
        
        private List<DraftRankRowUI> _rows = new List<DraftRankRowUI>();
        private int _currentRankIndex = 0;
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            // シングルトンパターン
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"[DraftRankTableUI] Instance set to: {gameObject.name}");
            }
            else if (Instance != this)
            {
                Debug.Log($"[DraftRankTableUI] Duplicate found, disabling: {gameObject.name}");
                enabled = false;
                return;
            }
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 閉じるボタンのイベント設定
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
        }
        
        private void Start()
        {
            // シングルトンインスタンスでない場合は何もしない
            if (Instance != this) return;
            
            // ゲーム開始時は非表示にする
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// テーブルを表示し、行を生成
        /// </summary>
        public void Show(List<CardData> draftPool)
        {
            gameObject.SetActive(true);
            
            var gm = GameManager.Instance;
            if (gm == null || gm.draftManager == null)
            {
                Debug.LogWarning("[DraftRankTableUI] GameManager or DraftManager is null");
                return;
            }
            
            var probTable = gm.draftManager.probabilityTable;
            if (probTable == null || probTable.Count == 0)
            {
                Debug.LogWarning("[DraftRankTableUI] Probability table is empty");
                Hide();
                return;
            }
            
            // 行を生成（まだ生成されていない場合）
            BuildRows(probTable);
            
            // 現在のランクを取得
            long currentImpressions = gm.resourceManager?.totalImpressions ?? 0;
            _currentRankIndex = gm.draftManager.GetCurrentRankIndex(currentImpressions);
            
            // スクロール位置を設定（アニメーションなし）
            ScrollToRank(_currentRankIndex, animate: false);
            
            // 残数を更新
            UpdateRemainingCounts(draftPool);
            
            // 次のランクまでの残りインプを更新
            UpdateNextRankInfo(currentImpressions, probTable);
            
            // フェードイン
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, 0.2f);
        }
        
        /// <summary>
        /// テーブルを非表示
        /// </summary>
        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, 0.15f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 確率テーブルに基づいて行を生成
        /// </summary>
        private void BuildRows(List<DraftTierProbability> probTable)
        {
            // 既存の行をクリア
            foreach (var row in _rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _rows.Clear();
            
            if (rowPrefab == null || contentContainer == null)
            {
                Debug.LogError("[DraftRankTableUI] Row prefab or content container is null");
                return;
            }
            
            // 上端の空行を追加
            var topEmpty = Instantiate(rowPrefab, contentContainer);
            topEmpty.SetEmpty();
            _rows.Add(topEmpty);
            
            // 各ランクの行を生成
            for (int i = 0; i < probTable.Count; i++)
            {
                var prob = probTable[i];
                var row = Instantiate(rowPrefab, contentContainer);
                
                // ランク名（最後はMAX）
                string rankName = (i == probTable.Count - 1) ? "MAX" : $"Rank {i + 1}";
                
                // 総重みからパーセンテージを計算
                int total = prob.GetTotalWeight();
                int commonPercent = total > 0 ? Mathf.RoundToInt((float)prob.commonWeight / total * 100) : 0;
                int rarePercent = total > 0 ? Mathf.RoundToInt((float)prob.rareWeight / total * 100) : 0;
                int epicPercent = total > 0 ? Mathf.RoundToInt((float)prob.epicWeight / total * 100) : 0;
                
                row.SetData(rankName, prob.minImpressions, commonPercent, rarePercent, epicPercent);
                _rows.Add(row);
            }
            
            // 下端の空行を追加
            var bottomEmpty = Instantiate(rowPrefab, contentContainer);
            bottomEmpty.SetEmpty();
            _rows.Add(bottomEmpty);
            
            // コンテナの高さを調整
            float totalHeight = _rows.Count * rowHeight;
            contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, totalHeight);
            
            Debug.Log($"[DraftRankTableUI] Built {_rows.Count} rows (including 2 empty rows)");
        }
        
        /// <summary>
        /// 指定ランクにスクロール
        /// </summary>
        /// <param name="rankIndex">0-indexed ランク番号</param>
        /// <param name="animate">アニメーションするか</param>
        public void ScrollToRank(int rankIndex, bool animate = true)
        {
            // rankIndex 0 は _rows[1] に対応（_rows[0] は上端の空行）
            // 中央に表示したいので、オフセットを計算
            // 表示領域は3行分 = rowHeight * 3
            // 中央行が見える位置 = (rankIndex + 1) * rowHeight - rowHeight
            
            float targetY = rankIndex * rowHeight;
            
            contentContainer.DOKill();
            
            if (animate)
            {
                contentContainer.DOAnchorPosY(targetY, scrollDuration).SetEase(scrollEase);
            }
            else
            {
                contentContainer.anchoredPosition = new Vector2(contentContainer.anchoredPosition.x, targetY);
            }
        }
        
        /// <summary>
        /// 残数を更新
        /// </summary>
        public void UpdateRemainingCounts(List<CardData> pool)
        {
            if (pool == null) return;
            
            var gm = GameManager.Instance;
            if (gm == null || gm.draftManager == null) return;
            
            // 選択済みカードを除外した残りを取得
            var remaining = gm.draftManager.GetRemainingCardCounts(pool);
            
            if (remainingCommonText != null)
                remainingCommonText.text = remaining.common.ToString();
            
            if (remainingRareText != null)
                remainingRareText.text = remaining.rare.ToString();
            
            if (remainingEpicText != null)
                remainingEpicText.text = remaining.epic.ToString();
        }
        
        /// <summary>
        /// 次のランクまでの残りインプレッションを更新
        /// </summary>
        private void UpdateNextRankInfo(long currentImpressions, List<DraftTierProbability> probTable)
        {
            if (nextRankImpText == null) return;
            
            // 現在のランクインデックス
            int currentRank = _currentRankIndex;
            
            // 最高ランクなら「MAX」表示
            if (currentRank >= probTable.Count - 1)
            {
                nextRankImpText.text = "MAX";
                return;
            }
            
            // 次のランクに必要なインプレッション
            long nextRankThreshold = probTable[currentRank + 1].minImpressions;
            long remaining = nextRankThreshold - currentImpressions;
            
            if (remaining <= 0)
            {
                nextRankImpText.text = "ランクアップ可能";
            }
            else
            {
                nextRankImpText.text = $"次まで: {FormatNumber(remaining)}";
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
