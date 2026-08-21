using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PersonaCards.Data
{
    /// <summary>
    /// 全局配置配表契约：与策划表格「全局配置」sheet 的表头与枚举值约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1F.md 记录）。
    /// </summary>
    public static class GlobalConfigTableContract
    {
        /// <summary>工作表名（全局配置数据）。</summary>
        public const string SheetName = "全局配置";

        /// <summary>列名：规则_ID（RULE_xxx；权威查询键，RULE_001~012 必须齐全）。</summary>
        public const string ColRuleId = "规则_ID";

        /// <summary>列名：规则名称（仅存值，Inspector/日志可读）。</summary>
        public const string ColRuleName = "规则名称";

        /// <summary>列名：数值类型（整数/小数）。</summary>
        public const string ColValueType = "数值类型";

        /// <summary>列名：配置数值（非负；整数规则整数字面量，小数规则 decimal 原文）。</summary>
        public const string ColValue = "配置数值";

        /// <summary>枚举值：数值类型「整数」。</summary>
        public const string ValueTypeInteger = "整数";

        /// <summary>枚举值：数值类型「小数」。</summary>
        public const string ValueTypeDecimal = "小数";

        /// <summary>数值类型合法值集合。</summary>
        public static readonly string[] ValueTypes = { ValueTypeInteger, ValueTypeDecimal };

        /// <summary>规则_ID 格式（RULE_001~RULE_999）。</summary>
        public const string RuleIdPattern = @"^RULE_\d{3}$";

        /// <summary>当前契约要求齐全的规则_ID 条数（RULE_001~012；Mapper 齐全校验用）。</summary>
        public const int RequiredRuleCount = 12;
    }

    /// <summary>全局配置映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class GlobalConfigMappingResult
    {
        public GlobalConfigMappingResult(bool succeeded, List<GlobalConfigEntry> entries,
            List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的资产条目（按规则_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<GlobalConfigEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与 P0-1C/1D/1E 结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 全局配置配表映射器：把 XlsxTableReader 输出的行字典列表转成 GlobalConfigEntry 列表。
    /// 规则：RULE_001~012 必须齐全（防策划误删，多出允许——规则可扩展）；数值类型「整数/小数」与配置数值
    /// 类型一致性校验；数值 string 原文保存（如 0.65 精确保存，供门面 parse）。
    /// </summary>
    public static class GlobalConfigTableMapper
    {
        /// <summary>
        /// 映射行字典列表（XlsxTableReader.ReadTable 的输出）。
        /// </summary>
        public static GlobalConfigMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("全局配置表没有任何数据行。");
                return new GlobalConfigMappingResult(false, null, errors, warnings);
            }

            var entries = new List<GlobalConfigEntry>();
            var seenRuleIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var ruleId = Get(row, GlobalConfigTableContract.ColRuleId);
                var label = $"第 {rowIndex + 2} 行「{ruleId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 规则_ID：非空、格式、唯一（权威查询键）
                if (string.IsNullOrEmpty(ruleId))
                {
                    errors.Add($"{label}：「规则_ID」为空（必填）。");
                    continue;
                }
                if (!Regex.IsMatch(ruleId, GlobalConfigTableContract.RuleIdPattern))
                {
                    errors.Add($"{label}：「规则_ID」值「{ruleId}」格式无效，应为 RULE_001~RULE_999。");
                    continue;
                }
                if (!seenRuleIds.Add(ruleId))
                {
                    errors.Add($"{label}：「规则_ID」重复，必须唯一。");
                    continue;
                }

                // 规则名称：仅存值但不得为空
                var ruleName = Get(row, GlobalConfigTableContract.ColRuleName);
                if (string.IsNullOrEmpty(ruleName))
                {
                    errors.Add($"{label}：「规则名称」为空（必填）。");
                    continue;
                }

                // 数值类型：整数/小数
                var valueType = Get(row, GlobalConfigTableContract.ColValueType);
                if (Array.IndexOf(GlobalConfigTableContract.ValueTypes, valueType) < 0)
                {
                    errors.Add($"{label}：「数值类型」值「{valueType}」无效，应为 {string.Join("/", GlobalConfigTableContract.ValueTypes)}。");
                    continue;
                }

                // 配置数值：必填、非负、类型一致（整数规则整数字面量；小数规则 decimal 原文精确保存）
                var valueText = Get(row, GlobalConfigTableContract.ColValue);
                if (!TryParseValueText(valueType, valueText, out var valueError))
                {
                    errors.Add($"{label}：「配置数值」值「{valueText}」无效：{valueError}");
                    continue;
                }

                entries.Add(new GlobalConfigEntry
                {
                    ruleId = ruleId,
                    ruleName = ruleName,
                    valueType = valueType,
                    valueText = valueText
                });
            }

            if (errors.Count > 0)
            {
                return new GlobalConfigMappingResult(false, null, errors, warnings);
            }

            // RULE_001~012 齐全检查（防策划误删行）：缺任一 = 错误；多出的 ID 允许（规则可扩展）
            for (var index = 1; index <= GlobalConfigTableContract.RequiredRuleCount; index++)
            {
                var expected = $"RULE_{index:D3}";
                if (!seenRuleIds.Contains(expected))
                {
                    errors.Add($"全局配置表缺少 {expected} 的行（RULE_001~RULE_{GlobalConfigTableContract.RequiredRuleCount:D3} 应齐全）：请确认该行未被误删。");
                }
            }
            if (errors.Count > 0)
            {
                return new GlobalConfigMappingResult(false, null, errors, warnings);
            }

            // 按规则_ID 升序排列条目（资产 Inspector 与日志的可读性；门面 Configure 不依赖顺序）
            entries.Sort((left, right) => string.CompareOrdinal(left.ruleId, right.ruleId));

            return new GlobalConfigMappingResult(true, entries, errors, warnings);
        }

        /// <summary>
        /// 配置数值三层防御共享校验（Mapper / 资产 Validate / 门面 Configure 同用，防漂移）：
        /// 非空；「整数」→ invariant 整数且 ≥0（整数字面量，4.5 不合格）；「小数」→ invariant decimal 且 ≥0（整数文本 3 合格）。
        /// 成功返回 true；失败返回 false 且 error 带原因（供行级错误文案）。
        /// </summary>
        public static bool TryParseValueText(string valueType, string valueText, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(valueText))
            {
                error = "为空（必填）。";
                return false;
            }
            if (valueType == GlobalConfigTableContract.ValueTypeInteger)
            {
                if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue)
                    || intValue < 0)
                {
                    error = "应为非负整数（整数规则不允许小数点）。";
                    return false;
                }
                return true;
            }
            if (!decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                || decimalValue < 0m)
            {
                error = "应为非负数字。";
                return false;
            }
            return true;
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
