using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;

namespace PersonaCards.UI
{
    public sealed class RunBehaviorTracker
    {
        private readonly Dictionary<HandType, int> _handCounts = new Dictionary<HandType, int>();

        public int Plays { get; private set; }
        public int Discards { get; private set; }
        public int CardsPlayed { get; private set; }
        public int CardsDiscarded { get; private set; }
        public long Score { get; private set; }
        public IReadOnlyDictionary<HandType, int> HandCounts => _handCounts;

        public void RecordPlay(HandType handType, int cardCount, long score)
        {
            Plays++;
            CardsPlayed += cardCount;
            Score += score;
            _handCounts[handType] = _handCounts.TryGetValue(handType, out var count) ? count + 1 : 1;
        }

        public void RecordDiscard(int cardCount)
        {
            Discards++;
            CardsDiscarded += cardCount;
        }

        public void Restore(int plays, int discards, int cardsPlayed, int cardsDiscarded, long score,
            IEnumerable<KeyValuePair<HandType, int>> handCounts)
        {
            Plays = Math.Max(0, plays);
            Discards = Math.Max(0, discards);
            CardsPlayed = Math.Max(0, cardsPlayed);
            CardsDiscarded = Math.Max(0, cardsDiscarded);
            Score = Math.Max(0L, score);
            _handCounts.Clear();
            if (handCounts == null) return;
            foreach (var pair in handCounts.Where(pair => pair.Value > 0)) _handCounts[pair.Key] = pair.Value;
        }

        public RunBehaviorReport CreateReport()
        {
            var dominantHand = _handCounts.Count == 0
                ? HandType.HighCard
                : _handCounts.OrderByDescending(pair => pair.Value).ThenByDescending(pair => (int)pair.Key).First().Key;
            var focus = Plays == 0 ? 0 : (int)Math.Round(100d * _handCounts.GetValueOrDefault(dominantHand) / Plays);
            var restraint = Plays + Discards == 0 ? 0 : (int)Math.Round(100d * Discards / (Plays + Discards));
            var efficiency = Plays == 0 ? 0 : (int)Math.Min(100L, Score / Plays / 5L);
            var title = restraint >= 40 ? "谨慎的筛选者" : efficiency >= 70 ? "果断的执行者" : "稳定的积累者";
            return new RunBehaviorReport(title, dominantHand, focus, restraint, efficiency, Plays, Discards, Score);
        }
    }

    public sealed class RunBehaviorReport
    {
        public RunBehaviorReport(string title, HandType dominantHand, int focus, int restraint, int efficiency,
            int plays, int discards, long score)
        {
            Title = title;
            DominantHand = dominantHand;
            Focus = focus;
            Restraint = restraint;
            Efficiency = efficiency;
            Plays = plays;
            Discards = discards;
            Score = score;
        }

        public string Title { get; }
        public HandType DominantHand { get; }
        public int Focus { get; }
        public int Restraint { get; }
        public int Efficiency { get; }
        public int Plays { get; }
        public int Discards { get; }
        public long Score { get; }
    }
}
