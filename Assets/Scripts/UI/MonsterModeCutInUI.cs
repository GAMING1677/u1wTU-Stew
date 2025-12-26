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
    public class MonsterModeCutInUI : MonoBehaviour, IPointerClickHandler
    {
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
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            gameObject.SetActive(false);
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
            
            Debug.Log("[MonsterModeCutInUI] Activating GameObject");
            gameObject.SetActive(true);
            
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
            
            // Store original positions
            Vector2 bgOriginalPos = bgRect != null ? bgRect.anchoredPosition : Vector2.zero;
            Vector2 charOriginalPos = charRect != null ? charRect.anchoredPosition : Vector2.zero;
            
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
                gameObject.SetActive(false);
                isShowing = false;
                canClick = false;
                
                // Reset alpha for next time
                if (canvasGroup != null) canvasGroup.alpha = 1f;
                
                onClickCallback?.Invoke();
                onClickCallback = null;
                
                Debug.Log("[MonsterModeCutInUI] Hidden, callback invoked");
            });
            
            hideSeq.Play();
        }
        
        public void ForceHide()
        {
            DOTween.Kill(this);
            gameObject.SetActive(false);
            isShowing = false;
            canClick = false;
            onClickCallback = null;
        }
    }
}
