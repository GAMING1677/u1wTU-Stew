using UnityEngine;
using System.Collections.Generic;
using ApprovalMonster.Data;

namespace ApprovalMonster.Core
{
    public class SaveDataManager : MonoBehaviour
    {
        public static SaveDataManager Instance { get; private set; }

        private const string KEY_HIGHSCORE = "HighScore";
        private const string KEY_CLEARED_STAGES = "ClearedStages";
        
        [SerializeField] private bool autoSaveOnExit = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 起動時に異常スコアをクリーンアップ
            // StageManagerの初期化後に実行するため、遅延呼び出し
            StartCoroutine(CleanupAfterDelay());
        }
        
        private System.Collections.IEnumerator CleanupAfterDelay()
        {
            // StageManagerの初期化を待つ
            yield return null;
            CleanupInvalidScores();
        }
        
        /// <summary>
        /// 異常なスコアデータを削除
        /// </summary>
        private void CleanupInvalidScores()
        {
            if (StageManager.Instance == null)
            {
                Debug.LogWarning("[SaveDataManager] StageManager not ready, skipping cleanup");
                return;
            }
            
            int deletedCount = 0;
            
            // ステージ別ハイスコアをチェック
            foreach (var stage in StageManager.Instance.allStages)
            {
                if (stage == null) continue;
                
                string key = $"HighScore_{stage.stageName}";
                if (ES3.KeyExists(key, GetSaveSettings()))
                {
                    long score = ES3.Load<long>(key, GetSaveSettings());
                    if (score > ResourceManager.MAX_SCORE)
                    {
                        ES3.DeleteKey(key, GetSaveSettings());
                        Debug.Log($"[SaveDataManager] Deleted invalid score {score} for '{stage.stageName}'");
                        deletedCount++;
                    }
                }
            }
            
            // グローバルハイスコアもチェック
            long globalScore = LoadHighScore();
            if (globalScore > ResourceManager.MAX_SCORE)
            {
                ES3.DeleteKey(KEY_HIGHSCORE, GetSaveSettings());
                Debug.Log($"[SaveDataManager] Deleted invalid global high score {globalScore}");
                deletedCount++;
            }
            
            if (deletedCount > 0)
            {
                SyncSave();
                Debug.Log($"[SaveDataManager] Cleanup complete. Deleted {deletedCount} invalid scores.");
            }
            else
            {
                Debug.Log("[SaveDataManager] Cleanup complete. No invalid scores found.");
            }
        }

        private void SyncSave()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // WebGLの場合、変更を即座にIndexedDBに同期させる
                ES3.StoreCachedFile();
                Debug.Log("[SaveDataManager] Synced to IndexedDB (WebGL)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveDataManager] SyncSave failed: {e.Message}");
            }
