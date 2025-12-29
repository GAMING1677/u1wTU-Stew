namespace ApprovalMonster.Data
{
    /// <summary>
    /// カードプレイの結果を表す
    /// </summary>
    public enum CardPlayResult
    {
        Success,               // プレイ成功
        InsufficientMotivation, // モチベーション不足
        HandConditionNotMet,   // 手札条件不足
        CardUnplayable,        // カード自体がプレイ不可
    }
    
    /// <summary>
    /// カードのプレイ条件
    /// </summary>
    public enum CardPlayCondition
    {
        None,              // 制限なし
        Never,             // 常時プレイ不可
        MonsterModeOnly,   // モンスターモード時のみプレイ可
        NormalModeOnly,    // 通常モード時のみプレイ可
    }
}
