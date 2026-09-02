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
    /// 商店商品刷新规则导入命令：读取 Docs/人格牌.xlsx 的「商店_商品刷新规则」sheet，映射并覆写 ShopPoolRefresh.asset（P0-1J）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console；商品池_ID 前缀混用仅警告（原文存储）。
    /// </summary>
    public static class ShopPoolRefreshImportCommand
    {
        /// <summary>资产路径：后续引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/ShopPoolRefresh.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入商品刷新规则数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ShopPoolRefresh] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[ShopPoolRefresh] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, ShopPoolRefreshTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ShopPoolRefresh] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = ShopPoolRefreshTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[ShopPoolRefresh] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[ShopPoolRefresh] {error}");
                Debug.LogError("[ShopPoolRefresh] 配表存在错误，导入中止：ShopPoolRefresh.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（权重已解析为 int），无需二次转换
            var temporary = ScriptableObject.CreateInstance<ShopPoolRefreshAsset>();
            temporary.name = "ShopPoolRefresh"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<ShopPoolRefreshEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[ShopPoolRefresh] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<ShopPoolRefreshAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "ShopPoolRefresh"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<ShopPoolRefreshAsset>(AssetPath);
            Debug.Log($"[ShopPoolRefresh] 配表导入完成：{mapping.Entries.Count} 个刷新规则条目已写入 ShopPoolRefresh.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入商品刷新规则数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
