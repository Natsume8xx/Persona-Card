using System;
using System.Collections.Generic;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Battle.Enhancements
{
    /// <summary>
    /// 花色强化计分效果（P0-11）：对已打出的计分牌，按其花色当前等级的额外筹码逐张加筹码
    /// （例：黑桃 Lv3 → 每张计分黑桃 +15）。多花色同时生效；未升级花色零值跳过。
    /// Phase = ScoringCards（自定义阶段白名单内，执行在卡面硬编码之后——加法无顺序依赖）。
    /// </summary>
    public sealed class SuitEnhancementEffect : IScoringEffect
    {
        private readonly EnhancementState _enhancements;

        public SuitEnhancementEffect(EnhancementState enhancements)
        {
            _enhancements = enhancements ?? new EnhancementState();
        }

        public ScoringPhase Phase => ScoringPhase.ScoringCards;

        public int Order => 50;

        public ScoringSourceType SourceType => ScoringSourceType.HeldOrGlobal;

        public string SourceId => "enhancement.suit";

        public void Apply(ScoringContext context, HandEvaluationResult evaluation)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (evaluation == null)
            {
                throw new ArgumentNullException(nameof(evaluation));
            }

            var scoringIds = new HashSet<string>(evaluation.ScoringCardIds, StringComparer.Ordinal);
            foreach (var card in evaluation.PlayedCards)
            {
                if (!scoringIds.Contains(card.Id)) continue;
                var chips = EnhancementConfig.SuitChipsOf(card.Suit, _enhancements.SuitLevelOf(card.Suit));
                if (chips <= 0) continue;
                context.AddChips(chips, "enhancement.suit_chips");
            }
        }
    }
}
