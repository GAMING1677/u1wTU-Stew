namespace ApprovalMonster.Data
{
    /// <summary>
    /// SE種別定義
    /// AudioManagerでSEを再生する際に使用
    /// </summary>
    public enum SEType
    {
        // UI系
        ButtonClick,
        CardDraftPanelShow,
        
        // カード系
        CardPlay,
        CardDraw,
        MonsterCardPlay,
        DeckShuffle,
        
        // リソース系
        FollowerGain,
        ImpressionGain,
        MentalDecrease,
        MentalRecover,
        MotivationUse,
        GoblinGain,
        BuffGain,
        
        // タイムライン系
        TimelinePost,
        
        // モンスターモード系
        MonsterModeTypewriter
    }
}
