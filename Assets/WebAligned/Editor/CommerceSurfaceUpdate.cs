using System;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.WebAligned.Editor
{
    public static class CommerceSurfaceUpdate
    {
        public static void Run()
        {
            try
            {
                foreach (var id in new[] { "Shop", "Forge", "PersonaGrowth" })
                {
                    var path = "Assets/WebAligned/UI/" + id + ".prefab";
                    var page = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        NativeSurfaceFinish.ApplyPage((RectTransform)page.transform);
                        PrefabUtility.SaveAsPrefabAsset(page, path);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(page); }
                }
                AssetDatabase.SaveAssets();
                Debug.Log("COMMERCE_SURFACE_UPDATE_PASSED");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}

