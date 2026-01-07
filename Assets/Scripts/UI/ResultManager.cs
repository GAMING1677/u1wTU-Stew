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
        
        [Header("Tweet")]
        [Tooltip("ツイートボタン")]
        [SerializeField] private Button tweetButton;
        [Tooltip("unityroomのゲームID（ゲーム設定 > その他 で確認）")]
        [SerializeField] private string gameId = "YOUR-GAMEID";
        [Tooltip("ツイートに含めるハッシュタグ（#なしで入力）")]
        [SerializeField] private string[] hashtags = new string[] { "unityroom", "unity1week" };
        
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
        
        [Header("Max Score")]
        [Tooltip("カンスト時のみ表示する追加情報テキスト")]
        [SerializeField] private TextMeshProUGUI maxScoreInfoText;
        
        private Coroutine animationCoroutine;
        private long currentScore = 0;
        private bool wasNewRecord = false;
        private bool wasMaxScore = false;
        private int totalCardsPlayed = 0;

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
                wasMaxScore = SceneNavigator.Instance.IsMaxScore;
                totalCardsPlayed = SceneNavigator.Instance.TotalCardsPlayed;
                Debug.Log($"[ResultManager] Score: {score}, Cleared: {wasCleared}, ScoreAttack: {isScoreAttackMode}, NewRecord: {isNewHighScore}, MaxScore: {wasMaxScore}, TotalCards: {totalCardsPlayed}");
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
            
            // ツイートボタン設定
            if (tweetButton != null)
            {
                tweetButton.onClick.RemoveListener(OnTweetButtonClicked);
                tweetButton.onClick.AddListener(OnTweetButtonClicked);
            }
            
            // カンスト時の追加情報表示
            SetupMaxScoreDisplay(wasMaxScore, totalCardsPlayed);
            
            // スコアを保存（ツイート用）
            currentScore = score;
            wasNewRecord = isNewHighScore;

            // 自動スコア送信
            SendScoreToUnityroom();
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
        
        private void SetupMaxScoreDisplay(bool isMaxScore, int cardCount)
        {
            if (maxScoreInfoText == null) return;
            
            if (isMaxScore)
            {
                maxScoreInfoText.gameObject.SetActive(true);
                maxScoreInfoText.text = $"★カンスト達成★\nカードプレイ枚数: {cardCount}枚";
                
                // アニメーション
                maxScoreInfoText.transform.localScale = Vector3.zero;
                maxScoreInfoText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
                
                Debug.Log($"[ResultManager] Displaying max score info: {cardCount} cards");
            }
            else
            {
                maxScoreInfoText.gameObject.SetActive(false);
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
        
        /// <summary>
        /// ツイートボタンクリック時の処理
        /// </summary>
        private void OnTweetButtonClicked()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            // クリアステージ数を取得
            int clearedStages = Core.SaveDataManager.Instance?.GetClearedStageCount() ?? 0;
            
            // ツイート内容を構築
            string recordText = wasNewRecord ? "🎉NEW RECORD🎉\n" : "";
            string tweetText;
            
            if (wasMaxScore)
            {
                // カンスト時の特別ツイート
                tweetText = $"★カンスト達成！★\nインプレッションモンスターガールで遊んだよ！\nスコア：{currentScore:N0}\nカードプレイ回数：{totalCardsPlayed}枚";
            }
            else
            {
                // 通常ツイート
                tweetText = $"{recordText}インプレッションモンスターガールで遊んだよ！\nクリアステージ数：{clearedStages}\nスコア：{currentScore:N0}";
            }
            
            Debug.Log($"[ResultManager] Tweeting: {tweetText}");
            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // ハッシュタグの数に応じて呼び分け
                if (hashtags != null && hashtags.Length >= 2)
                {
                    naichilab.UnityRoomTweet.Tweet(gameId, tweetText, hashtags[0], hashtags[1]);
                }
                else if (hashtags != null && hashtags.Length == 1)
                {
                    naichilab.UnityRoomTweet.Tweet(gameId, tweetText, hashtags[0]);
                }
                else
                {
                    naichilab.UnityRoomTweet.Tweet(gameId, tweetText);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ResultManager] Tweet failed: {e.Message}");
            }
#else
            Debug.Log($"[ResultManager] Tweet skipped (not WebGL build). Content: {tweetText}");
            
            // エディタ等でのデバッグ送信用ログ
            int debugClearedCount = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetClearedStageCount() : 0;
            long debugTotalHighScore = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetTotalScoreAttackHighScore() : 0;
             Debug.Log($"[ResultManager] (Simulation) Sent scores to unityroom - Board 1: {debugClearedCount}, Board 2: {debugTotalHighScore}");

            // エディタではURL出力で確認
            string url = $"https://twitter.com/intent/tweet?text={UnityEngine.Networking.UnityWebRequest.EscapeURL(tweetText)}";
            Debug.Log($"[ResultManager] Tweet URL: {url}");
#endif
        }

        /// <summary>
        /// unityroomへスコアを送信（WebGLのみ、例外安全）
        /// </summary>
        private void SendScoreToUnityroom()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (unityroom.Api.UnityroomApiClient.Instance != null)
                {
                    int clearedCount = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetClearedStageCount() : 0;
                    long totalHighScore = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetTotalScoreAttackHighScore() : 0;
                    
                    // クリア可能ステージ数（hasScoreGoal=true）とスコアアタックステージ数をカウント
                    int clearableStageCount = 0;
                    int scoreAttackStageCount = 0;
                    if (Core.StageManager.Instance != null)
                    {
                        foreach (var stage in Core.StageManager.Instance.allStages)
                        {
                            if (stage?.clearCondition != null)
                            {
                                if (stage.clearCondition.hasScoreGoal)
                                {
                                    clearableStageCount++; // クリア可能ステージ
                                }
                                else
                                {
                                    scoreAttackStageCount++; // スコアアタックステージ
                                }
                            }
                        }
                    }
                    int maxStages = clearableStageCount > 0 ? clearableStageCount : 100;
                    long maxTotalScore = Core.ResourceManager.MAX_SCORE * scoreAttackStageCount;
                    
                    // 異常値チェック: 合計上限を超えるスコアは送信しない
                    if (totalHighScore > maxTotalScore)
                    {
                        Debug.LogError($"[ResultManager] Invalid totalHighScore {totalHighScore} exceeds max {maxTotalScore}, skipping submission");
                        return;
                    }
                    
                    // クリア数の上限チェック（ステージ数を超えていたら上限値に補正）
                    if (clearedCount > maxStages)
                    {
                        Debug.LogWarning($"[ResultManager] clearedCount {clearedCount} exceeds max stages {maxStages}, clamping to max");
                        clearedCount = maxStages;
                    }
                    
                    unityroom.Api.UnityroomApiClient.Instance.SendScore(1, clearedCount, unityroom.Api.ScoreboardWriteMode.HighScoreDesc);
                    unityroom.Api.UnityroomApiClient.Instance.SendScore(2, totalHighScore, unityroom.Api.ScoreboardWriteMode.HighScoreDesc);
                    
                    Debug.Log($"[ResultManager] Sent scores to unityroom - Board 1: {clearedCount}, Board 2: {totalHighScore}");
                }
                else
                {
                    Debug.LogWarning("[ResultManager] UnityroomApiClient.Instance is null, skipping score submission");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ResultManager] Score submission failed: {e.Message}");
            }
#else
            // エディタ等でのデバッグ送信用ログ
            int debugClearedCount = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetClearedStageCount() : 0;
            long debugTotalHighScore = Core.SaveDataManager.Instance != null ? Core.SaveDataManager.Instance.GetTotalScoreAttackHighScore() : 0;
            Debug.Log($"[ResultManager] (Simulation) Sent scores to unityroom - Board 1: {debugClearedCount}, Board 2: {debugTotalHighScore}");
#endif
        }
    }
}

