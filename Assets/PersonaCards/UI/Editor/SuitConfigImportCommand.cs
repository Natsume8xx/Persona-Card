using System;
using System.Collections.Generic;
using System.IO;
using PersonaCards.Cards;
using PersonaCards.Core.Xlsx;
using PersonaCards.Data;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 花色配置导入命令：读取 Docs/人格牌.xlsx 的「花色配置」sheet，映射并覆写 SuitConfig.asset。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class SuitConfigImportCommand
    {
        /// <summary>资产路径：场景引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/SuitConfig.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入花色配置数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SuitConfig] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[SuitConfig] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, SuitConfigTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SuitConfig] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = SuitConfigTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[SuitConfig] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[SuitConfig] {error}");
                Debug.LogError("[SuitConfig] 配表存在错误，导入中止：SuitConfig.asset 未做任何改动。");
                return;
            }

            var temporary = ScriptableObject.CreateInstance<SuitConfigAsset>();
            temporary.name = "SuitConfig"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<SuitConfigAsset.Entry>();
            foreach (var entry in mapping.Entries)
                temporary.entries.Add(EntryOf(entry));
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[SuitConfig] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<SuitConfigAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "SuitConfig"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SuitConfigAsset>(AssetPath);
            Debug.Log($"[SuitConfig] 配表导入完成：{mapping.Entries.Count} 个花色条目已写入 SuitConfig.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入花色配置数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 取得或创建 SuitConfig.asset（缺失时填白盒条目）：首次导入与场景重建兜底共用。
        /// </summary>
        public static SuitConfigAsset CreateOrReset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SuitConfigAsset>(AssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<SuitConfigAsset>();
            asset.name = "SuitConfig";
            asset.entries = SuitConfigAsset.CreateFallbackEntries();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Cards 条目 → 资产条目。</summary>
        private static SuitConfigAsset.Entry EntryOf(SuitConfigEntry entry)
        {
            return new SuitConfigAsset.Entry
            {
                suit = entry.Suit,
                displayName = entry.DisplayName
            };
        }
    }
}
