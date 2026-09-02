using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Core.Random;
using PersonaCards.Data;

namespace PersonaCards.Battle.Bosses
{
    public static class BossEncounterCatalog
    {
        public const string MirrorKeeperEncounterId = "boss.mirror_keeper.final";
        public const string RepeatedJudgmentRuleId = "boss.rule.repeated_judgment";
        public const string FirstHandEncouragementId = "boss.intervention.first_hand_encouragement";

        /// <summary>全部 Boss 定义注册表（P0-3 目录框架：新 Boss 定义追加到此数组即自动参与池抽取与快照恢复）。</summary>
        private static readonly IReadOnlyList<BossEncounterDefinition> All = new[]
        {
            new BossEncounterDefinition(
                MirrorKeeperEncounterId,
                "镜厅守门人",
                BossPoolId.Primary,
                RepeatedJudgmentRuleId,
                "重复审判",
                "本手牌型与上一手相同，最终得分 ×0.60。",
                FirstHandEncouragementId,
                "先手鼓励",
                "第一手结算时获得 +30 筹码。")
        };

        public static BossEncounterRuntime CreateMirrorKeeper()
        {
            return new BossEncounterRuntime(All[0]);
        }

        /// <summary>按难度池抽取 Boss 遭遇（P0-3 目录框架）：池内过滤、剔除本局已用、种子随机抽取。
        /// 抽取为种子纯函数——同一节点揭示与开战用同一种子派生（局种子 + 节点序号 + 1），两次调用必得同一 Boss。
        /// 池内无可用定义（未配置内容或全部已用）时抛错：非法配置必须暴露，不静默回落。</summary>
        /// <param name="poolId">难度池，来自路线表节点配置。</param>
        /// <param name="seed">抽取种子（调用方按局种子 + 节点序号派生，保证同局存档恢复后抽取一致）。</param>
        /// <param name="usedEncounterIds">本局已用过的遭遇 id 集合（不重复机制）；null = 不限制。UI 维护随 P0-8 存档接入。</param>
        public static BossEncounterRuntime CreateFromPool(BossPoolId poolId, uint seed,
            IReadOnlyCollection<string> usedEncounterIds = null)
        {
            if (!Enum.IsDefined(typeof(BossPoolId), poolId) || poolId == BossPoolId.None)
                throw new ArgumentOutOfRangeException(nameof(poolId), poolId, "普通战节点没有 Boss 难度池（None 为哨兵值）。");
            return new BossEncounterRuntime(PickEncounter(All, poolId, seed, usedEncounterIds));
        }

        /// <summary>从候选集合按池抽取（P0-3 抽取工具，纯函数）：按池过滤、剔除已用、种子随机。候选空抛错。</summary>
        public static BossEncounterDefinition PickEncounter(IEnumerable<BossEncounterDefinition> candidates,
            BossPoolId poolId, uint seed, IReadOnlyCollection<string> usedEncounterIds = null)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            var available = candidates
                .Where(definition => definition.PoolId == poolId)
                .Where(definition => usedEncounterIds == null || !usedEncounterIds.Contains(definition.EncounterId))
                .ToArray();
            if (available.Length == 0)
                throw new InvalidOperationException($"难度池 {poolId} 没有可用的 Boss 定义（内容未配置或已全部使用）。");
            var index = available.Length == 1 ? 0 : new XorShift32Rng(seed).NextInt(available.Length);
            return available[index];
        }

        /// <summary>按快照恢复 Boss 运行时：按遭遇 id 查目录重建（未知 id 抛错），手数历史随快照。</summary>
        public static BossEncounterRuntime Restore(BossEncounterSnapshot snapshot)
        {
            if (snapshot == null) return null;
            var definition = All.FirstOrDefault(candidate =>
                string.Equals(candidate.EncounterId, snapshot.EncounterId, StringComparison.Ordinal));
            if (definition == null)
                throw new ArgumentException($"Unknown boss encounter '{snapshot.EncounterId}'.", nameof(snapshot));
            return new BossEncounterRuntime(definition, snapshot.HandsPlayed, snapshot.PreviousHandType);
        }
    }
}
