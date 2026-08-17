using System;

namespace PersonaCards.Cards
{
    public static class PlayingCardRules
    {
        public static int GetFaceChipValue(Rank rank)
        {
            if (!Enum.IsDefined(typeof(Rank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            }

            switch (rank)
            {
                case Rank.Jack:
                case Rank.Queen:
                case Rank.King:
                    return 10;
                case Rank.Ace:
                    return 11;
                default:
                    return (int)rank;
            }
        }
    }
}
