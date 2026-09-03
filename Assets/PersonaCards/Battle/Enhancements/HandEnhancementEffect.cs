using System;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;

namespace PersonaCards.Battle.Enhancements
{
    /// <summary>
    /// 牌型强化计分效果（P0-11）：本手牌型命中强化等级 N 时，把牌型基础值提升为 HandUp 表内
    /// LvN 绝对底值——用差值增量实现（SetChips/SetMultiplier 为管线 internal，效果类只能加）。
    /// Phase = HeldAndGlobal、Order = 0（早于 Boss 鼓励效果），与人格/Boss 效果同管线加性叠加。
    /// </summary>
    public sealed class HandEnhancementEffect : IScoringEffect
    {
        private readonly EnhancementState _enhancements;

        public HandEnhancementEffect(EnhancementState enhancements)
        {
            _enhancements = enhancements ?? new EnhancementState();
        }

        public ScoringPhase Phase => ScoringPhase.HeldAndGlobal;

        public int Order => 0;

        public ScoringSourceType SourceType => ScoringSourceType.HeldOrGlobal;

        public string SourceId => "enhancement.hand";

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

            var level = _enhancements.HandLevelOf(evaluation.HandType);
            if (!EnhancementConfig.TryGetHandDelta(evaluation.HandType, level, out var chipDelta, out var multDelta))
            {
                return; // 未升级或表缺行：零效果
            }

            if (chipDelta != 0)
            {
                context.AddChips(chipDelta, "enhancement.hand_chips");
            }

            if (multDelta != 0m)
            {
                context.AddMultiplier(multDelta, "enhancement.hand_multiplier");
            }
        }
    }
}
