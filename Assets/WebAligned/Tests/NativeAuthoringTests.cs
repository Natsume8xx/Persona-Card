using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Linq;
using UnityEngine.TestTools;

namespace PersonaCards.WebAligned.Tests
{
    public class NativeAuthoringTests
    {
        [Test]
        public void SerializedPagesRebindLiveMenuBattleAndDeckWithoutHierarchyAssumptions()
        {
            var host=new GameObject("Native UI binding test");
            try
            {
                var view=host.AddComponent<NativeGameView>();
                var templates=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/WebAligned/UI"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)).GetComponent<NativePageTemplate>()).ToArray();
                var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/WebAligned/Fonts/NotoSerifSC.ttf");
                using var rules=new NativeRules(false);
                view.ValidateTemplateBinding(rules.Snapshot(),templates,font);
                view.ValidateTemplateBinding(rules.Action("loadout"),templates,font);
                rules.Action("start");rules.Action("click","#stage-start");rules.Action("click","#tutorial-skip");
                view.ValidateTemplateBinding(rules.Snapshot(),templates,font);
                foreach(var text in host.GetComponentsInChildren<Text>().Where(t=>t.text=="人格牌01"||t.text=="0"))
                {
                    var generator=new TextGenerator();
                    generator.Populate(text.text,text.GetGenerationSettings(text.rectTransform.rect.size));
                    Assert.Greater(generator.vertexCount,0,"Text must remain visible with the shipped Chinese font: "+text.text);
                }
                view.ValidateTemplateBinding(rules.Action("click","#table-pile"),templates,font);
                LogAssert.NoUnexpectedReceived();
            }
            finally{Object.DestroyImmediate(host);}
        }
        [Test]
        public void AuthoredLayoutAndFontSurviveFreshDataBinding()
        {
            var root=new GameObject("Page",typeof(RectTransform),typeof(NativePageTemplate));
            try
            {
                var template=root.GetComponent<NativePageTemplate>();var factory=new NativePageFactory((RectTransform)root.transform,null);
                var r=factory.Rect(root.transform,"Score",10,20,200,60);var text=r.gameObject.AddComponent<Text>();text.text="950";text.fontSize=24;
                var binding=r.GetComponent<NativeAuthoredElement>();binding.CaptureBaseline();
                r.anchoredPosition=new Vector2(75,-80);r.sizeDelta=new Vector2(260,80);text.fontSize=36;
                var rebound=new NativePageFactory((RectTransform)root.transform,template);
                var same=rebound.Rect(root.transform,"Score",10,20,200,60);Assert.AreSame(r,same);
                text.text="1200";text.fontSize=24;rebound.Finish();
                Assert.AreEqual(new Vector2(75,-80),r.anchoredPosition);Assert.AreEqual(new Vector2(260,80),r.sizeDelta);
                Assert.AreEqual(36,text.fontSize);Assert.AreEqual("1200",text.text,"Authoring must not freeze live scores.");
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void SavedPagesHaveSerializableGraphicsAndAnEditableBattle()
        {
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/WebAligned/UI"});Assert.GreaterOrEqual(paths.Length,14);
            foreach(var guid in paths)
            {
                var page=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsNotNull(page.GetComponent<NativePageTemplate>());
                foreach(var transform in page.GetComponentsInChildren<Transform>(true))Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),transform.name);
                foreach(var graphic in page.GetComponentsInChildren<Graphic>(true))Assert.IsNotNull(graphic.GetComponent<CanvasRenderer>(),graphic.name);
            }
            var battle=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WebAligned/UI/Battle.prefab");
            Assert.Greater(battle.GetComponentsInChildren<Text>(true).Length,25);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/PersonaWebAligned.unity"));
        }
    }
}

