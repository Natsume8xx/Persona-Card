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
            ScoringPhase.ScoringCards, // P0-4：为花色审查/沉默/试探等卡面级词条开阶段口子（自定义效果按序插在卡面硬编码之后）
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
                    case CardEnhancement.ChipPlus: // UI 重排第二批：筹码强化 +5
                        context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.CardEnhancement, card.Id);
                        context.AddChips(5m, "enhancement.chip_plus");
                        break;
                    case CardEnhancement.MultPlus: // UI 重排第二批：倍率强化 +0.5
                        context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.CardEnhancement, card.Id);
                        context.AddMultiplier(0.5m, "enhancement.mult_plus");
                        break;
                    case CardEnhancement.IndependentMult: // UI 重排第二批：独立乘区强化，最终得分 ×1.03（多张叠乘）
                        context.BeginSource(ScoringPhase.ScoringCards, ScoringSourceType.CardEnhancement, card.Id);
                        context.MultiplyFinal(1.03m, "enhancement.independent_mult");
                        break;
                    // CoinBonus（金币强化）计分无效果：收益在胜利结算按牌库张数入账，见 JourneyDeckState.CoinBonusIncome
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
