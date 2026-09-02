using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店人格铸造条目（P0-1J）：配表「商店_人格铸造」sheet 3 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（ShopForgeAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class ShopForgeEntry
    {
        [Tooltip("功能_ID（FORGE_xxx；行标识）。")]
        public string forgeId;

        [Tooltip("功能名称（解锁第二词条/解锁第三词条…）。")]
        public string forgeName;

        [Tooltip("价格（非负整数）。")]
        public int price;
    }

    /// <summary>
    /// 商店人格铸造资产：条目由菜单「Persona Cards/导入人格铸造数据」写入。
    /// 商店运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/ShopForge", fileName = "ShopForge")]
    public sealed class ShopForgeAsset : ScriptableObject
    {
        [Tooltip("铸造功能条目列表（当前配表 2 行，按功能_ID 升序）。")]
        public List<ShopForgeEntry> entries = new List<ShopForgeEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、功能_ID 非空且唯一、功能名称非空、价格非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "商店人格铸造为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.forgeId))
                {
                    error = $"条目 {index} 的功能_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.forgeId))
                {
                    error = $"功能_ID {entry.forgeId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.forgeName))
                {
                    error = $"铸造功能 {entry.forgeId} 的功能名称为空。";
                    return false;
                }
                if (entry.price < 0)
                {
                    error = $"铸造功能 {entry.forgeId} 的价格不能为负数（当前 {entry.price}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
