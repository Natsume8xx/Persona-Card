namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌效果类型（配置数据面，P0-1E 新增）：6 种效果，按配表「人格牌配置」sheet 出现序。
    /// 运行时结算接入留给后续 P0 玩法任务；与运行时枚举 PersonaEffectKind
    /// （AddChips/AddMultiplier/MultiplyFinal）分离，运行时 switch 不受波及。
    /// 显式赋值从 1 起：防序列化把缺省值 0 误读为合法枚举。
    /// </summary>
    public enum PersonaEffectType
    {
        /// <summary>增加筹码（PER_001/PER_004）。</summary>
        AddChips = 1,

        /// <summary>增加倍率（PER_002/PER_005/PER_010）。</summary>
        AddMultiplier = 2,

        /// <summary>增加筹码和倍率（PER_003：参数1=筹码、参数2=倍率）。</summary>
        AddChipsAndMultiplier = 3,

        /// <summary>每单位增加倍率（PER_006/PER_007：按触发统计量每单位累加）。</summary>
        PerUnitMultiplier = 4,

        /// <summary>每单位增加筹码（PER_008：按触发统计量每单位累加）。</summary>
        PerUnitChips = 5,

        /// <summary>最终倍率乘算（PER_009/PER_011~016）。</summary>
        MultiplyFinal = 6
    }
}
