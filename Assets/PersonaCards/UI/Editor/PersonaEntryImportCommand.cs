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
    /// 词条导入命令：读取 Docs/人格牌.xlsx 的「人格牌_词条」sheet，映射并覆写 PersonaEntryCatalog.asset（P0-1J）。
    /// 「比较符定义表」sheet 的比较符_ID 集合用于对照警告（比较符不在定义表 → 警告不阻塞）。
    /// 条件参数数值与枚举文本混写（2/4/0/NORMAL/RARE），一律原文存储，语义解析留给 B7。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class PersonaEntryImportCommand
    {
        /// <summary>资产路径：后续引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/PersonaEntryCatalog.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入词条数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PersonaEntry] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[PersonaEntry] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, PersonaEntryTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersonaEntry] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            // 比较符定义表 sheet 独立读取（各自内存流互不影响）；缺 sheet 只降级为跳过比较符对照
            ICollection<string> comparatorIds = null;
            try
            {
                using var comparatorMemory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(comparatorMemory);
                var comparatorRows = XlsxTableReader.ReadTable(comparatorMemory, ComparatorDefinitionTableContract.SheetName);
                var ids = new HashSet<string>();
                foreach (var row in comparatorRows)
                {
                    var comparatorId = row.TryGetValue(ComparatorDefinitionTableContract.ColComparatorId, out var value) ? value : "";
                    if (!string.IsNullOrEmpty(comparatorId)) ids.Add(comparatorId);
                }
                comparatorIds = ids;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[PersonaEntry] 读取「比较符定义表」sheet 失败（{exception.Message}），跳过比较符对照校验。");
            }

            var mapping = PersonaEntryTableMapper.Map(rows, comparatorIds);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[PersonaEntry] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[PersonaEntry] {error}");
                Debug.LogError("[PersonaEntry] 配表存在错误，导入中止：PersonaEntryCatalog.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（string 原文），无需二次转换
            var temporary = ScriptableObject.CreateInstance<PersonaEntryAsset>();
            temporary.name = "PersonaEntryCatalog"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<PersonaEntryEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[PersonaEntry] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<PersonaEntryAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "PersonaEntryCatalog"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PersonaEntryAsset>(AssetPath);
            Debug.Log($"[PersonaEntry] 配表导入完成：{mapping.Entries.Count} 个词条条目已写入 PersonaEntryCatalog.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入词条数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
