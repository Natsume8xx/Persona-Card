using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Battle.Personas;

namespace PersonaCards.UI
{
    public sealed class PersonaLoadoutState
    {
        private static readonly PersonaCardDefinition[] Options =
        {
            InitialPersonaCatalog.Accumulator,
            InitialPersonaCatalog.Executor,
            InitialPersonaCatalog.Ambitious,
            null
        };

        private readonly PersonaCardDefinition[] _slots;

        public PersonaLoadoutState() : this(Options) { }

        public PersonaLoadoutState(IEnumerable<PersonaCardDefinition> slots)
        {
            _slots = (slots ?? throw new ArgumentNullException(nameof(slots))).ToArray();
            if (_slots.Length != PersonaLoadout.SlotCount)
                throw new ArgumentException("Persona loadout must contain four slots.", nameof(slots));
        }

        public IReadOnlyList<PersonaCardDefinition> Slots => Array.AsReadOnly(_slots);

        public void CycleSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PersonaLoadout.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            var nextSlotIndex = (slotIndex + 1) % PersonaLoadout.SlotCount;
            (_slots[slotIndex], _slots[nextSlotIndex]) = (_slots[nextSlotIndex], _slots[slotIndex]);
        }

        public PersonaLoadout CreateLoadout()
        {
            return new PersonaLoadout(_slots.Select((definition, index) => new PersonaSlot(index, definition)));
        }

        public int Equip(PersonaCardDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var slotIndex = Array.IndexOf(_slots, null);
            if (slotIndex < 0) return -1;
            EquipAt(definition, slotIndex);
            return slotIndex;
        }

        public void EquipAt(PersonaCardDefinition definition, int slotIndex)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ValidateSlotIndex(slotIndex);

            var equippedIndex = Array.FindIndex(_slots,
                item => item != null && item.TemplateId == definition.TemplateId);
            if (equippedIndex >= 0 && equippedIndex != slotIndex)
            {
                (_slots[equippedIndex], _slots[slotIndex]) = (_slots[slotIndex], _slots[equippedIndex]);
                return;
            }

            _slots[slotIndex] = definition;
        }

        public void Unequip(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            _slots[slotIndex] = null;
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PersonaLoadout.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}
