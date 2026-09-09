using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PersonaCards.WebAligned.Editor
{
    [CustomEditor(typeof(NativeGameView))]
    public sealed class NativeGameViewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("停止 Play 后可直接展开 Editable UI，编辑各页面的 RectTransform、Text、Image。场景内修改保存场景；双击 UI 文件夹内预制体可编辑共用样式。动态文字和牌面仍由游戏规则更新。",MessageType.Info);
            var view=(NativeGameView)target;
            if(Application.isPlaying)return;
            foreach(var page in view.EditablePages.Where(p=>p!=null))
            {
                if(!GUILayout.Button("预览 / 编辑 · "+page.PageId))continue;
                Undo.RecordObjects(view.EditablePages.Where(p=>p!=null).Select(p=>(Object)p.gameObject).ToArray(),"选择界面预览");
                foreach(var p in view.EditablePages)if(p!=null)p.gameObject.SetActive(p==page);
                Selection.activeGameObject=page.gameObject;EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
    }
    [InitializeOnLoad]
    public static class NativePlayEntry
    {
        static NativePlayEntry()
        {
            EditorApplication.delayCall+=()=>{
                var scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(EditableUICommands.ScenePath);
                if(scene!=null&&!EditorApplication.isPlayingOrWillChangePlaymode)EditorSceneManager.playModeStartScene=scene;
            };
        }
    }
}
