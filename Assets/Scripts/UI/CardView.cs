using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Data;
using ApprovalMonster.Core;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
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
        [SerializeField] private TextMeshProUGUI tagText; // Tag or Rarity display
        [SerializeField] private Image riskIcon;
        
        [Header("Selection Animation")]
        [SerializeField] private float hoverSlideDistance = 100f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverDuration = 0.2f;
        
        [Header("Idle Shadow Animation")]
        [Tooltip("待機中の影アニメーションを有効にするか")]
        [SerializeField] private bool enableIdleShadow = true;
        [Tooltip("影用のImage（カードの子オブジェクト）")]
        [SerializeField] private Image shadowImage;
        [Tooltip("影のスケール変化量")]
        [SerializeField] private float shadowPulseScale = 1.08f;
        [Tooltip("影の色変化（ハイライト時）")]
        [SerializeField] private Color shadowPulseColor = new Color(0, 0, 0, 0.6f);
        [Tooltip("影アニメーションの周期（秒）")]
        [SerializeField] private float shadowPulseDuration = 1.5f;
        
        [Header("Rarity Colors")]
        [SerializeField] private Color basicColor = new Color(0.6f, 0.6f, 0.6f); // 灰色
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color rareColor = new Color(0f, 0.75f, 1f); // 水色
        [SerializeField] private Color epicColor = new Color(1f, 0.84f, 0f); // 金色

        private CardData _data;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private Vector2 _originalPosition;
        
        private bool _isSelected = false;
        private static CardView _currentlySelectedCard = null;
        private Tween _pulseTween;
        private Vector3 _originalShadowScale;
        private Color _originalShadowColor;

        private Canvas _canvas;
        private GraphicRaycaster _graphicRaycaster;
        private LayoutElement _layoutElement;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;
            _originalPosition = _rectTransform.anchoredPosition;
            
            // Get or add LayoutElement (to disable layout control when selected)
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            // Setup Canvas for sorting override
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            
            _graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (_graphicRaycaster == null) _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            
            // 影の元のスケールと色を保存
            if (shadowImage != null)
            {
                _originalShadowScale = shadowImage.transform.localScale;
                _originalShadowColor = shadowImage.color;
            }
        }

        /// <summary>
        /// Set up card with data
        /// </summary>
        /// <param name="data">Card data to display</param>
        /// <param name="showTag">Whether to show tag/rarity (for draft UI only)</param>
        public void Setup(CardData data, bool showTag = false)
        {
            Debug.Log($"[CardView] Setup() START - Card: {data?.cardName ?? "NULL"}, FlavorText: '{data?.flavorText ?? "NULL"}', Description: '{data?.description ?? "NULL"}'");
            _data = data;

            if (cardImage != null)
                cardImage.sprite = data.cardImage;
            if (nameText != null)
                nameText.text = data.cardName;
            if (costText != null)
                costText.text = data.motivationCost.ToString();

            // Display flavor text
            if (flavorText != null && !string.IsNullOrEmpty(data.flavorText))
            {
                flavorText.text = data.flavorText;
                Debug.Log($"[CardView] Flavor text set: {data.flavorText}");
            }
            else
            {
                Debug.LogWarning($"[CardView] Flavor text is null or empty for {data.cardName}");
            }

            // Display description
            if (descriptionText != null && !string.IsNullOrEmpty(data.description))
            {
                descriptionText.text = data.description;
                Debug.Log($"[CardView] Description set: {data.description}");
            }
            else
            {
                Debug.LogWarning($"[CardView] Description is null or empty for {data.cardName}");
            }

            // Mental Cost - display number only
            if (mentalCostText != null)
            {
                mentalCostText.text = data.mentalCost.ToString();
            }

            // Follower Gain - K/M notation
            if (followerGainText != null)
            {
                followerGainText.text = FormatNumber(data.followerGain);
            }

            // Impression Rate - percentage (hide if 0)
            if (impressionRateText != null)
            {
                if (data.impressionRate > 0)
                {
                    float percentage = data.impressionRate * 100f;
                    impressionRateText.text = $"{percentage:F0}%";
                    impressionRateText.gameObject.SetActive(true);
                }
                else
                {
                    impressionRateText.gameObject.SetActive(false);
                }
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
            
            // Display tag or rarity (draft only)
            if (tagText != null)
            {
                if (showTag)
                {
                    tagText.gameObject.SetActive(true);
                    if (!string.IsNullOrEmpty(data.cardTag))
                    {
                        // Monster card: show custom tag
                        tagText.text = data.cardTag;
                    }
                    else
                    {
                        // Regular card: show rarity with color
                        tagText.text = data.rarity.ToString();
                        tagText.color = GetRarityColor(data.rarity);
                    }
                }
                else
                {
                    // Hide tag in normal card display
                    tagText.gameObject.SetActive(false);
                }
            }

            // Ensure RectTransform is initialized
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            // Store original position after setup
            _originalPosition = _rectTransform.anchoredPosition;
            
            // 待機中のパルスアニメーション開始
            StartPulse();
        }
        
        /// <summary>
        /// Format number with K/M notation
        /// 1000+ = K, 1000000+ = M
        /// Supports negative values (e.g., -1.5K, -2.3M)
        /// </summary>
        private string FormatNumber(int value)
        {
            if (value == 0)
                return "";
            
            // Handle negative values
            bool isNegative = value < 0;
            int absValue = Mathf.Abs(value);
            string sign = isNegative ? "-" : "";
            
            if (absValue >= 1000000)
            {
                float millions = absValue / 1000000f;
                return $"{sign}{millions:F1}M";
            }
            else if (absValue >= 1000)
            {
                float thousands = absValue / 1000f;
                return $"{sign}{thousands:F1}K";
            }
            else
            {
                return value.ToString(); // Includes sign naturally
            }
        }

        public string CardName => nameText.text;
        public CardData CardData => _data;
        public bool IsSelected => _isSelected;
        
        /// <summary>
        /// Update the original position (called by UIManager after layout changes)
        /// </summary>
        public void UpdateOriginalPosition()
        {
            _originalPosition = _rectTransform.anchoredPosition;
        }

        /// <summary>
        /// Two-step click interaction:
        /// 1st click: Select and enlarge card
        /// 2nd click: Confirm and play card
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[CardView] OnPointerClick called for {_data.cardName}, _isSelected: {_isSelected}");
            
            if (_isSelected)
            {
                // 2nd click: Play card
                Debug.Log($"[CardView] Playing card: {_data.cardName}");
                GameManager.Instance.TryPlayCard(_data);
                Deselect();
            }
            else
            {
                // 1st click: Select card
                Debug.Log($"[CardView] Selected card: {_data.cardName}");
                Select();
            }
        }

        /// <summary>
        /// Select this card (enlarge and bring to front)
        /// </summary>
        private void Select()
        {
            Debug.Log($"[CardView] Select() called. Current position: {_rectTransform.anchoredPosition}, Original: {_originalPosition}");
            
            // Deselect previously selected card
            if (_currentlySelectedCard != null && _currentlySelectedCard != this)
            {
                Debug.Log($"[CardView] Deselecting previous card: {_currentlySelectedCard._data.cardName}");
                _currentlySelectedCard.Deselect();
            }
            
            _isSelected = true;
            _currentlySelectedCard = this;
            
            // 選択時はパルスを停止
            StopPulse();
            
            // Disable layout control to prevent position override
            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = true;
                Debug.Log($"[CardView] LayoutElement.ignoreLayout set to TRUE");
            }
            
            float targetY = _originalPosition.y + hoverSlideDistance;
            Debug.Log($"[CardView] Animating to Y: {targetY} (original: {_originalPosition.y}, slide: {hoverSlideDistance})");
            Debug.Log($"[CardView] RectTransform null? {_rectTransform == null}");
            Debug.Log($"[CardView] Current localPosition: {transform.localPosition}");
            
            // Slide up and scale up
            // Use DOLocalMoveY instead of DOAnchorPosY to bypass anchor constraints
            float currentLocalY = transform.localPosition.y;
            float targetLocalY = currentLocalY + hoverSlideDistance;
            
            transform.DOKill();
            var tween = transform.DOLocalMoveY(targetLocalY, hoverDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            
            Debug.Log($"[CardView] Tween created: {tween != null}, Moving from {currentLocalY} to {targetLocalY}");
            
            transform.DOScale(_originalScale * hoverScale, hoverDuration);
            
            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 100;
                Debug.Log($"[CardView] Canvas sorting set to 100");
            }
            else
            {
                Debug.LogWarning($"[CardView] Canvas is null!");
            }
        }

        /// <summary>
        /// Deselect this card (return to original position and scale)
        /// </summary>
        public void Deselect()
        {
            Debug.Log($"[CardView] Deselect() called for {_data.cardName}");
            
            _isSelected = false;
            
            if (_currentlySelectedCard == this)
            {
                _currentlySelectedCard = null;
            }
            
            // Re-enable layout control
            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = false;
                Debug.Log($"[CardView] LayoutElement.ignoreLayout set to FALSE");
            }
            
            // Return to original position using localPosition
            transform.DOKill();
            
            // Calculate original local Y from stored anchored position
            float originalLocalY = transform.localPosition.y - hoverSlideDistance;
            transform.DOLocalMoveY(originalLocalY, hoverDuration);
            
            // Scale back to original
            transform.DOScale(_originalScale, hoverDuration);
            
            if (_canvas != null)
            {
                _canvas.overrideSorting = false;
                _canvas.sortingOrder = 0;
            }
            
            // 選択解除後にパルス再開
            StartPulse();
        }
        
        /// <summary>
        /// 待機中の影アニメーションを開始
        /// </summary>
        public void StartPulse()
        {
            if (!enableIdleShadow || _isSelected || shadowImage == null) return;
            
            StopPulse();
            
            // 影のスケールと色をアニメーション
            var sequence = DOTween.Sequence();
            
            // スケールを大きくしながら色も変化
            sequence.Append(shadowImage.transform
                .DOScale(shadowPulseScale, shadowPulseDuration / 2f)
                .SetEase(Ease.InOutSine));
            sequence.Join(shadowImage
                .DOColor(shadowPulseColor, shadowPulseDuration / 2f)
                .SetEase(Ease.InOutSine));
            
            sequence.SetLoops(-1, LoopType.Yoyo);
            _pulseTween = sequence;
        }
        
        /// <summary>
        /// 影アニメーションを停止
        /// </summary>
        public void StopPulse()
        {
            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }
            // 影を元に戻す
            if (shadowImage != null)
            {
                shadowImage.transform.localScale = _originalShadowScale;
                shadowImage.color = _originalShadowColor;
            }
        }
        
        private void OnDestroy()
        {
            // パルスを停止
            StopPulse();
            
            // Clean up if this was the selected card
            if (_currentlySelectedCard == this)
            {
                _currentlySelectedCard = null;
            }
            
            transform.DOKill();
        }
        
        /// <summary>
        /// レアリティに応じた色を取得
        /// </summary>
        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Basic:
                    return basicColor;
                case CardRarity.Rare:
                    return rareColor;
                case CardRarity.Epic:
                    return epicColor;
                case CardRarity.Common:
                default:
                    return commonColor;
            }
        }
    }
}
