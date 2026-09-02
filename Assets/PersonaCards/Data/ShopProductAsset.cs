using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品条目（P0-1J）：配表「商品_商品配置表」sheet 8 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（ShopProductAsset）只在 UI/Data 引擎程序集流转。
    /// 效果参数 1/2 存 string 原文（配表混写 1/基础筹码/0.5/Lv+1，语义解析留给后续阶段）。
    /// </summary>
    [System.Serializable]
    public sealed class ShopProductEntry
    {
        [Tooltip("商品_ID（SHOP_CARD_xxx / SHOP_PER_xxx / SHOP_SERVICE_xxx；行标识）。")]
        public string productId;

        [Tooltip("商品名称（仅存值，Inspector/日志可读）。")]
        public string productName;

        [Tooltip("商品类型：卡牌/人格牌/服务。")]
        public string productType;

        [Tooltip("价格（非负整数）。")]
        public int price;

        [Tooltip("购买次数限制（0 = 未填）。")]
        public int purchaseLimit;

        [Tooltip("效果类型（增加卡牌/增加人格牌/强化卡牌/移除卡牌/强化人格/强化花色/强化牌型等）。")]
        public string effectType;

        [Tooltip("效果参数1（配表原文，允许空）。")]
        public string effectParam1;

        [Tooltip("效果参数2（配表原文，允许空）。")]
        public string effectParam2;
    }

    /// <summary>
    /// 商店商品资产：条目由菜单「Persona Cards/导入商店商品数据」写入。
    /// 商店运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/ShopProduct", fileName = "ShopProduct")]
    public sealed class ShopProductAsset : ScriptableObject
    {
        [Tooltip("商品条目列表（当前配表 68 行，按商品_ID 升序）。")]
        public List<ShopProductEntry> entries = new List<ShopProductEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、商品_ID 非空且唯一、名称/类型/效果类型非空、价格与限购非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "商店商品为空：至少需要一个条目。";
                return false;
            }

            var seen = new HashSet<string>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    error = $"条目 {index} 为 null。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.productId))
                {
                    error = $"条目 {index} 的商品_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.productId))
                {
                    error = $"商品_ID {entry.productId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.productName))
                {
                    error = $"商品 {entry.productId} 的商品名称为空。";
                    return false;
                }
                if (System.Array.IndexOf(ShopProductTableContract.ProductTypes, entry.productType) < 0)
                {
                    error = $"商品 {entry.productId} 的商品类型「{entry.productType}」无效，应为 {string.Join("/", ShopProductTableContract.ProductTypes)}。";
                    return false;
                }
                if (entry.price < 0)
                {
                    error = $"商品 {entry.productId} 的价格不能为负数（当前 {entry.price}）。";
                    return false;
                }
                if (entry.purchaseLimit < 0)
                {
                    error = $"商品 {entry.productId} 的购买次数限制不能为负数（当前 {entry.purchaseLimit}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.effectType))
                {
                    error = $"商品 {entry.productId} 的效果类型为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
