using System;
using System.Reflection;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 人格铸造目录注入（UI 重排第二批）：Editor 下反射 AssetDatabase.LoadAssetAtPath 加载 5 个契约资产
    /// （PersonaCardCatalog/PersonaEntryCatalog/PersonaMainAttrCatalog/PersonaSubAttrCatalog/ShopForge）
    /// → PersonaForgeCatalog.Configure。
    /// UI 程序集不引用 UnityEditor（沿用 EnhancementTableBootstrap 反射先例），play build 无 UnityEditor 类型 →
    /// 静默跳过 → 目录保持空表 → 铸造页显示空列表（功能缺席不崩溃）。
    /// 调用顺序：必须在进入商店之前执行（FlowController.Awake 中紧随 EnhancementTableBootstrap.Load 之后）。
    /// </summary>
    public static class PersonaForgeCatalogBootstrap
    {
        private const string CardsPath = "Assets/PersonaCards/Data/PersonaCardCatalog.asset";
        private const string EntriesPath = "Assets/PersonaCards/Data/PersonaEntryCatalog.asset";
        private const string MainsPath = "Assets/PersonaCards/Data/PersonaMainAttrCatalog.asset";
        private const string SubsPath = "Assets/PersonaCards/Data/PersonaSubAttrCatalog.asset";
        private const string ForgePath = "Assets/PersonaCards/Data/ShopForge.asset";

        /// <summary>加载 5 资产并注入 PersonaForgeCatalog；Editor 外或资产缺失时静默回落空表。</summary>
        public static void Load()
        {
            var assetDatabase = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            if (assetDatabase == null)
            {
                Debug.Log("[Forge] 非 Editor 环境：人格铸造目录不注入（铸造页显示空列表）。");
                return;
            }
            var loadAssetAtPath = assetDatabase.GetMethod(
                "LoadAssetAtPath", new[] { typeof(string), typeof(Type) });
            if (loadAssetAtPath == null) return;

            var cards = Load<PersonaCardAsset>(loadAssetAtPath, CardsPath);
            var entries = Load<PersonaEntryAsset>(loadAssetAtPath, EntriesPath);
            var mains = Load<PersonaMainAttrAsset>(loadAssetAtPath, MainsPath);
            var subs = Load<PersonaSubAttrAsset>(loadAssetAtPath, SubsPath);
            var forge = Load<ShopForgeAsset>(loadAssetAtPath, ForgePath);
            PersonaForgeCatalog.Configure(cards, entries, mains, subs, forge);
            Debug.Log($"[Forge] 人格铸造目录已注入：{(PersonaForgeCatalog.HasContent ? $"8 人格列表可用。" : "无有效内容，铸造页显示空列表。")}");
        }

        private static T Load<T>(MethodInfo loadAssetAtPath, string path) where T : UnityEngine.Object
        {
            return loadAssetAtPath.Invoke(null, new object[] { path, typeof(T) }) as T;
        }
    }
}
