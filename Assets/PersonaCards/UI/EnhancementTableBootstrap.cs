using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 三线强化配表注入（P0-11）：Editor 下反射 AssetDatabase.LoadAssetAtPath 加载 3 个强化资产
    /// （PersonaUpRule/SuitUp/HandUp）→ EnhancementTablesBuilder 翻译 → EnhancementConfig.Configure。
    /// UI 程序集不引用 UnityEditor（沿用 QuitApplication 反射 EditorApplication 先例），play build
    /// 无 UnityEditor 类型 → 静默跳过 → EnhancementConfig 保持空表 → 强化服务不合成池规则不上架（功能缺席不崩溃）。
    /// 调用顺序依赖：必须先于 ShopCatalog.Configure（合成强化池规则依赖 HasTables）。
    /// </summary>
    public static class EnhancementTableBootstrap
    {
        private const string PersonaUpRulePath = "Assets/PersonaCards/Data/PersonaUpRule.asset";
        private const string SuitUpPath = "Assets/PersonaCards/Data/SuitUp.asset";
        private const string HandUpPath = "Assets/PersonaCards/Data/HandUp.asset";

        /// <summary>加载 3 资产并注入 EnhancementConfig；Editor 外或资产缺失时静默回落空表。</summary>
        public static void Load()
        {
            var assetDatabase = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            if (assetDatabase == null)
            {
                Debug.Log("[Enhance] 非 Editor 环境：强化配表不注入（强化服务不上架）。");
                return;
            }
            var loadAssetAtPath = assetDatabase.GetMethod(
                "LoadAssetAtPath", new[] { typeof(string), typeof(Type) });
            if (loadAssetAtPath == null) return;

            var personaRules = Load<PersonaUpRuleAsset>(loadAssetAtPath, PersonaUpRulePath);
            var suitUps = Load<SuitUpAsset>(loadAssetAtPath, SuitUpPath);
            var handUps = Load<HandUpAsset>(loadAssetAtPath, HandUpPath);
            var result = EnhancementTablesBuilder.Build(
                personaRules != null ? personaRules.entries : null,
                suitUps != null ? suitUps.entries : null,
                handUps != null ? handUps.entries : null);
            EnhancementConfig.Configure(result.Tables);
            if (result.Warnings.Count > 0)
                Debug.LogWarning($"[Enhance] 强化配表注入警告 {result.Warnings.Count} 条：{string.Join("；", result.Warnings)}");
            Debug.Log($"[Enhance] 强化配表已注入：{(result.Tables.HasContent ? "3 表生效，强化服务可上架。" : "无有效内容，强化服务不上架。")}");
        }

        private static T Load<T>(System.Reflection.MethodInfo loadAssetAtPath, string path) where T : UnityEngine.Object
        {
            return loadAssetAtPath.Invoke(null, new object[] { path, typeof(T) }) as T;
        }
    }
}
