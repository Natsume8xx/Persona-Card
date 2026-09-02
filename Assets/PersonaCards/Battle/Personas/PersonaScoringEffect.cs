using System;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core;

namespace PersonaCards.Battle.Personas
{
    public sealed class PersonaScoringEffect : IScoringEffect
    {
        private readonly PersonaSlot _slot;
        private readonly Func<int> _handsPlayed;

        public PersonaScoringEffect(PersonaSlot slot, Func<int> handsPlayed = null)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            if (slot.IsEmpty)
            {
                throw new ArgumentException("An empty persona slot has no scoring effect.", nameof(slot));
            }

            _handsPlayed = handsPlayed ?? (() => 0);
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
                    context.AddChips(_slot.Definition.EffectValue, "persona.add_chips");
                    break;
                case PersonaEffectKind.AddMultiplier:
                    context.AddMultiplier(_slot.Definition.EffectValue, "persona.add_multiplier");
                    break;
                case PersonaEffectKind.MultiplyFinal:
                    context.MultiplyFinal(_slot.Definition.EffectValue, "persona.multiply_final");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported persona effect {_slot.Definition.EffectKind}.");
            }
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
