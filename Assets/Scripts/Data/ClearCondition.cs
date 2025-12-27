using UnityEngine;

namespace ApprovalMonster.Data
{
    /// <summary>
    /// ステージのクリア条件を定義するクラス
    /// </summary>
    [System.Serializable]
    public class ClearCondition
    {
        [Header("Score Clear")]
        [Tooltip("目標スコアによるクリア条件を有効化")]
        public bool hasScoreGoal = false;
        
        [Tooltip("クリアに必要なスコア")]
        public long targetScore = 1000000;
        
        // 将来の拡張用コメント：
        // - 複数条件（AND/OR）
        // - 特定カードを全てプレイする
        // - 特定パラメータ条件（メンタル維持など）
    }
}
