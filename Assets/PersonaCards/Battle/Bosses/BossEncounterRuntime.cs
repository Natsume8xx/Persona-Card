using System;
using System.Collections.Generic;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Battle.Bosses
{
    public sealed class BossEncounterRuntime
    {
        public BossEncounterRuntime(BossEncounterDefinition definition, int handsPlayed = 0,
            HandType? previousHandType = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (handsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(handsPlayed));
            if (handsPlayed == 0 && previousHandType.HasValue)
                throw new ArgumentException("A previous hand requires at least one committed hand.", nameof(previousHandType));
            if (handsPlayed > 0 && !previousHandType.HasValue)
                throw new ArgumentException("Committed boss hands require a previous hand type.", nameof(previousHandType));
            if (previousHandType.HasValue && !Enum.IsDefined(typeof(HandType), previousHandType.Value))
                throw new ArgumentOutOfRangeException(nameof(previousHandType));

            HandsPlayed = handsPlayed;
            PreviousHandType = previousHandType;
        }

        public BossEncounterDefinition Definition { get; }
        public int HandsPlayed { get; private set; }
        public HandType? PreviousHandType { get; private set; }

        public IReadOnlyList<IScoringEffect> CreateScoringEffects()
        {
            return new IScoringEffect[]
            {
                new FirstHandEncouragementEffect(this),
                new RepeatedJudgmentEffect(this)
            };
        }

        public void CommitHand(HandType handType)
        {
            if (!Enum.IsDefined(typeof(HandType), handType)) throw new ArgumentOutOfRangeException(nameof(handType));
            PreviousHandType = handType;
            HandsPlayed++;
        }

        public BossEncounterSnapshot CreateSnapshot()
        {
            return new BossEncounterSnapshot(Definition.EncounterId, HandsPlayed, PreviousHandType);
        }

        private sealed class FirstHandEncouragementEffect : IScoringEffect
        {
            private readonly BossEncounterRuntime _runtime;
            public FirstHandEncouragementEffect(BossEncounterRuntime runtime) { _runtime = runtime; }
            public ScoringPhase Phase => ScoringPhase.HeldAndGlobal;
            public int Order => 100;
            public ScoringSourceType SourceType => ScoringSourceType.Boss;
            public string SourceId => _runtime.Definition.InterventionId;

            public void Apply(ScoringContext context, HandEvaluationResult evaluation)
            {
                if (_runtime.HandsPlayed == 0) context.AddChips(30m, "boss.first_hand_encouragement");
                else context.Skip("boss.intervention_expired");
            }
        }

        private sealed class RepeatedJudgmentEffect : IScoringEffect
        {
            private readonly BossEncounterRuntime _runtime;
            public RepeatedJudgmentEffect(BossEncounterRuntime runtime) { _runtime = runtime; }
            public ScoringPhase Phase => ScoringPhase.BossFinal;
            public int Order => 0;
            public ScoringSourceType SourceType => ScoringSourceType.Boss;
            public string SourceId => _runtime.Definition.RuleId;

            public void Apply(ScoringContext context, HandEvaluationResult evaluation)
            {
                if (_runtime.PreviousHandType == evaluation.HandType)
                    context.MultiplyFinal(0.60m, "boss.repeated_judgment");
                else
                    context.Skip("boss.hand_type_not_repeated");
            }
        }
    }
}
