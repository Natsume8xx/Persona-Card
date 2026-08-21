using System;
using System.Globalization;

namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌模板（配置数据面，P0-1E 新增）：配表「人格牌配置」15 列全量落地后的强类型 POCO。
    /// 由 InitialPersonaCatalog.Configure 从 PersonaConfigAsset.Entry（string 原文）转换而来，
    /// 供后续 P0 玩法任务做运行时结算接入（Template → Definition 转换届时另建）。
    /// 与运行时 PersonaCardDefinition 分离：Definition 只有 Always/MinimumHandPriority 两种条件，
    /// 模板承载 12 种统计类条件 + 4 比较符 + 6 效果，两套枚举互不干扰。
    /// </summary>
    public sealed class PersonaCardTemplate
    {
        public PersonaCardTemplate(
            string personaId,
            string displayName,
            PersonaQuality quality,
            string qualityParam,
            string behaviorTagId,
            PersonaTriggerCondition triggerCondition,
            PersonaComparator comparator,
            int? conditionThreshold,
            ExtraConditionSpec extraCondition,
            string extraConditionRaw,
            PersonaEffectType effectType,
            decimal effectParam1,
            decimal effectParam2,
            string effectRaw,
            decimal? effectCap,
            bool independentSettlement)
        {
            // 人格牌_ID 是权威查询键（TryFind 的 key），必须非空
            if (string.IsNullOrWhiteSpace(personaId))
            {
                throw new ArgumentException("Persona id cannot be empty.", nameof(personaId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(PersonaQuality), quality))
            {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            if (!Enum.IsDefined(typeof(PersonaTriggerCondition), triggerCondition))
            {
                throw new ArgumentOutOfRangeException(nameof(triggerCondition));
            }

            if (!Enum.IsDefined(typeof(PersonaComparator), comparator))
            {
                throw new ArgumentOutOfRangeException(nameof(comparator));
            }

            if (!Enum.IsDefined(typeof(PersonaEffectType), effectType))
            {
                throw new ArgumentOutOfRangeException(nameof(effectType));
            }

            if (conditionThreshold.HasValue && conditionThreshold.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(conditionThreshold));
            }

            // 效果参数非负（沿用 PersonaCardDefinition effectValue ≥ 0 语义）
            if (effectParam1 < 0m || effectParam2 < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(effectParam1));
            }

            if (effectCap.HasValue && effectCap.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(effectCap));
            }

            PersonaId = personaId;
            DisplayName = displayName;
            Quality = quality;
            QualityParam = qualityParam;
            BehaviorTagId = behaviorTagId;
            TriggerCondition = triggerCondition;
            Comparator = comparator;
            ConditionThreshold = conditionThreshold;
            ExtraCondition = extraCondition;
            ExtraConditionRaw = extraConditionRaw;
            EffectType = effectType;
            EffectParam1 = effectParam1;
            EffectParam2 = effectParam2;
            EffectRaw = effectRaw;
            EffectCap = effectCap;
            IndependentSettlement = independentSettlement;
        }

        /// <summary>人格牌_ID（PER_xxx，权威查询键与美术绑定 ID）。</summary>
        public string PersonaId { get; }

        /// <summary>人格牌名称（当前配表为暂定占位）。</summary>
        public string DisplayName { get; }

        /// <summary>品质（基础/进阶/稀有/异质）。</summary>
        public PersonaQuality Quality { get; }

        /// <summary>品质参数原文（颜色名，语义待策划 A9）。</summary>
        public string QualityParam { get; }

        /// <summary>行为标签_ID（T01~T16）。</summary>
        public string BehaviorTagId { get; }

        /// <summary>触发条件。</summary>
        public PersonaTriggerCondition TriggerCondition { get; }

        /// <summary>比较符。</summary>
        public PersonaComparator Comparator { get; }

        /// <summary>条件阈值（null = 无）。</summary>
        public int? ConditionThreshold { get; }

        /// <summary>附加条件（结构化；null = 无或不可解析）。</summary>
        public ExtraConditionSpec ExtraCondition { get; }

        /// <summary>附加条件原文（无论可解析与否都保留，供日志与策划核对 A8）。</summary>
        public string ExtraConditionRaw { get; }

        /// <summary>效果类型。</summary>
        public PersonaEffectType EffectType { get; }

        /// <summary>效果参数1（decimal 精确值，如 PER_013 = 2.4500000000000002）。</summary>
        public decimal EffectParam1 { get; }

        /// <summary>效果参数2（空 = 0）。</summary>
        public decimal EffectParam2 { get; }

        /// <summary>效果列原文（预留）。</summary>
        public string EffectRaw { get; }

        /// <summary>效果上限（null = 无上限）。</summary>
        public decimal? EffectCap { get; }

        /// <summary>是否独立结算。</summary>
        public bool IndependentSettlement { get; }

        /// <summary>品质文本 → 枚举。只认契约 4 规范值（「特殊」兼容映射只在 Data 层 Mapper，防双处兼容漂移）。</summary>
        public static bool TryMapQuality(string text, out PersonaQuality value)
        {
            switch (text)
            {
                case "基础": value = PersonaQuality.Basic; return true;
                case "进阶": value = PersonaQuality.Advanced; return true;
                case "稀有": value = PersonaQuality.Rare; return true;
                case "异质": value = PersonaQuality.Mutant; return true;
                default: value = default; return false;
            }
        }

        /// <summary>触发条件文本 → 枚举。12 种条件固定映射（与 Data 层契约常量交叉校验防漂移）。</summary>
        public static bool TryMapTrigger(string text, out PersonaTriggerCondition value)
        {
            switch (text)
            {
                case "与上一手牌型相同": value = PersonaTriggerCondition.SameHandTypeAsPrevious; return true;
                case "计分牌数量": value = PersonaTriggerCondition.ScoringCardCount; return true;
                case "已使用弃牌次数": value = PersonaTriggerCondition.DiscardsUsed; return true;
                case "命中AI偏好": value = PersonaTriggerCondition.HitAiPreference; return true;
                case "剩余弃牌次数": value = PersonaTriggerCondition.DiscardsRemaining; return true;
                case "本局移除牌数量": value = PersonaTriggerCondition.CardsRemovedThisRun; return true;
                case "本局新增牌数量": value = PersonaTriggerCondition.CardsAddedThisRun; return true;
                case "连续使用相同牌型次数": value = PersonaTriggerCondition.SameHandTypeStreak; return true;
                case "牌库数量": value = PersonaTriggerCondition.DeckSize; return true;
                case "其他人格触发次数": value = PersonaTriggerCondition.OtherPersonaTriggerCount; return true;
                case "剩余出牌次数": value = PersonaTriggerCondition.PlaysRemaining; return true;
                case "人格触发次数": value = PersonaTriggerCondition.PersonaTriggerCount; return true;
                default: value = default; return false;
            }
        }

        /// <summary>比较符文本 → 枚举。等于/大于等于/小于/小于等于。</summary>
        public static bool TryMapComparator(string text, out PersonaComparator value)
        {
            switch (text)
            {
                case "等于": value = PersonaComparator.Equal; return true;
                case "大于等于": value = PersonaComparator.GreaterOrEqual; return true;
                case "小于": value = PersonaComparator.Less; return true;
                case "小于等于": value = PersonaComparator.LessOrEqual; return true;
                default: value = default; return false;
            }
        }

        /// <summary>效果文本 → 枚举。6 种效果固定映射。</summary>
        public static bool TryMapEffectType(string text, out PersonaEffectType value)
        {
            switch (text)
            {
                case "增加筹码": value = PersonaEffectType.AddChips; return true;
                case "增加倍率": value = PersonaEffectType.AddMultiplier; return true;
                case "增加筹码和倍率": value = PersonaEffectType.AddChipsAndMultiplier; return true;
                case "每单位增加倍率": value = PersonaEffectType.PerUnitMultiplier; return true;
                case "每单位增加筹码": value = PersonaEffectType.PerUnitChips; return true;
                case "最终倍率乘算": value = PersonaEffectType.MultiplyFinal; return true;
                default: value = default; return false;
            }
        }
    }
}
