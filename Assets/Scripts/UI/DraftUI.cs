using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ApprovalMonster.Data;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ドラフトUIを管理するクラス
    /// カード候補を表示し、プレイヤーの選択を受け付ける
    /// </summary>
    public class DraftUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform cardOptionsContainer;
        [SerializeField] private CardView cardViewPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        private List<CardView> activeCardViews = new List<CardView>();
        private CardData selectedCard;

        private void Awake()
        {
            // 初期状態は非表示
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// ドラフト候補カードを表示
        /// </summary>
        public void ShowDraftOptions(List<CardData> options, bool isMonsterDraft = false)
        {
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("[DraftUI] No draft options provided!");
                return;
            }

            // モンスタードラフトならタイトル変更
            if (isMonsterDraft)
            {
                titleText.text = "モンスターの力を選べ";
                titleText.color = Color.red;
            }
            else
            {
                titleText.text = "カードを選択してください";
                titleText.color = Color.white;
            }
            
            if (cardViewPrefab == null)
            {
                Debug.LogError("[DraftUI] CardView Prefab is not assigned! Please assign it in the Inspector.");
                return;
            }




            // 既存のカードビューをクリア
            ClearCardViews();

            // UIを表示
            gameObject.SetActive(true);
            canvasGroup.DOFade(1f, fadeInDuration);

            // カードビューを生成
            foreach (var cardData in options)
            {
                var cardView = Instantiate(cardViewPrefab, cardOptionsContainer);
                cardView.Setup(cardData);
                
                // CardViewのクリックイベントを無効化（ドラフト中はプレイさせない）
                cardView.enabled = false;
                
                // クリックイベントを設定
                var button = cardView.GetComponent<Button>();
                if (button == null)
                {
                    button = cardView.gameObject.AddComponent<Button>();
                }
                
                // ラムダでキャプチャ
                var capturedCard = cardData;
                button.onClick.AddListener(() => OnCardSelected(capturedCard));
                
                activeCardViews.Add(cardView);

                // 出現アニメーション
                cardView.transform.localScale = Vector3.zero;
                cardView.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(0.1f * activeCardViews.Count);
            }

            Debug.Log($"[DraftUI] Showing {options.Count} draft options");
        }

        /// <summary>
        /// カードが選択された時の処理
        /// </summary>
        private void OnCardSelected(CardData card)
        {
            if (selectedCard != null)
            {
                // 既に選択済みの場合は無視
                return;
            }

            selectedCard = card;
            Debug.Log($"[DraftUI] Card selected: {card.cardName}");

            // 選択されたカード以外をフェードアウト
            foreach (var cardView in activeCardViews)
            {
                if (cardView.CardData != card)
                {
                    cardView.GetComponent<CanvasGroup>()?.DOFade(0.3f, 0.2f);
                }
                else
                {
                    // 選択されたカードを強調
                    cardView.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                }
            }

            // 少し待ってからUIを閉じる
            DOVirtual.DelayedCall(0.5f, () =>
            {
                HideDraftUI();
                
                // モンスタードラフトか通常ドラフトかで分岐
                if (titleText.text.Contains("モンスター"))
                {
                    Core.GameManager.Instance.OnMonsterDraftComplete(selectedCard);
                }
                else
                {
                    Core.GameManager.Instance.OnDraftComplete(selectedCard);
                }
            });
        }


        /// <summary>
        /// ドラフトUIを非表示
        /// </summary>
        public void HideDraftUI()
        {
            canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                ClearCardViews();
                selectedCard = null;
            });
        }

        /// <summary>
        /// カードビューをクリア
        /// </summary>
        private void ClearCardViews()
        {
            foreach (var cardView in activeCardViews)
            {
                if (cardView != null)
                {
                    Destroy(cardView.gameObject);
                }
            }
            activeCardViews.Clear();
        }
    }
}
