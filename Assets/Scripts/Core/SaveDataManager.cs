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

        private void SyncSave()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGLの場合、変更を即座にIndexedDBに同期させる
            ES3.StoreCachedFile();
            Debug.Log("[SaveDataManager] Synced to IndexedDB (WebGL)");
#endif
        }

        public void SaveHighScore(long score)
        {
            long current = LoadHighScore();
            if (score > current)
            {
                ES3.Save(KEY_HIGHSCORE, score);
                SyncSave();
            }
        }

        public long LoadHighScore()
        {
            return ES3.Load(KEY_HIGHSCORE, 0L);
        }

        public void SaveStageClear(string stageName)
        {
            List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>());
            if (!cleared.Contains(stageName))
            {
                cleared.Add(stageName);
                ES3.Save(KEY_CLEARED_STAGES, cleared);
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
             List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>());
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
                ES3.Save(key, score);
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
        /// </summary>
        public long LoadStageHighScore(string stageName)
        {
            string key = $"HighScore_{stageName}";
            return ES3.Load(key, 0L);
        }
        
        /// <summary>
        /// クリア済みステージの数を取得
        /// </summary>
        public int GetClearedStageCount()
        {
            List<string> cleared = ES3.Load(KEY_CLEARED_STAGES, new List<string>());
            return cleared.Count;
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
            
            foreach (var stage in StageManager.Instance.allStages)
            {
                if (stage == null) continue;
                
                // クリア条件がない＝スコアアタック/エンドレスステージ
                if (stage.clearCondition == null)
                {
                    long stageScore = LoadStageHighScore(stage.stageName);
                    totalScore += stageScore;
                }
            }
            
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
