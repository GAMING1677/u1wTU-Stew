using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(LayoutElement))]
    public class PostView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI contentText;
        [Header("Icon")]
        [SerializeField] private Image iconImage;
        [Header("Metrics")]
        [SerializeField] private TextMeshProUGUI likesText;
        [SerializeField] private TextMeshProUGUI rtText;
        [SerializeField] private TextMeshProUGUI repliesText;
        [SerializeField] private TextMeshProUGUI impressionsText;
        
        private CanvasGroup canvasGroup;
        private LayoutElement layoutElement;
        private float targetHeight;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            layoutElement = GetComponent<LayoutElement>();
        }

        public void SetContent(string text, long impressionCount, Sprite icon = null)
        {
            if (contentText != null)
            {
                contentText.text = text;
            }
            
            // アイコン設定（指定があれば変更＆表示）
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                // アルファ値を1に設定して表示
                Color c = iconImage.color;
                c.a = 1f;
                iconImage.color = c;
            }
            
            // Show Base Impressions
            if (impressionsText != null) impressionsText.text = impressionCount.ToString("N0");
            
            // Calculate Metrics
            // Likes: 20-50%
            int likes = Mathf.CeilToInt(impressionCount * Random.Range(0.2f, 0.5f));
            if (likesText != null) likesText.text = likes.ToString("N0");
            
            // RT: 5-60%
            int rt = Mathf.CeilToInt(impressionCount * Random.Range(0.05f, 0.6f));
            if (rtText != null) rtText.text = rt.ToString("N0");
            
            // Replies: 1-20%
            int replies = Mathf.CeilToInt(impressionCount * Random.Range(0.01f, 0.2f));
            if (repliesText != null) repliesText.text = replies.ToString("N0");
            
            // Start Animation
            AnimateEntry();
        }
        
        private void AnimateEntry()
        {
            // Initial State
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one;
            
            // RectTransformから目標の高さを取得
            RectTransform rectTransform = GetComponent<RectTransform>();
            float targetHeight = rectTransform.sizeDelta.y;
            
            // 高さが0以下の場合はデフォルト値を使用（Content Size Fitter使用時など）
            if (targetHeight <= 0)
            {
                // レイアウトを強制更新して高さを計算
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                targetHeight = LayoutUtility.GetPreferredHeight(rectTransform);
            }
            
            // それでも0なら固定値を使用
            if (targetHeight <= 0)
            {
                targetHeight = 120f;
            }
            
            // LayoutElementで高さを0からスタート
            layoutElement.preferredHeight = 0f;
            layoutElement.minHeight = 0f;
            
            // アニメーションシーケンス
            Sequence seq = DOTween.Sequence();
            
            // 1. 高さを広げる（他のアイテムが押し下がる）
            seq.Append(DOTween.To(
                () => layoutElement.preferredHeight,
                x => layoutElement.preferredHeight = x,
                targetHeight,
                0.3f
            ).SetEase(Ease.OutQuad));
            
            // 2. フェードインとスケールを同時に
            seq.Join(canvasGroup.DOFade(1f, 0.3f));
            seq.Join(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).From(0.8f));
            
            // 3. 軽いパンチ効果
            seq.Append(transform.DOPunchRotation(new Vector3(0, 0, 3f), 0.3f));
        }
    }
}
