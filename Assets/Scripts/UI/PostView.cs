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
            transform.localScale = Vector3.one * 0.5f;
            
            // Get height from RectTransform layout calculation (wait for one frame or force update)
            // For simplicity, we assume a reasonable height or measure it
            // LayoutElement height animation
            layoutElement.minHeight = 0f;
            layoutElement.preferredHeight = 0f;
            
            // Note: In a vertical layout group, we want to animate the preferred height specifically
            // Ideally we need to know the target height. 
            // A simple trick is to let layout calculate first, then animate.
            // But to avoid "pop", we will animate scale and alpha primarily, and height secondarily if possible.
            // Since User reported issues with LayoutGroup, let's keep it robust:
            // Animate Scale & Alpha is safe for LayoutGroups usually.
            
            Sequence seq = DOTween.Sequence();
            
            // 1. Expand (Layout workaround)
            // First we set preferred height to a target value (approximate or measured)
            // However, a safer approach in Unity UI is to animate Scale from 0 to 1, 
            // but LayoutGroup only respects Scale if valid.
            
            // Simplified Pop Animation
            transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).From(Vector3.zero);
            canvasGroup.DOFade(1f, 0.3f);
            
            // Optional: Shake effect on appear
            transform.DOPunchRotation(new Vector3(0, 0, 5f), 0.5f);
        }
    }
}
