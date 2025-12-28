namespace ApprovalMonster.Data
{
    /// <summary>
    /// SE種別定義
    /// AudioManagerでSEを再生する際に使用
    /// 
    /// ★重要: 新しいSEを追加する際は、必ず明示的な数値を指定すること
    /// 既存の数値を変更すると、AudioDatabaseの設定がずれます
    /// 
    /// カテゴリ別の数値範囲 (各100個):
    /// - UI系: 0-99
    /// - カード系: 100-199
    /// - リソース系: 200-299
    /// - タイムライン系: 300-399
    /// - モンスターモード系: 400-499
    /// - キャラクターリアクション系: 500-599
    /// </summary>
    public enum SEType
    {
        // UI系 (0-99)
        ButtonClick = 0,
        CardDraftPanelShow = 1,
        StageStart = 2,
        MotivationLow = 3,  // やる気不足カットイン
        
        // カード系 (100-199)
        CardPlay = 100,
        CardDraw = 101,
        MonsterCardPlay = 102,
        DeckShuffle = 103,
        
        // リソース系 (200-299)
        FollowerGain = 200,
        ImpressionGain = 201,
        MentalDecrease = 202,
        MentalRecover = 203,
        MotivationUse = 204,
        GoblinGain = 205,
        BuffGain = 206,
        
        // タイムライン系 (300-399)
        TimelinePost = 300,
        
        // モンスターモード系 (400-499)
        MonsterModeTypewriter = 400,
        
        // キャラクターリアクション系 (500-599)
        ReactionHappy_1 = 500,
        ReactionHappy_2 = 501,
        ReactionHappy_3 = 502,
        ReactionSad_1 = 503,
        ReactionSad_2 = 504
    }
}

