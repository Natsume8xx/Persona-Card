using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeRulesTests
    {
        NativeRules rules;
        [SetUp] public void Setup(){rules=new NativeRules(false);}
        [TearDown] public void Teardown(){rules.Dispose();}
        void Begin(){rules.Action("start");rules.Action("click","#stage-start");}
        JObject ColdRestore(){var storage=rules.Snapshot()["storage"].ToString();rules.Dispose();rules=new NativeRules(false);rules.EvaluateForTest("NativeRestoreStorage("+Newtonsoft.Json.JsonConvert.SerializeObject(storage)+")");return rules.Action("continue");}
        [Test] public void ColdRestartRestoresCommittedDeckAndDiscards(){Begin();rules.Action("toggle",index:0);rules.Action("discard");rules.Action("toggle",index:1);var before=rules.Action("play");var after=ColdRestore();Assert.AreEqual(before["hand"].ToString(),after["hand"].ToString());Assert.AreEqual(before["personas"].ToString(),after["personas"].ToString());Assert.AreEqual((int)before["score"],(int)after["score"]);rules.Action("click","#table-pile");after=rules.Action("deck-tab","discarded");Assert.AreEqual(1,after["deckCards"].Count());after=rules.Action("deck-tab","used");Assert.AreEqual(1,after["deckCards"].Count());after=rules.Action("menu");Assert.AreEqual("",(string)after["dialog"]);Assert.IsTrue((bool)after["canContinue"]);}
        [Test] public void FormalManifestMatchesWebSeventeenNodes(){var s=rules.Action("start");Assert.AreEqual(17,s["nodes"].Count());Assert.AreEqual(13,s["nodes"].Count(n=>(string)n["type"]=="BATTLE"));Assert.AreEqual(3,s["nodes"].Count(n=>(string)n["type"]=="PERSONA_GROWTH"));Assert.AreEqual("SHOP",(string)s["nodes"][15]["type"]);Assert.AreEqual(950,(int)s["target"]);Assert.AreEqual(4,s["personas"].Count(p=>p.Type!=JTokenType.Null));}
        [Test] public void ShopPurchasesAndAffixesUseOriginalCostsAndSurviveRestart()
        {
            rules.Action("start");for(int i=0;i<3;i++){rules.Action("click","#stage-start");rules.Action("toggle",index:0);rules.Action("play");rules.EvaluateForTest("score=battleTarget();finish(true);__flush();");rules.Action("click","#result-primary");}
            rules.Action("growth-slot",index:0);rules.Action("click","#persona-growth-confirm");rules.EvaluateForTest("coins=100;");var s=rules.Action("shop-tab","goods");int purchases=0;
            foreach(var offer in ((JArray)s["shop"]["offers"]).ToArray())
            {
                s=rules.Action("shop-select",(string)offer["item"]["id"]);if((bool?)s["disabled"]["#shop-buy"]==true)continue;int before=(int)s["coins"];s=rules.Action("click","#shop-buy");
                if((string)s["dialog"]=="#shop-upgrade-dialog"){rules.Action("upgrade-select",(string)s["upgradeTargets"][0]["id"]);s=rules.Action("click","#shop-upgrade-confirm");}
                else if((string)s["dialog"]=="#deck-dialog"){rules.Action("deck-select",(string)s["runDeck"][0]["uid"]);s=rules.Action("click","#deck-confirm");}
                Assert.AreEqual("#shop-dialog",(string)s["dialog"]);Assert.Less((int)s["coins"],before);purchases++;
            }
            Assert.Greater(purchases,0);s=rules.Action("shop-tab","forge");var persona=s["pool"].First(p=>(bool?)p["affixes"]?[0]?["availability"]?["allowed"]==true);var id=(string)persona["id"];int price=(int)persona["affixes"][0]["unlockCost"],balance=(int)s["coins"];s=rules.Action("unlock",id,0);Assert.AreEqual(balance-price,(int)s["coins"]);var unlocked=s["pool"].First(p=>(string)p["id"]==id);Assert.IsTrue((bool)unlocked["affixes"][0]["unlocked"]);Assert.IsNotEmpty((string)unlocked["affixes"][0]["effectText"]);var after=ColdRestore();Assert.AreEqual(s["runDeck"].ToString(),after["runDeck"].ToString());Assert.AreEqual(unlocked.ToString(),after["pool"].First(p=>(string)p["id"]==id).ToString());
        }
        [Test] public void NativeLoadoutUsesWebSelection(){rules.Action("loadout");rules.Action("loadout-place","collector",0);var s=rules.Action("click","#start-loadout-confirm");Assert.AreEqual("collector",(string)s["personas"][0]["instance"]["templateId"]);}
        [Test] public void PreviewDoesNotMutatePersonaAndCommitMatches(){Begin();rules.Action("toggle",index:0);var before=rules.EvaluateForTest("JSON.stringify(personaRuntime.getState())");var s=rules.Snapshot();var expected=(int)s["preview"]["total"];for(int i=0;i<5;i++)rules.Snapshot();Assert.AreEqual(before,rules.EvaluateForTest("JSON.stringify(personaRuntime.getState())"));s=rules.Action("play");Assert.AreEqual(expected,(int)s["score"]);Assert.AreEqual(3,(int)s["hands"]);Assert.IsFalse((bool)s["inputLocked"]);}
        [Test] public void ActiveSaveRestoresCardsScoreAndPersonaExactly(){Begin();rules.Action("toggle",index:0);rules.Action("play");var before=rules.Snapshot();rules.Action("menu");var after=rules.Action("continue");Assert.AreEqual(before["hand"].ToString(),after["hand"].ToString());Assert.AreEqual(before["personas"].ToString(),after["personas"].ToString());Assert.AreEqual((int)before["score"],(int)after["score"]);}
        [Test] public void DiscardChargesOnlyOnCommit(){rules.Action("loadout");rules.Action("loadout-place","collector",0);rules.Action("click","#start-loadout-confirm");rules.Action("click","#stage-start");rules.Action("toggle",index:0);rules.Action("discard");var s=rules.Snapshot();Assert.IsTrue((bool)s["personas"][0]["instance"]["runtimeState"]["charged"]);rules.Action("toggle",index:0);rules.Snapshot();Assert.IsTrue((bool)rules.Snapshot()["personas"][0]["instance"]["runtimeState"]["charged"]);rules.Action("play");Assert.IsFalse((bool)rules.Snapshot()["personas"][0]["instance"]["runtimeState"]["charged"]);}
        [Test] public void FailureCanFinishWithoutForgingInventedReward(){Begin();rules.EvaluateForTest("score=0;hands=0;finish(false);__flush();");rules.Action("click","#result-primary");Assert.AreEqual("#target-report-dialog",(string)rules.Snapshot()["dialog"]);rules.Action("click","#target-report-continue");Assert.AreEqual("#target-carry-dialog",(string)rules.Snapshot()["dialog"]);var s=rules.Action("click","#target-carry-confirm");Assert.IsTrue((bool)s["menu"]);Assert.IsFalse((bool)s["canContinue"]);}
        [Test] public void EntireSeventeenNodeJourneyKeepsShopAndGeneratedRewardsOnRestore()
        {
            rules.Action("start");int battles=0,growth=0,shops=0;
            for(int guard=0;guard<90;guard++)
            {
                var s=rules.Snapshot();if((bool)s["menu"]&&battles>0)break;
                switch((string)s["dialog"])
                {
                    case "#stage-intro-dialog": rules.Action("click","#stage-start");break;
                    case "#native-tutorial": rules.Action("click","#tutorial-skip");break;
                    case "":
                        // Test-only victory precondition; production has no score override action.
                        rules.Action("toggle",index:0);rules.Action("play");rules.EvaluateForTest("score=battleTarget();finish(true);__flush();");battles++;break;
                    case "#settlement-dialog":rules.Action("click","#result-primary");break;
                    case "#persona-growth-dialog":
                        var generated=s["growth"]["persona"].ToString();var restored=ColdRestore();Assert.AreEqual(generated,restored["growth"]["persona"].ToString());rules.Action("growth-slot",index:0);rules.Action("click","#persona-growth-confirm");growth++;break;
                    case "#shop-dialog":
                        var offers=s["shop"]["offers"].ToString();restored=ColdRestore();Assert.AreEqual(offers,restored["shop"]["offers"].ToString());rules.Action("click","#shop-refresh");rules.Action("click","#shop-leave");shops++;break;
                    case "#target-report-dialog":rules.Action("click","#target-report-continue");break;
                    case "#target-carry-dialog":var ids=(JArray)s["carry"]["ids"];if(ids.Count>0)rules.Action("carry-select",(string)ids[0]);rules.Action("click","#target-carry-confirm");break;
                    default:Assert.Fail("Unexpected screen: "+s["dialog"]);break;
                }
            }
            Assert.AreEqual(13,battles);Assert.AreEqual(3,growth);Assert.AreEqual(4,shops);Assert.IsTrue((bool)rules.Snapshot()["menu"]);Assert.IsFalse((bool)rules.Snapshot()["canContinue"]);
        }
    }
}
