using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
namespace PersonaCards.WebAligned.Editor
{
    public static class UpgradeLayoutUpdate
    {
        // Explicit one-page migration; ordinary imports/builds never regenerate UI.
        public static void Run()
        {
            try{
                using var rules=new NativeRules(false);var host=new GameObject("Upgrade migration");
                var view=host.AddComponent<NativeGameView>();rules.Action("start");rules.EvaluateForTest("openShop(false);openShopUpgradeTarget([...shopItemById.values()].find(item=>item.effect.type===\"UPGRADE_HAND_TYPE\"));__flush();");var page=view.BakePage(rules.Snapshot(),false,AssetDatabase.LoadAssetAtPath<Font>("Assets/WebAligned/Fonts/NotoSerifSC.ttf"));
                foreach(var image in page.GetComponentsInChildren<Image>(true)){
                    var sprite=image.sprite;if(sprite==null||AssetDatabase.Contains(sprite))continue;
                    var path="Assets/WebAligned/UI/Sprite_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite.texture))+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(saved==null){saved=UnityEngine.Object.Instantiate(sprite);AssetDatabase.CreateAsset(saved,path);}image.sprite=saved;
                }
                foreach(var element in page.GetComponentsInChildren<NativeAuthoredElement>(true))element.CaptureBaseline();
                PrefabUtility.SaveAsPrefabAsset(page.gameObject,"Assets/WebAligned/UI/Upgrade.prefab");UnityEngine.Object.DestroyImmediate(host);AssetDatabase.SaveAssets();
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PersonaWebAligned.unity");var driver=UnityEngine.Object.FindAnyObjectByType<NativeGameView>();
                if(!driver.EditablePages.Any(p=>p!=null&&p.PageId=="Upgrade")){var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WebAligned/UI/Upgrade.prefab"),driver.transform.Find("Editable UI"));instance.SetActive(false);driver.EditablePages=driver.EditablePages.Concat(new[]{instance.GetComponent<NativePageTemplate>()}).ToArray();EditorSceneManager.SaveScene(scene);}
                Debug.Log("UPGRADE_LAYOUT_UPDATE_PASSED");
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);return;}
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}

