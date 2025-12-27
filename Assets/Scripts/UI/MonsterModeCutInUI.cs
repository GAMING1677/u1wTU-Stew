using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// モンスターモード専用カットインUI
    /// ステージごとの画像・サイズ・アニメーションを管理
    /// </summary>
    public class MonsterModeCutInUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject targetObject; // MonsterCutIn GameObject
        
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image characterImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        
        [Header("Animation Settings")]
        [SerializeField] private float backgroundSlideInDuration = 0.5f;
        [SerializeField] private float characterSlideInDelay = 0.15f;
        [SerializeField] private float characterSlideInDuration = 0.4f;
        [SerializeField] private float inertiaDuration = 1.5f;
        [SerializeField] private float inertiaDistance = 20f;
        [SerializeField] private float slideOutDuration = 0.4f;
        [SerializeField] private float clickDelay = 0.5f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip showSound;
        [SerializeField] private AudioClip clickSound;
        
        private Action onClickCallback;
        private bool isShowing = false;
        private bool canClick = false;
        private float showTime;
        private string fullMessageText = ""; // Store message text for typewriter effect
        
        // Store original positions for reset
        private Vector2 originalBackgroundPosition;
        private Vector2 originalCharacterPosition;
        private bool positionsInitialized = false;
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            // Store original positions for reset on subsequent uses
            if (backgroundImage != null)
            {
                RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    originalBackgroundPosition = bgRect.anchoredPosition;
                    positionsInitialized = true;
                }
            }
            
            if (characterImage != null)
            {
                RectTransform charRect = characterImage.GetComponent<RectTransform>();
                if (charRect != null)
                {
                    originalCharacterPosition = charRect.anchoredPosition;
                }
            }
            
            // Setup click detection on target object
            SetupClickDetection();
            
            // Hide target object on start
            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// targetObjectに透明なImageとEventTriggerを追加してクリック検知を有効化
        /// </summary>
        private void SetupClickDetection()
        {
            if (targetObject == null)
            {
                Debug.LogWarning("[MonsterModeCutInUI] targetObject is null, cannot setup click detection");
                return;
            }
            
            // Add transparent Image for raycast target (if not already present)
            Image clickDetector = targetObject.GetComponent<Image>();
            if (clickDetector == null)
            {
                clickDetector = targetObject.AddComponent<Image>();
                clickDetector.color = new Color(0, 0, 0, 0); // Fully transparent
                clickDetector.raycastTarget = true;
                
                // Stretch to fill entire screen
                RectTransform rect = targetObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                }
            }
            
            // Add EventTrigger for click detection (if not already present)
            EventTrigger trigger = targetObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = targetObject.AddComponent<EventTrigger>();
            }
            
            // Add pointer click event
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnPointerClick((PointerEventData)data); });
            trigger.triggers.Add(entry);
            
            Debug.Log("[MonsterModeCutInUI] Click detection setup complete on targetObject");
        }
        
        /// <summary>
        /// IPointerClickHandler - 画面クリック時に呼ばれる
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isShowing || !canClick) return;
            
            // クリック遅延チェック
            if (Time.time - showTime < clickDelay) return;
            
            OnClick();
        }
        
        /// <summary>
        /// モンスターモードカットインを表示
        /// </summary>
        /// <param name="preset">モンスターモードプリセット（nullの場合はデフォルト設定）</param>
        /// <param name="onComplete">完了コールバック</param>
        public void Show(Data.MonsterModePreset preset = null, Action onComplete = null)
        {
            Debug.Log($"[MonsterModeCutInUI] Show() called. isShowing={isShowing}, preset null? {preset == null}");
            
            if (isShowing) 
            {
                Debug.LogWarning("[MonsterModeCutInUI] Already showing, returning");
                return;
            }
            
            isShowing = true;
            canClick = false;
            showTime = Time.time;
            onClickCallback = onComplete;
            
            Debug.Log("[MonsterModeCutInUI] Showing UI");
            
            // Activate target object
            if (targetObject != null)
            {
                targetObject.SetActive(true);
            }
            
            // Debug: Check GameObject state
            Debug.Log($"[MonsterModeCutInUI] targetObject null: {targetObject == null}");
            Debug.Log($"[MonsterModeCutInUI] targetObject.activeInHierarchy: {targetObject?.activeInHierarchy}");
            Debug.Log($"[MonsterModeCutInUI] canvasGroup null: {canvasGroup == null}, alpha: {canvasGroup?.alpha}");
            Debug.Log($"[MonsterModeCutInUI] backgroundImage null: {backgroundImage == null}");
            Debug.Log($"[MonsterModeCutInUI] characterImage null: {characterImage == null}");
            Debug.Log($"[MonsterModeCutInUI] titleText null: {titleText == null}");
            Debug.Log($"[MonsterModeCutInUI] messageText null: {messageText == null}");
            
            // Apply preset settings
            if (preset != null)
            {
                Debug.Log($"[MonsterModeCutInUI] Applying preset: {preset.name}");
                ApplyPreset(preset);
            }
            else
            {
                // Use default settings
                Debug.Log("[MonsterModeCutInUI] Using default settings (no preset)");
                if (titleText != null) titleText.text = "MONSTER MODE";
                if (messageText != null) messageText.text = "承認欲求が暴走を始めた...";
            }
            
            Debug.Log("[MonsterModeCutInUI] Starting slide-in animation");
            PlaySlideInAnimation();
        }
        
        /// <summary>
        /// プリセットを適用
        /// </summary>
        private void ApplyPreset(Data.MonsterModePreset preset)
        {
            // Apply images
            if (backgroundImage != null && preset.backgroundImage != null)
            {
                backgroundImage.sprite = preset.backgroundImage;
            }
            
            if (characterImage != null && preset.characterImage != null)
            {
                characterImage.sprite = preset.characterImage;
                
                // Apply character size
                RectTransform charRect = characterImage.GetComponent<RectTransform>();
                if (charRect != null)
                {
                    charRect.sizeDelta = preset.characterSize;
                }
            }
            
            // Apply text
            if (titleText != null)
            {
                titleText.text = preset.titleText;
            }
            
            if (messageText != null)
            {
                messageText.text = preset.messageText;
            }
            
            // Update sounds
            if (preset.showSound != null)
            {
                showSound = preset.showSound;
            }
            
            if (preset.clickSound != null)
            {
                clickSound = preset.clickSound;
            }
            
            // Play sound
            if (audioSource != null && showSound != null)
            {
                audioSource.PlayOneShot(showSound);
            }
        }
        
        private void PlaySlideInAnimation()
        {
            RectTransform bgRect = backgroundImage != null ? backgroundImage.GetComponent<RectTransform>() : null;
            RectTransform charRect = characterImage != null ? characterImage.GetComponent<RectTransform>() : null;
            
            // Reset to original positions before animation (fixes position drift on 2nd+ use)
            if (positionsInitialized)
            {
                if (bgRect != null) bgRect.anchoredPosition = originalBackgroundPosition;
                if (charRect != null) charRect.anchoredPosition = originalCharacterPosition;
            }
            
            // Use stored original positions for animation targets
            Vector2 bgOriginalPos = positionsInitialized ? originalBackgroundPosition : (bgRect != null ? bgRect.anchoredPosition : Vector2.zero);
            Vector2 charOriginalPos = positionsInitialized ? originalCharacterPosition : (charRect != null ? charRect.anchoredPosition : Vector2.zero);
            
            // Set initial off-screen positions (LEFT side)
            float screenWidth = Screen.width;
            if (bgRect != null) bgRect.anchoredPosition = new Vector2(-screenWidth, bgOriginalPos.y);
            if (charRect != null) charRect.anchoredPosition = new Vector2(-screenWidth, charOriginalPos.y);
            
            // Initial alpha
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            
            // Hide message text initially (but store it first)
            if (messageText != null)
            {
                fullMessageText = messageText.text;
                messageText.text = "";
            }
            
            // Animation sequence
            Sequence animSeq = DOTween.Sequence();
            
            // 1. Background slides in (from left to right)
            if (bgRect != null)
            {
                animSeq.Append(bgRect.DOAnchorPos(bgOriginalPos, backgroundSlideInDuration).SetEase(Ease.OutCubic));
            }
            
            // 2. Character slides in with delay
            if (charRect != null)
            {
                animSeq.Insert(characterSlideInDelay, charRect.DOAnchorPos(charOriginalPos, characterSlideInDuration).SetEase(Ease.OutQuad));
                
                // 3. Character continues sliding slightly (inertia effect to the RIGHT)
                Vector2 inertiaPos = new Vector2(charOriginalPos.x + inertiaDistance, charOriginalPos.y);
                animSeq.Append(charRect.DOAnchorPos(inertiaPos, inertiaDuration).SetEase(Ease.OutSine));
            }
            
            // 4. Start typewriter effect when character inertia begins
            float typewriterStartTime = backgroundSlideInDuration + characterSlideInDelay + characterSlideInDuration;
            if (messageText != null && !string.IsNullOrEmpty(fullMessageText))
            {
                animSeq.InsertCallback(typewriterStartTime, () => {
                    PlayTypewriterEffect(fullMessageText, inertiaDuration);
                });
            }
            
            // Enable clicking after animation starts
            animSeq.AppendCallback(() => {
                canClick = true;
            });
            
            animSeq.Play();
        }
        
        /// <summary>
        /// Typewriter effect for message text
        /// </summary>
        private void PlayTypewriterEffect(string text, float duration)
        {
            if (messageText == null || string.IsNullOrEmpty(text)) return;
            
            messageText.text = "";
            messageText.DOText(text, duration).SetEase(Ease.Linear);
        }
        
        private void OnClick()
        {
            if (!isShowing) return;
            
            canClick = false;
            
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            
            PlaySlideOutAnimation();
        }
        
        private void PlaySlideOutAnimation()
        {
            RectTransform charRect = characterImage != null ? characterImage.GetComponent<RectTransform>() : null;
            
            float screenWidth = Screen.width;
            Sequence hideSeq = DOTween.Sequence();
            
            // Character slides out to the RIGHT
            if (charRect != null)
            {
                hideSeq.Join(charRect.DOAnchorPosX(screenWidth + 100f, slideOutDuration).SetEase(Ease.InCubic));
            }
            
            // Background fades out (no slide)
            if (canvasGroup != null)
            {
                hideSeq.Join(canvasGroup.DOFade(0f, slideOutDuration).SetEase(Ease.InQuad));
            }
            
            hideSeq.OnComplete(() =>
            {
                // Deactivate target object
                if (targetObject != null)
                {
                    targetObject.SetActive(false);
                }
                
                isShowing = false;
                canClick = false;
                
                onClickCallback?.Invoke();
                onClickCallback = null;
                
                Debug.Log("[MonsterModeCutInUI] Hidden, callback invoked");
            });
            
            hideSeq.Play();
        }
        
        public void ForceHide()
        {
            DOTween.Kill(this);
            
            // Reset positions
            if (positionsInitialized)
            {
                if (backgroundImage != null)
                {
                    RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                    if (bgRect != null) bgRect.anchoredPosition = originalBackgroundPosition;
                }
                if (characterImage != null)
                {
                    RectTransform charRect = characterImage.GetComponent<RectTransform>();
                    if (charRect != null) charRect.anchoredPosition = originalCharacterPosition;
                }
            }
            
            // Deactivate target object
            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }
            
            isShowing = false;
            canClick = false;
            onClickCallback = null;
        }
    }
}
