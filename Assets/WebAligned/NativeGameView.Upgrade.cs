using Newtonsoft.Json.Linq;
using UnityEngine;
namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        void RenderUpgradePage(RectTransform panel)
        {
            TextScroll(panel,T("#shop-upgrade-copy"),55,115,1230,48,21,Muted);
            var targets=(JArray)state["upgradeTargets"];
            var body=ScrollBody(panel,55,180,1230,480,Mathf.Ceil(targets.Count/3f)*125);
            if(targets.Count==0)Label(body,"当前没有可强化的目标。",20,30,1180,60,26,Muted);
            for(int i=0;i<targets.Count;i++){
                var target=targets[i];var button=Btn(body,S(target["name"])+"  Lv."+S(target["level"])+"\n"+S(target["detail"]),(i%3)*410,(i/3)*125,395,105,()=>Act("upgrade-select",S(target["id"])),S(state["upgradeSelected"])==S(target["id"]));
                button.name="升级目标 · "+S(target["id"]);var selected=S(state["upgradeSelected"])==S(target["id"]);
                button.GetComponent<UnityEngine.UI.Image>().color=selected?new Color32(64,43,18,255):new Color32(24,18,12,255);
                var frame=button.GetComponentInChildren<NativeFrameGraphic>();frame.color=selected?Gold:new Color(Gold.r,Gold.g,Gold.b,.28f);frame.Thickness=selected?2:1;
                var text=button.GetComponentInChildren<UnityEngine.UI.Text>();text.color=selected?Pale:new Color32(216,199,168,255);text.fontStyle=FontStyle.Normal;text.lineSpacing=1.2f;text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=24;
            }
            Label(panel,T("#shop-upgrade-price"),300,695,740,50,25,Gold,TextAnchor.MiddleCenter);
            Click(panel,T("#shop-upgrade-confirm"),740,800,420,"#shop-upgrade-confirm");Click(panel,"取消",170,800,420,"#shop-upgrade-cancel",false);
        }
    }
}
