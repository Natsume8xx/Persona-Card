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
    /// 全局配置导入命令：读取 Docs/人格牌.xlsx 的「全局配置」sheet，映射并覆写 GlobalConfig.asset（P0-1F）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console；RULE_001~017 齐全校验防误删。
    /// </summary>
    public static class GlobalConfigImportCommand
    {
        /// <summary>资产路径：场景引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/GlobalConfig.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入全局配置数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Global] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[Global] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, GlobalConfigTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Global] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = GlobalConfigTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[Global] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[Global] {error}");
                Debug.LogError("[Global] 配表存在错误，导入中止：GlobalConfig.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（string 原文），无需二次转换
            var temporary = ScriptableObject.CreateInstance<GlobalConfigAsset>();
            temporary.name = "GlobalConfig"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<GlobalConfigEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[Global] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<GlobalConfigAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "GlobalConfig"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GlobalConfigAsset>(AssetPath);
            Debug.Log($"[Global] 配表导入完成：{mapping.Entries.Count} 条全局配置已写入 GlobalConfig.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入全局配置数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 取得或创建 GlobalConfig.asset（缺失时创建空条目资产）：首次导入与场景重建兜底共用。
        /// 空条目 = 白盒合法（门面回落空配置，出牌/弃牌回落 4/3 行为零差异）——不存在「白盒 17 条拷贝」。
        /// </summary>
        public static GlobalConfigAsset CreateOrReset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GlobalConfigAsset>(AssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<GlobalConfigAsset>();
            asset.name = "GlobalConfig";
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
