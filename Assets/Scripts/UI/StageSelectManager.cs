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
        /// 各ボタンにクリックリスナーを設定
        /// </summary>
        private void SetupStageButtons()
        {
            if (stageButtons == null || stageButtons.Count == 0)
            {
                Debug.LogWarning("[StageSelectManager] No stage buttons assigned!");
                return;
            }

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
                bool isUnlocked = true;
                if (StageManager.Instance != null)
                {
                    isUnlocked = StageManager.Instance.IsStageUnlocked(stageButton.stage);
                }
                
                stageButton.button.interactable = isUnlocked;

                // クリック時の処理を設定（クロージャ対策）
                StageData capturedStage = stageButton.stage;
                stageButton.button.onClick.AddListener(() => OnStageSelected(capturedStage));

                Debug.Log($"[StageSelectManager] Setup button for stage: {stageButton.stage.stageName} (Unlocked: {isUnlocked})");
            }
        }

        /// <summary>
        /// ステージが選択された時の処理
        /// </summary>
        /// <param name="stage">選択されたステージ</param>
        private void OnStageSelected(StageData stage)
        {
            Debug.Log($"[StageSelectManager] Stage selected: {stage.stageName}");

            // StageManagerにステージを選択させる
            if (StageManager.Instance != null)
            {
                StageManager.Instance.SelectStage(stage);
            }

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

        /// <summary>
        /// 戻るボタンが押された時の処理
        /// </summary>
        private void OnBackButtonClicked()
        {
            Debug.Log("[StageSelectManager] Back button clicked. Returning to title.");
            
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
