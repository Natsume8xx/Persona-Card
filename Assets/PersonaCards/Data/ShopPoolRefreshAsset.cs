using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品刷新规则条目（P0-1J）：配表「商店_商品刷新规则」sheet 3 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（ShopPoolRefreshAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class ShopPoolRefreshEntry
    {
        [Tooltip("商品池_ID（POLL_CARD_xxx / POOL_PERSONA_xxx / POOL_SERVICE_xxx；行标识，原文存储）。")]
        public string poolId;

        [Tooltip("商品_ID（引用商品配置表；不 join，断链检查留给后续阶段）。")]
        public string productId;

        [Tooltip("权重（≥1；当前配表三档 1/10/20）。")]
        public int weight;
    }

    /// <summary>
    /// 商店商品刷新规则资产：条目由菜单「Persona Cards/导入商品刷新规则数据」写入。
    /// 商店运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/ShopPoolRefresh", fileName = "ShopPoolRefresh")]
    public sealed class ShopPoolRefreshAsset : ScriptableObject
    {
        [Tooltip("刷新规则条目列表（当前配表 65 行，按商品池_ID 升序）。")]
        public List<ShopPoolRefreshEntry> entries = new List<ShopPoolRefreshEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、商品池_ID 非空且唯一、商品_ID 非空、权重 ≥1。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "商店商品刷新规则为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.poolId))
                {
                    error = $"条目 {index} 的商品池_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.poolId))
                {
                    error = $"商品池_ID {entry.poolId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.productId))
                {
                    error = $"刷新规则 {entry.poolId} 的商品_ID 为空。";
                    return false;
                }
                if (entry.weight < 1)
                {
                    error = $"刷新规则 {entry.poolId} 的权重必须 ≥1（当前 {entry.weight}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
