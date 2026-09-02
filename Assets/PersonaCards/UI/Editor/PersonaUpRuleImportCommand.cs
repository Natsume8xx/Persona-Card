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
    /// 人格牌强化规则导入命令：读取 Docs/人格牌.xlsx 的「商品_人格牌强化规则」sheet，映射并覆写 PersonaUpRule.asset（P0-1J）。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class PersonaUpRuleImportCommand
    {
        /// <summary>资产路径：后续引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/PersonaUpRule.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入人格牌强化规则数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PersonaUpRule] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[PersonaUpRule] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, PersonaUpRuleTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersonaUpRule] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = PersonaUpRuleTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[PersonaUpRule] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[PersonaUpRule] {error}");
                Debug.LogError("[PersonaUpRule] 配表存在错误，导入中止：PersonaUpRule.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（价格已解析为 int，每级增加为原文），无需二次转换
            var temporary = ScriptableObject.CreateInstance<PersonaUpRuleAsset>();
            temporary.name = "PersonaUpRule"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<PersonaUpRuleEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[PersonaUpRule] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<PersonaUpRuleAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "PersonaUpRule"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PersonaUpRuleAsset>(AssetPath);
            Debug.Log($"[PersonaUpRule] 配表导入完成：{mapping.Entries.Count} 条强化规则条目已写入 PersonaUpRule.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入人格牌强化规则数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
