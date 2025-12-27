using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Core;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    public class ResultManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI resultLabel;
        [SerializeField] private Button titleButton;

        private void OnEnable()
        {
            Debug.Log("[ResultManager] OnEnable called");
            
            long score = 0;
            if (SceneNavigator.Instance != null)
            {
                score = SceneNavigator.Instance.LastGameScore;
                Debug.Log($"[ResultManager] Score from SceneNavigator: {score}");
            }
            else
            {
                Debug.LogWarning("[ResultManager] SceneNavigator.Instance is null!");
            }

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
                bool wasCleared = false;
                bool isScoreAttackMode = false;
                
                if (SceneNavigator.Instance != null)
                {
                    wasCleared = SceneNavigator.Instance.WasStageCleared;
                    isScoreAttackMode = SceneNavigator.Instance.IsScoreAttackMode;
                }
                
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

        private void OnReturnToTitle()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            SceneNavigator.Instance.GoToTitle();
        }
    }
}
