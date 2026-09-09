using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeScoreDetailsTests
    {
        [Test] public void RemovedDetailsActionCannotOpenDialogOrChangeBattle()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
            rules.Action("toggle",index:0);var played=rules.Action("play");
            string details=(string)played["scoreDetails"];
            StringAssert.Contains("基础",details);StringAssert.Contains("结算",details);
            StringAssert.Contains(((int)played["lastScore"]["total"])+" 分",details);
            Assert.Catch(()=>rules.Action("score-details"));Assert.AreEqual("",(string)rules.Snapshot()["dialog"]);
            Assert.Catch(()=>rules.Action("score-details-close"));var closed=rules.Snapshot();Assert.AreEqual("",(string)closed["dialog"]);
            foreach(var key in new[]{"score","hands","discards","hand","personas","storage"})Assert.AreEqual(played[key].ToString(),closed[key].ToString(),key);
            Assert.AreEqual(details,(string)closed["scoreDetails"]);
        }
        [Test] public void NewSelectionClearsPreviousHandDetailsAsOnWeb()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
            Assert.Catch(()=>rules.Action("score-details"));Assert.AreEqual("",(string)rules.Snapshot()["dialog"]);
            rules.Action("toggle",index:0);Assert.IsNotEmpty((string)rules.Action("play")["scoreDetails"]);
            Assert.AreEqual("",(string)rules.Action("toggle",index:0)["scoreDetails"]);
            Assert.Catch(()=>rules.Action("score-details"));Assert.AreEqual("",(string)rules.Snapshot()["dialog"]);
        }
    }
}


