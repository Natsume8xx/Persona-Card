using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeSixFixTests
    {
        [Test] public void SettingsPreviewCancelDefaultsAndCommitAreSeparate()
        {
            var session=new NativeSettingsSession();session.Draft.Brightness=.75f;session.Draft.Volume=.25f;session.Draft.Motion=false;
            string committed=session.Commit();session.Draft.Brightness=1.2f;session.Defaults();Assert.AreEqual(1,session.Draft.Brightness);
            Assert.AreEqual(committed,session.SavedJson);session.Cancel();Assert.AreEqual(.75f,session.Draft.Brightness);Assert.IsFalse(session.Draft.Motion);
            var restored=new NativeSettingsSession(committed);Assert.AreEqual(.25f,restored.Draft.Volume);Assert.AreEqual(.75f,restored.Draft.Brightness);
            Assert.AreEqual(.7f,new NativeSettingsSession("{\"brightness\":0.2}").Draft.Brightness);
            Assert.AreEqual(1.2f,new NativeSettingsSession("{\"brightness\":1.2}").Draft.Brightness);
        }
        [Test] public void CancelNewRunAndCancelLoadoutPreserveExistingSave()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");rules.Action("toggle",index:0);rules.Action("play");
            var before=rules.Action("save-menu");string storage=before["storage"].ToString();
            Assert.AreEqual("#native-new-run-confirm",(string)rules.Action("loadout")["dialog"]);
            Assert.AreEqual(storage,rules.Action("new-run-cancel")["storage"].ToString());
            rules.Action("loadout");Assert.AreEqual("#start-loadout-dialog",(string)rules.Action("new-run-confirm")["dialog"]);
            Assert.AreEqual(storage,rules.Action("escape")["storage"].ToString());
            var resumed=rules.Action("continue");Assert.AreEqual((int)before["score"],(int)resumed["score"]);Assert.AreEqual(before["hand"].ToString(),resumed["hand"].ToString());
        }
        [Test] public void ExplicitSortUsesWebComparatorAndKeepsSelectedCards()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
            var selected=rules.Action("toggle",index:0);string uid=(string)selected["hand"][0]["uid"];
            var suit=rules.Action("sort","suit");Assert.AreEqual("suit",(string)suit["handSortMode"]);Assert.AreEqual(uid,(string)suit["hand"][(int)suit["selected"][0]]["uid"]);
            Assert.AreEqual("true",rules.EvaluateForTest("JSON.stringify(hand)===JSON.stringify([...hand].sort((a,b)=>({'♥':0,'♦':1,'♣':2,'♠':3}[a.s]-{'♥':0,'♦':1,'♣':2,'♠':3}[b.s]||b.ri-a.ri)))").ToLowerInvariant());
            var rank=rules.Action("sort","rank");Assert.AreEqual("rank",(string)rank["handSortMode"]);Assert.AreEqual(uid,(string)rank["hand"][(int)rank["selected"][0]]["uid"]);
            Assert.AreEqual("true",rules.EvaluateForTest("hand.every((c,i)=>i===0||hand[i-1].ri>=c.ri)").ToLowerInvariant());
        }
        [Test] public void DetailsReturnToLoadoutWithoutChangingEquippedPersonas()
        {
            using var rules=new NativeRules(false);var before=rules.Action("loadout");string id=(string)before["library"][0]["id"];
            var detail=rules.Action("persona-detail",id);Assert.AreEqual("#persona-detail-dialog",(string)detail["dialog"]);Assert.IsNotEmpty((string)detail["personaDetail"]["trigger"]);Assert.IsNotEmpty((string)detail["personaDetail"]["effect"]);
            var after=rules.Action("escape");Assert.AreEqual("#start-loadout-dialog",(string)after["dialog"]);Assert.AreEqual(before["pendingLoadout"].ToString(),after["pendingLoadout"].ToString());
            rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");var battle=rules.Snapshot();id=(string)battle["personas"].First(p=>p.Type==JTokenType.Object)["id"];
            detail=rules.Action("persona-detail",id);Assert.AreEqual((string)battle["personas"][0]["name"],(string)detail["personaDetail"]["name"]);Assert.AreEqual(battle["personas"].ToString(),rules.Action("escape")["personas"].ToString());
        }
        [Test] public void TutorialEscapeReturnsToSettingsAndModalEscapeDoesNotAdvanceRun()
        {
            using var rules=new NativeRules(false);var intro=rules.Action("start");Assert.AreEqual(intro["node"].ToString(),rules.Action("escape")["node"].ToString());Assert.AreEqual("#stage-intro-dialog",(string)rules.Snapshot()["dialog"]);
            rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");rules.Action("click","#replay-tutorial");Assert.AreEqual("#native-tutorial",(string)rules.Snapshot()["dialog"]);
            var returned=rules.Action("escape");Assert.IsTrue((bool)returned["settingsRequested"]);Assert.AreEqual("",(string)returned["dialog"]);
            rules.Action("click","#table-pile");Assert.AreEqual("",(string)rules.Action("escape")["dialog"]);
        }
    }
}
