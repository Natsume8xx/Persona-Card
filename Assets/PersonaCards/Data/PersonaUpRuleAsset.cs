using System;
using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌强化规则条目（P0-1J）：配表「商品_人格牌强化规则」sheet 5 列全量落地。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），P0-7 接线门面接收本条目列表而非资产；资产（PersonaUpRuleAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [System.Serializable]
    public sealed class PersonaUpRuleEntry
    {
        [Tooltip("强化规则_ID（PERSONA_UP_xxx；行标识）。")]
        public string ruleId;

        [Tooltip("主属性类型：筹码型/倍率型/独立倍率型。")]
        public string mainAttrType;

        [Tooltip("每级增加（原文存储；混写 +10筹码/+0.3倍率/+10%独立倍率）。")]
        public string perLevelIncrease;

        [Tooltip("基础价格（非负整数）。")]
        public int basePrice;

        [Tooltip("每级涨价（非负整数）。")]
        public int levelPriceStep;
    }

    /// <summary>
    /// 人格牌强化规则资产：条目由菜单「Persona Cards/导入人格牌强化规则数据」写入。
    /// 强化运行时逻辑（P0-7）经门面接收条目列表；本任务只做契约读取 + 资产化，运行时零接线。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaUpRule", fileName = "PersonaUpRule")]
    public sealed class PersonaUpRuleAsset : ScriptableObject
    {
        [Tooltip("强化规则条目列表（当前配表 3 行，按强化规则_ID 升序）。")]
        public List<PersonaUpRuleEntry> entries = new List<PersonaUpRuleEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因。
        /// 规则：条目非空、强化规则_ID 非空且唯一、主属性类型合法、每级增加非空、基础价格/每级涨价非负。
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;
            if (entries == null || entries.Count == 0)
            {
                error = "人格牌强化规则为空：至少需要一个条目。";
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
                if (string.IsNullOrWhiteSpace(entry.ruleId))
                {
                    error = $"条目 {index} 的强化规则_ID 为空。";
                    return false;
                }
                if (!seen.Add(entry.ruleId))
                {
                    error = $"强化规则_ID {entry.ruleId} 重复出现（条目 {index}）。";
                    return false;
                }
                if (Array.IndexOf(PersonaUpRuleTableContract.MainAttrTypes, entry.mainAttrType) < 0)
                {
                    error = $"强化规则 {entry.ruleId} 的主属性类型「{entry.mainAttrType}」无效，应为 {string.Join("/", PersonaUpRuleTableContract.MainAttrTypes)}。";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(entry.perLevelIncrease))
                {
                    error = $"强化规则 {entry.ruleId} 的每级增加为空。";
                    return false;
                }
                if (entry.basePrice < 0)
                {
                    error = $"强化规则 {entry.ruleId} 的基础价格不能为负数（当前 {entry.basePrice}）。";
                    return false;
                }
                if (entry.levelPriceStep < 0)
                {
                    error = $"强化规则 {entry.ruleId} 的每级涨价不能为负数（当前 {entry.levelPriceStep}）。";
                    return false;
                }
            }

            return true;
        }
    }
}
