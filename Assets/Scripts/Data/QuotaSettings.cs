using UnityEngine;
using NaughtyAttributes;

namespace ApprovalMonster.Data
{
    [CreateAssetMenu(fileName = "QuotaSettings", menuName = "ApprovalMonster/QuotaSettings")]
    public class QuotaSettings : ScriptableObject
    {
        [Header("Turn Quotas")]
        [Tooltip("各ターンのノルマ（インプレッション目標）。インデックス0=ターン1")]
        [ReorderableList]
        public long[] turnQuotas = new long[20]
        {
            100,    // Turn 1
            200,    // Turn 2
            400,    // Turn 3
            600,    // Turn 4
            1000,   // Turn 5
            1500,   // Turn 6
            2000,   // Turn 7
            3000,   // Turn 8
            4000,   // Turn 9
            5000,   // Turn 10
            7000,   // Turn 11
            10000,  // Turn 12
            15000,  // Turn 13
            20000,  // Turn 14
            30000,  // Turn 15
            50000,  // Turn 16
            75000,  // Turn 17
            100000, // Turn 18
            150000, // Turn 19
            200000  // Turn 20
        };

        /// <summary>
        /// 指定ターンのノルマを取得（1-indexed）
        /// </summary>
        public long GetQuotaForTurn(int turnNumber)
        {
            int index = turnNumber - 1;
            if (index >= 0 && index < turnQuotas.Length)
            {
                return turnQuotas[index];
            }
            // 配列外の場合は最後の値を返す
            return turnQuotas.Length > 0 ? turnQuotas[turnQuotas.Length - 1] : 0;
        }
    }
}
