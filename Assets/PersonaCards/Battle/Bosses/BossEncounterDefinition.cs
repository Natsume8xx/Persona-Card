using System;

namespace PersonaCards.Battle.Bosses
{
    public sealed class BossEncounterDefinition
    {
        public BossEncounterDefinition(string encounterId, string displayName, string ruleId, string ruleName,
            string ruleDescription, string interventionId, string interventionName, string interventionDescription)
        {
            EncounterId = RequireText(encounterId, nameof(encounterId));
            DisplayName = RequireText(displayName, nameof(displayName));
            RuleId = RequireText(ruleId, nameof(ruleId));
            RuleName = RequireText(ruleName, nameof(ruleName));
            RuleDescription = RequireText(ruleDescription, nameof(ruleDescription));
            InterventionId = RequireText(interventionId, nameof(interventionId));
            InterventionName = RequireText(interventionName, nameof(interventionName));
            InterventionDescription = RequireText(interventionDescription, nameof(interventionDescription));
        }

        public string EncounterId { get; }
        public string DisplayName { get; }
        public string RuleId { get; }
        public string RuleName { get; }
        public string RuleDescription { get; }
        public string InterventionId { get; }
        public string InterventionName { get; }
        public string InterventionDescription { get; }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A value is required.", parameterName);
            return value;
        }
    }
}
