using UnityEngine;
using System.Collections.Generic;
using ApprovalMonster.Data;
using NaughtyAttributes;

namespace ApprovalMonster.Core
{
    /// <summary>
    /// ステージデータの集中管理クラス
    /// 全ステージのリストを保持し、選択されたステージを管理する
    /// 将来的なアンロック機能のための骨組みも含む
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [Header("Stage Data")]
        [Tooltip("利用可能な全ステージのリスト")]
        [ReorderableList]
        public List<StageData> allStages = new List<StageData>();

        [Header("Current State")]
        [ReadOnly]
        [SerializeField] private StageData selectedStage;
        
        [ReadOnly]
        [SerializeField] private int selectedStageIndex = -1;

        /// <summary>
        /// 現在選択されているステージ（読み取り専用）
        /// </summary>
        public StageData SelectedStage => selectedStage;

        /// <summary>
        /// 選択中のステージのインデックス（読み取り専用）
        /// </summary>
        public int SelectedStageIndex => selectedStageIndex;

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

        /// <summary>
        /// ステージを選択する
        /// </summary>
        /// <param name="stage">選択するステージデータ</param>
        public void SelectStage(StageData stage)
        {
            if (stage == null)
            {
                Debug.LogWarning("[StageManager] Attempted to select null stage!");
                return;
            }

            selectedStage = stage;
            selectedStageIndex = allStages.IndexOf(stage);
            
            Debug.Log($"[StageManager] Stage selected: {stage.stageName} (Index: {selectedStageIndex})");
        }

        /// <summary>
        /// インデックスでステージを選択する
        /// </summary>
        /// <param name="index">選択するステージのインデックス</param>
        public void SelectStageByIndex(int index)
        {
            if (index < 0 || index >= allStages.Count)
            {
                Debug.LogError($"[StageManager] Invalid stage index: {index}");
                return;
            }

            SelectStage(allStages[index]);
        }

        /// <summary>
        /// 次のステージを取得する（リザルト画面からの遷移用）
        /// </summary>
        /// <returns>次のステージ。存在しない場合はnull</returns>
        public StageData GetNextStage()
        {
            if (selectedStageIndex < 0 || selectedStageIndex >= allStages.Count - 1)
            {
                Debug.Log("[StageManager] No next stage available.");
                return null;
            }

            return allStages[selectedStageIndex + 1];
        }

        /// <summary>
        /// 指定されたステージがアンロック済みかを判定する
        /// 現在は常にtrueを返すが、将来のアンロック機能実装のための骨組み
        /// </summary>
        /// <param name="stage">判定するステージ</param>
        /// <returns>アンロック済みならtrue</returns>
        public bool IsStageUnlocked(StageData stage)
        {
            Debug.Log($"[StageManager] ===== IsStageUnlocked('{stage?.stageName}') START =====");
            
            if (stage == null)
            {
                Debug.LogError("[StageManager] Stage is NULL, returning false");
                return false;
            }
            
            // requiredStagesをチェック
            Debug.Log($"[StageManager] Stage '{stage.stageName}' requiredStages: {(stage.requiredStages != null ? stage.requiredStages.Count.ToString() : "NULL")}");
            
            if (stage.requiredStages != null && stage.requiredStages.Count > 0)
            {
                Debug.Log($"[StageManager] Checking {stage.requiredStages.Count} required stages for '{stage.stageName}':");
                
                for (int i = 0; i < stage.requiredStages.Count; i++)
                {
                    var requiredStage = stage.requiredStages[i];
                    if (requiredStage == null)
                    {
                        Debug.LogWarning($"[StageManager]   Required stage [{i}] is NULL, skipping");
                        continue;
                    }
                    
                    Debug.Log($"[StageManager]   Required stage [{i}]: '{requiredStage.stageName}'");
                }
            }
            else
            {
                Debug.Log($"[StageManager] Stage '{stage.stageName}' has no requiredStages, UNLOCKED");
                return true;
            }
            
            // SaveDataManagerをチェック
            if (SaveDataManager.Instance == null)
            {
                Debug.LogWarning("[StageManager] SaveDataManager.Instance is NULL, defaulting to UNLOCKED");
                return true;
            }
            
            Debug.Log($"[StageManager] SaveDataManager found, checking clear status...");
            
            // 全ての必須ステージがクリアされているかチェック（AND条件）
            foreach (var requiredStage in stage.requiredStages)
            {
                if (requiredStage == null) continue;
                
                bool isCleared = SaveDataManager.Instance.IsStageCleared(requiredStage.stageName);
                Debug.Log($"[StageManager]   '{requiredStage.stageName}' cleared: {isCleared}");
                
                if (!isCleared)
                {
                    Debug.Log($"[StageManager] Stage '{stage.stageName}' LOCKED: required '{requiredStage.stageName}' not cleared");
                    Debug.Log($"[StageManager] ===== IsStageUnlocked('{stage.stageName}') END: FALSE =====");
                    return false;
                }
            }
            
            // 全ての必須ステージがクリア済み
            Debug.Log($"[StageManager] Stage '{stage.stageName}' UNLOCKED: all required stages cleared");
            Debug.Log($"[StageManager] ===== IsStageUnlocked('{stage.stageName}') END: TRUE =====");
            return true;
        }

        /// <summary>
        /// 指定されたインデックスのステージがアンロック済みかを判定する
        /// </summary>
        /// <param name="index">判定するステージのインデックス</param>
        /// <returns>アンロック済みならtrue</returns>
        public bool IsStageUnlockedByIndex(int index)
        {
            if (index < 0 || index >= allStages.Count)
            {
                return false;
            }

            return IsStageUnlocked(allStages[index]);
        }

        /// <summary>
        /// デバッグ用：最初のステージを自動選択
        /// </summary>
        [Button("Select First Stage")]
        private void SelectFirstStage()
        {
            if (allStages.Count > 0)
            {
                SelectStageByIndex(0);
            }
            else
            {
                Debug.LogWarning("[StageManager] No stages available!");
            }
        }
    }
}
