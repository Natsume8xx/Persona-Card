using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeDeckSortingTests
    {
        [Test] public void SortButtonsCoverAllPilesWithoutChangingBattleOrOtherSort()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
            rules.Action("toggle",index:0);rules.Action("discard");rules.Action("toggle",index:0);rules.Action("play");
            var before=rules.Snapshot();rules.Action("click","#table-pile");
            foreach(var pile in new[]{"deck","used","discarded"}){
                var initial=rules.Action("deck-tab",pile);var ids=initial["deckCards"].Select(c=>(string)c["uid"]).ToArray();Assert.IsNotEmpty(ids);
                foreach(var mode in new[]{"suit","rank"}){
                    var sorted=rules.Action("click","#deck-sort-"+mode);Assert.AreEqual(mode,(string)sorted["deckSortMode"]);
                    CollectionAssert.AreEquivalent(ids,sorted["deckCards"].Select(c=>(string)c["uid"]).ToArray());
                    string expected=rules.EvaluateForTest("JSON.stringify(DeckSortRuntime.sortCards(deckCardsForTab(), '"+mode+"'))");
                    Assert.IsTrue(JToken.DeepEquals(JArray.Parse(expected),sorted["deckCards"]));
                    foreach(var key in new[]{"hand","hands","discards","score","storage","handSortMode"})Assert.AreEqual(before[key].ToString(),sorted[key].ToString(),key);
                }
            }
        }
        [Test] public void TargetCardUidAndRunDeckSurviveSortAndEmptyPilesAreValid()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");rules.Action("click","#table-pile");
            rules.Action("deck-tab","discarded");Assert.AreEqual(0,rules.Action("click","#deck-sort-suit")["deckCards"].Count());
            // Target selection fixture uses a real production item; it never buys or removes a card.
            rules.EvaluateForTest("deckShopItemId=[...shopItemById.values()].find(item=>item.effect.type==='REMOVE_CARD').id;openDeckDialog();");
            var s=rules.Snapshot();string uid=(string)s["runDeck"][0]["uid"];var selected=rules.Action("deck-select",uid);
            foreach(var mode in new[]{"rank","suit"}){
                var sorted=rules.Action("click","#deck-sort-"+mode);Assert.AreEqual(uid,(string)sorted["deckSelected"]);
                Assert.AreEqual(selected["runDeck"].ToString(),sorted["runDeck"].ToString());Assert.AreEqual(selected["coins"],sorted["coins"]);
            }
        }
    }
}
