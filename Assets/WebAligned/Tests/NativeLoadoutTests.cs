using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeLoadoutTests
    {
        [Test] public void SwapReplaceRemoveAndConfirmUseWebRules()
        {
            using var rules=new NativeRules(false);var initial=rules.Action("loadout");
            var ids=initial["pendingLoadout"].Values<string>().ToArray();var saved=initial["storage"].ToString();
            var swapped=rules.Action("loadout-place",ids[0],1);
            Assert.AreEqual(ids[1],(string)swapped["pendingLoadout"][0]);Assert.AreEqual(ids[0],(string)swapped["pendingLoadout"][1]);
            var spare=initial["library"].First(p=>!ids.Contains((string)p["id"]));
            var replaced=rules.Action("loadout-place",(string)spare["id"],2);Assert.AreEqual((string)spare["id"],(string)replaced["pendingLoadout"][2]);
            var removed=rules.Action("loadout-remove",index:0);Assert.AreEqual(JTokenType.Null,removed["pendingLoadout"][0].Type);
            Assert.IsTrue((bool)removed["disabled"]["#start-loadout-confirm"]);
            Assert.AreEqual("#start-loadout-dialog",(string)rules.Action("click","#start-loadout-confirm")["dialog"]);
            Assert.AreEqual(saved,rules.Snapshot()["storage"].ToString(),"Pending edits must not persist or start a run.");
            var filled=rules.Action("loadout-place",ids[1],0);Assert.IsFalse((bool)filled["disabled"]["#start-loadout-confirm"]);
            var confirmed=rules.Action("click","#start-loadout-confirm");
            Assert.AreNotEqual("#start-loadout-dialog",(string)confirmed["dialog"]);
            Assert.AreEqual(filled["pendingLoadout"].ToString(),JArray.Parse((string)confirmed["storage"]["persona-loadout"]).ToString());
        }
        [Test] public void InvalidPlacementAndCancelledLoadoutDoNotPersist()
        {
            using var rules=new NativeRules(false);var initial=rules.Action("loadout");var pending=initial["pendingLoadout"].ToString();
            foreach(var slot in new[]{-1,99}){rules.Action("loadout-place",(string)initial["library"][0]["id"],slot);rules.Action("loadout-remove",index:slot);}
            Assert.AreEqual(pending,rules.Action("loadout-place","missing-persona",0)["pendingLoadout"].ToString());
            rules.Action("loadout-remove",index:0);var closed=rules.Action("click","#start-loadout-close");Assert.AreEqual(initial["storage"].ToString(),closed["storage"].ToString());
            var restored=rules.Action("loadout");Assert.AreEqual(pending,restored["pendingLoadout"].ToString());
        }
    }
}
