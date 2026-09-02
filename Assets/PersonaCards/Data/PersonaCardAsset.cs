using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌配置条目（P0-1J 三表之一）：配表「人格牌配置」sheet 8 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），B7 接线门面接收本条目列表而非资产；资产（PersonaCardAsset）只在 UI/Data 引擎程序集流转。
    /// 引用列（词条_ID/主属性_ID/次级属性_ID）只存原文不 join；数量列非负整数。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaCardEntry
    {
        [Tooltip("人格牌_ID（PER_xxx；行标识，权威查询键）。")]
        public string personaId;

        [Tooltip("人格牌名称（显示名）。")]
        public string personaName;

        [Tooltip("词条_ID（引「人格牌_词条」sheet，ENTRY_xxx；原文存储）。")]
        public string entryId;

        [Tooltip("主属性_ID（引「人格牌_主属性」sheet，MAIN_xxx；原文存储）。")]
        public string mainAttrId;

        [Tooltip("次级属性_ID（次级属性池起点，SUB_xxx；原文存储）。")]
        public string subAttrId;

        [Tooltip("最大属性数量（非负整数）。")]
        public int maxAttrs;

        [Tooltip("最大次级属性数量（非负整数）。")]
        public int maxSubAttrs;

        [Tooltip("次级属性池数量（非负整数）。")]
        public int subPoolSize;
    }

    /// <summary>
    /// 人格牌卡牌目录资产：条目由菜单「Persona Cards/导入人格牌卡牌目录数据」写入。
    /// 运行时接线（人格牌实例化/词条装配）是 B7 的事；本资产只存契约数据。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaCardCatalog", fileName = "PersonaCardCatalog")]
    public sealed class PersonaCardAsset : ScriptableObject
    {
        [Tooltip("人格牌条目列表（当前配表 8 行，后 8 张待策划补表）。")]
        public List<PersonaCardEntry> entries = new List<PersonaCardEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、人格牌_ID 非空且唯一、名称与三个引用列非空、数量列非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "人格牌配置为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.personaId))
                {
                    error = $"条目 {index} 的人格牌_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.personaId))
                {
                    error = $"人格牌_ID {entry.personaId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.personaName))
                {
                    error = $"人格牌 {entry.personaId} 的名称为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.entryId))
                {
                    error = $"人格牌 {entry.personaId} 的词条_ID 为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.mainAttrId))
                {
                    error = $"人格牌 {entry.personaId} 的主属性_ID 为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.subAttrId))
                {
                    error = $"人格牌 {entry.personaId} 的次级属性_ID 为空。";
                    return false;
                }
                if (entry.maxAttrs < 0)
                {
                    error = $"人格牌 {entry.personaId} 的最大属性数量为负数（{entry.maxAttrs}）。";
                    return false;
                }
                if (entry.maxSubAttrs < 0)
                {
                    error = $"人格牌 {entry.personaId} 的最大次级属性数量为负数（{entry.maxSubAttrs}）。";
                    return false;
                }
                if (entry.subPoolSize < 0)
                {
                    error = $"人格牌 {entry.personaId} 的次级属性池数量为负数（{entry.subPoolSize}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
