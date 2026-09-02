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
    /// 次级属性导入命令：读取 Docs/人格牌.xlsx 的「人格牌_次级属性」sheet，映射并覆写 PersonaSubAttrCatalog.asset（P0-1J）。
    /// 「所属人格」列当前填人格牌名称（非 PER_ID）；属性参数2 整数与小数混写（8/20/0.3/0.03/0.5/1/5），一律原文存储。
    /// 任一行校验失败则整体中止（资产零改动），错误全部输出到 Console。
    /// </summary>
    public static class PersonaSubAttrImportCommand
    {
        /// <summary>资产路径：后续引用通过 CopySerialized 覆写保 GUID，路径不可变更。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/PersonaSubAttrCatalog.asset";

        /// <summary>xlsx 文件路径：项目根目录下 Docs/人格牌.xlsx（与策划交付位置一致，不随 Assets 迁移）。</summary>
        public static string XlsxPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "人格牌.xlsx");

        [MenuItem("Persona Cards/导入次级属性数据")]
        public static void Import()
        {
            // 双重守卫：Play Mode 下域重载会破坏资产引用，菜单置灰 + 方法内拦截
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PersonaSubAttr] Play Mode 中禁止导入配表，请退出 Play Mode 后重试。");
                return;
            }

            var path = XlsxPath;
            if (!File.Exists(path))
            {
                Debug.LogError($"[PersonaSubAttr] 找不到配表：{path}。请确认文件存在后重试（导入未执行，资产未改动）。");
                return;
            }

            // 先读全字节再解析：FileShare.ReadWrite 容忍 Excel 打开占用的文件锁；内存流解析避免期间文件被改
            List<Dictionary<string, string>> rows;
            try
            {
                using var memory = new MemoryStream();
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    file.CopyTo(memory);
                rows = XlsxTableReader.ReadTable(memory, PersonaSubAttrTableContract.SheetName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PersonaSubAttr] 读取配表失败：{exception.Message}（导入未执行，资产未改动）。");
                return;
            }

            var mapping = PersonaSubAttrTableMapper.Map(rows);
            foreach (var warning in mapping.Warnings)
                Debug.LogWarning($"[PersonaSubAttr] {warning}");

            if (!mapping.Succeeded)
            {
                foreach (var error in mapping.Errors)
                    Debug.LogError($"[PersonaSubAttr] {error}");
                Debug.LogError("[PersonaSubAttr] 配表存在错误，导入中止：PersonaSubAttrCatalog.asset 未做任何改动。");
                return;
            }

            // Mapper 直接产出资产条目（string 原文 + 权重 int），无需二次转换
            var temporary = ScriptableObject.CreateInstance<PersonaSubAttrAsset>();
            temporary.name = "PersonaSubAttrCatalog"; // 主对象名与文件名一致，避免 Unity 命名警告
            temporary.entries = new List<PersonaSubAttrEntry>(mapping.Entries);
            if (!temporary.Validate(out var validateError))
            {
                Debug.LogError($"[PersonaSubAttr] 映射结果未通过资产校验，导入中止：{validateError}");
                UnityEngine.Object.DestroyImmediate(temporary);
                return;
            }

            // 覆写既有资产对象（CopySerialized 保留场景引用与 GUID）
            var existing = AssetDatabase.LoadAssetAtPath<PersonaSubAttrAsset>(AssetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temporary, AssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(temporary, existing);
                existing.name = "PersonaSubAttrCatalog"; // CopySerialized 不拷贝对象名，需显式设置
                UnityEngine.Object.DestroyImmediate(temporary);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PersonaSubAttrAsset>(AssetPath);
            Debug.Log($"[PersonaSubAttr] 配表导入完成：{mapping.Entries.Count} 个次级属性条目已写入 PersonaSubAttrCatalog.asset。");
        }

        /// <summary>菜单校验：Play Mode 时置灰。</summary>
        [MenuItem("Persona Cards/导入次级属性数据", true)]
        private static bool ValidateImport() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
