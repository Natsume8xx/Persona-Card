using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;

namespace PersonaCards.Cards.Scoring
{
    // The same pure pipeline is used by score preview and committed play resolution.
    public sealed class ScoringPipeline
    {
        private static readonly ScoringPhase[] EffectPhases =
        {
            ScoringPhase.HeldAndGlobal,
            ScoringPhase.Persona,
            ScoringPhase.BossFinal
        };

        private readonly HandEvaluator _handEvaluator;

        public ScoringPipeline(HandEvaluator handEvaluator = null)
        {
            _handEvaluator = handEvaluator ?? new HandEvaluator();
        }

        public ScoringResult Score(
            IEnumerable<PlayingCardInstance> playedCards,
            IEnumerable<IScoringEffect> effects = null)
        {
            if (playedCards == null)
            {
                throw new ArgumentNullException(nameof(playedCards));
            }

            var cards = playedCards.ToList();
            var evaluation = _handEvaluator.Evaluate(cards);
            var orderedEffects = ValidateAndOrderEffects(effects).ToList();
            var context = new ScoringContext();

            InitializeHand(context, evaluation);
            ResolveScoringCards(context, cards, evaluation);

            foreach (var phase in EffectPhases)
            {
                ResolveEffects(context, orderedEffects, phase, evaluation);
            }

            context.BeginSource(ScoringPhase.FinalCalculation, ScoringSourceType.System, "final-calculation");
            context.CalculateRawScore();
            context.BeginSource(ScoringPhase.ScoreCommit, ScoringSourceType.System, "score-commit");
            context.CommitScore();

            return new ScoringResult(context);
        }

        private static void InitializeHand(ScoringContext context, HandEvaluationResult evaluation)
        {
            var sourceId = evaluation.HandType.ToString();
            context.BeginSource(ScoringPhase.HandInitialization, ScoringSourceType.HandType, sourceId);
            context.SetChips(evaluation.BaseChips, "hand.base_chips");
            context.SetMultiplier(evaluation.BaseMultiplier, "hand.base_multiplier");
        }

        private static void ResolveScoringCards(
            ScoringContext context,
            IEnumerable<PlayingCardInstance> cards,
            HandEvaluationResult evaluation)
        {
            var scoringIds = new HashSet<string>(evaluation.ScoringCardIds, StringComparer.Ordinal);

            foreach (var card in cards.Where(card => scoringIds.Contains(card.Id)))
            {
                context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.PlayingCard, card.Id);
                context.AddChips(card.FaceChipValue, "card.face_chips");

                switch (card.Enhancement)
                {
                    case CardEnhancement.ChipBoost:
                        context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.CardEnhancement, card.Id);
                        context.AddChips(20m, "enhancement.chip_boost");
                        break;
                    case CardEnhancement.MultBoost:
                        context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.CardEnhancement, card.Id);
                        context.AddMultiplier(3m, "enhancement.mult_boost");
                        break;
                }
            }
        }

        private static IEnumerable<IScoringEffect> ValidateAndOrderEffects(IEnumerable<IScoringEffect> effects)
        {
            if (effects == null)
            {
                return Array.Empty<IScoringEffect>();
            }

            var indexed = effects.Select((effect, index) => new { Effect = effect, Index = index }).ToList();
            if (indexed.Any(item => item.Effect == null))
            {
                throw new ArgumentException("Scoring effects cannot contain null.", nameof(effects));
            }

            foreach (var item in indexed)
            {
                if (!EffectPhases.Contains(item.Effect.Phase))
                {
                    throw new ArgumentException(
                        $"Custom scoring effect phase {item.Effect.Phase} is reserved by the pipeline.",
                        nameof(effects));
                }
            }

            return indexed
                .OrderBy(item => item.Effect.Phase)
                .ThenBy(item => item.Effect.Order)
                .ThenBy(item => item.Index)
                .Select(item => item.Effect)
                .ToArray();
        }

        private static void ResolveEffects(
            ScoringContext context,
            IEnumerable<IScoringEffect> effects,
            ScoringPhase phase,
            HandEvaluationResult evaluation)
        {
            foreach (var effect in effects.Where(effect => effect.Phase == phase))
            {
                context.BeginSource(phase, effect.SourceType, effect.SourceId);
                effect.Apply(context, evaluation);
            }
        }
    }
}
