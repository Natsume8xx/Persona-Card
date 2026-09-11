namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌触发条件（配置数据面，P0-1E 新增）：16 种统计类条件，1~12 为旧 15 列扁平结构、13~16 为
    /// 新版 8 列引用式词条表追加（牌型品质/弃牌后出牌/连续使用不同牌型次数/出牌数量）。
    /// 运行时结算接入留给后续 P0 玩法任务（含依赖未开发 AI 系统的「命中AI偏好」）；
    /// 与运行时枚举 PersonaConditionKind（Always/MinimumHandPriority）分离，运行时 switch 不受波及。
    /// 显式赋值从 1 起：防序列化把缺省值 0 误读为合法枚举。
    /// </summary>
    public enum PersonaTriggerCondition
    {
        /// <summary>与上一手牌型相同（PER_001/PER_005）。</summary>
        SameHandTypeAsPrevious = 1,

        /// <summary>计分牌数量（PER_002/PER_012）。</summary>
        ScoringCardCount = 2,

        /// <summary>已使用弃牌次数（PER_003）。</summary>
        DiscardsUsed = 3,

        /// <summary>命中AI偏好（PER_004；依赖未开发的 AI 系统，运行时后续接入）。</summary>
        HitAiPreference = 4,

        /// <summary>剩余弃牌次数（PER_006）。</summary>
        DiscardsRemaining = 5,

        /// <summary>本局移除牌数量（PER_007）。</summary>
        CardsRemovedThisRun = 6,

        /// <summary>本局新增牌数量（PER_008）。</summary>
        CardsAddedThisRun = 7,

        /// <summary>连续使用相同牌型次数（PER_009/PER_014）。</summary>
        SameHandTypeStreak = 8,

        /// <summary>牌库数量（PER_010/PER_013）。</summary>
        DeckSize = 9,

        /// <summary>其他人格触发次数（PER_011）。</summary>
        OtherPersonaTriggerCount = 10,

        /// <summary>剩余出牌次数（PER_015）。</summary>
        PlaysRemaining = 11,

        /// <summary>人格触发次数（PER_016）。</summary>
        PersonaTriggerCount = 12,

        /// <summary>牌型品质（新版 8 列引用式词条表；条件参数为品质文本，存 ConditionParam）。</summary>
        HandTypeQuality = 13,

        /// <summary>弃牌后出牌（新版词条表；阈值 1 = 弃牌蓄力后的下一次出牌）。</summary>
        AfterDiscardPlay = 14,

        /// <summary>连续使用不同牌型次数（新版词条表；与「连续使用相同牌型次数」互补）。</summary>
        DifferentHandTypeStreak = 15,

        /// <summary>出牌数量（新版词条表；本次出牌张数）。</summary>
        SubmittedCardCount = 16
    }
}
