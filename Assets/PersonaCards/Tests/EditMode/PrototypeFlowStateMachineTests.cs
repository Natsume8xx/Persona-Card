using NUnit.Framework;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    public sealed class PrototypeFlowStateMachineTests
    {
        [Test]
        public void HappyPathMovesThroughEveryPrototypeStage()
        {
            var flow = new PrototypeFlowStateMachine();

            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
            Assert.That(flow.StartNewRun(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaSetup));
            Assert.That(flow.ConfirmPersonaSetup(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Battle));
            Assert.That(flow.BattleNumber, Is.EqualTo(1));
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Reward));
            Assert.That(flow.ContinueFromReward(), Is.True);
            Assert.That(flow.BattleNumber, Is.EqualTo(2));
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.Shop));
            Assert.That(flow.ContinueFromShop(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.BossReveal));
            Assert.That(flow.BeginBossBattle(), Is.True);
            Assert.That(flow.BattleNumber, Is.EqualTo(3));
            Assert.That(flow.CompleteBattle(true), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.RunReport));
            Assert.That(flow.ContinueToForge(), Is.True);
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.PersonaForge));

            flow.ReturnToMainMenu();
            Assert.That(flow.Stage, Is.EqualTo(PrototypeFlowStage.MainMenu));
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
    }
}
