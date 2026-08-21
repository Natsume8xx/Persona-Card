using System;

namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌附加条件（结构化，P0-1E 新增）：PER_015「剩余弃牌次数=0」这类可解析的附加条件
    /// 由 Data 层 Mapper 拆成三字段，门面 Configure 时组装成本 POCO。
    /// 不可解析的附加条件（如 PER_013 带星号未定稿）不产生本对象，原文存在 ExtraConditionRaw。
    /// </summary>
    public sealed class ExtraConditionSpec
    {
        public ExtraConditionSpec(
            PersonaTriggerCondition triggerCondition,
            PersonaComparator comparator,
            int threshold)
        {
            if (!Enum.IsDefined(typeof(PersonaTriggerCondition), triggerCondition))
            {
                throw new ArgumentOutOfRangeException(nameof(triggerCondition));
            }

            if (!Enum.IsDefined(typeof(PersonaComparator), comparator))
            {
                throw new ArgumentOutOfRangeException(nameof(comparator));
            }

            if (threshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }

            TriggerCondition = triggerCondition;
            Comparator = comparator;
            Threshold = threshold;
        }

        /// <summary>附加条件的触发条件。</summary>
        public PersonaTriggerCondition TriggerCondition { get; }

        /// <summary>附加条件的比较符。</summary>
        public PersonaComparator Comparator { get; }

        /// <summary>附加条件的阈值（非负整数）。</summary>
        public int Threshold { get; }
    }
}
