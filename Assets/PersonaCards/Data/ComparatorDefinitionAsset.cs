using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 比较符定义条目（P0-1J）：配表「比较符定义表」sheet 3 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），B7 接线门面接收本条目列表而非资产；资产（ComparatorDefinitionAsset）只在 UI/Data 引擎程序集流转。
    /// 词条表「比较符」列引用本表 ID（EQ/NEQ/GT/GTE/LT/LTE/IN/NOT_IN），运行时判定接线留给 B7。
    /// </summary>
    [System.Serializable]
    public sealed class ComparatorDefinitionEntry
    {
        [Tooltip("比较符_ID（EQ/NEQ/GT/GTE/LT/LTE/IN/NOT_IN；行标识）。")]
        public string comparatorId;

        [Tooltip("中文名称（等于/不等于/大于…）。")]
        public string name;

        [Tooltip("说明（原文存储，允许空）。")]
        public string description;
    }

    /// <summary>
    /// 比较符定义资产：条目由菜单「Persona Cards/导入比较符定义数据」写入。
    /// 导入命令用本表 ID 集合对词条表「比较符」列做对照警告；运行时判定接线留给 B7。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/ComparatorDefinition", fileName = "ComparatorDefinition")]
    public sealed class ComparatorDefinitionAsset : ScriptableObject
    {
        [Tooltip("比较符定义条目列表（当前配表 8 行）。")]
        public List<ComparatorDefinitionEntry> entries = new List<ComparatorDefinitionEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、比较符_ID 非空且唯一、中文名称非空。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "比较符定义为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.comparatorId))
                {
                    error = $"条目 {index} 的比较符_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.comparatorId))
                {
                    error = $"比较符_ID {entry.comparatorId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.name))
                {
                    error = $"比较符 {entry.comparatorId} 的中文名称为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
