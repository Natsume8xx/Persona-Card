using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    public sealed class PrototypeFlowStateMachineTests
    {
        /// <summary>测试间隔离：RunRoute 是静态门面，测试内注入的自定义资产不能泄漏到其他测试。</summary>
        [TearDown]
        public void ResetRouteToDefault()
        {
            RunRoute.Configure(null);
        }

        /// <summary>从当前 Battle 阶段打一场胜仗并推进到下一节点的入场阶段：胜利 → 奖励 →（节点配置了商店则过商店）→ 按下一节点类型分派。</summary>
        private static void WinAndAdvanceToNextNode(PrototypeFlowStateMachine flow)
        {
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Reward), $"节点 {flow.NodeIndex} 胜利后未进入奖励");
            var node = RunRoute.GetNode(flow.NodeIndex);
            Assert.That(flow.ContinueFromReward(), Is.True);
            if (node.hasShopAfter)
            {
                Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Shop), $"节点 {flow.NodeIndex} 奖励后未进入商店");
                Assert.That(flow.ContinueFromShop(), Is.True);
            }
            var nextKind = RunRoute.GetNode(flow.NodeIndex).kind;
            var expected = nextKind == RunNodeKind.BossBattle ? PrototypeFlowStage.BossReveal
                : nextKind == RunNodeKind.PersonaGen ? PrototypeFlowStage.PersonaGen
                : PrototypeFlowStage.Battle;
            Assert.That(flow.Stage, Is.EqualTo(expected), $"节点 {flow.NodeIndex}（{nextKind}）的入场阶段错误");
        }

        /// <summary>从当前节点的入场阶段进入战斗（Boss 揭示开战 / 生成节点确认获得 / 普通战无操作）。</summary>
        private static void EnterCurrentBattle(PrototypeFlowStateMachine flow)
        {
            var kind = RunRoute.GetNode(flow.NodeIndex).kind;
            if (kind == RunNodeKind.BossBattle) Assert.That(flow.BeginBossBattle(), Is.True);
            if (kind == RunNodeKind.PersonaGen) Assert.That(flow.CompletePersonaGen(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle), $"节点 {flow.NodeIndex}（{kind}）未进入战斗");
        }

        [Test]
        public void DefaultRouteWalksEndToEndThroughGenNodes()
        {
            RunRoute.Configure(null); // 内置白盒：13 阶段 = 10 战斗 + 3 生成节点
            var flow = new PrototypeFlowStateMachine();

            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
            Assert.That(flow.StartNewRun(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaSetup));
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));

            // 依次走完路线表全部节点：胜利 → 奖励 →（商店）→ 下一节点入场（Boss 揭示 / 铸牌 / 直接开战）
            while (!RunRoute.IsFinalNode(flow.NodeIndex))
            {
                WinAndAdvanceToNextNode(flow);
                EnterCurrentBattle(flow);
            }
            Assert.That(flow.NodeIndex, Is.EqualTo(RunRoute.StageCount - 1));

            // 最终战斗胜利 → 局终报告 → 人格铸造
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.RunReport));
            Assert.That(flow.ContinueToForge(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaForge));

            flow.ReturnToMainMenu();
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));
        }

        [Test]
        public void MixedChainWithBossWalksEndToEnd()
        {
            // 覆盖全部去向分支：商店后 Boss 揭示、无商店直连生成节点、生成节点直连普通战、无商店直连普通战、商店后最终 Boss
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1),
                new RunBattleNode(RunNodeKind.NormalBattle, 300, BossPoolId.None, false),
                new RunBattleNode(RunNodeKind.NormalBattle, 400, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 500, BossPoolId.Advanced, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();

            // 节点 0 普通战 → 商店 → Boss 揭示（节点 1）
            WinAndAdvanceToNextNode(flow);
            Assert.That(flow.NodeIndex, Is.EqualTo(1));
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.BeginBossBattle(), Is.True);

            // 节点 1 Boss → 无商店直达生成节点（节点 2）
            WinAndAdvanceToNextNode(flow);
            Assert.That(flow.NodeIndex, Is.EqualTo(2));
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaGen));

            // 生成节点确认 → 直接开战节点 3
            Assert.That(flow.CompletePersonaGen(), Is.True);
            Assert.That(flow.NodeIndex, Is.EqualTo(3));
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));

            // 节点 3 无商店 → 节点 4 普通战
            WinAndAdvanceToNextNode(flow);
            Assert.That(flow.NodeIndex, Is.EqualTo(4));
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));

            // 节点 4 → 商店 → 最终 Boss 揭示（节点 5）
            WinAndAdvanceToNextNode(flow);
            Assert.That(flow.NodeIndex, Is.EqualTo(5));
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.BeginBossBattle(), Is.True);

            // 最终 Boss 胜利 → 局终报告 → 铸造
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.RunReport));
            Assert.That(flow.ContinueToForge(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaForge));
        }

        [Test]
        public void FirstNodeBossStartsAtBossReveal()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.BossBattle, 100, BossPoolId.Primary, false),
                new RunBattleNode(RunNodeKind.NormalBattle, 200, BossPoolId.None, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));
            Assert.That(flow.BeginBossBattle(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
        }

        [Test]
        public void FirstNodePersonaGenStartsAtForge()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1),
                new RunBattleNode(RunNodeKind.NormalBattle, 200, BossPoolId.None, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaGen)); // 节点 0 是生成节点：先进铸牌
            Assert.That(flow.NodeIndex, Is.EqualTo(0));
            Assert.That(flow.CompletePersonaGen(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
        }

        [Test]
        public void CannotSkipRequiredStages()
        {
            var flow = new PrototypeFlowStateMachine();

            Assert.That(flow.BeginBossBattle(), Is.False);
            Assert.That(flow.CompleteBattle(true), Is.False);
            Assert.That(flow.ContinueFromReward(), Is.False);
            Assert.That(flow.ContinueFromShop(), Is.False);
            Assert.That(flow.CompletePersonaGen(), Is.False);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
        }

        [Test]
        public void CompletePersonaGenRejectedOutsidePersonaGenStage()
        {
            var flow = new PrototypeFlowStateMachine();
            Assert.That(flow.CompletePersonaGen(), Is.False); // 主菜单阶段

            flow.StartNewRun();
            flow.ConfirmPersonaSetup(); // 默认路线节点 0 是普通战 → Battle 阶段
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.CompletePersonaGen(), Is.False); // 战斗阶段同样拒绝
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
        }

        [Test]
        public void LosingAnyBattleEndsTheRun()
        {
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();

            Assert.That(flow.CompleteBattle(false), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.FailureResult));
        }

        [Test]
        public void LosingAtFinalNodeEndsTheRunInsteadOfReport()
        {
            // 紧凑自定义路线：最终 Boss 失败同样进失败结算而非局终报告
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();

            WinAndAdvanceToNextNode(flow); // 节点 0 胜利 → 商店 → Boss 揭示（节点 1）
            Assert.That(flow.NodeIndex, Is.EqualTo(1));
            Assert.That(flow.BeginBossBattle(), Is.True);

            Assert.That(flow.CompleteBattle(false), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.FailureResult));
        }

        [Test]
        public void RestoreAcceptsValidNodeIndexAndRejectsInvalid()
        {
            var flow = new PrototypeFlowStateMachine();

            flow.Restore(PrototypeFlowStage.Battle, RunRoute.StageCount - 1);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(RunRoute.StageCount - 1));

            flow.Restore(PrototypeFlowStage.PersonaGen, 3); // 生成节点也是合法存档位置
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaGen));
            Assert.That(flow.NodeIndex, Is.EqualTo(3));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore(PrototypeFlowStage.Battle, RunRoute.StageCount));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore(PrototypeFlowStage.Battle, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore((PrototypeFlowStage)999, 0));
        }

        [Test]
        public void ReturningToEquipmentFromBossRevealKeepsCurrentNode()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();      // 节点 0 战斗
            flow.CompleteBattle(true);
            flow.ContinueFromReward();
            flow.ContinueFromShop();         // → Boss 揭示，节点 1

            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.NodeIndex, Is.EqualTo(1));

            Assert.That(flow.ReturnToPersonaSetup(), Is.True); // 返回检查装备
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaSetup));
            Assert.That(flow.NodeIndex, Is.EqualTo(1));        // 节点必须保留，不得重置为 0

            Assert.That(flow.ConfirmPersonaSetup(), Is.True);  // 确认装备
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal)); // 回到揭示界面而非第 0 节点战斗
            Assert.That(flow.NodeIndex, Is.EqualTo(1));

            Assert.That(flow.BeginBossBattle(), Is.True);      // 开战：仍在本 Boss 节点
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(1));
        }

        [Test]
        public void EquipmentCheckOnlyAppliesToTheImmediateConfirm()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false)
            };
            RunRoute.Configure(asset);
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();
            flow.CompleteBattle(true);
            flow.ContinueFromReward();
            flow.ContinueFromShop();                 // Boss 揭示，节点 1
            flow.ReturnToPersonaSetup();             // 装备检查
            flow.ReturnToMainMenu();                 // 放弃本局
            Assert.That(flow.PersonaSetupReturnsToBossReveal, Is.False); // 回主菜单应清掉回程标记

            flow.StartNewRun();                      // 开新局
            Assert.That(flow.PersonaSetupReturnsToBossReveal, Is.False);
            flow.ConfirmPersonaSetup();              // 新局确认：正常进第 0 节点
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));
        }

        [Test]
        public void RestoreHonorsEquipmentCheckFlag()
        {
            var flow = new PrototypeFlowStateMachine();

            // 存档场景：Boss 揭示返回装备检查后中途退出，继续游戏应恢复回程标记
            flow.Restore(PrototypeFlowStage.PersonaSetup, 1, personaSetupReturnsToBossReveal: true);
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.NodeIndex, Is.EqualTo(1));

            // 防御：非 PersonaSetup 阶段携带的标记应被忽略
            flow.Restore(PrototypeFlowStage.Battle, 0, personaSetupReturnsToBossReveal: true);
            Assert.That(flow.PersonaSetupReturnsToBossReveal, Is.False);
        }
    }
}
