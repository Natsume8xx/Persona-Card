using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品槽位刷新规则条目（P0-1J）：配表「商店_商品槽位刷新规则」sheet 5 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（ShopSlotRefreshAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class ShopSlotRefreshEntry
    {
        [Tooltip("刷新_ID（REFRESH_xxx；行标识，不透明字符串，跳号合法）。")]
        public string refreshId;

        [Tooltip("商店刷新节点（AI1/AI2/AI3，原文存储）。")]
        public string node;

        [Tooltip("商品类型：卡牌/人格牌/服务（旧写法「人格」已归一）。")]
        public string productType;

        [Tooltip("出现数量（非负整数）。")]
        public int count;

        [Tooltip("出现权重（≥1；当前配表 20~45）。")]
        public int weight;
    }

    /// <summary>
    /// 商店商品槽位刷新规则资产：条目由菜单「Persona Cards/导入商品槽位刷新规则数据」写入。
    /// 商店运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/ShopSlotRefresh", fileName = "ShopSlotRefresh")]
    public sealed class ShopSlotRefreshAsset : ScriptableObject
    {
        [Tooltip("槽位刷新规则条目列表（当前配表 9 行，按刷新_ID 升序）。")]
        public List<ShopSlotRefreshEntry> entries = new List<ShopSlotRefreshEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、刷新_ID 非空且唯一、节点非空、商品类型合法、数量非负、权重 ≥1。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "商店商品槽位刷新规则为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.refreshId))
                {
                    error = $"条目 {index} 的刷新_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.refreshId))
                {
                    error = $"刷新_ID {entry.refreshId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.node))
                {
                    error = $"槽位刷新规则 {entry.refreshId} 的商店刷新节点为空。";
                    return false;
                }
                if (System.Array.IndexOf(ShopProductTableContract.ProductTypes, entry.productType) < 0)
                {
                    error = $"槽位刷新规则 {entry.refreshId} 的商品类型「{entry.productType}」无效，应为 {string.Join("/", ShopProductTableContract.ProductTypes)}。";
                    return false;
                }
                if (entry.count < 0)
                {
                    error = $"槽位刷新规则 {entry.refreshId} 的出现数量不能为负数（当前 {entry.count}）。";
                    return false;
                }
                if (entry.weight < 1)
                {
                    error = $"槽位刷新规则 {entry.refreshId} 的出现权重必须 ≥1（当前 {entry.weight}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
