using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 全局配置条目：配表 4 列全量落地，数值类型与配置数值均存 string 原文（空 = 无）。
    /// 顶层纯 C# 类（P0-1E 模式）：门面（UI 层 GlobalConfig）接收本条目列表而非资产，
    /// 资产（GlobalConfigAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [Serializable]
    public sealed class GlobalConfigEntry
    {
        [Tooltip("规则_ID（RULE_xxx，权威查询键；RULE_001~017 应齐全）。")]
        public string ruleId;

        [Tooltip("规则名称（仅存值，供 Inspector 与日志可读）。")]
        public string ruleName;

        [Tooltip("数值类型：整数/小数（与契约 GlobalConfigTableContract.ValueTypes 对照）。")]
        public string valueType;

        [Tooltip("配置数值原文（非负；整数规则要求整数字面量；如 0.65 精确保存）。")]
        public string valueText;
    }

    /// <summary>
    /// 全局配置资产：17 条规则（RULE_001~017）的配表落地，由菜单「导入全局配置数据」写入。
    /// P0-1F 白盒语义：空条目资产合法（UI 门面回落 = 空配置，出牌/弃牌回落 Battle 编译期常量 4/3，行为与 P0-1F 前零差异），
    /// 因此 RULE_001~017 齐全校验不在此层（在 GlobalConfigTableMapper 导入层，防误删）。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/GlobalConfig", fileName = "GlobalConfig")]
    public sealed class GlobalConfigAsset : ScriptableObject
    {
        /// <summary>资产固定路径（导入命令与场景构建器共用）。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/GlobalConfig.asset";

        [Tooltip("全局配置条目（RULE_001~017 齐全，导入后按规则_ID 升序；空列表 = 白盒合法）。")]
        public List<GlobalConfigEntry> entries = new List<GlobalConfigEntry>();

        /// <summary>
        /// 单错误模式校验（同 PersonaConfigAsset 惯例）：返回 false 时 error 带原因，调用方警告并回落白盒。
        /// 空条目列表 = 合法（白盒）。配置数值校验与 Mapper/门面共享 TryParseValueText（三层防御同一实现，防漂移）。
        /// </summary>
        public bool Validate(out string error)
        {
            var seenRuleIds = new HashSet<string>();
            foreach (var entry in entries)
            {
                // 条目与权威键
                if (entry == null)
                {
                    error = "存在空条目：请删除或重新导入。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.ruleId))
                {
                    error = "存在「规则_ID」为空的条目：请删除或重新导入。";
                    return false;
                }
                if (!Regex.IsMatch(entry.ruleId, GlobalConfigTableContract.RuleIdPattern))
                {
                    error = $"「规则_ID」值「{entry.ruleId}」格式无效，应为 RULE_001~RULE_999。";
                    return false;
                }
                if (!seenRuleIds.Add(entry.ruleId))
                {
                    error = $"「规则_ID」重复：{entry.ruleId}（必须唯一）。";
                    return false;
                }

                // 名称（仅存值但不得为空）
                if (string.IsNullOrEmpty(entry.ruleName))
                {
                    error = $"「{entry.ruleId}」的「规则名称」为空。";
                    return false;
                }

                // 数值类型只认契约两值
                if (Array.IndexOf(GlobalConfigTableContract.ValueTypes, entry.valueType) < 0)
                {
                    error = $"「{entry.ruleId}」的「数值类型」值「{entry.valueType}」无效，应为 {string.Join("/", GlobalConfigTableContract.ValueTypes)}。";
                    return false;
                }

                // 配置数值：必填、非负、类型一致（共享实现，见 GlobalConfigTableMapper.TryParseValueText）
                if (!GlobalConfigTableMapper.TryParseValueText(entry.valueType, entry.valueText, out var valueError))
                {
                    error = $"「{entry.ruleId}」的「配置数值」值「{entry.valueText}」无效：{valueError}";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
