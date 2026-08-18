using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>战斗节点类型：决定该场战斗的规则来源与开战前是否揭示。</summary>
    public enum RunNodeKind
    {
        /// <summary>普通战斗：无 Boss 规则与介入事件，商店结束后直接开战。</summary>
        NormalBattle,
        /// <summary>Boss 战斗：开战前先进入 BossReveal 揭示阶段。</summary>
        BossBattle
    }

    /// <summary>Boss 难度池（GDD 2.5 共 3 池）。用枚举而非字符串：Inspector 下拉框杜绝策划拼写错误。None 为普通战哨兵值。</summary>
    public enum BossPoolId
    {
        /// <summary>非 Boss 节点（普通战）使用；Unity 无法序列化可空枚举，故用 None 哨兵代替 null。</summary>
        None = 0,
        /// <summary>初级池：第一场 Boss（节点 1）。</summary>
        Primary = 1,
        /// <summary>中级池：第二场 Boss（节点 3）。</summary>
        Intermediate = 2,
        /// <summary>高级池：最终 Boss（节点 5）。</summary>
        Advanced = 3
    }

    /// <summary>单局路线中的一个战斗节点（可序列化配置，Inspector 直接编辑）。</summary>
    [Serializable]
    public sealed class RunBattleNode
    {
        [Tooltip("节点类型：普通战或 Boss 战。")]
        public RunNodeKind kind;

        [Tooltip("本场目标分（GDD 2.6 白盒初值）。")]
        public long targetScore;

        [Tooltip("Boss 难度池；普通战选 None。")]
        public BossPoolId bossPoolId;

        [Tooltip("胜利后是否进入商店（最终 Boss 为 false，直接局终结算）。")]
        public bool hasShopAfter;

        /// <summary>节点序号（0 起，对应"第 Index+1 场战斗"）。资产加载时由 RunRoute 门面按列表位置写入，策划无需填写。</summary>
        public int Index { get; set; }

        /// <summary>无参构造：供 Unity 反序列化资产使用。</summary>
        public RunBattleNode()
        {
        }

        /// <summary>便捷构造：供内置默认路线与编辑器生成器使用。</summary>
        public RunBattleNode(RunNodeKind kind, long targetScore, BossPoolId bossPoolId, bool hasShopAfter)
        {
            this.kind = kind;
            this.targetScore = targetScore;
            this.bossPoolId = bossPoolId;
            this.hasShopAfter = hasShopAfter;
        }
    }

    /// <summary>单局路线表资产（GDD 冻结决策 #6：MVP 无分支地图，固定线性流程）。</summary>
    [CreateAssetMenu(menuName = "PersonaCards/RunRoute", fileName = "RunRoute")]
    public sealed class RunRouteAsset : ScriptableObject
    {
        [Tooltip("按顺序排列的战斗节点；节点数即单局战斗总数。")]
        public List<RunBattleNode> battleNodes = new List<RunBattleNode>();

        /// <summary>Inspector 修改资产时自动轻量校验（仅提示，不阻断保存）。</summary>
        private void OnValidate()
        {
            Validate(out _);
        }

        /// <summary>轻量校验（OnValidate 与单元测试共用）：节点非空、Boss 节点必须有池 id、目标分为正、只有最后一个节点可不进商店。</summary>
        public bool Validate(out string error)
        {
            error = null;
            if (battleNodes == null || battleNodes.Count == 0)
            {
                error = "路线表为空：至少需要一个战斗节点。";
                return false;
            }

            for (var index = 0; index < battleNodes.Count; index++)
            {
                var node = battleNodes[index];
                if (node == null)
                {
                    error = $"节点 {index} 为 null。";
                    return false;
                }
                if (node.kind == RunNodeKind.BossBattle && node.bossPoolId == BossPoolId.None)
                {
                    error = $"节点 {index} 是 Boss 战但难度池未指定（None）。";
                    return false;
                }
                if (node.targetScore <= 0)
                {
                    error = $"节点 {index} 目标分必须大于 0（当前 {node.targetScore}）。";
                    return false;
                }
                if (!node.hasShopAfter && index != battleNodes.Count - 1)
                {
                    error = $"只有最后一个节点可以不进商店（节点 {index} 位于路线中段却 hasShopAfter=false）。";
                    return false;
                }
            }

            return true;
        }
    }
}
