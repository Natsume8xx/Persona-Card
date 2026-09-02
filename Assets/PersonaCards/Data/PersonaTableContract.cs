namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌配置配表契约：与策划表格「人格牌配置」sheet 的表头与枚举值约定（P0-1E 遗留）。
    /// P0-1J 人格牌三表化后，旧「人格牌配置」sheet 已由「人格牌配置/人格牌_词条/人格牌_主属性/人格牌_次级属性」
    /// 四表取代（新契约见 PersonaCardTableContract 等），旧导入层（PersonaTableMapper/PersonaImportCommand）已删除。
    /// 本类仅保留常量，供旧运行时链路的 PersonaConfigAsset.Validate 与 InitialPersonaCatalogTests 对照使用，
    /// 直到 B7 把运行时切换到新三表目录为止。
    /// 程序集边界（P0-1E）：Battle 引用 Data 不能反向，故本契约只含 string 常量（枚举在 Battle.Personas 的配置枚举，
    /// 两处的顺序与取值由交叉校验测试锁住，防漂移）。
    /// </summary>
    public static class PersonaTableContract
    {
        /// <summary>工作表名（人格牌数据；P0-1E 的旧单表，已下线）。</summary>
        public const string SheetName = "人格牌配置";

        /// <summary>列名：人格牌_ID（PER_xxx；权威查询键，PER_001~016 必须齐全）。</summary>
        public const string ColPersonaId = "人格牌_ID";

        /// <summary>列名：人格牌名称（显示名；当前配表为「1111xx（暂定」占位，仅存值不参与逻辑）。</summary>
        public const string ColName = "人格牌名称";

        /// <summary>列名：品质类型（基础/进阶/稀有/异质；「特殊」= 旧写法，兼容规范化 + 警告，A1）。</summary>
        public const string ColQuality = "品质类型";

        /// <summary>列名：品质参数（当前填颜色名，语义待策划说明 A9，仅存原文）。</summary>
        public const string ColQualityParam = "品质参数";

        /// <summary>列名：行为标签_ID（T01~T16，格式校验；范围不校验，A5 预留 T09~T16）。</summary>
        public const string ColBehaviorTag = "行为标签_ID";

        /// <summary>列名：触发条件（12 种统计类条件，固定映射）。</summary>
        public const string ColTrigger = "触发条件";

        /// <summary>列名：比较符（等于/大于等于/小于/小于等于，固定映射）。</summary>
        public const string ColComparator = "比较符";

        /// <summary>列名：条件阈值（非负整数；允许空）。</summary>
        public const string ColThreshold = "条件阈值";

        /// <summary>列名：附加条件（可解析的「条件+符号+阈值」结构化；其余存原文 + 警告，A8）。</summary>
        public const string ColExtra = "附加条件";

        /// <summary>列名：效果类型1（6 种效果，固定映射）。</summary>
        public const string ColEffect = "效果类型1";

        /// <summary>列名：效果参数1（必填，非负 decimal）。</summary>
        public const string ColEffectParam1 = "效果参数1";

        /// <summary>列名：效果参数2（非负 decimal；空 = 0）。</summary>
        public const string ColEffectParam2 = "效果参数2";

        /// <summary>列名：效果（原文列，当前全空=预留，仅存原文）。</summary>
        public const string ColEffectRaw = "效果";

        /// <summary>列名：效果上限（非负 decimal；允许空 = 无上限）。</summary>
        public const string ColEffectCap = "效果上限";

        /// <summary>列名：独立结算（是/否）。</summary>
        public const string ColIndependent = "独立结算";

        /// <summary>枚举值：品质「基础」。</summary>
        public const string QualityBasic = "基础";

        /// <summary>枚举值：品质「进阶」。</summary>
        public const string QualityAdvanced = "进阶";

        /// <summary>枚举值：品质「稀有」。</summary>
        public const string QualityRare = "稀有";

        /// <summary>枚举值：品质「异质」（A1 拍板统一名）。</summary>
        public const string QualityMutant = "异质";

        /// <summary>兼容值：品质旧写法「特殊」（A1 拍板前配表用词，规范化 + 警告）。</summary>
        public const string LegacyQualityMutant = "特殊";

        /// <summary>品质规范值集合（与 Battle 的 PersonaQuality 枚举对应，顺序一致）。</summary>
        public static readonly string[] QualityValues = { QualityBasic, QualityAdvanced, QualityRare, QualityMutant };

        /// <summary>枚举值：触发条件「与上一手牌型相同」。</summary>
        public const string TriggerSameHandTypeAsPrevious = "与上一手牌型相同";

        /// <summary>枚举值：触发条件「计分牌数量」。</summary>
        public const string TriggerScoringCardCount = "计分牌数量";

        /// <summary>枚举值：触发条件「已使用弃牌次数」。</summary>
        public const string TriggerDiscardsUsed = "已使用弃牌次数";

        /// <summary>枚举值：触发条件「命中AI偏好」。</summary>
        public const string TriggerHitAiPreference = "命中AI偏好";

        /// <summary>枚举值：触发条件「剩余弃牌次数」。</summary>
        public const string TriggerDiscardsRemaining = "剩余弃牌次数";

        /// <summary>枚举值：触发条件「本局移除牌数量」。</summary>
        public const string TriggerCardsRemovedThisRun = "本局移除牌数量";

        /// <summary>枚举值：触发条件「本局新增牌数量」。</summary>
        public const string TriggerCardsAddedThisRun = "本局新增牌数量";

        /// <summary>枚举值：触发条件「连续使用相同牌型次数」。</summary>
        public const string TriggerSameHandTypeStreak = "连续使用相同牌型次数";

        /// <summary>枚举值：触发条件「牌库数量」。</summary>
        public const string TriggerDeckSize = "牌库数量";

        /// <summary>枚举值：触发条件「其他人格触发次数」。</summary>
        public const string TriggerOtherPersonaTriggerCount = "其他人格触发次数";

        /// <summary>枚举值：触发条件「剩余出牌次数」。</summary>
        public const string TriggerPlaysRemaining = "剩余出牌次数";

        /// <summary>枚举值：触发条件「人格触发次数」。</summary>
        public const string TriggerPersonaTriggerCount = "人格触发次数";

        /// <summary>触发条件合法值集合（与 Battle 的 PersonaTriggerCondition 枚举按表出现序对应）。</summary>
        public static readonly string[] TriggerValues =
        {
            TriggerSameHandTypeAsPrevious,
            TriggerScoringCardCount,
            TriggerDiscardsUsed,
            TriggerHitAiPreference,
            TriggerDiscardsRemaining,
            TriggerCardsRemovedThisRun,
            TriggerCardsAddedThisRun,
            TriggerSameHandTypeStreak,
            TriggerDeckSize,
            TriggerOtherPersonaTriggerCount,
            TriggerPlaysRemaining,
            TriggerPersonaTriggerCount
        };

        /// <summary>枚举值：比较符「等于」。</summary>
        public const string ComparatorEqual = "等于";

        /// <summary>枚举值：比较符「大于等于」。</summary>
        public const string ComparatorGreaterOrEqual = "大于等于";

        /// <summary>枚举值：比较符「小于」。</summary>
        public const string ComparatorLess = "小于";

        /// <summary>枚举值：比较符「小于等于」。</summary>
        public const string ComparatorLessOrEqual = "小于等于";

        /// <summary>比较符合法值集合（与 Battle 的 PersonaComparator 枚举对应）。</summary>
        public static readonly string[] ComparatorValues =
            { ComparatorEqual, ComparatorGreaterOrEqual, ComparatorLess, ComparatorLessOrEqual };

        /// <summary>枚举值：效果「增加筹码」。</summary>
        public const string EffectAddChips = "增加筹码";

        /// <summary>枚举值：效果「增加倍率」。</summary>
        public const string EffectAddMultiplier = "增加倍率";

        /// <summary>枚举值：效果「增加筹码和倍率」。</summary>
        public const string EffectAddChipsAndMultiplier = "增加筹码和倍率";

        /// <summary>枚举值：效果「每单位增加倍率」。</summary>
        public const string EffectPerUnitMultiplier = "每单位增加倍率";

        /// <summary>枚举值：效果「每单位增加筹码」。</summary>
        public const string EffectPerUnitChips = "每单位增加筹码";

        /// <summary>枚举值：效果「最终倍率乘算」。</summary>
        public const string EffectMultiplyFinal = "最终倍率乘算";

        /// <summary>效果类型合法值集合（与 Battle 的 PersonaEffectType 枚举对应）。</summary>
        public static readonly string[] EffectValues =
        {
            EffectAddChips,
            EffectAddMultiplier,
            EffectAddChipsAndMultiplier,
            EffectPerUnitMultiplier,
            EffectPerUnitChips,
            EffectMultiplyFinal
        };

        /// <summary>枚举值：独立结算「是」。</summary>
        public const string IndependentYes = "是";

        /// <summary>枚举值：独立结算「否」。</summary>
        public const string IndependentNo = "否";

        /// <summary>行为标签格式（T01~T16）。</summary>
        public const string BehaviorTagPattern = @"^T\d{2}$";
    }
}
