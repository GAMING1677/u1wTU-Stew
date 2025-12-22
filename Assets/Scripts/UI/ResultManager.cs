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

        private void Start()
        {
            // In a real implementation, we would pass the score via a static persistent object or SceneNavigator
            // For now, let's grab it from ResourceManager if it persists, or SaveDataManager
            
            long score = 0;
            // Assuming ResourceManager is destroyed on scene load, we might need a way to pass data.
            // For MVP, let's assume SaveDataManager holds the 'LastRunScore' or similar.
            // Or simpler: SceneNavigator can hold "LastScore".
            
            // For now, dummy display or fetch from SceneNavigator (need to add property there)
            if (SceneNavigator.Instance != null)
            {
                score = SceneNavigator.Instance.LastGameScore;
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

            if (titleButton != null)
            {
                titleButton.onClick.AddListener(OnReturnToTitle);
            }
        }

        private void OnReturnToTitle()
        {
            SceneNavigator.Instance.GoToTitle();
        }
    }
}
