using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>路线节点类型：决定该节点的内容与流程去向。</summary>
    public enum RunNodeKind
    {
        /// <summary>普通战斗：无 Boss 规则与介入事件，商店结束后直接开战。</summary>
        NormalBattle,
        /// <summary>Boss 战斗：开战前先进入 BossReveal 揭示阶段。</summary>
        BossBattle,
        /// <summary>人格牌生成节点：不战斗，进入铸牌界面生成人格牌，确认后直接推进到下一节点。追加在枚举末尾，旧序列化值不变。</summary>
        PersonaGen
    }

    /// <summary>Boss 难度池（GDD 2.5 共 3 池）。用枚举而非字符串：Inspector 下拉框杜绝策划拼写错误。None 为普通战哨兵值。</summary>
    public enum BossPoolId
    {
        /// <summary>非 Boss 节点（普通战）使用；Unity 无法序列化可空枚举，故用 None 哨兵代替 null。</summary>
        None = 0,
        /// <summary>初级池：第一场 Boss。</summary>
        Primary = 1,
        /// <summary>中级池：第二场 Boss。</summary>
        Intermediate = 2,
        /// <summary>高级池：第三场及之后的 Boss（配表导入按出现顺序自动分配）。</summary>
        Advanced = 3
    }

    /// <summary>单局路线中的一个节点（可序列化配置，Inspector 直接编辑；正式数据由 xlsx 导入命令写入）。</summary>
    [Serializable]
    public sealed class RunBattleNode
    {
        [Tooltip("节点类型：普通战斗、Boss 战斗或人格牌生成。")]
        public RunNodeKind kind;

        [Tooltip("本场目标分（仅战斗类节点有效；人格牌生成节点忽略）。")]
        public long targetScore;

        [Tooltip("Boss 难度池；普通战选 None。")]
        public BossPoolId bossPoolId;

        [Tooltip("胜利后是否进入商店（最终战斗与人格牌生成节点必须为 false）。")]
        public bool hasShopAfter;

        [Tooltip("本场出牌次数上限（0 = 使用默认值 4）。仅战斗类节点有效。")]
        public int playsLimit;

        [Tooltip("本场弃牌次数上限（0 = 使用默认值 3）。仅战斗类节点有效。")]
        public int discardsLimit;

        [Tooltip("人格牌生成数量（仅人格牌生成节点有效，当前版本固定 1）。")]
        public int genCount;

        [Tooltip("阶段编号（配表「阶段_ID」列原文，如 STAGE_01；仅定位与展示用）。")]
        public string stageId;

        [Tooltip("奖励 1 类型（配表「奖励类型1」列原文，如 金币/无/人格牌；空 = 无数据）。")]
        public string rewardType1;

        [Tooltip("奖励 1 参数（配表「奖励参数1」列原文，如 3；空 = 无数据）。")]
        public string rewardParam1;

        [Tooltip("奖励 2 类型（配表「奖励类型2」列原文）。")]
        public string rewardType2;

        [Tooltip("奖励 2 参数（配表「奖励参数2」列原文）。")]
        public string rewardParam2;

        /// <summary>节点序号（0 起）。资产加载时由 RunRoute 门面按列表位置写入，策划无需填写。</summary>
        public int Index { get; set; }

        /// <summary>无参构造：供 Unity 反序列化资产使用。</summary>
        public RunBattleNode()
        {
        }

        /// <summary>便捷构造：供内置默认路线与编辑器生成器使用。playsLimit/discardsLimit 传 0 表示使用默认值 4/3；genCount 仅人格牌生成节点需要；stageId/奖励列为配表原文，默认空串。</summary>
        public RunBattleNode(RunNodeKind kind, long targetScore, BossPoolId bossPoolId, bool hasShopAfter,
            int playsLimit = 0, int discardsLimit = 0, int genCount = 0,
            string stageId = "", string rewardType1 = "", string rewardParam1 = "",
            string rewardType2 = "", string rewardParam2 = "")
        {
            this.kind = kind;
            this.targetScore = targetScore;
            this.bossPoolId = bossPoolId;
            this.hasShopAfter = hasShopAfter;
            this.playsLimit = playsLimit;
            this.discardsLimit = discardsLimit;
            this.genCount = genCount;
            this.stageId = stageId;
            this.rewardType1 = rewardType1;
            this.rewardParam1 = rewardParam1;
            this.rewardType2 = rewardType2;
            this.rewardParam2 = rewardParam2;
        }
    }

    /// <summary>单局路线表资产（GDD 冻结决策 #6：MVP 无分支地图，固定线性流程；关卡配置由 xlsx 导入命令覆写）。</summary>
    [CreateAssetMenu(menuName = "PersonaCards/RunRoute", fileName = "RunRoute")]
    public sealed class RunRouteAsset : ScriptableObject
    {
        [Tooltip("按顺序排列的路线节点；节点数即单局阶段总数（战斗 + 人格牌生成）。")]
        public List<RunBattleNode> battleNodes = new List<RunBattleNode>();

        /// <summary>是否战斗类节点（普通战或 Boss 战）。</summary>
        public static bool IsBattleKind(RunNodeKind kind) => kind == RunNodeKind.NormalBattle || kind == RunNodeKind.BossBattle;

        /// <summary>
        /// 内置白盒路线（= 配表"关卡流程"当前初值）：17 个阶段 = 12 场普通战斗 + 4 个人格牌生成节点（顺序 4/8/12/16）+ 最终 Boss（顺序 17）。
        /// P0-6：普通战金币奖励列按配表补齐（3,3,4 三组 + 末组 2,3,4）；生成节点与最终 Boss 奖励为「无」，留空等价。
        /// RunRoute 门面兜底与"Regenerate Run Route Asset"菜单共用此工厂，消灭多份拷贝漂移；正式数据以 xlsx 导入命令写入为准。
        /// </summary>
        public static List<RunBattleNode> CreateDefaultNodes()
        {
            return new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 950, BossPoolId.None, true, stageId: "STAGE_01", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1100, BossPoolId.None, true, stageId: "STAGE_02", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1250, BossPoolId.None, true, stageId: "STAGE_03", rewardType1: "金币", rewardParam1: "4"),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1, stageId: "STAGE_04"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1350, BossPoolId.None, true, stageId: "STAGE_05", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1500, BossPoolId.None, true, stageId: "STAGE_06", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1650, BossPoolId.None, true, stageId: "STAGE_07", rewardType1: "金币", rewardParam1: "4"),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1, stageId: "STAGE_08"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1750, BossPoolId.None, true, stageId: "STAGE_09", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 1950, BossPoolId.None, true, stageId: "STAGE_10", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 2150, BossPoolId.None, true, stageId: "STAGE_11", rewardType1: "金币", rewardParam1: "4"),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1, stageId: "STAGE_12"),
                new RunBattleNode(RunNodeKind.NormalBattle, 2300, BossPoolId.None, true, stageId: "STAGE_13", rewardType1: "金币", rewardParam1: "2"),
                new RunBattleNode(RunNodeKind.NormalBattle, 2500, BossPoolId.None, true, stageId: "STAGE_14", rewardType1: "金币", rewardParam1: "3"),
                new RunBattleNode(RunNodeKind.NormalBattle, 2750, BossPoolId.None, true, stageId: "STAGE_15", rewardType1: "金币", rewardParam1: "4"),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1, stageId: "STAGE_16"),
                new RunBattleNode(RunNodeKind.BossBattle, 3200, BossPoolId.Primary, false, stageId: "STAGE_17")
            };
        }

        /// <summary>Inspector 修改资产时自动轻量校验（仅提示，不阻断保存）。</summary>
        private void OnValidate()
        {
            Validate(out _);
        }

        /// <summary>轻量校验（OnValidate、导入命令与单元测试共用）：按节点类型逐条检查，最终节点必须是战斗类且不接商店。</summary>
        public bool Validate(out string error)
        {
            error = null;
            if (battleNodes == null || battleNodes.Count == 0)
            {
                error = "路线表为空：至少需要一个节点。";
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

                // 按节点类型分派：每种类型只校验属于自己的字段
                switch (node.kind)
                {
                    case RunNodeKind.NormalBattle:
                        if (node.targetScore <= 0)
                        {
                            error = $"节点 {index} 是普通战斗但目标分必须大于 0（当前 {node.targetScore}）。";
                            return false;
                        }
                        break;
                    case RunNodeKind.BossBattle:
                        if (node.bossPoolId == BossPoolId.None)
                        {
                            error = $"节点 {index} 是 Boss 战但难度池未指定（None）。";
                            return false;
                        }
                        if (node.targetScore <= 0)
                        {
                            error = $"节点 {index} 是 Boss 战但目标分必须大于 0（当前 {node.targetScore}）。";
                            return false;
                        }
                        break;
                    case RunNodeKind.PersonaGen:
                        if (node.genCount < 1)
                        {
                            error = $"节点 {index} 是人格牌生成节点但生成数量必须至少为 1（当前 {node.genCount}）。";
                            return false;
                        }
                        if (node.hasShopAfter)
                        {
                            error = $"节点 {index} 是人格牌生成节点，不能在其后接商店（hasShopAfter 必须为 false）。";
                            return false;
                        }
                        break;
                    default:
                        error = $"节点 {index} 的节点类型未知（{(int)node.kind}）。";
                        return false;
                }

                // 战斗类节点的出牌/弃牌限制只允许 0（=默认）或正数
                if (node.playsLimit < 0 || node.discardsLimit < 0)
                {
                    error = $"节点 {index} 的出牌/弃牌限制不能为负数（当前 {node.playsLimit}/{node.discardsLimit}）。";
                    return false;
                }
            }

            // 最终节点：必须是战斗类（生成节点之后必须有下一节点），且不能接商店（否则流程会越界推进）
            var final = battleNodes[battleNodes.Count - 1];
            if (!IsBattleKind(final.kind))
            {
                error = $"最终节点必须是战斗类型（当前为 {final.kind}）。";
                return false;
            }
            if (final.hasShopAfter)
            {
                error = "最终节点不能接商店（hasShopAfter 必须为 false）。";
                return false;
            }

            return true;
        }
    }
}
