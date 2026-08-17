using System;
using System.Collections.Generic;

namespace PersonaCards.Cards.Scoring
{
    public sealed class ScoringContext
    {
        private readonly List<ScoringEvent> _events = new List<ScoringEvent>();
        private ScoringPhase _phase;
        private ScoringSourceType _sourceType;
        private string _sourceId;

        public decimal Chips { get; private set; }
        public decimal Multiplier { get; private set; }
        public decimal FinalMultiplier { get; private set; } = 1m;
        public decimal RawScore { get; private set; }
        public long FinalScore { get; private set; }
        public IReadOnlyList<ScoringEvent> Events => _events.AsReadOnly();

        public void AddChips(decimal value, string displayTextKey)
        {
            var before = Chips;
            Chips += value;
            Record(ScoringOperation.AddChips, value, before, Chips, displayTextKey);
        }

        public void AddMultiplier(decimal value, string displayTextKey)
        {
            var before = Multiplier;
            Multiplier += value;
            Record(ScoringOperation.AddMultiplier, value, before, Multiplier, displayTextKey);
        }

        public void MultiplyFinal(decimal value, string displayTextKey)
        {
            if (value < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Final multiplier cannot be negative.");
            }

            var before = FinalMultiplier;
            FinalMultiplier *= value;
            Record(ScoringOperation.MultiplyFinal, value, before, FinalMultiplier, displayTextKey);
        }

        public void Skip(string displayTextKey)
        {
            Record(ScoringOperation.Skip, 0m, 0m, 0m, displayTextKey);
        }

        internal void BeginSource(ScoringPhase phase, ScoringSourceType sourceType, string sourceId)
        {
            _phase = phase;
            _sourceType = sourceType;
            _sourceId = sourceId ?? string.Empty;
        }

        internal void SetChips(decimal value, string displayTextKey)
        {
            var before = Chips;
            Chips = value;
            Record(ScoringOperation.SetChips, value, before, Chips, displayTextKey);
        }

        internal void SetMultiplier(decimal value, string displayTextKey)
        {
            var before = Multiplier;
            Multiplier = value;
            Record(ScoringOperation.SetMultiplier, value, before, Multiplier, displayTextKey);
        }

        internal void CalculateRawScore()
        {
            var before = RawScore;
            RawScore = Chips * Multiplier * FinalMultiplier;
            Record(ScoringOperation.CalculateRawScore, RawScore, before, RawScore, "score.raw");
        }

        internal void CommitScore()
        {
            var rounded = decimal.Round(RawScore, 0, MidpointRounding.AwayFromZero);
            var clamped = Math.Max(1m, rounded);
            FinalScore = decimal.ToInt64(clamped);
            Record(ScoringOperation.RoundAndClamp, 1m, RawScore, FinalScore, "score.commit");
        }

        private void Record(
            ScoringOperation operation,
            decimal value,
            decimal before,
            decimal after,
            string displayTextKey)
        {
            _events.Add(new ScoringEvent(
                _phase,
                _sourceType,
                _sourceId,
                operation,
                value,
                before,
                after,
                displayTextKey ?? string.Empty));
        }
    }
}
