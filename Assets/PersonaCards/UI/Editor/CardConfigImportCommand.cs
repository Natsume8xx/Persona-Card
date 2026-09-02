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
    /// 卡牌配置导入命令：读取 Docs/人格牌.xlsx 的「卡牌配置」sheet，映射并覆写 CardConfig.asset。
    /// 「图片配置」sheet 的绑定 ID 集合用于卡牌_ID 对照警告（策划改 ID 只需同步图片配置）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class CardConfigImportCommand
    {
        /// <summary>资产路径：场景引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/CardConfig.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入卡牌配置数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Card] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[Card] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, CardConfigTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Card] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            // 图片配置 sheet 独立读取（各自内存流互不影响）；缺 sheet / 无「绑定ID」列（最新版配表已删）只降级为跳过卡牌_ID 对照
            ICollection<string> imageBindingIds = null;
            try
            {
                using var imageMemory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(imageMemory);
                var imageRows = XlsxTableReader.ReadTable(imageMemory, ImageSheetContract.SheetName);
                if (ImageSheetContract.TryBuildBindingIds(imageRows, out var ids))
                {
                    imageBindingIds = ids;
                }
                else
                {
                    Debug.LogWarning("[Card] 「图片配置」sheet 缺少「绑定ID」列（最新版配表已删除该列），跳过卡牌_ID 对照校验。");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Card] 读取「图片配置」sheet 失败（{exception.Message}），跳过卡牌_ID 对照校验。");
            }

            var mapping = CardConfigTableMapper.Map(rows, imageBindingIds);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[Card] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[Card] {error}");
                Debug.LogError("[Card] 配表存在错误，导入中止：CardConfig.asset 未做任何改动。");
                return;
            }

            // 条目 → 资产条目（全 int/枚举，无小数转换）
            var temporary = ScriptableObject.CreateInstance<CardConfigAsset>();
            temporary.name = "CardConfig"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<CardConfigAsset.Entry>();
            foreach (var entry in mapping.Entries)
                temporary.entries.Add(EntryOf(entry));
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[Card] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<CardConfigAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "CardConfig"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CardConfigAsset>(AssetPath);
            Debug.Log($"[Card] 配表导入完成：{mapping.Entries.Count} 个卡牌条目已写入 CardConfig.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入卡牌配置数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 取得或创建 CardConfig.asset（缺失时填白盒条目）：首次导入与场景重建兜底共用。
        /// 保证运行时接线（PrototypeFlowController.Awake）总能拿到资产对象。
        /// </summary>
        public static CardConfigAsset CreateOrReset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CardConfigAsset>(AssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<CardConfigAsset>();
            asset.name = "CardConfig";
            asset.entries = CardConfigAsset.CreateFallbackEntries();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Cards 条目 → 资产条目（全 int/枚举，无转换损耗）。</summary>
        private static CardConfigAsset.Entry EntryOf(PersonaCards.Cards.CardConfigEntry entry)
        {
            return new CardConfigAsset.Entry
            {
                cardId = entry.CardId,
                displayName = entry.DisplayName,
                cardKind = entry.CardKind,
                suit = entry.Suit,
                rank = entry.Rank,
                paramType = entry.ParamType,
                paramValue = entry.ParamValue
            };
        }
    }
}
