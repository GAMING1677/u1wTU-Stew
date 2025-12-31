using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Core;
using DG.Tweening;
using System.Collections;

namespace ApprovalMonster.UI
{
    public class ResultManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI resultLabel;
        [SerializeField] private Button titleButton;
        
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite clearBackground;
        [SerializeField] private Sprite failBackground;
        
        [Header("Toggle Animation")]
        [SerializeField] private GameObject animatedObject1;
        [SerializeField] private GameObject animatedObject2;
        [SerializeField] private float toggleInterval = 0.5f;
        
        [Header("New Record")]
        [Tooltip("ハイスコア更新時に表示するテキスト")]
        [SerializeField] private TextMeshProUGUI newRecordText;
        
        private Coroutine animationCoroutine;

        private void OnEnable()
        {
            Debug.Log("[ResultManager] OnEnable called");
            
            long score = 0;
            bool wasCleared = false;
            bool isScoreAttackMode = false;
            bool isNewHighScore = false;
            
            if (SceneNavigator.Instance != null)
            {
                score = SceneNavigator.Instance.LastGameScore;
                wasCleared = SceneNavigator.Instance.WasStageCleared;
                isScoreAttackMode = SceneNavigator.Instance.IsScoreAttackMode;
                isNewHighScore = SceneNavigator.Instance.IsNewHighScore;
                Debug.Log($"[ResultManager] Score: {score}, Cleared: {wasCleared}, ScoreAttack: {isScoreAttackMode}, NewRecord: {isNewHighScore}");
            }
            else
            {
                Debug.LogWarning("[ResultManager] SceneNavigator.Instance is null!");
            }
            
            // 背景切り替え（スコアアタックはクリア扱い）
            SetupBackground(wasCleared || isScoreAttackMode);
            
            // スプライトアニメーション開始
            StartSpriteAnimation();
            
            // ハイスコア更新表示
            SetupNewRecordDisplay(isNewHighScore);

            if (scoreText != null)
            {
                Debug.Log($"[ResultManager] scoreText is assigned. Starting animation from 0 to {score}");
                scoreText.text = "0";
                
                // DOTween.To to animate number (use float for interpolation)
                float currentDisplayScore = 0;
                var tween = DOTween.To(() => currentDisplayScore, x => {
                    currentDisplayScore = x;
                    scoreText.text = $"{(long)currentDisplayScore:N0}";
                }, (float)score, 1.5f)
                .SetEase(Ease.OutExpo)
                .OnStart(() => {
                    Debug.Log("[ResultManager] Score animation STARTED");
                })
                .OnUpdate(() => {
                    // Log every 10th update to avoid spam
                    if (Time.frameCount % 10 == 0)
                    {
                        Debug.Log($"[ResultManager] Score animation UPDATE: {currentDisplayScore}");
                    }
                })
                .OnComplete(() => {
                    Debug.Log($"[ResultManager] Score animation COMPLETE: {currentDisplayScore}");
                    // Ensure final value is exact
                    scoreText.text = $"{score:N0}";
                });
                
                Debug.Log($"[ResultManager] Tween created: {tween != null}");
            }
            else
            {
                Debug.LogWarning("[ResultManager] scoreText is not assigned!");
            }
            
            // クリア状態を表示
            if (resultLabel != null)
            {
                // スコアアタックモードの場合は非表示
                if (isScoreAttackMode)
                {
                    resultLabel.gameObject.SetActive(false);
                    Debug.Log("[ResultManager] Score attack mode - hiding result label");
                }
                else
                {
                    resultLabel.gameObject.SetActive(true);
                    
                    if (wasCleared)
                    {
                        resultLabel.text = "ステージクリア！";
                    }
                    else
                    {
                        resultLabel.text = "ゲームオーバー";
                    }
                    
                    Debug.Log($"[ResultManager] Displaying clear status: {(wasCleared ? "CLEARED" : "FAILED")}");
                }
            }
            else
            {
                Debug.LogWarning("[ResultManager] resultLabel is not assigned!");
            }

            if (titleButton != null)
            {
                titleButton.onClick.RemoveListener(OnReturnToTitle);
                titleButton.onClick.AddListener(OnReturnToTitle);
            }
        }
        
        private void OnDisable()
        {
            StopSpriteAnimation();
        }
        
        private void SetupBackground(bool isClear)
        {
            if (backgroundImage == null) return;
            
            if (isClear && clearBackground != null)
            {
                backgroundImage.sprite = clearBackground;
            }
            else if (!isClear && failBackground != null)
            {
                backgroundImage.sprite = failBackground;
            }
            
            // 右から左へスライドインアニメーション
            RectTransform rt = backgroundImage.GetComponent<RectTransform>();
            if (rt != null)
            {
                float screenWidth = rt.rect.width;
                Vector2 startPos = rt.anchoredPosition;
                startPos.x += screenWidth; // 右側に配置
                rt.anchoredPosition = startPos;
                
                // 元の位置（0, y）にスライドイン
                rt.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutQuad);
            }
        }
        
        private void SetupNewRecordDisplay(bool isNewHighScore)
        {
            if (newRecordText == null) return;
            
            if (isNewHighScore)
            {
                newRecordText.gameObject.SetActive(true);
                newRecordText.text = "NEW RECORD!";
                
                // パルスアニメーション
                newRecordText.transform.localScale = Vector3.zero;
                Sequence seq = DOTween.Sequence();
                seq.Append(newRecordText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
                seq.Append(newRecordText.transform.DOScale(1f, 0.1f));
                seq.Append(newRecordText.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 1).SetLoops(-1));
                
                Debug.Log("[ResultManager] Displaying NEW RECORD!");
            }
            else
            {
                newRecordText.gameObject.SetActive(false);
            }
        }
        
        private void StartSpriteAnimation()
        {
            if (animatedObject1 == null || animatedObject2 == null)
                return;
            
            // 初期状態：1を表示、2を非表示
            animatedObject1.SetActive(true);
            animatedObject2.SetActive(false);
            
            animationCoroutine = StartCoroutine(ToggleAnimationCoroutine());
        }
        
        private void StopSpriteAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }
        
        private IEnumerator ToggleAnimationCoroutine()
        {
            bool showFirst = true;
            while (true)
            {
                yield return new WaitForSeconds(toggleInterval);
                showFirst = !showFirst;
                animatedObject1.SetActive(showFirst);
                animatedObject2.SetActive(!showFirst);
            }
        }

        private void OnReturnToTitle()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            SceneNavigator.Instance.GoToTitle();
        }
    }
}