#endif
        }

        // WebGLでの永続化を確実にするため、明示的にCacheを使用する設定
        private ES3Settings GetSaveSettings()
        {
            return new ES3Settings(ES3.Location.Cache);
        }

        public void SaveHighScore(long score)
        {
            long current = LoadHighScore();
            if (score > current)
            {
                ES3.Save(KEY_HIGHSCORE, score, GetSaveSettings());
                SyncSave();
            }
        }

        public long LoadHighScore()
        {
            return ES3.Load(KEY_HIGHSCORE, 0L, GetSaveSettings());
        }

        public void SaveStageClear(string stageName)
        {
            List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>(), GetSaveSettings());
            if (!cleared.Contains(stageName))
            {
                cleared.Add(stageName);
                ES3.Save(KEY_CLEARED_STAGES, cleared, GetSaveSettings());
                SyncSave();
                Debug.Log($"[SaveDataManager] Stage '{stageName}' saved as cleared. Total cleared: {cleared.Count}");
            }
            else
            {
                Debug.Log($"[SaveDataManager] Stage '{stageName}' already in cleared list");
            }
        }

        public bool IsStageCleared(string stageName)
        {
             List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>(), GetSaveSettings());
             bool isCleared = cleared.Contains(stageName);
             return isCleared;
        }
        
        /// <summary>
        /// ステージ別ハイスコアを保存（現在の記録より高い場合のみ）
        /// </summary>
        /// <returns>新記録の場合true</returns>
        public bool SaveStageHighScore(string stageName, long score)
        {
            string key = $"HighScore_{stageName}";
            long currentHighScore = LoadStageHighScore(stageName);
            
            if (score > currentHighScore)
            {
                ES3.Save(key, score, GetSaveSettings());
                SyncSave();
                Debug.Log($"[SaveDataManager] New high score for '{stageName}': {score} (previous: {currentHighScore})");
                return true;
            }
            else
            {
                Debug.Log($"[SaveDataManager] Score {score} did not beat high score {currentHighScore} for '{stageName}'");
                return false;
            }
        }
        
        /// <summary>
        /// ステージ別ハイスコアを読み込み
        /// MAX_SCOREを超える異常値は自動削除
        /// </summary>
        public long LoadStageHighScore(string stageName)
        {
            string key = $"HighScore_{stageName}";
            long score = ES3.Load(key, 0L, GetSaveSettings());
            
            // 異常値チェック: MAX_SCOREを超えていたら0にリセット
            if (score > ResourceManager.MAX_SCORE)
            {
                Debug.LogWarning($"[SaveDataManager] Invalid score {score} for '{stageName}', resetting to 0");
                ES3.DeleteKey(key, GetSaveSettings());
                SyncSave();
                return 0;
            }
            
            return score;
        }
        
        /// <summary>
        /// クリア済みステージの数を取得
        /// 実際にStageManagerに存在するステージのみをカウントすることで、
        /// ゴミデータや不正なデータの混入を防ぐ
        /// </summary>
        public int GetClearedStageCount()
        {
            List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>(), GetSaveSettings());
            
            if (StageManager.Instance == null)
            {
                // フォールバック: StageManagerがない場合はそのまま数を返す
                // (通常はありえないが、単体テスト時などへの配慮)
                return cleared.Count;
            }
            
            int validClearCount = 0;
            
            // 全ステージリストと照合
            foreach (var stage in StageManager.Instance.allStages)
            {
                if (stage == null) continue;
                
                if (cleared.Contains(stage.stageName))
                {
                    validClearCount++;
                }
            }
            
            return validClearCount;
        }
        
        /// <summary>
        /// スコアアタック（エンドレス）ステージの合計ハイスコアを取得
        /// </summary>
        public long GetTotalScoreAttackHighScore()
        {
            if (StageManager.Instance == null)
            {
                Debug.LogWarning("[SaveDataManager] StageManager is null, cannot calculate total score");
                return 0;
            }
            
            long totalScore = 0;
            int scoreAttackCount = 0;
            
            Debug.Log($"[SaveDataManager] Calculating total score from {StageManager.Instance.allStages.Count} total stages...");
            
            foreach (var stage in StageManager.Instance.allStages)
            {
                if (stage == null)
                {
                    Debug.LogWarning("[SaveDataManager] Null stage found in allStages");
                    continue;
                }
                
                // スコアアタックステージの判定: clearConditionがあり、hasScoreGoal=falseのステージ
                bool isScoreAttack = (stage.clearCondition != null && !stage.clearCondition.hasScoreGoal);
                
                if (isScoreAttack)
                {
                    long stageScore = LoadStageHighScore(stage.stageName);
                    totalScore += stageScore;
                    scoreAttackCount++;
                    Debug.Log($"[SaveDataManager] ScoreAttack '{stage.stageName}': {stageScore} (cumulative: {totalScore})");
                }
                else
                {
                    string reason = stage.clearCondition == null ? "no clearCondition" 
                                  : stage.clearCondition.hasScoreGoal ? "hasScoreGoal=true" 
                                  : "unknown";
                    Debug.Log($"[SaveDataManager] Skipping '{stage.stageName}' ({reason})");
                }
            }
            
            Debug.Log($"[SaveDataManager] Total ScoreAttack stages: {scoreAttackCount}, Total High Score: {totalScore}");
            return totalScore;
        }

        
        /// <summary>
        /// クリア済みステージ名のリストを取得
        /// </summary>
        public List<string> GetClearedStageNames()
        {
            return ES3.Load(KEY_CLEARED_STAGES, new List<string>());
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnExit)
            {
                // Auto save miscellaneous state if needed
            }
        }
    }
}
