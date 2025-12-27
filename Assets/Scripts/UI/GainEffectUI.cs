using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// リソース獲得時の演出（浮き上がって消える）を制御するクラス
    /// 事前にシーンに配置し、使い回す想定
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GainEffectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private TextMeshProUGUI subText; // Optional: For rate or secondary info
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float moveDistance = 50f;
        [SerializeField] private float duration = 1.5f;

        private Vector2 originalPosition;
        private bool isInitialized = false;

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            // 初期位置を保存
            originalPosition = rectTransform.anchoredPosition;
            isInitialized = true;
            
            // 初期状態は非表示
            gameObject.SetActive(false);
        }



        /// <summary>
        /// 演出を再生する
        /// </summary>
        /// <param name="text">メインテキスト（獲得数）</param>
        /// <param name="subMessage">サブテキスト（率など） - オプション</param>
        public void PlayEffect(string text, string subMessage = null)
        {
            if (!isInitialized && rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
                isInitialized = true;
            }

            // アクティブ化
            gameObject.SetActive(true);

            // 状態リセット
            rectTransform.DOKill();
            canvasGroup.DOKill();
            
            rectTransform.anchoredPosition = originalPosition;
            canvasGroup.alpha = 1f;

            // テキスト設定（色はInspectorで設定）
            if (amountText != null)
            {
                amountText.text = text;
            }

            // サブテキスト設定（あれば）
            if (subText != null)
            {
                if (!string.IsNullOrEmpty(subMessage))
                {
                    subText.gameObject.SetActive(true);
                    subText.text = subMessage;
                }
                else
                {
                    subText.gameObject.SetActive(false);
                }
            }

            // アニメーション実行
            // 1. 上に移動
            rectTransform.DOAnchorPosY(originalPosition.y + moveDistance, duration)
                .SetEase(Ease.OutQuad);

            // 2. フェードアウト（後半で消える）
            canvasGroup.DOFade(0f, duration * 0.5f)
                .SetDelay(duration * 0.5f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // 完了したら非表示
                    gameObject.SetActive(false);
                });
        }
    }
}
