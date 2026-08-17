using PersonaCards.Cards.Hands;

namespace PersonaCards.Battle.Bosses
{
    public sealed class BossEncounterSnapshot
    {
        public BossEncounterSnapshot(string encounterId, int handsPlayed, HandType? previousHandType)
        {
            EncounterId = encounterId;
            HandsPlayed = handsPlayed;
            PreviousHandType = previousHandType;
        }

        public string EncounterId { get; }
        public int HandsPlayed { get; }
        public HandType? PreviousHandType { get; }
    }
}
