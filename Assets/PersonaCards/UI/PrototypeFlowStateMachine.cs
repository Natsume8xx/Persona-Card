using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>整局流程状态机：阶段 + 节点索引。节点数据全部来自 RunRoute 路线表（含人格牌生成节点），本机不含任何场次数值。</summary>
    public sealed class PrototypeFlowStateMachine
    {
        public PrototypeFlowStage Stage { get; private set; } = PrototypeFlowStage.MainMenu;

        /// <summary>当前节点序号（0 起，指向 RunRoute.BattleNodes；0 表示尚未开始首战）。</summary>
        public int NodeIndex { get; private set; }

        /// <summary>本次装备阶段是否由 Boss 揭示"返回检查装备"进入：true 时确认装备后回到揭示界面（保留节点），false 时开新局进第 0 节点。随存档持久化。</summary>
        public bool PersonaSetupReturnsToBossReveal { get; private set; }

        /// <summary>主菜单开始新局：进入人格装备阶段。</summary>
        public bool StartNewRun()
        {
            if (!Move(PrototypeFlowStage.MainMenu, PrototypeFlowStage.PersonaSetup)) return false;
            PersonaSetupReturnsToBossReveal = false; // 新局：确认装备后进第 0 节点
            return true;
        }

        /// <summary>Boss 揭示界面"返回检查装备"：保留本局进度与当前节点，进入人格装备阶段；确认装备后回到揭示界面。</summary>
        public bool ReturnToPersonaSetup()
        {
            if (!Move(PrototypeFlowStage.BossReveal, PrototypeFlowStage.PersonaSetup)) return false;
            PersonaSetupReturnsToBossReveal = true;
            Debug.Log($"[Flow] 从 Boss 揭示返回装备检查：节点 {NodeIndex} 保留。");
            return true;
        }

        /// <summary>确认人格装备：若来自 Boss 揭示的装备检查，回到揭示界面并保留当前节点；否则开新局，按节点 0 的类型分派（Boss 战先揭示、生成节点先进铸牌）。</summary>
        public bool ConfirmPersonaSetup()
        {
            if (Stage != PrototypeFlowStage.PersonaSetup) return false;

            if (PersonaSetupReturnsToBossReveal)
            {
                PersonaSetupReturnsToBossReveal = false; // 标记已消费：装备检查完毕，回揭示界面
                Stage = PrototypeFlowStage.BossReveal;
                Debug.Log($"[Flow] 装备检查完毕：返回 Boss 揭示，节点 {NodeIndex} 不变。");
                return true;
            }

            NodeIndex = 0;
            var firstKind = RunRoute.GetNode(0).kind;
            Stage = firstKind == RunNodeKind.BossBattle ? PrototypeFlowStage.BossReveal
                : firstKind == RunNodeKind.PersonaGen ? PrototypeFlowStage.PersonaGen
                : PrototypeFlowStage.Battle; // 节点 0 为生成节点时先进铸牌（与后续节点同规则）
            Debug.Log($"[Flow] 装备确认：进入节点 0（{RunRoute.GetNode(0).kind}），阶段 {Stage}。");
            return true;
        }

        /// <summary>战斗结算：失败进失败结算；胜利且为最终节点进局终报告；胜利非最终进奖励。</summary>
        public bool CompleteBattle(bool won)
        {
            if (Stage != PrototypeFlowStage.Battle) return false;
            if (!won)
            {
                Stage = PrototypeFlowStage.FailureResult;
                Debug.Log($"[Flow] 战斗失败：节点 {NodeIndex} 结算为失败，本局结束。");
                return true;
            }

            Stage = RunRoute.IsFinalNode(NodeIndex)
                ? PrototypeFlowStage.RunReport
                : PrototypeFlowStage.Reward;
            return true;
        }

        /// <summary>奖励领取完毕：当前节点配置了商店则进商店，否则直接推进到下一节点（配表可指定任意节点不带商店）。</summary>
        public bool ContinueFromReward()
        {
            if (Stage != PrototypeFlowStage.Reward) return false;
            if (RunRoute.GetNode(NodeIndex).hasShopAfter)
            {
                Stage = PrototypeFlowStage.Shop;
                Debug.Log($"[Flow] 奖励领取完毕：节点 {NodeIndex} 配置了商店，进入商店。");
            }
            else
            {
                AdvanceToNextStage();
            }
            return true;
        }

        /// <summary>离开商店：推进到下一节点，按节点类型分派去向（Boss 揭示 / 铸牌 / 直接开战）。</summary>
        public bool ContinueFromShop()
        {
            if (Stage != PrototypeFlowStage.Shop) return false;
            AdvanceToNextStage();
            return true;
        }

        /// <summary>人格牌生成节点确认获得：直接推进到下一节点。</summary>
        public bool CompletePersonaGen()
        {
            if (Stage != PrototypeFlowStage.PersonaGen) return false;
            AdvanceToNextStage();
            return true;
        }

        /// <summary>Boss 揭示确认开战：进入当前 Boss 节点战斗（节点序号已在推进时写入）。</summary>
        public bool BeginBossBattle() => Move(PrototypeFlowStage.BossReveal, PrototypeFlowStage.Battle);

        /// <summary>局终报告确认：进入人格铸造。</summary>
        public bool ContinueToForge() => Move(PrototypeFlowStage.RunReport, PrototypeFlowStage.PersonaForge);

        /// <summary>回到主菜单并重置节点序号与回程标记（本局结束）。</summary>
        public void ReturnToMainMenu()
        {
            Stage = PrototypeFlowStage.MainMenu;
            NodeIndex = 0;
            PersonaSetupReturnsToBossReveal = false;
            Debug.Log("[Flow] 回到主菜单，节点序号重置为 0。");
        }

        /// <summary>从存档恢复流程位置；stage 非法或 nodeIndex 超出路线范围时抛异常。personaSetupReturnsToBossReveal 仅在 stage 为 PersonaSetup 时生效。</summary>
        public void Restore(PrototypeFlowStage stage, int nodeIndex, bool personaSetupReturnsToBossReveal = false)
        {
            if (!System.Enum.IsDefined(typeof(PrototypeFlowStage), stage))
                throw new System.ArgumentOutOfRangeException(nameof(stage));
            if (nodeIndex < 0 || nodeIndex > RunRoute.StageCount - 1)
                throw new System.ArgumentOutOfRangeException(nameof(nodeIndex),
                    $"节点序号 {nodeIndex} 超出路线范围 0..{RunRoute.StageCount - 1}。");
            Stage = stage;
            NodeIndex = nodeIndex;
            // 回程标记只在装备阶段有意义；防御性忽略其他阶段携带的脏标记
            PersonaSetupReturnsToBossReveal = stage == PrototypeFlowStage.PersonaSetup && personaSetupReturnsToBossReveal;
            Debug.Log($"[Flow] 恢复存档位置：阶段 {stage}，节点 {nodeIndex}，回程标记 {PersonaSetupReturnsToBossReveal}。");
        }

        /// <summary>推进到下一节点并按类型分派入场阶段：Boss 战先进揭示、生成节点进铸牌、普通战直接开战。</summary>
        private void AdvanceToNextStage()
        {
            NodeIndex += 1;
            var kind = RunRoute.GetNode(NodeIndex).kind;
            Stage = kind == RunNodeKind.BossBattle ? PrototypeFlowStage.BossReveal
                : kind == RunNodeKind.PersonaGen ? PrototypeFlowStage.PersonaGen
                : PrototypeFlowStage.Battle;
            Debug.Log($"[Flow] 节点推进到 {NodeIndex}（{kind}），进入阶段 {Stage}。");
        }

        /// <summary>通用阶段转移：当前阶段必须等于 expected，否则拒绝并记录警告。</summary>
        private bool Move(PrototypeFlowStage expected, PrototypeFlowStage next)
        {
            if (Stage != expected)
            {
                Debug.LogWarning($"[Flow] 非法转移被拒绝：{Stage} → {next}（要求当前阶段为 {expected}）。");
                return false;
            }

            Debug.Log($"[Flow] 阶段转移：{expected} → {next}（节点 {NodeIndex}）。");
            Stage = next;
            return true;
        }
    }
}
