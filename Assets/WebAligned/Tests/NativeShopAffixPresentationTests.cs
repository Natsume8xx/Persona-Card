using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace PersonaCards.WebAligned.Tests
{
    public class NativeShopAffixPresentationTests
    {
        [TestCase("PROFILE_LOCKED","需 TARGET_SHOP_LATE 商店")]
        [TestCase("PREVIOUS_SLOT_LOCKED","请先解锁第二属性")]
        public void ExplainsSourceRestrictionWithoutChangingAvailability(string reason,string expected)
        {
            var affix=JObject.FromObject(new{unlocked=false,availability=new{allowed=false,reason,nextProfileId="TARGET_SHOP_LATE"}});string before=affix.ToString();
            StringAssert.Contains(expected,NativeShopAffixPresentation.Description(affix));Assert.AreEqual(before,affix.ToString());
            affix["unlocked"]=true;affix["effectText"]="原有效果";Assert.AreEqual("原有效果",NativeShopAffixPresentation.Description(affix));
        }
    }
}
