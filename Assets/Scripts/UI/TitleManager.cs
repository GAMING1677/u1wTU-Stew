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
            Debug.Log("[TitleManager] Start Button Clicked. Going to Stage Select.");
            SceneNavigator.Instance.GoToStageSelect();
        }
    }
}
