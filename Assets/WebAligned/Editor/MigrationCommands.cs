using System.Linq;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PersonaCards.WebAligned.Editor
{
    public static class MigrationCommands
    {
        public const string ScenePath = "Assets/Scenes/PersonaWebAligned.unity";
        [MenuItem("Persona Cards/Web Aligned/Create Scene")]
        public static void CreateScene()
        {
            if(File.Exists(ScenePath)){EditableUICommands.OpenEditable();return;}
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Persona Native Game").AddComponent<NativeGameView>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Persona Cards/Web Aligned/Build Windows")]
        public static void BuildWindows()
        {
            try {
                foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/WebAligned/Resources/WebArt"}))
                {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    if(importer.textureType==TextureImporterType.Default&&importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed)continue;
                    importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                }
                if(!File.Exists(ScenePath))throw new Exception("Editable scene is missing; generate it explicitly before building.");
                PlayerSettings.productName="人格牌";
                EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/WebAligned/PersonaCards.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
                Debug.Log("NATIVE_WINDOWS_BUILD_PASSED");
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);return;}
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        public static void Smoke()
        {
            try
            {
                using var core = new NativeRules(false);
                var s = core.Action("start");
                if ((int)s["target"] != 950 || s["nodes"].Count() != 17) throw new Exception("Template mismatch");
                s = core.Action("click", "#stage-start");
                if ((bool)s["inputLocked"]) throw new Exception("Opening did not unlock");
                s = core.Action("toggle", index: 0); var expected = (int)s["preview"]["total"];
                s = core.Action("play");
                if ((int)s["score"] != expected || (int)s["hands"] != 3) throw new Exception("Preview/commit mismatch");
                s = core.Action("toggle", index: 0); s = core.Action("discard");
                if ((int)s["discards"] != 2 || (bool)s["inputLocked"]) throw new Exception("Discard mismatch");
                Directory.CreateDirectory("Docs/Migration");
                File.WriteAllText("Docs/Migration/native-smoke.json", s.ToString());
                Debug.Log("NATIVE_RULES_SMOKE_PASSED");
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); return; }
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}

