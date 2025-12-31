using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApprovalMonster.Core;
using ApprovalMonster.Data;
using System.Collections.Generic;
using NaughtyAttributes;
using DG.Tweening;

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
            
            [Tooltip("ハイスコアを表示するテキスト（スコアアタック用）")]
            public TextMeshProUGUI highScoreText;
            
            [HideInInspector]
            public Tween pulseTween;
        }

        [Header("Stage Buttons")]
        [Tooltip("各ボタンとステージの手動割り当て")]
        [ReorderableList]
        public List<StageButton> stageButtons = new List<StageButton>();
        
        [Header("Coming Soon")]
        [Tooltip("開発中パネル（クリック時に表示）")]
        [SerializeField] private GameObject comingSoonPanel;
        
        [Tooltip("開発中パネルを閉じるボタン")]
        [SerializeField] private Button comingSoonCloseButton;
        
        [Tooltip("開発中ステージのボタンリスト（ステージ未設定のボタン）")]
        [SerializeField] private List<Button> comingSoonButtons = new List<Button>();
        
        [Header("Optional")]
        [Tooltip("戻るボタン（オプション）")]
        [SerializeField] private Button backButton;
        
        [Header("Pulse Animation")]
        [SerializeField] private float pulseDuration = 1.2f;
        [SerializeField] private float pulseScale = 1.05f;

        private void Start()
        {
            SetupStageButtons();
            SetupComingSoonButtons();
            
            // 戻るボタンがあればタイトルに戻る処理を追加
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
            
            // 開発中パネルを初期非表示
            if (comingSoonPanel != null)
            {
                comingSoonPanel.SetActive(false);
            }
            
            // 開発中パネルの閉じるボタン
            if (comingSoonCloseButton != null)
            {
                comingSoonCloseButton.onClick.AddListener(HideComingSoonPanel);
            }
        }
        
        private void OnEnable()
        {
            // パネルが表示されるたびにパルスアニメーションを再開
            RefreshUnlockStates();
        }
        
        /// <summary>
        /// ステージのアンロック状態を再チェックして更新
        /// リザルトから戻った際などに外部から呼び出される
        /// </summary>
        public void RefreshUnlockStates()
        {
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
                
                // アンロック済みならパルスアニメーション開始
                if (isUnlocked)
                {
                    StartPulseAnimation(stageButton);
                }
                
                // 説明文はStageDataから削除されたため、空にするか非表示にする
                if (stageButton.descriptionText != null)
                {
                    stageButton.descriptionText.gameObject.SetActive(false);
                }
                
                // ハイスコア表示（スコアアタックステージのみ）
                UpdateHighScoreDisplay(stageButton);

                // クリック時の処理を設定（クロージャ対策）
                StageData capturedStage = stageButton.stage;
                Button capturedButton = stageButton.button;
                int capturedIndex = i;
                stageButton.button.onClick.AddListener(() => OnStageSelected(capturedStage, capturedButton, capturedIndex));

                Debug.Log($"[StageSelectManager] Setup button {i} for stage: {stageButton.stage.stageName} (Unlocked: {isUnlocked})");
            }
            
            Debug.Log("[StageSelectManager] All buttons setup complete");
        }
        
        private void StartPulseAnimation(StageButton stageButton)
        {
            if (stageButton.button == null) return;
            
            // 既存のパルスをキャンセル
            stageButton.pulseTween?.Kill();
            
            var transform = stageButton.button.transform;
            transform.localScale = Vector3.one;
            
            stageButton.pulseTween = transform.DOScale(pulseScale, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        private void StopPulseAnimation(StageButton stageButton)
        {
            stageButton.pulseTween?.Kill();
            if (stageButton.button != null)
            {
                stageButton.button.transform.localScale = Vector3.one;
            }
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
                
                // アンロック状態に応じてパルスを制御
                if (isUnlocked)
                {
                    StartPulseAnimation(stageButton);
                }
                else
                {
                    StopPulseAnimation(stageButton);
                }
                
                // ハイスコア表示を更新
                UpdateHighScoreDisplay(stageButton);
                
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
        /// ハイスコア表示を更新（スコアアタックステージのみ）
        /// </summary>
        private void UpdateHighScoreDisplay(StageButton stageButton)
        {
            if (stageButton.highScoreText == null || stageButton.stage == null)
            {
                Debug.LogWarning($"[StageSelectManager] UpdateHighScoreDisplay skipped - highScoreText or stage is null");
                return;
            }
            
            // スコアアタックモード判定
            bool isScoreAttack = (stageButton.stage.clearCondition == null || 
                                  !stageButton.stage.clearCondition.hasScoreGoal);
            
          
            if (isScoreAttack && SaveDataManager.Instance != null)
            {
                long highScore = SaveDataManager.Instance.LoadStageHighScore(stageButton.stage.stageName);
                
                if (highScore > 0)
                {
                    stageButton.highScoreText.text = $"{highScore:N0}";
                    stageButton.highScoreText.gameObject.SetActive(true);
                }
                else
                {
                    // スコアが0の場合は「---」を表示
                    stageButton.highScoreText.text = "---";
                    stageButton.highScoreText.gameObject.SetActive(true);
                }
            }
            else
            {
                // スコアアタックでない場合は非表示
                stageButton.highScoreText.gameObject.SetActive(false);

            }
        }

        /// <summary>
        /// ステージが選択された時の処理
        /// </summary>
        private void OnStageSelected(StageData stage, Button button, int buttonIndex)
        {
            
            // パルスを停止
            if (buttonIndex >= 0 && buttonIndex < stageButtons.Count)
            {
                StopPulseAnimation(stageButtons[buttonIndex]);
            }
            
            // クリックリアクション
            button.transform.DOKill();
            button.transform.localScale = Vector3.one;
            button.transform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 5, 0.5f)
                .OnComplete(() => {
                    Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
                    
                    // StageManagerにステージを選択させる
                    if (StageManager.Instance != null)
                    {
                        StageManager.Instance.SelectStage(stage);
                        
                        // 初回プレイ時のチュートリアル表示チェック
                        if (ShouldShowTutorialForFirstTime(stage))
                        {
                            ShowTutorialThenGoToMain();
                        }
                        else
                        {
                            // 通常のゲーム画面に遷移
                            GoToMainGame();
                        }
                    }
                    else
                    {
                        Debug.LogError("[StageSelectManager] StageManager.Instance is null!");
                    }
                });
        }
        
        /// <summary>
        /// 初回プレイ時にチュートリアルを表示すべきかチェック
        /// </summary>
        private bool ShouldShowTutorialForFirstTime(StageData stage)
        {
            // 既にチュートリアルを見たことがあればfalse
            if (TutorialPlayer.HasShownTutorial())
            {
                return false;
            }
            
            // 初心者ステージ（前提ステージがない）かチェック
            bool isBeginnerStage = stage != null && 
                                   (stage.requiredStages == null || 
                                    stage.requiredStages.Count == 0);
            
            return isBeginnerStage;
        }
        
        /// <summary>
        /// チュートリアルを表示してからゲーム画面に遷移
        /// </summary>
        private void ShowTutorialThenGoToMain()
        {
            Debug.Log("[StageSelectManager] First time playing - showing tutorial before game");
            
            // TutorialPlayerを探す
            var tutorialPlayer = FindObjectOfType<TutorialPlayer>(true);
            if (tutorialPlayer != null)
            {
                // チュートリアル終了時にゲーム画面へ遷移するコールバックを設定
                tutorialPlayer.onTutorialClosed = () => {
                    tutorialPlayer.onTutorialClosed = null; // コールバックをクリア
                    GoToMainGame();
                };
                tutorialPlayer.Show();
            }
            else
            {
                Debug.LogWarning("[StageSelectManager] TutorialPlayer not found - going to main directly");
                GoToMainGame();
            }
        }
        
        /// <summary>
        /// メインゲーム画面に遷移
        /// </summary>
        private void GoToMainGame()
        {
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
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            Debug.Log("[StageSelectManager] Back button clicked");
            
            if (SceneNavigator.Instance != null)
            {
                SceneNavigator.Instance.GoToTitle();
            }
        }
        
        /// <summary>
        /// 開発中ボタンのセットアップ
        /// </summary>
        private void SetupComingSoonButtons()
        {
            if (comingSoonButtons == null || comingSoonButtons.Count == 0) return;
            
            foreach (var button in comingSoonButtons)
            {
                if (button != null)
                {
                    button.onClick.AddListener(ShowComingSoonPanel);
                }
            }
            
            Debug.Log($"[StageSelectManager] Setup {comingSoonButtons.Count} coming soon buttons");
        }
        
        /// <summary>
        /// 開発中パネルを表示
        /// </summary>
        private void ShowComingSoonPanel()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            if (comingSoonPanel != null)
            {
                comingSoonPanel.SetActive(true);
                
                // フェードインアニメーション
                var canvasGroup = comingSoonPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.DOFade(1f, 0.3f);
                }
                
                Debug.Log("[StageSelectManager] Showing Coming Soon panel");
            }
        }
        
        /// <summary>
        /// 開発中パネルを非表示
        /// </summary>
        private void HideComingSoonPanel()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            if (comingSoonPanel != null)
            {
                var canvasGroup = comingSoonPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
                        comingSoonPanel.SetActive(false);
                    });
                }
                else
                {
                    comingSoonPanel.SetActive(false);
                }
                
                Debug.Log("[StageSelectManager] Hiding Coming Soon panel");
            }
        }

        private void OnDestroy()
        {
            // パルスアニメーションのクリーンアップ
            foreach (var stageButton in stageButtons)
            {
                stageButton.pulseTween?.Kill();
            }
            
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

