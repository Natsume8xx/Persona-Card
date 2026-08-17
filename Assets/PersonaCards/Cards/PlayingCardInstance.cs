using System;

namespace PersonaCards.Cards
{
    [Serializable]
    public sealed class PlayingCardInstance
    {
        public PlayingCardInstance(
            string id,
            Suit suit,
            Rank rank,
            CardEnhancement enhancement = CardEnhancement.None)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id cannot be empty.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(Suit), suit))
            {
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");
            }

            if (!Enum.IsDefined(typeof(Rank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            }

            if (!Enum.IsDefined(typeof(CardEnhancement), enhancement))
            {
                throw new ArgumentOutOfRangeException(nameof(enhancement), enhancement, "Unknown enhancement.");
            }

            Id = id;
            Suit = suit;
            Rank = rank;
            Enhancement = enhancement;
        }

        public string Id { get; }

        public Suit Suit { get; }

        public Rank Rank { get; }

        public CardEnhancement Enhancement { get; }

        public int FaceChipValue => PlayingCardRules.GetFaceChipValue(Rank);
    }
}
