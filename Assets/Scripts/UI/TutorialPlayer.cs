using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// チュートリアル画像アニメーションを再生するUIコンポーネント
    /// </summary>
    public class TutorialPlayer : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("アニメーションを表示するImage")]
        [SerializeField] private Image displayImage;
        
        [Tooltip("タイトルテキスト（オプショナル）")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Tooltip("説明テキスト（オプショナル）")]
        [SerializeField] private TextMeshProUGUI descriptionText;
        
        [Tooltip("進捗表示テキスト（オプショナル）")]
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Controls")]
        [Tooltip("再生/一時停止ボタン（オプショナル）")]
        [SerializeField] private Button playPauseButton;
        
        [Tooltip("次のチュートリアルへボタン（オプショナル）")]
        [SerializeField] private Button nextButton;
        
        [Tooltip("前のチュートリアルへボタン（オプショナル）")]
        [SerializeField] private Button prevButton;
        
        [Tooltip("閉じるボタン（オプショナル）")]
        [SerializeField] private Button closeButton;
        
        [Tooltip("クリックで次へ進むエリア（displayImageやパネル全体など）")]
        [SerializeField] private Button clickArea;
        
        [Header("Tutorials")]
        [Tooltip("再生するチュートリアルデータ配列")]
        [SerializeField] private Data.TutorialData[] tutorials;
        
        [Header("Settings")]
        [Tooltip("チュートリアルパネル（表示/非表示を管理するオブジェクト）")]
        [SerializeField] private GameObject tutorialPanel;
        
        [Tooltip("パネル全体のCanvasGroup（フェード用、オプショナル）")]
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Tooltip("フェード時間")]
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Header("Transition")]
        [Tooltip("チュートリアル切り替え時のトランジション時間")]
        [SerializeField] private float transitionDuration = 0.2f;
        
        [Header("Typewriter")]
        [Tooltip("1文字あたりの表示間隔（秒）")]
        [SerializeField] private float typewriterInterval = 0.03f;
        
        [Header("Button Pulse")]
        [Tooltip("パルスの大きさ")]
        [SerializeField] private float pulseScale = 1.1f;
        [Tooltip("パルスの周期（秒）")]
        [SerializeField] private float pulseDuration = 0.8f;
        [Tooltip("ボタン間のずらし時間（秒）")]
        [SerializeField] private float pulseOffset = 0.2f;
        
        // State
        private int currentTutorialIndex = 0;
        private int currentFrameIndex = 0;
        private bool isPlaying = false;
        private Coroutine playCoroutine;
        private Coroutine typewriterCoroutine;
        private Data.TutorialData currentData;
        private bool isTypewriting = false;
        private string currentFullText = "";
        private bool isTransitioning = false;
        
        /// <summary>
        /// チュートリアルが閉じられた時に呼ばれるコールバック（外部から設定可能）
        /// </summary>
        public System.Action onTutorialClosed;
        
        private void Start()
        {
            SetupButtons();
            
            // 初期状態で非表示
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }
        
        private void SetupButtons()
        {
            if (playPauseButton != null)
            {
                playPauseButton.onClick.AddListener(TogglePlayPause);
            }
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextTutorial);
            }
            if (prevButton != null)
            {
                prevButton.onClick.AddListener(PrevTutorial);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            
            // クリックエリアで次へ進む（最後なら閉じる）
            if (clickArea != null)
            {
                clickArea.onClick.AddListener(OnClickAreaClicked);
            }
        }
        
        /// <summary>
        /// チュートリアルパネルを表示
        /// </summary>
        public void Show()
        {
            if (tutorials == null || tutorials.Length == 0)
            {
                Debug.LogWarning("[TutorialPlayer] No tutorials assigned!");
                return;
            }
            
            // パネルを表示
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            
            currentTutorialIndex = 0;
            LoadTutorialInternal(currentTutorialIndex);
            
            // フェードイン（canvasGroupがあれば）
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeDuration);
            }
            
            // ボタンのずらしパルス開始
            StartStaggeredButtonPulse();
            
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        /// <summary>
        /// 特定のインデックスのチュートリアルを表示
        /// </summary>
        public void Show(int tutorialIndex)
        {
            if (tutorials == null || tutorialIndex < 0 || tutorialIndex >= tutorials.Length)
            {
                Debug.LogWarning($"[TutorialPlayer] Invalid tutorial index: {tutorialIndex}");
                return;
            }
            
            // パネルを表示
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            
            currentTutorialIndex = tutorialIndex;
            LoadTutorialInternal(currentTutorialIndex);
            
            // フェードイン（canvasGroupがあれば）
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeDuration);
            }
        }
        
        /// <summary>
        /// チュートリアルパネルを非表示
        /// </summary>
        public void Hide()
        {
            Stop();
            
            // チュートリアルを見たことをマーク
            MarkTutorialAsShown();
            
            // フェードアウト（canvasGroupがあれば）してから非表示
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => {
                    if (tutorialPanel != null)
                    {
                        tutorialPanel.SetActive(false);
                    }
                    // コールバックを呼び出し
                    onTutorialClosed?.Invoke();
                });
            }
            else
            {
                // フェードなしで即座に非表示
                if (tutorialPanel != null)
                {
                    tutorialPanel.SetActive(false);
                }
                // コールバックを呼び出し
                onTutorialClosed?.Invoke();
            }
            
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        // PlayerPrefsキー
        private const string TUTORIAL_SHOWN_KEY = "TutorialShown";
        
        /// <summary>
        /// チュートリアルを見たことがあるかどうか
        /// </summary>
        public static bool HasShownTutorial()
        {
            return PlayerPrefs.GetInt(TUTORIAL_SHOWN_KEY, 0) == 1;
        }
        
        /// <summary>
        /// チュートリアルを見たことをマーク
        /// </summary>
        public static void MarkTutorialAsShown()
        {
            PlayerPrefs.SetInt(TUTORIAL_SHOWN_KEY, 1);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// チュートリアルデータを読み込んで再生開始（内部用）
        /// </summary>
        private void LoadTutorialInternal(int index)
        {
            if (tutorials == null || index < 0 || index >= tutorials.Length) return;
            
            currentData = tutorials[index];
            if (currentData == null || currentData.frames == null || currentData.frames.Length == 0)
            {
                Debug.LogWarning($"[TutorialPlayer] Tutorial at index {index} has no frames!");
                return;
            }
            
            // UI更新
            if (titleText != null)
            {
                titleText.text = currentData.tutorialTitle;
            }
            
            // 説明文をタイプライター効果で表示
            if (descriptionText != null && !string.IsNullOrEmpty(currentData.description))
            {
                PlayTypewriter(currentData.description);
            }
            else if (descriptionText != null)
            {
                descriptionText.text = "";
            }
            
            // ナビゲーションボタンの有効/無効
            UpdateNavigationButtons();
            
            // 再生開始
            currentFrameIndex = 0;
            Play();
        }
        
        private void UpdateNavigationButtons()
        {
            if (prevButton != null)
            {
                prevButton.interactable = currentTutorialIndex > 0;
            }
            if (nextButton != null)
            {
                nextButton.interactable = currentTutorialIndex < tutorials.Length - 1;
            }
        }
        
        /// <summary>
        /// 再生開始
        /// </summary>
        public void Play()
        {
            if (currentData == null) return;
            
            Stop();
            isPlaying = true;
            playCoroutine = StartCoroutine(PlayAnimation());
        }
        
        /// <summary>
        /// 一時停止
        /// </summary>
        public void Pause()
        {
            isPlaying = false;
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
        }
        
        /// <summary>
        /// 停止（フレームを先頭に戻す）
        /// </summary>
        public void Stop()
        {
            Pause();
            currentFrameIndex = 0;
        }
        
        /// <summary>
        /// 再生/一時停止切り替え
        /// </summary>
        public void TogglePlayPause()
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }
        
        /// <summary>
        /// 次のチュートリアルへ
        /// </summary>
        public void NextTutorial()
        {
            if (tutorials == null || currentTutorialIndex >= tutorials.Length - 1) return;
            if (isTransitioning) return;
            
            currentTutorialIndex++;
            TransitionToTutorial(currentTutorialIndex);
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        /// <summary>
        /// 前のチュートリアルへ
        /// </summary>
        public void PrevTutorial()
        {
            if (currentTutorialIndex <= 0) return;
            if (isTransitioning) return;
            
            currentTutorialIndex--;
            TransitionToTutorial(currentTutorialIndex);
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        /// <summary>
        /// トランジション付きでチュートリアルを切り替え
        /// </summary>
        private void TransitionToTutorial(int index)
        {
            isTransitioning = true;
            
            // displayImageをフェードアウト
            if (displayImage != null)
            {
                displayImage.DOKill();
                displayImage.DOFade(0f, transitionDuration).OnComplete(() => {
                    // データを読み込んで再生開始
                    LoadTutorialInternal(index);
                    // フェードイン
                    displayImage.DOFade(1f, transitionDuration).OnComplete(() => {
                        isTransitioning = false;
                    });
                });
            }
            else
            {
                LoadTutorialInternal(index);
                isTransitioning = false;
            }
        }
        
        /// <summary>
        /// クリックエリアがクリックされた時の処理
        /// タイプライター中ならスキップ、完了済みなら次へ進む
        /// </summary>
        private void OnClickAreaClicked()
        {
            if (tutorials == null) return;
            
            // タイプライター中ならテキストをすべて表示
            if (isTypewriting)
            {
                SkipTypewriter();
                return;
            }
            
            // テキスト表示完了済みなら次へ進む
            if (currentTutorialIndex < tutorials.Length - 1)
            {
                // 次へ進む
                NextTutorial();
            }
            else
            {
                // 最後のチュートリアルなら閉じる
                Hide();
            }
        }
        
        /// <summary>
        /// タイプライターをスキップしてテキスト全体を表示
        /// </summary>
        private void SkipTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = currentFullText;
            }
            
            isTypewriting = false;
        }
        
        /// <summary>
        /// アニメーション再生コルーチン
        /// </summary>
        private IEnumerator PlayAnimation()
        {
            if (currentData == null || currentData.frames == null) yield break;
            
            while (isPlaying)
            {
                // フレーム表示
                if (displayImage != null && currentFrameIndex < currentData.frames.Length)
                {
                    displayImage.sprite = currentData.frames[currentFrameIndex];
                }
                
                // 進捗更新
                UpdateProgressText();
                
                // 次のフレームへ
                currentFrameIndex++;
                
                // ループ処理
                if (currentFrameIndex >= currentData.frames.Length)
                {
                    if (currentData.loop)
                    {
                        currentFrameIndex = 0;
                    }
                    else
                    {
                        isPlaying = false;
                        yield break;
                    }
                }
                
                yield return new WaitForSeconds(currentData.frameInterval);
            }
        }
        
        private void UpdateProgressText()
        {
            if (progressText != null && tutorials != null)
            {
                // 現在のチュートリアルが全体の何番目かを表示 (例: 1/5)
                progressText.text = $"{currentTutorialIndex + 1}/{tutorials.Length}";
            }
        }
        
        /// <summary>
        /// タイプライター効果で説明文を表示
        /// </summary>
        private IEnumerator TypewriterCoroutine(string fullText)
        {
            if (descriptionText == null) yield break;
            
            isTypewriting = true;
            currentFullText = fullText;
            descriptionText.text = "";
            
            foreach (char c in fullText)
            {
                descriptionText.text += c;
                yield return new WaitForSeconds(typewriterInterval);
            }
            
            isTypewriting = false;
        }
        
        /// <summary>
        /// 説明文をタイプライター効果で表示
        /// </summary>
        private void PlayTypewriter(string text)
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }
        
        /// <summary>
        /// ボタンにずらしながらパルスアニメーションを開始
        /// </summary>
        private void StartStaggeredButtonPulse()
        {
            float delay = 0f;
            
            if (prevButton != null)
            {
                StartButtonPulseWithDelay(prevButton.transform, delay);
                delay += pulseOffset;
            }
            if (nextButton != null)
            {
                StartButtonPulseWithDelay(nextButton.transform, delay);
                delay += pulseOffset;
            }
            if (closeButton != null)
            {
                StartButtonPulseWithDelay(closeButton.transform, delay);
            }
        }
        
        private void StartButtonPulseWithDelay(Transform target, float delay)
        {
            // 既存のアニメーションを停止
            target.DOKill();
            target.localScale = Vector3.one;
            
            // 遅延後にパルス開始
            DOVirtual.DelayedCall(delay, () => {
                target.DOScale(pulseScale, pulseDuration / 2)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });
        }
        
        /// <summary>
        /// ボタンパルスを停止
        /// </summary>
        private void StopButtonPulse()
        {
            if (prevButton != null)
            {
                prevButton.transform.DOKill();
                prevButton.transform.localScale = Vector3.one;
            }
            if (nextButton != null)
            {
                nextButton.transform.DOKill();
                nextButton.transform.localScale = Vector3.one;
            }
            if (closeButton != null)
            {
                closeButton.transform.DOKill();
                closeButton.transform.localScale = Vector3.one;
            }
        }
        
        private void OnDisable()
        {
            Stop();
            StopButtonPulse();
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
        }
    }
}
