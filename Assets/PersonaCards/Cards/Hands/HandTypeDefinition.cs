using System;

namespace PersonaCards.Cards.Hands
{
    public sealed class HandTypeDefinition
    {
        public HandTypeDefinition(
            HandType handType,
            string displayName,
            int baseChips,
            int baseMultiplier)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (baseChips < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseChips));
            }

            if (baseMultiplier < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMultiplier));
            }

            HandType = handType;
            DisplayName = displayName;
            Priority = (int)handType;
            BaseChips = baseChips;
            BaseMultiplier = baseMultiplier;
        }

        public HandType HandType { get; }

        public string DisplayName { get; }

        public int Priority { get; }

        public int BaseChips { get; }

        public int BaseMultiplier { get; }
    }
}
