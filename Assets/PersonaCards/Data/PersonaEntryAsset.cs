using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌词条条目（P0-1J 三表之一）：配表「人格牌_词条」sheet 5 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），B7 接线门面接收本条目列表而非资产；资产（PersonaEntryAsset）只在 UI/Data 引擎程序集流转。
    /// 比较符引「比较符定义表」；条件参数数值与枚举文本混写，一律原文存储。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaEntryEntry
    {
        [Tooltip("词条_ID（ENTRY_xxx；行标识，人格牌配置「词条_ID」列引用）。")]
        public string entryId;

        [Tooltip("触发条件描述（显示文本）。")]
        public string description;

        [Tooltip("条件类型（统计类条件，原文存储，B7 接线时解析）。")]
        public string conditionType;

        [Tooltip("比较符（引「比较符定义表」sheet 的 比较符_ID）。")]
        public string comparator;

        [Tooltip("条件参数（数值/枚举文本混写，原文存储，允许空）。")]
        public string conditionParam;
    }

    /// <summary>
    /// 词条目录资产：条目由菜单「Persona Cards/导入词条数据」写入。
    /// 运行时接线（词条条件判定）是 B7 的事；本资产只存契约数据。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaEntryCatalog", fileName = "PersonaEntryCatalog")]
    public sealed class PersonaEntryAsset : ScriptableObject
    {
        [Tooltip("词条条目列表（当前配表 8 行）。")]
        public List<PersonaEntryEntry> entries = new List<PersonaEntryEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、词条_ID 非空且唯一、描述/条件类型/比较符非空。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "词条配置为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.entryId))
                {
                    error = $"条目 {index} 的词条_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.entryId))
                {
                    error = $"词条_ID {entry.entryId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.description))
                {
                    error = $"词条 {entry.entryId} 的触发条件描述为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.conditionType))
                {
                    error = $"词条 {entry.entryId} 的条件类型为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.comparator))
                {
                    error = $"词条 {entry.entryId} 的比较符为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
