using System;
using System.Collections.Generic;

namespace PersonaCards.Data
{
    /// <summary>
    /// 比较符定义配表契约（P0-1J）：与策划表格「比较符定义表」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 词条表「比较符」列引用本表 比较符_ID（EQ/NEQ/GT/GTE/LT/LTE/IN/NOT_IN），导入命令用本表 ID 集合做对照警告。
    /// </summary>
    public static class ComparatorDefinitionTableContract
    {
        /// <summary>工作表名（比较符定义数据）。</summary>
        public const string SheetName = "比较符定义表";

        /// <summary>列名：比较符_ID（EQ/NEQ/GT/GTE/LT/LTE/IN/NOT_IN；行标识）。</summary>
        public const string ColComparatorId = "比较符_ID";

        /// <summary>列名：中文名称（等于/不等于/大于…）。</summary>
        public const string ColName = "中文名称";

        /// <summary>列名：说明（原文存储，允许空）。</summary>
        public const string ColDescription = "说明";
    }

    /// <summary>比较符定义配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class ComparatorDefinitionMappingResult
    {
        public ComparatorDefinitionMappingResult(bool succeeded, List<ComparatorDefinitionEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（资产形态，按比较符_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<ComparatorDefinitionEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 比较符定义配表映射器：把 XlsxTableReader 输出的行字典列表转成 ComparatorDefinitionEntry 列表。
    /// 规则：比较符_ID 必填唯一；中文名称必填；说明原文存储（允许空）。
    /// </summary>
    public static class ComparatorDefinitionTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static ComparatorDefinitionMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("比较符定义表没有任何数据行。");
                return new ComparatorDefinitionMappingResult(false, null, errors, warnings);
            }

            var entries = new List<ComparatorDefinitionEntry>();
            var seenComparatorIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var comparatorId = Get(row, ComparatorDefinitionTableContract.ColComparatorId);
                var label = $"第 {rowIndex + 2} 行「{comparatorId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 比较符_ID：必填、唯一
                if (string.IsNullOrEmpty(comparatorId))
                {
                    errors.Add($"{label}：「比较符_ID」为空（必填）。");
                    continue;
                }
                if (!seenComparatorIds.Add(comparatorId))
                {
                    errors.Add($"{label}：「比较符_ID」重复，必须唯一。");
                    continue;
                }

                // 中文名称：必填（显示名）
                var name = Get(row, ComparatorDefinitionTableContract.ColName);
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{label}：「中文名称」为空（必填）。");
                    continue;
                }

                // 说明：原文存储（允许空）
                var description = Get(row, ComparatorDefinitionTableContract.ColDescription);

                entries.Add(new ComparatorDefinitionEntry
                {
                    comparatorId = comparatorId,
                    name = name,
                    description = description
                });
            }

            if (errors.Count > 0)
            {
                return new ComparatorDefinitionMappingResult(false, null, errors, warnings);
            }

            // 按比较符_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.comparatorId, right.comparatorId));

            return new ComparatorDefinitionMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
