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
                scoreText.text = "0";
                // DOTween.To to animate number
                long currentDisplayScore = 0;
                DOTween.To(() => currentDisplayScore, x => {
                    currentDisplayScore = x;
                    scoreText.text = $"{currentDisplayScore:N0}";
                }, score, 1.5f).SetEase(Ease.OutExpo);
            }
            else
            {
                Debug.LogWarning("[ResultManager] scoreText is not assigned!");
            }

            if (titleButton != null)
            {
                titleButton.onClick.RemoveListener(OnReturnToTitle);
                titleButton.onClick.AddListener(OnReturnToTitle);
            }
        }

        private void OnReturnToTitle()
        {
            SceneNavigator.Instance.GoToTitle();
        }
    }
}
