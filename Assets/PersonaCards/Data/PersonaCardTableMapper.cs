using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌配置配表契约（P0-1J 三表之一）：与策划表格「人格牌配置」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 引用列（词条_ID/主属性_ID/次级属性_ID）只存原文不 join——三表各自独立导入，运行时接线是 B7 的事。
    /// </summary>
    public static class PersonaCardTableContract
    {
        /// <summary>工作表名（人格牌数据）。</summary>
        public const string SheetName = "人格牌配置";

        /// <summary>列名：人格牌_ID（PER_xxx；行标识）。</summary>
        public const string ColPersonaId = "人格牌_ID";

        /// <summary>列名：人格牌名称（显示名）。</summary>
        public const string ColName = "人格牌名称";

        /// <summary>列名：词条_ID（引「人格牌_词条」sheet，ENTRY_xxx；原文存储）。</summary>
        public const string ColEntryId = "词条_ID";

        /// <summary>列名：主属性_ID（引「人格牌_主属性」sheet，MAIN_xxx；原文存储）。</summary>
        public const string ColMainAttrId = "主属性_ID";

        /// <summary>列名：次级属性_ID（次级属性池起点，SUB_xxx；原文存储）。</summary>
        public const string ColSubAttrId = "次级属性_ID";

        /// <summary>列名：最大属性数量（非负整数）。</summary>
        public const string ColMaxAttrs = "最大属性数量";

        /// <summary>列名：最大次级属性数量（非负整数）。</summary>
        public const string ColMaxSubAttrs = "最大次级属性数量";

        /// <summary>列名：次级属性池数量（非负整数）。</summary>
        public const string ColSubPoolSize = "次级属性池数量";
    }

    /// <summary>人格牌配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class PersonaCardMappingResult
    {
        public PersonaCardMappingResult(bool succeeded, List<PersonaCardEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（资产形态，按人格牌_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaCardEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 人格牌配置配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaCardEntry 列表。
    /// 规则：人格牌_ID 必填唯一；三个引用列必填（存原文不 join，断链由 B7 接线时再报）；数量列非负整数。
    /// 不做 PER_xxx 齐全检查——当前配表仅 8 行，后 8 张待策划补表（见代策划确认）。
    /// </summary>
    public static class PersonaCardTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static PersonaCardMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("人格牌配置表没有任何数据行。");
                return new PersonaCardMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaCardEntry>();
            var seenPersonaIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var personaId = Get(row, PersonaCardTableContract.ColPersonaId);
                var label = $"第 {rowIndex + 2} 行「{personaId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 人格牌_ID：必填、唯一
                if (string.IsNullOrEmpty(personaId))
                {
                    errors.Add($"{label}：「人格牌_ID」为空（必填）。");
                    continue;
                }
                if (!seenPersonaIds.Add(personaId))
                {
                    errors.Add($"{label}：「人格牌_ID」重复，必须唯一。");
                    continue;
                }

                // 人格牌名称：必填
                var personaName = Get(row, PersonaCardTableContract.ColName);
                if (string.IsNullOrWhiteSpace(personaName))
                {
                    errors.Add($"{label}：「人格牌名称」为空（必填）。");
                    continue;
                }

                // 三个引用列：必填，存原文不 join（断链由 B7 接线时报）
                var entryId = Get(row, PersonaCardTableContract.ColEntryId);
                if (string.IsNullOrEmpty(entryId))
                {
                    errors.Add($"{label}：「词条_ID」为空（必填）。");
                    continue;
                }
                var mainAttrId = Get(row, PersonaCardTableContract.ColMainAttrId);
                if (string.IsNullOrEmpty(mainAttrId))
                {
                    errors.Add($"{label}：「主属性_ID」为空（必填）。");
                    continue;
                }
                var subAttrId = Get(row, PersonaCardTableContract.ColSubAttrId);
                if (string.IsNullOrEmpty(subAttrId))
                {
                    errors.Add($"{label}：「次级属性_ID」为空（必填）。");
                    continue;
                }

                // 数量列：非负整数
                var maxAttrsText = Get(row, PersonaCardTableContract.ColMaxAttrs);
                if (!int.TryParse(maxAttrsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxAttrs) || maxAttrs < 0)
                {
                    errors.Add($"{label}：「最大属性数量」值「{maxAttrsText}」不是非负整数。");
                    continue;
                }
                var maxSubAttrsText = Get(row, PersonaCardTableContract.ColMaxSubAttrs);
                if (!int.TryParse(maxSubAttrsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxSubAttrs) || maxSubAttrs < 0)
                {
                    errors.Add($"{label}：「最大次级属性数量」值「{maxSubAttrsText}」不是非负整数。");
                    continue;
                }
                var subPoolSizeText = Get(row, PersonaCardTableContract.ColSubPoolSize);
                if (!int.TryParse(subPoolSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var subPoolSize) || subPoolSize < 0)
                {
                    errors.Add($"{label}：「次级属性池数量」值「{subPoolSizeText}」不是非负整数。");
                    continue;
                }

                entries.Add(new PersonaCardEntry
                {
                    personaId = personaId,
                    personaName = personaName,
                    entryId = entryId,
                    mainAttrId = mainAttrId,
                    subAttrId = subAttrId,
                    maxAttrs = maxAttrs,
                    maxSubAttrs = maxSubAttrs,
                    subPoolSize = subPoolSize
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaCardMappingResult(false, null, errors, warnings);
            }

            // 按人格牌_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.personaId, right.personaId));

            return new PersonaCardMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
