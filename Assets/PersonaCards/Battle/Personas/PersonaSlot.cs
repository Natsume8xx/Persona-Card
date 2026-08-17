using System;

namespace PersonaCards.Battle.Personas
{
    public sealed class PersonaSlot
    {
        public PersonaSlot(int slotIndex, PersonaCardDefinition definition, bool isDisabled = false)
        {
            if (slotIndex < 0 || slotIndex >= PersonaLoadout.SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            SlotIndex = slotIndex;
            Definition = definition;
            IsDisabled = isDisabled;
        }

        public int SlotIndex { get; }
        public PersonaCardDefinition Definition { get; }
        public bool IsDisabled { get; }
        public bool IsEmpty => Definition == null;

        public PersonaSlot WithDisabled(bool isDisabled)
        {
            return new PersonaSlot(SlotIndex, Definition, isDisabled);
        }
    }
}
