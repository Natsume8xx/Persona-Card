using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace PersonaCards.WebAligned.Tests
{
    public class NativeBattleInputTests
    {
        static JObject Battle() => JObject.Parse("{menu:false,inputLocked:false,dialog:'',selected:[0],hands:4,discards:3}");
        [TestCase("#native-tutorial")]
        [TestCase("#persona-growth-dialog")]
        [TestCase("#settlement-dialog")]
        public void ModalRejectsBattleCommands(string dialog)
        {
            var state=Battle();state["dialog"]=dialog;
            Assert.IsFalse(NativeBattleInput.CanResolve(state,"play"));
            Assert.IsFalse(NativeBattleInput.CanResolve(state,"discard"));
        }
        [Test] public void BattleRequiresSelectionAndAvailableAction()
        {
            var state=Battle();Assert.IsTrue(NativeBattleInput.CanResolve(state,"play"));
            state["hands"]=0;Assert.IsFalse(NativeBattleInput.CanResolve(state,"play"));
            Assert.IsTrue(NativeBattleInput.CanResolve(state,"discard"));
            state["selected"]=new JArray();Assert.IsFalse(NativeBattleInput.CanResolve(state,"discard"));
        }
        [Test] public void LockedBattleRejectsCommands()
        {
            var state=Battle();state["inputLocked"]=true;Assert.IsFalse(NativeBattleInput.CanResolve(state,"play"));
        }
    }
}
