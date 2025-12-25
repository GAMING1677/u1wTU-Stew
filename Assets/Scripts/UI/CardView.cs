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
        [SerializeField] private TextMeshProUGUI mentalCostText;
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
            flavorText.text = data.flavorText;

            if (data.cardImage != null)
            {
                cardImage.sprite = data.cardImage;
            }

            // Positive mental cost means damage (Red), Negative is heal (Green)
            if (data.mentalCost > 0)
                mentalCostText.text = $"-{data.mentalCost} Men";
            else if (data.mentalCost < 0)
                mentalCostText.text = $"+{-data.mentalCost} Men";
            else
                mentalCostText.text = "";

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
