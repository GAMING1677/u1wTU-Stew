using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Core;
using ApprovalMonster.Data;
using System.Collections.Generic;
using NaughtyAttributes;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ステージ選択画面のUIを管理するクラス
    /// ボタンとステージを手動で割り当てる方式
    /// </summary>
    public class StageSelectManager : MonoBehaviour
    {
        /// <summary>
        /// ボタンとステージのペア
        /// </summary>
        [System.Serializable]
        public class StageButton
        {
            [Tooltip("ステージ選択ボタン")]
            public Button button;
            
            [Tooltip("このボタンに割り当てるステージ")]
            public StageData stage;
            
            [Tooltip("ステージの説明文を表示するテキスト")]
            public TextMeshProUGUI descriptionText;
        }

        [Header("Stage Buttons")]
        [Tooltip("各ボタンとステージの手動割り当て")]
        [ReorderableList]
        public List<StageButton> stageButtons = new List<StageButton>();
        
        [Header("Optional")]
        [Tooltip("戻るボタン（オプション）")]
        [SerializeField] private Button backButton;

        private void Start()
        {
            SetupStageButtons();
            
            // 戻るボタンがあればタイトルに戻る処理を追加
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }
        
        /// <summary>
        /// ステージのアンロック状態を再チェックして更新
        /// リザルトから戻った際などに外部から呼び出される
        /// </summary>
        public void RefreshUnlockStates()
        {
            Debug.Log("[StageSelectManager] RefreshUnlockStates called - updating button states");
            UpdateButtonStates();
        }

        /// <summary>
        /// 各ボタンにクリックリスナーを設定
        /// </summary>
        private void SetupStageButtons()
        {
            if (stageButtons == null || stageButtons.Count == 0)
            {
                Debug.LogWarning("[StageSelectManager] No stage buttons assigned!");
                return;
            }
            
            Debug.Log($"[StageSelectManager] Setting up {stageButtons.Count} stage buttons...");

            for (int i = 0; i < stageButtons.Count; i++)
            {
                var stageButton = stageButtons[i];
                
                if (stageButton.button == null)
                {
                    Debug.LogWarning($"[StageSelectManager] Button at index {i} is null, skipping.");
                    continue;
                }

                if (stageButton.stage == null)
                {
                    Debug.LogWarning($"[StageSelectManager] Stage at index {i} is null, skipping.");
                    continue;
                }

                // アンロック状態をチェック
                bool isUnlocked = CheckUnlockState(stageButton.stage);
                stageButton.button.interactable = isUnlocked;
                
                // 説明文はStageDataから削除されたため、空にするか非表示にする
                if (stageButton.descriptionText != null)
                {
                    stageButton.descriptionText.gameObject.SetActive(false);
                }

                // クリック時の処理を設定（クロージャ対策）
                StageData capturedStage = stageButton.stage;
                stageButton.button.onClick.AddListener(() => OnStageSelected(capturedStage));

                Debug.Log($"[StageSelectManager] Setup button {i} for stage: {stageButton.stage.stageName} (Unlocked: {isUnlocked})");
            }
            
            Debug.Log("[StageSelectManager] All buttons setup complete");
        }
        
        /// <summary>
        /// ボタンのアンロック状態のみを更新（リスナーは再設定しない）
        /// </summary>
        private void UpdateButtonStates()
        {
            if (stageButtons == null || stageButtons.Count == 0) return;
            
            Debug.Log($"[StageSelectManager] Updating {stageButtons.Count} button states...");

            for (int i = 0; i < stageButtons.Count; i++)
            {
                var stageButton = stageButtons[i];
                if (stageButton.button == null || stageButton.stage == null) continue;

                bool isUnlocked = CheckUnlockState(stageButton.stage);
                stageButton.button.interactable = isUnlocked;
                
                Debug.Log($"[StageSelectManager] Updated button {i}: '{stageButton.stage.stageName}' unlocked={isUnlocked}");
            }
            
            Debug.Log("[StageSelectManager] Button states update complete");
        }
        
        /// <summary>
        /// ステージのアンロック状態をチェック
        /// </summary>
        private bool CheckUnlockState(StageData stage)
        {
            if (StageManager.Instance != null)
            {
                return StageManager.Instance.IsStageUnlocked(stage);
            }
            else
            {
                Debug.LogError("[StageSelectManager] StageManager.Instance is NULL!");
                return true; // フォールバック
            }
        }

        /// <summary>
        /// ステージが選択された時の処理
        /// </summary>
        /// <param name="stage">選択されたステージ</param>
        private void OnStageSelected(StageData stage)
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            Debug.Log($"[StageSelectManager] Stage selected: {stage.stageName}");

            // StageManagerにステージを選択させる
            if (StageManager.Instance != null)
            {
                StageManager.Instance.SelectStage(stage);
                // ゲーム画面に遷移
                if (SceneNavigator.Instance != null)
                {
                    SceneNavigator.Instance.GoToMain();
                }
                else
                {
                    Debug.LogError("[StageSelectManager] SceneNavigator not found!");
                }
            }
            else
            {
                Debug.LogError("[StageSelectManager] StageManager.Instance is null!");
            }
        }

        /// <summary>
        /// 戻るボタンが押された時の処理
        /// </summary>
        private void OnBackButtonClicked()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            Debug.Log("[StageSelectManager] Back button clicked");
            
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoToTitle();
            }
        }

        private void OnDestroy()
        {
            // ボタンリスナーのクリーンアップ
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
            
            // ステージボタンのリスナーもクリーンアップ
            foreach (var stageButton in stageButtons)
            {
                if (stageButton.button != null)
                {
                    stageButton.button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
