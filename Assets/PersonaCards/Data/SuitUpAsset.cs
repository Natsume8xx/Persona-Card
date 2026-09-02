using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 花色强化条目（P0-1J）：配表「商品_花色强化」sheet 6 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（SuitUpAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class SuitUpEntry
    {
        [Tooltip("花色强化_ID（SUIT_UP_xxx；行标识）。")]
        public string suitUpId;

        [Tooltip("花色_ID（SUIT_xxx；引用花色配置表，原文存储）。")]
        public string suitId;

        [Tooltip("花色名称（显示名）。")]
        public string suitName;

        [Tooltip("等级（Lv.1~Lv.4，原文存储）。")]
        public string level;

        [Tooltip("额外筹码（非负整数）。")]
        public int extraChips;

        [Tooltip("价格（非负整数）。")]
        public int price;
    }

    /// <summary>
    /// 花色强化资产：条目由菜单「Persona Cards/导入花色强化数据」写入。
    /// 强化运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/SuitUp", fileName = "SuitUp")]
    public sealed class SuitUpAsset : ScriptableObject
    {
        [Tooltip("花色强化条目列表（当前配表 16 行，按花色强化_ID 升序）。")]
        public List<SuitUpEntry> entries = new List<SuitUpEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、花色强化_ID 非空且唯一、花色_ID/名称/等级非空、额外筹码与价格非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "花色强化为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.suitUpId))
                {
                    error = $"条目 {index} 的花色强化_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.suitUpId))
                {
                    error = $"花色强化_ID {entry.suitUpId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.suitId))
                {
                    error = $"花色强化 {entry.suitUpId} 的花色_ID 为空。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.suitName))
                {
                    error = $"花色强化 {entry.suitUpId} 的花色名称为空。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.level))
                {
                    error = $"花色强化 {entry.suitUpId} 的等级为空。";
                    return false;
                }
                if (entry.extraChips < 0)
                {
                    error = $"花色强化 {entry.suitUpId} 的额外筹码不能为负数（当前 {entry.extraChips}）。";
                    return false;
                }
                if (entry.price < 0)
                {
                    error = $"花色强化 {entry.suitUpId} 的价格不能为负数（当前 {entry.price}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
