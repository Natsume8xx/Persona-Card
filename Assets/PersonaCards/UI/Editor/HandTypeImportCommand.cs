using System;
using System.Collections.Generic;
using System.IO;
using PersonaCards.Core;
using PersonaCards.Core.Xlsx;
using PersonaCards.Data;
using UnityEditor;
using UnityEngine;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 牌型配置导入命令：读取 Docs/人格牌.xlsx 的「牌型配置」sheet，映射并覆写 HandTypeCatalog.asset。
    /// 「图片配置」sheet 的绑定 ID 集合用于 card_id 对照警告（A2 拍板：策划会改，程序警告容错）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class HandTypeImportCommand
    {
        /// <summary>资产路径：场景引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/HandTypeCatalog.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入牌型配置数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[HandType] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[HandType] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, HandTypeTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[HandType] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            // 图片配置 sheet 独立读取（各自内存流互不影响）；缺 sheet 只降级为跳过 card_id 对照
            ICollection<string> imageBindingIds = null;
            try
            {
                using var imageMemory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(imageMemory);
                var imageRows = XlsxTableReader.ReadTable(imageMemory, HandTypeTableContract.ImageSheetName);
                var ids = new HashSet<string>();
                foreach (var row in imageRows)
                {
                    var bindingId = row.TryGetValue(HandTypeTableContract.ImageColBindingId, out var value) ? value : "";
                    if (!string.IsNullOrEmpty(bindingId)) ids.Add(bindingId);
                }
                imageBindingIds = ids;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[HandType] 读取「图片配置」sheet 失败（{exception.Message}），跳过 card_id 对照校验。");
            }

            var mapping = HandTypeTableMapper.Map(rows, imageBindingIds);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[HandType] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[HandType] {error}");
                Debug.LogError("[HandType] 配表存在错误，导入中止：HandTypeCatalog.asset 未做任何改动。");
                return;
            }

            // 条目（decimal 倍率）→ 资产条目（double 倍率，Unity 不序列化 decimal；配表值 ≤2 位小数转换无损）
            var temporary = ScriptableObject.CreateInstance<HandTypeAsset>();
            temporary.name = "HandTypeCatalog"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<HandTypeAsset.Entry>();
            foreach (var entry in mapping.Entries)
                temporary.entries.Add(EntryOf(entry));
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[HandType] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<HandTypeAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "HandTypeCatalog"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<HandTypeAsset>(AssetPath);
            Debug.Log($"[HandType] 配表导入完成：{mapping.Entries.Count} 个牌型条目已写入 HandTypeCatalog.asset（五条/同花五条由目录白盒补齐）。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入牌型配置数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 取得或创建 HandTypeCatalog.asset（缺失时填白盒条目）：首次导入与场景重建兜底共用。
        /// 保证运行时接线（PrototypeFlowController.Awake）总能拿到资产对象。
        /// </summary>
        public static HandTypeAsset CreateOrReset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<HandTypeAsset>(AssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<HandTypeAsset>();
            asset.name = "HandTypeCatalog";
            asset.entries = HandTypeAsset.CreateFallbackEntries();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Core 条目（decimal 倍率）→ 资产条目（double 倍率）：配表值 ≤2 位小数，转换无损。</summary>
        private static HandTypeAsset.Entry EntryOf(HandTypeEntry entry)
        {
            return new HandTypeAsset.Entry
            {
                handType = entry.HandType,
                displayName = entry.DisplayName,
                baseChips = entry.BaseChips,
                baseMultiplier = (double)entry.BaseMultiplier,
                displayOrder = entry.DisplayOrder,
                cardId = entry.CardId
            };
        }
    }
}
