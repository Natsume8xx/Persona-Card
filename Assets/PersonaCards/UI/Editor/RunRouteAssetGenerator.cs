using PersonaCards.Data;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>路线资产生成器：按内置白盒值创建/重置 RunRoute.asset（白盒 = 配表"关卡流程"当前初值；正式数据以 xlsx 导入命令为准，本菜单只作兜底重置）。</summary>
    public static class RunRouteAssetGenerator
    {
        /// <summary>路线资产路径（PersonaCards/Data 文件夹下）。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/RunRoute.asset";

        /// <summary>按内置白盒路线创建或覆盖 RunRoute.asset。覆盖时保留原资产对象（场景引用不失效）。</summary>
        [MenuItem("Persona Cards/Regenerate Run Route Asset")]
        public static void CreateOrReset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(AssetPath);
            var temporary = ScriptableObject.CreateInstance<RunRouteAsset>();
            temporary.name = "RunRoute"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.battleNodes = RunRouteAsset.CreateDefaultNodes(); // 与 RunRoute 门面兜底同源，消灭拷贝漂移

            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
                Debug.Log($"[RunRoute] 路线资产已创建：{AssetPath}（内置白盒：13 个阶段 = 10 场战斗 + 3 个人格牌生成节点）。");
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "RunRoute"; // CopySerialized 不拷贝对象名，需显式设置
                Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[RunRoute] 路线资产已重置为内置白盒：{AssetPath}（13 个阶段 = 10 场战斗 + 3 个人格牌生成节点）。");
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(AssetPath);
        }
    }
}
