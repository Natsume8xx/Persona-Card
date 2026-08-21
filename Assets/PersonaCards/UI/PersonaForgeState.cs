using System;
using System.Collections.Generic;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards.Hands;
using PersonaCards.Core.Random;
using PersonaCards.Core;

namespace PersonaCards.UI
{
    public sealed class PersonaForgeState
    {
        public PersonaForgeState(RunBehaviorReport report, uint seed)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var rng = new XorShift32Rng(seed);
            Rolls = Array.AsReadOnly(new[] { Roll(rng), Roll(rng), Roll(rng) });
            Candidates = Array.AsReadOnly(new[]
            {
                Candidate("映照", "洞察者", PersonaEffectKind.AddChips, 10m + Rolls[0], report.DominantHand),
                Candidate("偏转", "调律者", PersonaEffectKind.AddMultiplier, 1m + Rolls[1] / 10m, report.DominantHand),
                Candidate("裂变", "破局者", PersonaEffectKind.MultiplyFinal, 1m + Rolls[2] / 100m, report.DominantHand)
            });
        }

        public IReadOnlyList<int> Rolls { get; }
        public IReadOnlyList<PersonaCardDefinition> Candidates { get; }

        private static int Roll(ISeededRng rng) => rng.NextInt(20) + 1;

        private static PersonaCardDefinition Candidate(string mode, string name, PersonaEffectKind effect,
            decimal value, HandType handType)
        {
            return new PersonaCardDefinition($"persona.forge.{mode}.{name}", $"{mode}·{name}",
                PersonaConditionKind.MinimumHandPriority, handType, effect, value);
        }
    }
}
