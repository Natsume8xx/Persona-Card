using System;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;

namespace PersonaCards.Battle.Personas
{
    public sealed class PersonaCardDefinition
    {
        public PersonaCardDefinition(
            string templateId,
            string displayName,
            PersonaConditionKind conditionKind,
            HandType minimumHandType,
            PersonaEffectKind effectKind,
            decimal effectValue,
            int delayHands = 0)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException("Template id cannot be empty.", nameof(templateId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(PersonaConditionKind), conditionKind))
            {
                throw new ArgumentOutOfRangeException(nameof(conditionKind));
            }

            if (!Enum.IsDefined(typeof(PersonaEffectKind), effectKind))
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            if (!Enum.IsDefined(typeof(HandType), minimumHandType))
            {
                throw new ArgumentOutOfRangeException(nameof(minimumHandType));
            }

            if (effectValue < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(effectValue));
            }

            if (delayHands < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayHands));
            }

            TemplateId = templateId;
            DisplayName = displayName;
            ConditionKind = conditionKind;
            MinimumHandType = minimumHandType;
            EffectKind = effectKind;
            EffectValue = effectValue;
            DelayHands = delayHands;
        }

        public string TemplateId { get; }
        public string DisplayName { get; }
        public PersonaConditionKind ConditionKind { get; }
        public HandType MinimumHandType { get; }
        public PersonaEffectKind EffectKind { get; }
        public decimal EffectValue { get; }
        /// <summary>生效延迟手数（P0-5 人格延迟框架）：0 = 立即生效；N = 已出手数 ≥ N 后生效。</summary>
        public int DelayHands { get; }
    }
}
