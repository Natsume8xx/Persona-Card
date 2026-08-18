using System.Collections.Generic;
using PersonaCards.Data;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>路线资产生成器：按 GDD 2.6 白盒值创建/重置 RunRoute.asset（保持资产与代码内置默认值一致）。</summary>
    public static class RunRouteAssetGenerator
    {
        /// <summary>路线资产路径（PersonaCards/Data 文件夹下）。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/RunRoute.asset";

        /// <summary>按默认路线表创建或覆盖 RunRoute.asset。覆盖时保留原资产对象（场景引用不失效）。</summary>
        [MenuItem("Persona Cards/Regenerate Run Route Asset")]
        public static void CreateOrReset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(AssetPath);
            var temporary = ScriptableObject.CreateInstance<RunRouteAsset>();
            temporary.battleNodes = DefaultBattleNodes();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
                Debug.Log($"[RunRoute] 路线资产已创建：{AssetPath}（6 战 5 店）。");
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[RunRoute] 路线资产已重置为 GDD 默认值：{AssetPath}（6 战 5 店）。");
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(AssetPath);
        }

        /// <summary>GDD 2.6 白盒节点表（与 RunRoute 内置默认路线保持一致；调数值只改此处与 RunRoute.DefaultNodes）。</summary>
        private static List<RunBattleNode> DefaultBattleNodes() => new List<RunBattleNode>
        {
            new RunBattleNode(RunNodeKind.NormalBattle, 350, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 550, BossPoolId.Primary, true),
            new RunBattleNode(RunNodeKind.NormalBattle, 1000, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 1500, BossPoolId.Intermediate, true),
            new RunBattleNode(RunNodeKind.NormalBattle, 2800, BossPoolId.None, true),
            new RunBattleNode(RunNodeKind.BossBattle, 4200, BossPoolId.Advanced, false)
        };
    }
}
