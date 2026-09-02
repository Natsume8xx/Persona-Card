using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌主属性条目（P0-1J 三表之一）：配表「人格牌_主属性」sheet 5 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），B7 接线门面接收本条目列表而非资产；资产（PersonaMainAttrAsset）只在 UI/Data 引擎程序集流转。
    /// 属性参数2 混写整数与小数（15/40/30/1/0.05），一律原文存储——语义解析留给 B7，不在导入层引入格式判定。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaMainAttrEntry
    {
        [Tooltip("主属性_ID（MAIN_xxx；行标识，人格牌配置「主属性_ID」列引用）。")]
        public string attrId;

        [Tooltip("属性类型（基础筹码/基础倍率/独立倍率等，原文存储）。")]
        public string attrType;

        [Tooltip("属性参数1（当前全「增加」，原文存储）。")]
        public string param1;

        [Tooltip("属性参数2（数值混写，原文存储，允许空）。")]
        public string param2;

        [Tooltip("开放节点（默认/AI1…，原文存储）。")]
        public string unlockNode;
    }

    /// <summary>
    /// 主属性目录资产：条目由菜单「Persona Cards/导入主属性数据」写入。
    /// 运行时接线（主属性效果实现）是 B7 的事；本资产只存契约数据。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaMainAttrCatalog", fileName = "PersonaMainAttrCatalog")]
    public sealed class PersonaMainAttrAsset : ScriptableObject
    {
        [Tooltip("主属性条目列表（当前配表 8 行）。")]
        public List<PersonaMainAttrEntry> entries = new List<PersonaMainAttrEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、主属性_ID 非空且唯一、属性类型/属性参数1/开放节点非空。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "主属性配置为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.attrId))
                {
                    error = $"条目 {index} 的主属性_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.attrId))
                {
                    error = $"主属性_ID {entry.attrId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.attrType))
                {
                    error = $"主属性 {entry.attrId} 的属性类型为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.param1))
                {
                    error = $"主属性 {entry.attrId} 的属性参数1为空。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.unlockNode))
                {
                    error = $"主属性 {entry.attrId} 的开放节点为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
