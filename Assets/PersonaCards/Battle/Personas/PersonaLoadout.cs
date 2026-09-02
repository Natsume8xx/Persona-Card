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

        public IReadOnlyList<IScoringEffect> CreateScoringEffects(Func<int> handsPlayed = null)
        {
            return _slots
                .Where(slot => !slot.IsEmpty)
                .Select(slot => (IScoringEffect)new PersonaScoringEffect(slot, handsPlayed))
                .ToArray();
        }

        /// <summary>
        /// 派生：把指定槽位标记为禁用（P0-5 封印框架）。槽号越界抛错；禁用空槽无害；重复槽号幂等。
        /// 随机性（封印哪几个槽）由调用方决定，本方法只负责应用禁用集合。
        /// </summary>
        public PersonaLoadout WithDisabledSlots(IReadOnlyCollection<int> slotIndices)
        {
            if (slotIndices == null)
            {
                throw new ArgumentNullException(nameof(slotIndices));
            }

            var disabled = new HashSet<int>(slotIndices);
            if (disabled.Any(index => index < 0 || index >= SlotCount))
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndices), "禁用槽号必须在 0~3 之间。");
            }

            return new PersonaLoadout(_slots.Select(slot =>
                disabled.Contains(slot.SlotIndex) ? slot.WithDisabled(true) : slot));
        }
    }
}
