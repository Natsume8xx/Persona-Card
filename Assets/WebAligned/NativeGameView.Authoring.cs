using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        public static string PageId(JObject snapshot,bool settings=false)
        {
            if(settings)return (bool)snapshot["menu"]?"SettingsMenu":"Settings";
            string dialog=(string)snapshot["dialog"]??"";
            return dialog switch{
                "#native-new-run-confirm"=>"NewRunConfirm","#persona-detail-dialog"=>(bool)snapshot["menu"]?"PersonaDetailMenu":"PersonaDetail",
                "#start-loadout-dialog"=>"Loadout","#native-gallery"=>"Gallery",
                "#stage-intro-dialog"=>"StageIntro","#settlement-dialog"=>"Settlement",
                "#shop-dialog"=>(string)snapshot["shop"]?["tab"]=="forge"?"Forge":"Shop",
                "#persona-growth-dialog"=>"PersonaGrowth","#target-report-dialog"=>"Report",
                "#target-carry-dialog"=>"Carry","#deck-dialog"=>"Deck",
                "#hand-rules-dialog"=>"HandRules","#shop-upgrade-dialog"=>"Upgrade",
                "#native-tutorial"=>"Tutorial"+((int?)snapshot["tutorial"]?["index"]??0),
                _=>(bool)snapshot["menu"]?"Menu":"Battle"
            };
        }
        // Authoring creates actual UGUI assets, independent of a saved run and without entering Play.
        public NativePageTemplate BakePage(JObject snapshot,bool settings,Font persistentFont)
        {
            baking=true;state=snapshot;settingsOpen=settings;font=InterfaceFont=persistentFont;
            if(root==null)
            {
                var canvas=new GameObject("Bake Canvas",typeof(RectTransform),typeof(Canvas));
                canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                root=(RectTransform)canvas.transform;
            }
            Render();Canvas.ForceUpdateCanvases();
            var template=NativePageFactory.Ensure<NativePageTemplate>(pageRoot.gameObject);
            template.PageId=activePageId;
            return template;
        }
#if UNITY_EDITOR
        public void ValidateTemplateBinding(JObject snapshot,NativePageTemplate[] templates,Font persistentFont)
        {
            BakePage(snapshot,false,persistentFont);
            EditablePages=templates;baking=false;Render();Canvas.ForceUpdateCanvases();
        }
#endif
    }
}


