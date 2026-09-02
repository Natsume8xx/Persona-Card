using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌强化规则配表契约（P0-1J）：与策划表格「商品_人格牌强化规则」sheet 的表头与枚举值约定。
    /// 修改表格结构或枚举值必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 「主属性类型」三值同时被「商品_人格牌强化」表引用（PersonaUpTableContract）。
    /// </summary>
    public static class PersonaUpRuleTableContract
    {
        /// <summary>工作表名（人格牌强化规则数据）。</summary>
        public const string SheetName = "商品_人格牌强化规则";

        /// <summary>列名：强化规则_ID（PERSONA_UP_xxx；行标识）。</summary>
        public const string ColRuleId = "强化规则_ID";

        /// <summary>列名：主属性类型（筹码型/倍率型/独立倍率型）。</summary>
        public const string ColMainAttrType = "主属性类型";

        /// <summary>列名：每级增加（原文存储；混写 +10筹码/+0.3倍率/+10%独立倍率，语义解析留给后续阶段）。</summary>
        public const string ColPerLevelIncrease = "每级增加";

        /// <summary>列名：基础价格（必填，非负整数）。</summary>
        public const string ColBasePrice = "基础价格";

        /// <summary>列名：每级涨价（必填，非负整数）。</summary>
        public const string ColLevelPriceStep = "每级涨价";

        /// <summary>主属性类型枚举值：筹码型。</summary>
        public const string MainAttrTypeChips = "筹码型";

        /// <summary>主属性类型枚举值：倍率型。</summary>
        public const string MainAttrTypeMult = "倍率型";

        /// <summary>主属性类型枚举值：独立倍率型。</summary>
        public const string MainAttrTypeXMult = "独立倍率型";

        /// <summary>主属性类型合法值集合（同时约束「商品_人格牌强化」表）。</summary>
        public static readonly string[] MainAttrTypes = { MainAttrTypeChips, MainAttrTypeMult, MainAttrTypeXMult };
    }

    /// <summary>人格牌强化规则映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）。</summary>
    public sealed class PersonaUpRuleMappingResult
    {
        public PersonaUpRuleMappingResult(bool succeeded, List<PersonaUpRuleEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的强化规则条目（按强化规则_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaUpRuleEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与同阶段结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 人格牌强化规则配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaUpRuleEntry 列表。
    /// 规则：强化规则_ID 必填唯一；主属性类型只认三值；每级增加必填原文存储；基础价格/每级涨价必填非负整数。
    /// </summary>
    public static class PersonaUpRuleTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static PersonaUpRuleMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商品_人格牌强化规则没有任何数据行。");
                return new PersonaUpRuleMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaUpRuleEntry>();
            var seenRuleIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var ruleId = Get(row, PersonaUpRuleTableContract.ColRuleId);
                var label = $"第 {rowIndex + 2} 行「{ruleId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 强化规则_ID：必填、唯一
                if (string.IsNullOrEmpty(ruleId))
                {
                    errors.Add($"{label}：「强化规则_ID」为空（必填）。");
                    continue;
                }
                if (!seenRuleIds.Add(ruleId))
                {
                    errors.Add($"{label}：「强化规则_ID」重复，必须唯一。");
                    continue;
                }

                // 主属性类型：只认三值（与「商品_人格牌强化」表同集合）
                var mainAttrType = Get(row, PersonaUpRuleTableContract.ColMainAttrType);
                if (Array.IndexOf(PersonaUpRuleTableContract.MainAttrTypes, mainAttrType) < 0)
                {
                    errors.Add($"{label}：「主属性类型」值「{mainAttrType}」无效，应为 {string.Join("/", PersonaUpRuleTableContract.MainAttrTypes)}。");
                    continue;
                }

                // 每级增加：必填原文（混写 +10筹码/+0.3倍率/+10%独立倍率，语义解析留给后续阶段）
                var perLevelIncrease = Get(row, PersonaUpRuleTableContract.ColPerLevelIncrease);
                if (string.IsNullOrEmpty(perLevelIncrease))
                {
                    errors.Add($"{label}：「每级增加」为空（必填）。");
                    continue;
                }

                // 基础价格：必填非负整数
                var basePriceText = Get(row, PersonaUpRuleTableContract.ColBasePrice);
                if (!int.TryParse(basePriceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var basePrice) || basePrice < 0)
                {
                    errors.Add($"{label}：「基础价格」值「{basePriceText}」不是非负整数（必填）。");
                    continue;
                }

                // 每级涨价：必填非负整数
                var levelPriceStepText = Get(row, PersonaUpRuleTableContract.ColLevelPriceStep);
                if (!int.TryParse(levelPriceStepText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var levelPriceStep) || levelPriceStep < 0)
                {
                    errors.Add($"{label}：「每级涨价」值「{levelPriceStepText}」不是非负整数（必填）。");
                    continue;
                }

                entries.Add(new PersonaUpRuleEntry
                {
                    ruleId = ruleId,
                    mainAttrType = mainAttrType,
                    perLevelIncrease = perLevelIncrease,
                    basePrice = basePrice,
                    levelPriceStep = levelPriceStep
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaUpRuleMappingResult(false, null, errors, warnings);
            }

            // 按强化规则_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.ruleId, right.ruleId));

            return new PersonaUpRuleMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
