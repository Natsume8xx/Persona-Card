using System;
using PersonaCards.Data;

namespace PersonaCards.Battle.Bosses
{
    public static class BossEncounterCatalog
    {
        public const string MirrorKeeperEncounterId = "boss.mirror_keeper.final";
        public const string RepeatedJudgmentRuleId = "boss.rule.repeated_judgment";
        public const string FirstHandEncouragementId = "boss.intervention.first_hand_encouragement";

        private static readonly BossEncounterDefinition MirrorKeeper = new BossEncounterDefinition(
            MirrorKeeperEncounterId,
            "镜厅守门人",
            RepeatedJudgmentRuleId,
            "重复审判",
            "本手牌型与上一手相同，最终得分 ×0.60。",
            FirstHandEncouragementId,
            "先手鼓励",
            "第一手结算时获得 +30 筹码。");

        public static BossEncounterRuntime CreateMirrorKeeper()
        {
            return new BossEncounterRuntime(MirrorKeeper);
        }

        /// <summary>按难度池创建 Boss 遭遇（P0-3 前临时实现：任意池一律返回镜厅守门人；本程序集为无引擎依赖的纯逻辑层，日志由 UI 层调用方负责）。</summary>
        /// <param name="poolId">难度池 id，来自路线表节点配置。</param>
        public static BossEncounterRuntime CreateFromPool(BossPoolId poolId)
        {
            // TODO(P0-3)：按 Primary/Intermediate/Advanced 三池落地 Boss 定义与规则抽取
            return CreateMirrorKeeper();
        }

        public static BossEncounterRuntime Restore(BossEncounterSnapshot snapshot)
        {
            if (snapshot == null) return null;
            if (!string.Equals(snapshot.EncounterId, MirrorKeeperEncounterId, StringComparison.Ordinal))
                throw new ArgumentException($"Unknown boss encounter '{snapshot.EncounterId}'.", nameof(snapshot));
            return new BossEncounterRuntime(MirrorKeeper, snapshot.HandsPlayed, snapshot.PreviousHandType);
        }
    }
}
