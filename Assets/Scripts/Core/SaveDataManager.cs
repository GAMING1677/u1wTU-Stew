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

        public void SaveHighScore(long score)
        {
            long current = LoadHighScore();
            if (score > current)
            {
                ES3.Save(KEY_HIGHSCORE, score);
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
             Debug.Log($"[SaveDataManager] IsStageCleared('{stageName}') = {isCleared}");
             return isCleared;
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
