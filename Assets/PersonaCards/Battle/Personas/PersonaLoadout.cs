using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Battle.Personas
{
    public sealed class PersonaLoadout
    {
        public const int SlotCount = 4;
        private readonly IReadOnlyList<PersonaSlot> _slots;

        public PersonaLoadout(IEnumerable<PersonaSlot> slots)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var ordered = slots.OrderBy(slot => slot?.SlotIndex ?? -1).ToArray();
            if (ordered.Length != SlotCount || ordered.Any(slot => slot == null))
            {
                throw new ArgumentException($"A loadout must contain exactly {SlotCount} slots.", nameof(slots));
            }

            for (var index = 0; index < SlotCount; index++)
            {
                if (ordered[index].SlotIndex != index)
                {
                    throw new ArgumentException("Loadout slots must be uniquely numbered 0 through 3.", nameof(slots));
                }
            }

            _slots = Array.AsReadOnly(ordered);
        }

        public IReadOnlyList<PersonaSlot> Slots => _slots;

        public IReadOnlyList<IScoringEffect> CreateScoringEffects()
        {
            return _slots
                .Where(slot => !slot.IsEmpty)
                .Select(slot => (IScoringEffect)new PersonaScoringEffect(slot))
                .ToArray();
        }
    }
}
