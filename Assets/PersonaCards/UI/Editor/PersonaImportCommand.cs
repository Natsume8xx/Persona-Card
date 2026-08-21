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
    /// 人格牌配置导入命令：读取 Docs/人格牌.xlsx 的「人格牌配置」sheet，映射并覆写 PersonaConfig.asset（P0-1E）。
    /// 「图片配置」sheet 的绑定 ID 集合用于人格牌_ID 对照警告（策划改 ID 只需同步图片配置）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console；警告（「特殊→异质」规范化、附加条件存原文）不阻塞导入。
    /// </summary>
    public static class PersonaImportCommand
    {
        /// <summary>资产路径：场景引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/PersonaConfig.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入人格牌配置数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Persona] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[Persona] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, PersonaTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Persona] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            // 图片配置 sheet 独立读取（各自内存流互不影响）；缺 sheet 只降级为跳过人格牌_ID 对照
            ICollection<string> imageBindingIds = null;
            try
            {
                using var imageMemory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(imageMemory);
                var imageRows = XlsxTableReader.ReadTable(imageMemory, ImageSheetContract.SheetName);
                var ids = new HashSet<string>();
                foreach (var row in imageRows)
                {
                    var bindingId = row.TryGetValue(ImageSheetContract.ColBindingId, out var value) ? value : "";
                    if (!string.IsNullOrEmpty(bindingId)) ids.Add(bindingId);
                }
                imageBindingIds = ids;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Persona] 读取「图片配置」sheet 失败（{exception.Message}），跳过人格牌_ID 对照校验。");
            }

            var mapping = PersonaTableMapper.Map(rows, imageBindingIds);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[Persona] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[Persona] {error}");
                Debug.LogError("[Persona] 配表存在错误，导入中止：PersonaConfig.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（string 原文，Data 不引用 Battle 类型），无需二次转换
            var temporary = ScriptableObject.CreateInstance<PersonaConfigAsset>();
            temporary.name = "PersonaConfig"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<PersonaConfigEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[Persona] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<PersonaConfigAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "PersonaConfig"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PersonaConfigAsset>(AssetPath);
            Debug.Log($"[Persona] 配表导入完成：{mapping.Entries.Count} 个人格牌条目已写入 PersonaConfig.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入人格牌配置数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 取得或创建 PersonaConfig.asset（缺失时创建空条目资产）：首次导入与场景重建兜底共用。
        /// 空条目 = 白盒合法（门面回落空模板目录，教学 3 张静态锚点行为零差异）——不存在「白盒 16 张拷贝」。
        /// </summary>
        public static PersonaConfigAsset CreateOrReset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PersonaConfigAsset>(AssetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<PersonaConfigAsset>();
            asset.name = "PersonaConfig";
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
