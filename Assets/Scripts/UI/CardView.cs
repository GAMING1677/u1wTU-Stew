using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Data;
using ApprovalMonster.Core;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image cardImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI flavorText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI mentalCostText;
        [SerializeField] private TextMeshProUGUI followerGainText;
        [SerializeField] private TextMeshProUGUI impressionRateText;
        [SerializeField] private Image riskIcon;
        
        [Header("Animation")]
        [SerializeField] private float hoverSlideDistance = 100f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverDuration = 0.2f;

        private CardData _data;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private Vector2 _originalPosition;

        private Canvas _canvas;
        private GraphicRaycaster _graphicRaycaster;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            _originalPosition = _rectTransform.anchoredPosition;

            // Setup Canvas for sorting override
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            
            _graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (_graphicRaycaster == null) _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        public void Setup(CardData data)
        {
            _data = data;
            nameText.text = data.cardName;
            costText.text = data.motivationCost.ToString();
            
            // Flavor text
            if (flavorText != null)
            {
                flavorText.text = data.flavorText;
                Debug.Log($"[CardView] Set flavorText for {data.cardName}: '{data.flavorText}'");
            }
            else
            {
                Debug.LogWarning($"[CardView] flavorText is null for {data.cardName}");
            }
            
            // Description text (new)
            if (descriptionText != null)
            {
                descriptionText.text = data.description;
                Debug.Log($"[CardView] Set description for {data.cardName}: '{data.description}'");
            }
            else
            {
                Debug.LogWarning($"[CardView] descriptionText is null for {data.cardName}");
            }

            if (data.cardImage != null)
            {
                cardImage.sprite = data.cardImage;
            }

            // Mental cost: number only
            if (mentalCostText != null)
            {
                if (data.mentalCost > 0)
                    mentalCostText.text = data.mentalCost.ToString();
                else if (data.mentalCost < 0)
                    mentalCostText.text = $"+{-data.mentalCost}";
                else
                    mentalCostText.text = "";
            }
            
            // Follower gain with K/M formatting
            if (followerGainText != null)
            {
                followerGainText.text = FormatNumber(data.followerGain);
            }
            
            // Impression rate as percentage
            if (impressionRateText != null)
            {
                if (data.impressionRate != 0)
                    impressionRateText.text = $"{(data.impressionRate * 100):F0}%";
                else
                    impressionRateText.text = "";
            }

            if (data.HasRisk())
            {
                riskIcon.gameObject.SetActive(true);
                // Set risk sprite based on type if needed
            }
            else
            {
                riskIcon.gameObject.SetActive(false);
            }

            // Ensure RectTransform is initialized
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            // Store original position after setup
            _originalPosition = _rectTransform.anchoredPosition;
        }
        
        /// <summary>
        /// Format number with K/M notation
        /// 1000+ = K, 1000000+ = M
        /// </summary>
        private string FormatNumber(int value)
        {
            if (value == 0)
                return "";
            
            if (value >= 1000000)
            {
                float millions = value / 1000000f;
                return $"{millions:F1}M";
            }
            else if (value >= 1000)
            {
                float thousands = value / 1000f;
                return $"{thousands:F1}K";
            }
            else
            {
                return value.ToString();
            }
        }

        public string CardName => nameText.text;
        public CardData CardData => _data;
        
        /// <summary>
        /// Update the original position (called by UIManager after layout changes)
        /// </summary>
        public void UpdateOriginalPosition()
        {
            _originalPosition = _rectTransform.anchoredPosition;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnPointerClick called for {_data.cardName}");
            // Simple click to play for now
            GameManager.Instance.TryPlayCard(_data);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Slide up to reveal full card (using stored original position)
            _rectTransform.DOKill();
            _rectTransform.DOAnchorPosY(_originalPosition.y + hoverSlideDistance, hoverDuration);
            
            // Scale up (Do not call DOKill here again as it kills the position tween above)
            transform.DOScale(_originalScale * hoverScale, hoverDuration);
            
            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 100; // Bring to front visually
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Slide back to original position
            _rectTransform.DOKill();
            _rectTransform.DOAnchorPosY(_originalPosition.y, hoverDuration);
            
            // Scale back to original
            transform.DOScale(_originalScale, hoverDuration);
            
            if (_canvas != null)
            {
                _canvas.overrideSorting = false;
                _canvas.sortingOrder = 0;
            }
        }
    }
}
