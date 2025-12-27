using UnityEngine;
using ApprovalMonster.Core;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// セーブデータをリセットするエディター用ユーティリティ
    /// </summary>
    public class SaveDataResetter : MonoBehaviour
    {
        [ContextMenu("Clear All Save Data")]
        public void ClearAllSaveData()
        {
            // Delete the default save file
            if (ES3.FileExists())
            {
                ES3.DeleteFile();
                Debug.Log("[SaveDataResetter] All save data cleared!");
            }
            else
            {
                Debug.Log("[SaveDataResetter] No save file found");
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(
                "Save Data Cleared", 
                "All save data has been deleted. Please restart the game.", 
                "OK"
            );
            #endif
        }
        
        [ContextMenu("Clear Stage Clear Data Only")]
        public void ClearStageClearData()
        {
            ES3.DeleteKey("ClearedStages");
            Debug.Log("[SaveDataResetter] Stage clear data cleared!");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(
                "Clear Data Cleared", 
                "Stage clear data has been deleted.", 
                "OK"
            );
            #endif
        }
        
        [ContextMenu("Show Current Save Data")]
        public void ShowCurrentSaveData()
        {
            var clearedStages = ES3.Load("ClearedStages", new System.Collections.Generic.List<string>());
            var highScore = ES3.Load("HighScore", 0L);
            
            string message = $"High Score: {highScore}\n\nCleared Stages:\n";
            foreach (var stageName in clearedStages)
            {
                message += $"- {stageName}\n";
            }
            
            Debug.Log($"[SaveDataResetter]\n{message}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog(
                "Current Save Data", 
                message, 
                "OK"
            );
            #endif
        }
    }
}
