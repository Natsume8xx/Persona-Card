using System;
using PersonaCards.Data;

namespace PersonaCards.Battle.Bosses
{
    public sealed class BossEncounterDefinition
    {
        public BossEncounterDefinition(string encounterId, string displayName, BossPoolId poolId,
            string ruleId, string ruleName,
            string ruleDescription, string interventionId, string interventionName, string interventionDescription,
            string revealLine = "")
        {
            EncounterId = RequireText(encounterId, nameof(encounterId));
            DisplayName = RequireText(displayName, nameof(displayName));
            if (!Enum.IsDefined(typeof(BossPoolId), poolId) || poolId == BossPoolId.None)
                throw new ArgumentOutOfRangeException(nameof(poolId), poolId, "Boss 定义必须归属一个难度池（None 仅为普通战哨兵值）。");
            PoolId = poolId;
            RuleId = RequireText(ruleId, nameof(ruleId));
            RuleName = RequireText(ruleName, nameof(ruleName));
            RuleDescription = RequireText(ruleDescription, nameof(ruleDescription));
            InterventionId = RequireText(interventionId, nameof(interventionId));
            InterventionName = RequireText(interventionName, nameof(interventionName));
            InterventionDescription = RequireText(interventionDescription, nameof(interventionDescription));
            RevealLine = revealLine; // P0-9 揭示/观察者台词：允许为空（无台词不显示）
        }

        public string EncounterId { get; }
        public string DisplayName { get; }
        /// <summary>所属难度池（P0-3 目录按池过滤抽取；None 为非法归属，仅路线普通战节点使用）。</summary>
        public BossPoolId PoolId { get; }
        public string RuleId { get; }
        public string RuleName { get; }
        public string RuleDescription { get; }
        public string InterventionId { get; }
        public string InterventionName { get; }
        public string InterventionDescription { get; }
        /// <summary>揭示/观察者台词（P0-9 数据驱动：揭示屏 Boss Line 与战斗屏 Observer Line 共用；空 = 无台词）。</summary>
        public string RevealLine { get; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A value is required.", parameterName);
            return value;
        }
    }
}
