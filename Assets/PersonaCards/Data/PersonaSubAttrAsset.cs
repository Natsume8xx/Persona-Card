using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌次级属性条目（P0-1J 三表之一）：配表「人格牌_次级属性」sheet 7 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），B7 接线门面接收本条目列表而非资产；资产（PersonaSubAttrAsset）只在 UI/Data 引擎程序集流转。
    /// 「所属人格」列当前填人格牌名称（非 PER_ID）；属性参数2 混写整数与小数（8/20/0.3/0.03/0.5/1/5），一律原文存储。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaSubAttrEntry
    {
        [Tooltip("次级属性_ID（SUB_xxx；行标识）。")]
        public string subAttrId;

        [Tooltip("所属人格（当前填人格牌名称，原文存储；接线时再定名称↔ID 映射）。")]
        public string ownerPersona;

        [Tooltip("权重（非负整数，池内抽取权重）。")]
        public int weight;

        [Tooltip("属性类型（基础筹码/基础倍率/独立倍率/出牌次数/弃牌次数/金币等，原文存储）。")]
        public string attrType;

        [Tooltip("属性参数1（当前全「增加」，原文存储）。")]
        public string param1;

        [Tooltip("属性参数2（数值混写，原文存储，允许空）。")]
        public string param2;

        [Tooltip("开放节点（AI1/AI2/AI3，原文存储）。")]
        public string unlockNode;
    }

    /// <summary>
    /// 次级属性目录资产：条目由菜单「Persona Cards/导入次级属性数据」写入。
    /// 运行时接线（次级属性池抽取与效果实现）是 B7 的事；本资产只存契约数据。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaSubAttrCatalog", fileName = "PersonaSubAttrCatalog")]
    public sealed class PersonaSubAttrAsset : ScriptableObject
    {
        [Tooltip("次级属性条目列表（当前配表 40 行）。")]
        public List<PersonaSubAttrEntry> entries = new List<PersonaSubAttrEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、次级属性_ID 非空且唯一、所属人格/属性类型/属性参数1/开放节点非空、权重非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "次级属性配置为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.subAttrId))
                {
                    error = $"条目 {index} 的次级属性_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.subAttrId))
                {
                    error = $"次级属性_ID {entry.subAttrId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.ownerPersona))
                {
                    error = $"次级属性 {entry.subAttrId} 的所属人格为空。";
                    return false;
                }
                if (entry.weight < 0)
                {
                    error = $"次级属性 {entry.subAttrId} 的权重为负数（{entry.weight}）。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.attrType))
                {
                    error = $"次级属性 {entry.subAttrId} 的属性类型为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.param1))
                {
                    error = $"次级属性 {entry.subAttrId} 的属性参数1为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.unlockNode))
                {
                    error = $"次级属性 {entry.subAttrId} 的开放节点为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
