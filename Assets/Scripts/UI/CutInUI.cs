using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System;
using ApprovalMonster.Data;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// 汎用カットイン表示システム
    /// 画面のどこかをクリック/タップで次に進む
    /// IPointerClickHandlerを使用してInput Systemに対応
    /// </summary>
    public class CutInUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        
        [Header("Default Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        [Tooltip("クリック可能になるまでの待機時間")]
        [SerializeField] private float clickDelay = 0.5f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        
        [Header("Presets")]
        [Tooltip("ゲームオーバー用プリセット")]
        [SerializeField] private CutInPreset gameOverPreset;
        [Tooltip("ステージクリア用プリセット")]
        [SerializeField] private CutInPreset stageClearPreset;
        [Tooltip("モンスターモード用プリセット")]
        [SerializeField] private CutInPreset monsterModePreset;
        [Tooltip("モチベーション不足用プリセット")]
        [SerializeField] private CutInPreset motivationLowPreset;
        
        private Action onClickCallback;
        private bool isShowing = false;
        private bool canClick = false;
        private float showTime;
        private float currentFadeOutDuration;
        private AudioClip currentClickSound;
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            // Ensure this object can receive pointer events
            // The backgroundImage should have Raycast Target enabled
            
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
        /// プリセットを使用してカットインを表示
        /// </summary>
        public void ShowPreset(CutInPreset preset, Action onComplete = null)
        {
            if (preset == null)
            {
                Debug.LogWarning("[CutInUI] Preset is null!");
                onComplete?.Invoke();
                return;
            }
            
            ApplyPreset(preset);
            ShowInternal(preset.fadeInDuration, onComplete);
            
            // Play preset sound
            if (audioSource != null && preset.showSound != null)
            {
                audioSource.PlayOneShot(preset.showSound);
            }
            
            currentFadeOutDuration = preset.fadeOutDuration;
            currentClickSound = preset.clickSound;
        }
        
        /// <summary>
        /// ゲームオーバー用プリセットで表示
        /// </summary>
        public void ShowGameOver(Action onComplete = null)
        {
            if (gameOverPreset != null)
            {
                ShowPreset(gameOverPreset, onComplete);
            }
            else
            {
                Show("GAME OVER", "メンタルが限界を迎えました...", onComplete);
            }
        }
        
        /// <summary>
        /// ステージクリア用プリセットで表示
        /// </summary>
        public void ShowStageClear(Action onComplete = null)
        {
            if (stageClearPreset != null)
            {
                ShowPreset(stageClearPreset, onComplete);
            }
            else
            {
                Show("STAGE CLEAR!", "", onComplete);
            }
        }
        
        /// <summary>
        /// モンスターモード用プリセットで表示
        /// NOTE: モンスターモード専用のMonsterModeCutInUIを使用することを推奨
        /// </summary>
        public void ShowMonsterMode(Action onComplete = null)
        {
            if (monsterModePreset != null)
            {
                ShowPreset(monsterModePreset, onComplete);
            }
            else
            {
                Show("MONSTER MODE", "承認欲求が暴走を始めた...", onComplete);
            }
        }
        
        /// <summary>
        /// モチベーション不足用プリセットで表示
        /// </summary>
        public void ShowMotivationLow(Action onComplete = null)
        {
            if (motivationLowPreset != null)
            {
                ShowPreset(motivationLowPreset, onComplete);
            }
            else
            {
                Show("やる気が足りない", "なんか面倒だからいいや…", onComplete);
            }
        }
        
        /// <summary>
        /// カットインを表示（テキスト指定）
        /// </summary>
        public void Show(string title, string message = "", Action onComplete = null)
        {
            if (titleText != null)
            {
                titleText.text = title;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            if (messageText != null)
            {
                messageText.text = message;
                messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
            
            // テキスト指定の場合はアイコンを非表示
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
            
            currentFadeOutDuration = fadeOutDuration;
            currentClickSound = null;
            
            ShowInternal(fadeInDuration, onComplete);
        }
        
        /// <summary>
        /// カットインを表示（色指定あり）
        /// </summary>
        public void Show(string title, string message, Color bgColor, Action onComplete = null)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = bgColor;
            }
            Show(title, message, onComplete);
        }
        
        private void ApplyPreset(CutInPreset preset)
        {
            // タイトル
            if (titleText != null)
            {
                if (!string.IsNullOrEmpty(preset.title))
                {
                    titleText.text = preset.title;
                    titleText.color = preset.titleColor;
                    titleText.gameObject.SetActive(true);
                }
                else
                {
                    titleText.gameObject.SetActive(false);
                }
            }
            
            // メッセージ
            if (messageText != null)
            {
                if (!string.IsNullOrEmpty(preset.message))
                {
                    messageText.text = preset.message;
                    messageText.color = preset.messageColor;
                    messageText.gameObject.SetActive(true);
                }
                else
                {
                    messageText.gameObject.SetActive(false);
                }
            }
            
            // 背景画像
            if (backgroundImage != null)
            {
                if (preset.backgroundImage != null)
                {
                    backgroundImage.sprite = preset.backgroundImage;
                    backgroundImage.color = Color.white;
                    backgroundImage.gameObject.SetActive(true);
                }
                else
                {
                    // 背景画像がなくても背景色で表示する場合はアクティブのまま
                    backgroundImage.sprite = null;
                    backgroundImage.color = preset.backgroundColor;
                    // 背景色のアルファが0なら非表示
                    backgroundImage.gameObject.SetActive(preset.backgroundColor.a > 0);
                }
            }
            
            // アイコン
            if (iconImage != null)
            {
                if (preset.showIcon && preset.iconImage != null)
                {
                    iconImage.sprite = preset.iconImage;
                    iconImage.gameObject.SetActive(true);
                    
                    // パンチスケールアニメーション
                    iconImage.transform.localScale = Vector3.one;
                    iconImage.transform.DOKill();
                    iconImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 8, 0.5f);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
        }
        
        private void ShowInternal(float fadeIn, Action onComplete)
        {
            // 既に表示中の場合は前のカットインを強制終了してから表示
            if (isShowing)
            {
                ForceHide();
            }
            
            isShowing = true;
            canClick = true;
            showTime = Time.time;
            onClickCallback = onComplete;
            
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeIn).SetEase(Ease.OutQuad);
            
            if (titleText != null)
            {
                titleText.transform.localScale = Vector3.one * 0.5f;
                titleText.transform.DOScale(1f, fadeIn * 1.5f).SetEase(Ease.OutBack);
            }
            
            Debug.Log($"[CutInUI] Showing: {titleText?.text}");
        }
        
        private void OnClick()
        {
            if (!isShowing) return;
            
            canClick = false; // Prevent double clicks
            
            if (audioSource != null && currentClickSound != null)
            {
                audioSource.PlayOneShot(currentClickSound);
            }
            
            Hide();
        }
        
        private void Hide()
        {
            canvasGroup.DOFade(0f, currentFadeOutDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                gameObject.SetActive(false);
                isShowing = false;
                canClick = false;
                
                onClickCallback?.Invoke();
                onClickCallback = null;
                
                Debug.Log("[CutInUI] Hidden, callback invoked");
            });
        }
        
        public void ForceHide()
        {
            canvasGroup.DOKill();
            gameObject.SetActive(false);
            isShowing = false;
            canClick = false;
            onClickCallback = null;
        }
    }
}

