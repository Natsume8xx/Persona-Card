using System;

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

        public static BossEncounterRuntime Restore(BossEncounterSnapshot snapshot)
        {
            if (snapshot == null) return null;
            if (!string.Equals(snapshot.EncounterId, MirrorKeeperEncounterId, StringComparison.Ordinal))
                throw new ArgumentException($"Unknown boss encounter '{snapshot.EncounterId}'.", nameof(snapshot));
            return new BossEncounterRuntime(MirrorKeeper, snapshot.HandsPlayed, snapshot.PreviousHandType);
        }
    }
}
