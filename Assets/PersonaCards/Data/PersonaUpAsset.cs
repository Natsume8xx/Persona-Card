using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌强化条目（P0-1J）：配表「商品_人格牌强化」sheet 8 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（PersonaUpAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaUpEntry
    {
        [Tooltip("人格_ID（PER_xxx；行标识）。")]
        public string personaId;

        [Tooltip("人格名称（显示名）。")]
        public string personaName;

        [Tooltip("主属性类型：筹码型/倍率型/独立倍率型。")]
        public string mainAttrType;

        [Tooltip("Lv0 数值（原文存储；整数与小数混写）。")]
        public string lv0;

        [Tooltip("Lv1 数值（原文存储）。")]
        public string lv1;

        [Tooltip("Lv2 数值（原文存储）。")]
        public string lv2;

        [Tooltip("Lv3 数值（原文存储）。")]
        public string lv3;

        [Tooltip("Lv4 数值（原文存储）。")]
        public string lv4;
    }

    /// <summary>
    /// 人格牌强化资产：条目由菜单「Persona Cards/导入人格牌强化数据」写入。
    /// 强化运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaUp", fileName = "PersonaUp")]
    public sealed class PersonaUpAsset : ScriptableObject
    {
        [Tooltip("人格牌强化条目列表（当前配表 8 行，按人格_ID 升序）。")]
        public List<PersonaUpEntry> entries = new List<PersonaUpEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、人格_ID 非空且唯一、人格名称非空、主属性类型合法、Lv0~Lv4 非空。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "人格牌强化为空：至少需要一个条目。";
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
                    error = $"条目 {index} 的人格_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.personaId))
                {
                    error = $"人格_ID {entry.personaId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.personaName))
                {
                    error = $"人格 {entry.personaId} 的人格名称为空。";
                    return false;
                }
                if (Array.IndexOf(PersonaUpRuleTableContract.MainAttrTypes, entry.mainAttrType) < 0)
                {
                    error = $"人格 {entry.personaId} 的主属性类型「{entry.mainAttrType}」无效，应为 {string.Join("/", PersonaUpRuleTableContract.MainAttrTypes)}。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.lv0) || string.IsNullOrEmpty(entry.lv1) ||
                    string.IsNullOrEmpty(entry.lv2) || string.IsNullOrEmpty(entry.lv3) || string.IsNullOrEmpty(entry.lv4))
                {
                    error = $"人格 {entry.personaId} 的 Lv0~Lv4 数值不能为空。";
                    return false;
                }
            }

            return true;
        }
    }
}
