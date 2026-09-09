using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace PersonaCards.WebAligned.Editor
{
    public static class RemoveScoreDetailsUI
    {
        public static void Run()
        {
            try
            {
                foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/WebAligned/UI"}))
                {
                    var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool changed=false;
                        foreach(var button in root.GetComponentsInChildren<Button>(true).Where(b=>b.name=="查看本手得分详情").ToArray())
                        {
                            var go=button.gameObject;go.SetActive(false);
                            foreach(Transform child in go.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                            foreach(var component in go.GetComponents<Component>().Where(c=>!(c is Transform)&&!(c is NativeAuthoredElement)).OrderBy(c=>c is CanvasRenderer?2:c is Graphic?1:0).ToArray())UnityEngine.Object.DestroyImmediate(component);
                            go.name="Retired score details layout slot";changed=true;
                        }
                        if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/PersonaWebAligned.unity");
                var view=UnityEngine.Object.FindAnyObjectByType<NativeGameView>();
                var removed=view.EditablePages.Where(p=>p!=null&&p.PageId=="ScoreDetails").ToArray();
                view.EditablePages=view.EditablePages.Where(p=>p!=null&&p.PageId!="ScoreDetails").ToArray();
                foreach(var page in removed)UnityEngine.Object.DestroyImmediate(page.gameObject);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("SCORE_DETAILS_REMOVAL_PASSED");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}

