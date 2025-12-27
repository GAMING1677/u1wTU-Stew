using UnityEngine;
using UnityEngine.UI;
using ApprovalMonster.Core;

namespace ApprovalMonster.UI
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Start()
        {
            // タイトル画面でメインテーマBGMを再生
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMainTheme();
            }
            
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartDates);
                Debug.Log("[TitleManager] Start button listener added.");
            }
            else
            {
                Debug.LogError("[TitleManager] Start Button reference is missing!");
            }
        }

        private void OnStartDates()
        {
            AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            Debug.Log("[TitleManager] Start Button Clicked. Going to Stage Select.");
            SceneNavigator.Instance.GoToStageSelect();
        }
    }
}
