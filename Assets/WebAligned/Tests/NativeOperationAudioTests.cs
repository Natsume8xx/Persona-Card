using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeOperationAudioTests
    {
        [Test] public void AudioIsConsumedOnceAndSnapshotsDoNotReplayOrChangeState()
        {
            using var rules=new NativeRules(false);rules.Action("loadout");var initial=rules.Snapshot();
            rules.Action("loadout-place",(string)initial["pendingLoadout"][0],1);
            var before=rules.Snapshot().ToString();rules.Snapshot();
            CollectionAssert.Contains(rules.DrainAudioCues().Values<string>().ToArray(),"select");
            Assert.IsEmpty(rules.DrainAudioCues());Assert.AreEqual(before,rules.Snapshot().ToString());
            rules.Action("loadout-remove",index:0);CollectionAssert.Contains(rules.DrainAudioCues().Values<string>().ToArray(),"discard");
            foreach(var name in new[]{"chips-handle-1","open_001","card-slide-1","click_001","card-shove-1"})Assert.NotNull(Resources.Load<AudioClip>("WebAudio/sfx/"+name),name);
        }
        [Test] public void PurchaseSuccessEmitsBuyButInsufficientCoinsDoesNot()
        {
            using var rules=new NativeRules(false);rules.Action("start");
            // Real production purchase gate with a test-only effect callback; no invented gameplay values.
            rules.EvaluateForTest("openShop(false);coins=10000;");rules.DrainAudioCues();
            Assert.AreEqual("true",rules.EvaluateForTest("completeShopPurchase(shopItemById.get(selectedShopItemId),()=> 'test purchase')").ToLowerInvariant());
            CollectionAssert.Contains(rules.DrainAudioCues().Values<string>().ToArray(),"buy");
            rules.EvaluateForTest("coins=0;completeShopPurchase(shopItemById.get(selectedShopItemId),()=> 'must not run');");
            CollectionAssert.DoesNotContain(rules.DrainAudioCues().Values<string>().ToArray(),"buy");
        }
    }
}
