using NUnit.Framework;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeShopFeedbackTests
    {
        [Test] public void PurchaseFeedbackRequiresCommittedPurchaseAndDoesNotMutateStorage()
        {
            using var rules=new NativeRules(false);rules.Action("start");rules.EvaluateForTest("openShop(false);coins=10000;__flush();");
            var before=rules.Snapshot();
            rules.EvaluateForTest("completeShopPurchase(shopItemById.get(selectedShopItemId),()=> 'test purchase');__flush();");
            var after=rules.Snapshot();var saved=after.ToString();
            Assert.AreEqual("购买成功",NativeShopFeedback.Success(before,after,"click","#shop-buy",0));
            Assert.IsNull(NativeShopFeedback.Success(before,after,"click","#shop-refresh",0));
            Assert.IsNull(NativeShopFeedback.Success(after,after,"click","#shop-buy",0));
            Assert.AreEqual(saved,after.ToString());
            rules.EvaluateForTest("coins=0;");before=rules.Snapshot();
            rules.EvaluateForTest("completeShopPurchase(shopItemById.get(selectedShopItemId),()=> 'must not run');__flush();");
            Assert.IsNull(NativeShopFeedback.Success(before,rules.Snapshot(),"click","#shop-buy",0));
        }
        [Test] public void UnlockFeedbackRequiresTheRequestedSlotToChange()
        {
            var before=Newtonsoft.Json.Linq.JObject.Parse("{dialog:'#shop-dialog',pool:[{id:'p',affixes:[{unlocked:false},{unlocked:false}]}]}");
            var after=(Newtonsoft.Json.Linq.JObject)before.DeepClone();after["pool"][0]["affixes"][0]["unlocked"]=true;
            Assert.AreEqual("副属性已解锁",NativeShopFeedback.Success(before,after,"unlock","p",0));
            Assert.IsNull(NativeShopFeedback.Success(before,after,"unlock","p",1));
            Assert.IsNull(NativeShopFeedback.Success(before,after,"unlock","p",-1));
            Assert.IsNull(NativeShopFeedback.Success(after,after,"unlock","p",0));
        }
    }
}
