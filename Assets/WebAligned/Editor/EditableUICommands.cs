using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.WebAligned.Editor
{
    public static class EditableUICommands
    {
        public const string ScenePath="Assets/Scenes/PersonaWebAligned.unity";
        const string Folder="Assets/WebAligned/UI";
        [MenuItem("Persona Cards/打开可编辑界面")]
        public static void OpenEditable()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("请先停止 Play，再打开可编辑界面。");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            var view=UnityEngine.Object.FindFirstObjectByType<NativeGameView>();
            var battle=view!=null?view.EditablePages.FirstOrDefault(p=>p!=null&&p.PageId=="Battle"):null;
            if(battle!=null){Selection.activeGameObject=battle.gameObject;SceneView.lastActiveSceneView?.FrameSelected();}
        }
        // Explicit build command; never regenerates or overwrites authored assets on ordinary import/build.
        public static void BakeAll()
        {
            try
            {
                Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
                string fontPath="Assets/WebAligned/Fonts/NotoSerifSC.ttf";
                var font=AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                if(font==null)throw new Exception("Noto Serif SC font was not imported.");
                using var rules=new NativeRules(false);
                var pages=new Dictionary<string,NativePageTemplate>();
                var host=new GameObject("UI authoring builder");var view=host.AddComponent<NativeGameView>();
                void Bake(JObject s,bool settings=false)
                {
                    string id=NativeGameView.PageId(s,settings);
                    if(pages.ContainsKey(id))return;
                    var page=view.BakePage(s,settings,font);
                    // Save button sprites as real assets, not transient Sprite.Create objects.
                    foreach(var image in page.GetComponentsInChildren<Image>(true))
                    {
                        var sprite=image.sprite;if(sprite==null||AssetDatabase.Contains(sprite))continue;
                        string texturePath=AssetDatabase.GetAssetPath(sprite.texture);
                        string spritePath=Folder+"/Sprite_"+AssetDatabase.AssetPathToGUID(texturePath)+".asset";
                        var saved=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if(saved==null){saved=UnityEngine.Object.Instantiate(sprite);AssetDatabase.CreateAsset(saved,spritePath);}
                        image.sprite=saved;
                    }
                    foreach(var element in page.GetComponentsInChildren<NativeAuthoredElement>(true))element.CaptureBaseline();
                    var prefab=PrefabUtility.SaveAsPrefabAsset(page.gameObject,Folder+"/"+id+".prefab");pages[id]=prefab.GetComponent<NativePageTemplate>();
                }
                Bake(rules.Snapshot());Bake(rules.Snapshot(),true);var loadout=rules.Action("loadout");Bake(loadout);Bake(rules.Action("persona-detail",(string)loadout["library"][0]["id"]));rules.Action("escape");rules.Action("click","#start-loadout-close");Bake(rules.Action("gallery"));rules.Action("close-gallery");
                Bake(rules.Action("start"));var battle=rules.Action("click","#stage-start");
                if((string)battle["dialog"]=="#native-tutorial")battle=rules.Action("click","#tutorial-skip");
                Bake(battle);Bake(battle,true);Bake(rules.Action("persona-detail",(string)battle["personas"][0]["id"]));rules.Action("escape");Bake(rules.Action("click","#table-pile"));rules.Action("click","#deck-done");Bake(rules.Action("click","#open-hand-rules"));rules.Action("click","#hand-rules-done");
                rules.Action("toggle",index:0);rules.Action("play");
                for(int i=0;i<4;i++){var tutorial=rules.Action(i==0?"tutorial":"click",i==0?null:"#tutorial-next");Bake(tutorial);}rules.Action("click","#tutorial-skip");
                rules.Action("menu");Bake(rules.Action("loadout"));rules.Action("new-run-cancel");rules.Action("continue");
                // Walk source-declared nodes with explicit preview fixtures, never player saves.
                for(int guard=0;guard<85;guard++)
                {
                    var s=rules.Snapshot();Bake(s);string dialog=(string)s["dialog"];
                    if((bool)s["menu"])break;
                    switch(dialog)
                    {
                        case "#stage-intro-dialog":rules.Action("click","#stage-start");break;
                        case "#native-tutorial":rules.Action("click","#tutorial-skip");break;
                        case "":rules.Action("toggle",index:0);rules.Action("play");rules.EvaluateForTest("score=battleTarget();finish(true);__flush();");break;
                        case "#settlement-dialog":rules.Action("click","#result-primary");break;
                        case "#persona-growth-dialog":rules.Action("growth-slot",index:0);rules.Action("click","#persona-growth-confirm");break;
                        case "#shop-dialog":rules.Action("shop-tab","forge");Bake(rules.Action("shop-persona",(string)s["pool"][0]["id"]));rules.Action("click","#shop-leave");break;
                        case "#target-report-dialog":rules.Action("click","#target-report-continue");break;
                        case "#target-carry-dialog":if(s["carry"]["ids"].Any())rules.Action("carry-select",(string)s["carry"]["ids"][0]);rules.Action("click","#target-carry-confirm");break;
                        default:throw new Exception("Unknown authoring page "+dialog);
                    }
                }
                UnityEngine.Object.DestroyImmediate(host);
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var game=new GameObject("人格牌 · 可编辑界面");var driver=game.AddComponent<NativeGameView>();driver.InterfaceFont=font;
                var canvas=new GameObject("Editable UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(game.transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var instances=new List<NativePageTemplate>();
                foreach(var pair in pages)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+pair.Key+".prefab"),canvas.transform);
                    instance.name=pair.Key;instance.SetActive(pair.Key=="Battle");instances.Add(instance.GetComponent<NativePageTemplate>());
                }
                driver.EditablePages=instances.ToArray();
                EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
                AssetDatabase.SaveAssets();Debug.Log("EDITABLE_UI_BAKE_PASSED pages="+pages.Count);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);return;}
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}





