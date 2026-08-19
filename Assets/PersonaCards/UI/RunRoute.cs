using System;
using System.Collections.Generic;
using PersonaCards.Battle;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 整局流程的唯一权威访问口（状态机与控制器只通过这里读取路线）。
    /// 数据来自 RunRouteAsset（Inspector 可编辑或由 xlsx 导入命令写入）；资产缺失或校验失败时回落内置默认路线（= 配表白盒），保证任何情况下流程可跑。
    /// </summary>
    public static class RunRoute
    {
        /// <summary>出牌次数默认值：节点配置为 0（未指定）时回落此值；引用 BattleStateMachine.StartingPlays 保证单一来源。</summary>
        public const int DefaultPlaysLimit = BattleStateMachine.StartingPlays;

        /// <summary>弃牌次数默认值：节点配置为 0（未指定）时回落此值；引用 BattleStateMachine.StartingDiscards 保证单一来源。</summary>
        public const int DefaultDiscardsLimit = BattleStateMachine.StartingDiscards;

        /// <summary>内置默认路线（= 配表"关卡流程"当前初值，与 RunRouteAsset.CreateDefaultNodes() 同源）：13 个阶段 = 10 场战斗 + 3 个人格牌生成节点（顺序 4/8/12）。</summary>
        private static readonly IReadOnlyList<RunBattleNode> DefaultNodes = RunRouteAsset.CreateDefaultNodes();

        private static IReadOnlyList<RunBattleNode> _nodes = DefaultNodes;

        /// <summary>当前生效的节点列表（资产注入或默认路线）。</summary>
        public static IReadOnlyList<RunBattleNode> BattleNodes => _nodes;

        /// <summary>单局阶段总数（= 当前路线节点数：战斗 + 人格牌生成节点，从列表推导，策划无需维护数字）。</summary>
        public static int StageCount => _nodes.Count;

        /// <summary>单局战斗场数（= 战斗类节点数；进度文案"旅程 x / N"的分母，生成节点不计入）。</summary>
        public static int BattleCount
        {
            get
            {
                var count = 0;
                foreach (var node in _nodes)
                {
                    if (RunRouteAsset.IsBattleKind(node.kind)) count++;
                }
                return count;
            }
        }

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
            Debug.Log($"[RunRoute] 已加载路线资产 {asset.name}：{StageCount} 个阶段（{BattleCount} 场战斗、{StageCount - BattleCount} 个人格牌生成节点）、{ShopCount} 次商店。");
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

        /// <summary>是否为最终节点（胜利后进局终报告而非商店）。</summary>
        public static bool IsFinalNode(int index) => index == _nodes.Count - 1;

        /// <summary>下一节点的类型（商店结束后或生成节点完成后的去向）。最终节点没有下一节点，越界抛 ArgumentOutOfRangeException。</summary>
        public static RunNodeKind NextNodeKindOf(int index) => GetNode(index + 1).kind;

        /// <summary>节点的战斗序号（1 起，只计战斗类节点；人格牌生成节点没有战斗序号，抛 InvalidOperationException）。</summary>
        public static int BattleOrdinalOf(int index)
        {
            var node = GetNode(index);
            if (!RunRouteAsset.IsBattleKind(node.kind))
                throw new InvalidOperationException($"节点 {index} 是人格牌生成节点，没有战斗序号。");
            var ordinal = 0;
            for (var i = 0; i <= index; i++)
            {
                if (RunRouteAsset.IsBattleKind(_nodes[i].kind)) ordinal++;
            }
            return ordinal;
        }

        /// <summary>节点的出牌次数上限（配置为 0 时回落默认值）；人格牌生成节点无此概念，抛 InvalidOperationException。</summary>
        public static int PlaysLimitOf(int index)
        {
            var node = GetNode(index);
            if (!RunRouteAsset.IsBattleKind(node.kind))
                throw new InvalidOperationException($"节点 {index} 是人格牌生成节点，没有出牌限制。");
            return node.playsLimit > 0 ? node.playsLimit : DefaultPlaysLimit;
        }

        /// <summary>节点的弃牌次数上限（配置为 0 时回落默认值）；人格牌生成节点无此概念，抛 InvalidOperationException。</summary>
        public static int DiscardsLimitOf(int index)
        {
            var node = GetNode(index);
            if (!RunRouteAsset.IsBattleKind(node.kind))
                throw new InvalidOperationException($"节点 {index} 是人格牌生成节点，没有弃牌限制。");
            return node.discardsLimit > 0 ? node.discardsLimit : DefaultDiscardsLimit;
        }
    }
}
