using System;
using System.Collections.Generic;
using System.IO;
using PersonaCards.Core.Xlsx;
using PersonaCards.Data;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 配表导入命令：读取 Docs/人格牌.xlsx 的「关卡流程」sheet，映射并覆写 RunRoute.asset。
    /// 策划改表后点菜单 "Persona Cards/Import Run Route From Xlsx" 一键完成；
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class RunRouteImportCommand
    {
        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/Import Run Route From Xlsx")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[RunRoute] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[RunRoute] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, RunRouteTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RunRoute] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = RunRouteTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[RunRoute] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[RunRoute] {error}");
                Debug.LogError("[RunRoute] 配表存在错误，导入中止：RunRoute.asset 未做任何改动。");
                return;
            }

            // 防御校验：映射结果必须通过资产校验才写入（正常情况 mapper 与 Validate 规则一致）
            var temporary = ScriptableObject.CreateInstance<RunRouteAsset>();
            temporary.name = "RunRoute"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.battleNodes = new List<RunBattleNode>(mapping.Nodes);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[RunRoute] 映射结果未通过路线校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（复用 RunRouteAssetGenerator 模式：CopySerialized 保留场景引用）
            var existing = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(RunRouteAssetGenerator.AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, RunRouteAssetGenerator.AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "RunRoute"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(RunRouteAssetGenerator.AssetPath);
            Debug.Log($"[RunRoute] 配表导入完成：{SummaryOf(mapping.Nodes)}。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/Import Run Route From Xlsx", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>生成导入摘要日志：阶段/战斗/生成节点/商店/Boss 计数（直接数映射结果，不依赖门面状态）。</summary>
        private static string SummaryOf(IReadOnlyList<RunBattleNode> nodes)
        {
            var battle = 0;
            var boss = 0;
            var gen = 0;
            var shop = 0;
            foreach (var node in nodes)
            {
                switch (node.kind)
                {
                    case RunNodeKind.NormalBattle:
                        battle++;
                        break;
                    case RunNodeKind.BossBattle:
                        battle++;
                        boss++;
                        break;
                    case RunNodeKind.PersonaGen:
                        gen++;
                        break;
                }
                if (node.hasShopAfter) shop++;
            }
            return $"{nodes.Count} 个阶段 = {battle} 场战斗（含 Boss {boss} 场）+ {gen} 个人格牌生成节点，{shop} 次商店";
        }
    }
}
