using NUnit.Framework;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    public sealed class PrototypeFlowStateMachineTests
    {
        [Test]
        public void HappyPathMovesThroughSixBattlesAndFiveShops()
        {
            var flow = new PrototypeFlowStateMachine();

            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
            Assert.That(flow.StartNewRun(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaSetup));
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));

            // 依次走完路线表全部节点：非最终节点胜利 → 奖励 → 商店 →（Boss 则揭示）→ 下一场；最终节点胜利 → 局终报告
            for (var node = 0; node < RunRoute.BattleCount; node++)
            {
                Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle), $"节点 {node} 未进入战斗");
                Assert.That(flow.CompleteBattle(true), Is.True);

                if (RunRoute.IsFinalNode(node))
                {
                    Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.RunReport));
                    continue;
                }

                Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Reward), $"节点 {node} 胜利后未进入奖励");
                Assert.That(flow.ContinueFromReward(), Is.True);
                Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Shop), $"节点 {node} 奖励后未进入商店");
                Assert.That(flow.ContinueFromShop(), Is.True);
                Assert.That(flow.NodeIndex, Is.EqualTo(node + 1));
                if (RunRoute.NextNodeIsBoss(node))
                {
                    Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal), $"节点 {node} 的下一场是 Boss 但未进揭示");
                    Assert.That(flow.BeginBossBattle(), Is.True);
                    Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
                }
                else
                {
                    Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle), $"节点 {node} 的下一场是普通战但未直接开战");
                }
            }

            Assert.That(flow.ContinueToForge(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaForge));

            flow.ReturnToMainMenu();
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
            Assert.That(flow.NodeIndex, Is.EqualTo(0));
        }

        [Test]
        public void CannotSkipRequiredStages()
        {
            var flow = new PrototypeFlowStateMachine();

            Assert.That(flow.BeginBossBattle(), Is.False);
            Assert.That(flow.CompleteBattle(true), Is.False);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
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
            var flow = new PrototypeFlowStateMachine();
            flow.StartNewRun();
            flow.ConfirmPersonaSetup();

            // 推进到最终节点 5（胜利走完全部前序节点）
            for (var node = 0; node < RunRoute.BattleCount - 1; node++)
            {
                Assert.That(flow.CompleteBattle(true), Is.True);
                Assert.That(flow.ContinueFromReward(), Is.True);
                Assert.That(flow.ContinueFromShop(), Is.True);
                if (RunRoute.NextNodeIsBoss(node))
                    Assert.That(flow.BeginBossBattle(), Is.True);
            }

            Assert.That(flow.NodeIndex, Is.EqualTo(RunRoute.BattleCount - 1));
            Assert.That(flow.CompleteBattle(false), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.FailureResult));
        }

        [Test]
        public void RestoreAcceptsValidNodeIndexAndRejectsInvalid()
        {
            var flow = new PrototypeFlowStateMachine();

            flow.Restore(PrototypeFlowStage.Battle, RunRoute.BattleCount - 1);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.NodeIndex, Is.EqualTo(RunRoute.BattleCount - 1));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore(PrototypeFlowStage.Battle, RunRoute.BattleCount));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore(PrototypeFlowStage.Battle, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => flow.Restore((PrototypeFlowStage)999, 0));
        }

        [Test]
        public void ReturningToEquipmentFromBossRevealKeepsCurrentNode()
        {
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
