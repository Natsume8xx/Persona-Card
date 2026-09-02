using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 牌型强化条目（P0-1J）：配表「商品_牌型强化」sheet 7 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（HandUpAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class HandUpEntry
    {
        [Tooltip("牌型强化_ID（HAND_UP_xxx；行标识）。")]
        public string handUpId;

        [Tooltip("牌型_ID（HAND_xx；引用牌型配置表，原文存储）。")]
        public string handId;

        [Tooltip("牌型名称（显示名）。")]
        public string handName;

        [Tooltip("等级（Lv.1~Lv.4，原文存储）。")]
        public string level;

        [Tooltip("基础筹码（非负整数）。")]
        public int baseChips;

        [Tooltip("基础倍率（原文存储；混写 1.1/3/3.25/11 等）。")]
        public string baseMult;

        [Tooltip("价格（非负整数）。")]
        public int price;
    }

    /// <summary>
    /// 牌型强化资产：条目由菜单「Persona Cards/导入牌型强化数据」写入。
    /// 强化运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/HandUp", fileName = "HandUp")]
    public sealed class HandUpAsset : ScriptableObject
    {
        [Tooltip("牌型强化条目列表（当前配表 44 行，按牌型强化_ID 升序）。")]
        public List<HandUpEntry> entries = new List<HandUpEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、牌型强化_ID 非空且唯一、牌型_ID/名称/等级/基础倍率非空、基础筹码与价格非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "牌型强化为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.handUpId))
                {
                    error = $"条目 {index} 的牌型强化_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.handUpId))
                {
                    error = $"牌型强化_ID {entry.handUpId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.handId))
                {
                    error = $"牌型强化 {entry.handUpId} 的牌型_ID 为空。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.handName))
                {
                    error = $"牌型强化 {entry.handUpId} 的牌型名称为空。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.level))
                {
                    error = $"牌型强化 {entry.handUpId} 的等级为空。";
                    return false;
                }
                if (entry.baseChips < 0)
                {
                    error = $"牌型强化 {entry.handUpId} 的基础筹码不能为负数（当前 {entry.baseChips}）。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.baseMult))
                {
                    error = $"牌型强化 {entry.handUpId} 的基础倍率为空。";
                    return false;
                }
                if (entry.price < 0)
                {
                    error = $"牌型强化 {entry.handUpId} 的价格不能为负数（当前 {entry.price}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
