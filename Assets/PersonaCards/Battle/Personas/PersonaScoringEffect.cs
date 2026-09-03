using System;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Battle.Personas
{
    public sealed class PersonaScoringEffect : IScoringEffect
    {
        private readonly PersonaSlot _slot;
        private readonly Func<int> _handsPlayed;
        private readonly int _level;

        public PersonaScoringEffect(PersonaSlot slot, Func<int> handsPlayed = null, int level = 0)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            if (slot.IsEmpty)
            {
                throw new ArgumentException("An empty persona slot has no scoring effect.", nameof(slot));
            }

            if (level < 0 || level > EnhancementState.PersonaMaxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Persona enhancement level must be within 0..4.");
            }

            _handsPlayed = handsPlayed ?? (() => 0);
            _level = level; // P0-11 人格强化：按等级放大效果值（Lv0 = 旧行为，零差异）
        }

        public ScoringPhase Phase => ScoringPhase.Persona;
        public int Order => _slot.SlotIndex;
        public ScoringSourceType SourceType => ScoringSourceType.Persona;
        public string SourceId => _slot.Definition.TemplateId;

        public void Apply(ScoringContext context, HandEvaluationResult evaluation)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (evaluation == null)
            {
                throw new ArgumentNullException(nameof(evaluation));
            }

            if (_slot.IsDisabled)
            {
                context.Skip("persona.disabled");
                return;
            }

            if (_slot.Definition.DelayHands > 0 && _handsPlayed() < _slot.Definition.DelayHands)
            {
                context.Skip("persona.delayed"); // P0-5 人格延迟：已出手数不足时不生效
                return;
            }

            if (!ConditionMatches(evaluation))
            {
                context.Skip("persona.condition_not_met");
                return;
            }

            switch (_slot.Definition.EffectKind)
            {
                case PersonaEffectKind.AddChips:
                    context.AddChips(UpgradedValue(), "persona.add_chips");
                    break;
                case PersonaEffectKind.AddMultiplier:
                    context.AddMultiplier(UpgradedValue(), "persona.add_multiplier");
                    break;
                case PersonaEffectKind.MultiplyFinal:
                    context.MultiplyFinal(UpgradedValue(), "persona.multiply_final");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported persona effect {_slot.Definition.EffectKind}.");
            }
        }

        /// <summary>P0-11 人格强化：效果值 = Lv0 值 + 每级增量 × 等级（增量由 EnhancementConfig 按效果类型查表）。</summary>
        private decimal UpgradedValue()
        {
            return _slot.Definition.EffectValue
                   + EnhancementConfig.PersonaPerLevelIncreaseOf(_slot.Definition.EffectKind) * _level;
        }

        private bool ConditionMatches(HandEvaluationResult evaluation)
        {
            switch (_slot.Definition.ConditionKind)
            {
                case PersonaConditionKind.Always:
                    return true;
                case PersonaConditionKind.MinimumHandPriority:
                    return evaluation.Priority >= (int)_slot.Definition.MinimumHandType;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported persona condition {_slot.Definition.ConditionKind}.");
            }
        }
    }
}
