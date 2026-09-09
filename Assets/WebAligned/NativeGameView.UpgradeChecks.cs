using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        IEnumerator CheckUpgradePage(string folder)
        {
            core.Action("start");core.EvaluateForTest("openShop(false);openShopUpgradeTarget([...shopItemById.values()].find(item=>item.effect.type==='UPGRADE_HAND_TYPE'));__flush();");state=core.Snapshot();Render();yield return null;
            Require(EditablePages.Any(p=>p!=null&&p.PageId=="Upgrade")&&pageRoot.GetComponent<NativePageTemplate>()!=null,"Upgrade scene template is not linked.");
            var coins=S(state["coins"]);Require(pageRoot.GetComponentsInChildren<Button>().Single(b=>b.name==T("#shop-upgrade-confirm")).interactable==false,"Upgrade confirmation is enabled without a target.");
            var target=S(state["upgradeTargets"][0]["id"]);ClickVisible("升级目标 · "+target);yield return null;
            Require(S(state["upgradeSelected"])==target&&S(state["coins"])==coins,"Target preview changed coins or lost selection.");
            Require(pageRoot.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Lv.")),"Upgrade target level missing.");
            yield return CapturePage(folder,"upgrade-targets");ClickVisible("取消");yield return null;
            Require(S(state["dialog"])=="#shop-dialog"&&S(state["coins"])==coins,"Upgrade cancellation did not return without charging.");
            Debug.Log("NATIVE_UPGRADE_PAGE_PASSED");
        }
    }
}
