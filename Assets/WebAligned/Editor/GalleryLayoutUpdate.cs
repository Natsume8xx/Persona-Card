using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace PersonaCards.WebAligned.Editor
{
    public static class GalleryLayoutUpdate
    {
        // Explicit one-page migration; ordinary imports/builds never regenerate UI.
        public static void Run()
        {
            try{
                using var rules=new NativeRules(false);var host=new GameObject("Gallery migration");
                var view=host.AddComponent<NativeGameView>();var page=view.BakePage(rules.Action("gallery"),false,AssetDatabase.LoadAssetAtPath<Font>("Assets/WebAligned/Fonts/NotoSerifSC.ttf"));
                foreach(var image in page.GetComponentsInChildren<Image>(true)){
                    var sprite=image.sprite;if(sprite==null||AssetDatabase.Contains(sprite))continue;
                    var path="Assets/WebAligned/UI/Sprite_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite.texture))+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(saved==null){saved=UnityEngine.Object.Instantiate(sprite);AssetDatabase.CreateAsset(saved,path);}image.sprite=saved;
                }
                foreach(var element in page.GetComponentsInChildren<NativeAuthoredElement>(true))element.CaptureBaseline();
                PrefabUtility.SaveAsPrefabAsset(page.gameObject,"Assets/WebAligned/UI/Gallery.prefab");UnityEngine.Object.DestroyImmediate(host);AssetDatabase.SaveAssets();Debug.Log("GALLERY_LAYOUT_UPDATE_PASSED");
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);return;}
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
