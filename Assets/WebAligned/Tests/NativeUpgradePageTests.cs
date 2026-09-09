using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace PersonaCards.WebAligned.Tests
{
    public class NativeUpgradePageTests
    {
        [TestCase("UPGRADE_HAND_TYPE")]
        [TestCase("UPGRADE_SUIT")]
        public void UpgradeHasTemplateAndSelectionDoesNotSpend(string effect)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WebAligned/UI/Upgrade.prefab");Assert.NotNull(prefab);Assert.AreEqual("Upgrade",prefab.GetComponent<NativePageTemplate>().PageId);
            using var rules=new NativeRules(false);rules.Action("start");rules.EvaluateForTest("openShop(false);openShopUpgradeTarget([...shopItemById.values()].find(item=>item.effect.type==='"+effect+"'));__flush();");
            var before=rules.Snapshot();Assert.IsTrue((bool)before["disabled"]["#shop-upgrade-confirm"]);Assert.Greater(before["upgradeTargets"].Count(),0);
            var after=rules.Action("upgrade-select",(string)before["upgradeTargets"][0]["id"]);Assert.AreEqual((int)before["coins"],(int)after["coins"]);
            after=rules.Action("click","#shop-upgrade-cancel");Assert.AreEqual("#shop-dialog",(string)after["dialog"]);Assert.AreEqual((int)before["coins"],(int)after["coins"]);
        }
    }
}

