using System;
using System.Collections.Generic;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 整局流程的唯一权威访问口（状态机与控制器只通过这里读取路线）。
    /// 数据来自 RunRouteAsset（Inspector 可编辑）；资产缺失或校验失败时回落内置默认路线（GDD 2.6 白盒表），保证任何情况下流程可跑。
    /// </summary>
    public static class RunRoute
    {
        /// <summary>内置默认路线（GDD 2.6 白盒初值）：6 场战斗、5 次商店，Boss 在节点 1/3/5。与 RunRoute.asset 初值保持一致。</summary>
        private static readonly IReadOnlyList<RunBattleNode> DefaultNodes = new List<RunBattleNode>
        {
            new RunBattleNode(RunNodeKind.NormalBattle, 350, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 550, BossPoolId.Primary, true),
            new RunBattleNode(RunNodeKind.NormalBattle, 1000, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 1500, BossPoolId.Intermediate, true),
            new RunBattleNode(RunNodeKind.NormalBattle, 2800, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 4200, BossPoolId.Advanced, false)
        };

        private static IReadOnlyList<RunBattleNode> _nodes = DefaultNodes;

        /// <summary>当前生效的战斗节点列表（资产注入或默认路线）。</summary>
        public static IReadOnlyList<RunBattleNode> BattleNodes => _nodes;

        /// <summary>单局战斗总数（= 当前路线节点数，从列表推导，策划无需维护数字）。</summary>
        public static int BattleCount => _nodes.Count;

        /// <summary>单局商店次数（= hasShopAfter 为 true 的节点数）。</summary>
        public static int ShopCount
        {
            get
            {
                var count = 0;
                foreach (var node in _nodes)
                {
                    if (node.hasShopAfter) count++;
                }
                return count;
            }
        }

        /// <summary>由控制器 Awake 注入路线资产；null 表示回到内置默认路线（供测试与容错，静默执行）。</summary>
        public static void Configure(RunRouteAsset asset)
        {
            if (asset == null)
            {
                AssignIndices(DefaultNodes);
                _nodes = DefaultNodes;
                return;
            }

            if (!asset.Validate(out var error))
            {
                Debug.LogError($"[RunRoute] 路线资产 {asset.name} 校验失败，忽略该资产并使用默认路线：{error}");
                AssignIndices(DefaultNodes);
                _nodes = DefaultNodes;
                return;
            }

            AssignIndices(asset.battleNodes);
            _nodes = asset.battleNodes;
            Debug.Log($"[RunRoute] 已加载路线资产 {asset.name}：{BattleCount} 场战斗、{ShopCount} 次商店。");
        }

        /// <summary>节点序号按列表位置写入（0 起），不信任手填值。</summary>
        private static void AssignIndices(IEnumerable<RunBattleNode> nodes)
        {
            var index = 0;
            foreach (var node in nodes)
                node.Index = index++;
        }

        /// <summary>取指定节点；越界抛 ArgumentOutOfRangeException。</summary>
        public static RunBattleNode GetNode(int index)
        {
            if (index < 0 || index >= _nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"节点序号 {index} 超出路线范围 0..{_nodes.Count - 1}。");
            return _nodes[index];
        }

        /// <summary>是否为最终战斗节点（胜利后进局终报告而非商店）。</summary>
        public static bool IsFinalNode(int index) => index == _nodes.Count - 1;

        /// <summary>下一战斗是否为 Boss 战（决定商店结束后的去向）。</summary>
        public static bool NextNodeIsBoss(int index) => GetNode(index + 1).kind == RunNodeKind.BossBattle;
    }
}
