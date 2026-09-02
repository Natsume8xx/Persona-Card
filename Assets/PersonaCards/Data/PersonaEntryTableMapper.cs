using System;
using System.Collections.Generic;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌词条配表契约（P0-1J 三表之一）：与策划表格「人格牌_词条」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 比较符引「比较符定义表」sheet（EQ/GTE/NEQ/LTE…）；条件参数混写数值与枚举文本（2/4/0/NORMAL/RARE），一律原文存储。
    /// </summary>
    public static class PersonaEntryTableContract
    {
        /// <summary>工作表名（词条数据）。</summary>
        public const string SheetName = "人格牌_词条";

        /// <summary>列名：词条_ID（ENTRY_xxx；行标识，人格牌配置「词条_ID」列引用）。</summary>
        public const string ColEntryId = "词条_ID";

        /// <summary>列名：触发条件描述（显示文本）。</summary>
        public const string ColDescription = "触发条件描述";

        /// <summary>列名：条件类型（统计类条件，原文存储，B7 接线时解析）。</summary>
        public const string ColConditionType = "条件类型";

        /// <summary>列名：比较符（引「比较符定义表」sheet 的 比较符_ID）。</summary>
        public const string ColComparator = "比较符";

        /// <summary>列名：条件参数（数值或枚举文本混写，原文存储）。</summary>
        public const string ColConditionParam = "条件参数";
    }

    /// <summary>词条配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class PersonaEntryMappingResult
    {
        public PersonaEntryMappingResult(bool succeeded, List<PersonaEntryEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（资产形态，按词条_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaEntryEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（比较符不在比较符定义表内等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 词条配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaEntryEntry 列表。
    /// 规则：词条_ID 必填唯一；触发条件描述/条件类型/比较符必填；条件参数原文存储（允许空）；
    /// 比较符不在比较符定义表集合 → 警告不阻塞（comparatorIds 为 null 跳过对照，测试用）。
    /// </summary>
    public static class PersonaEntryTableMapper
    {
        /// <summary>
        /// 映射行字典列表（XlsxTableReader.ReadTable 的输出）。
        /// comparatorIds = 比较符定义表的 比较符_ID 集合（null 表示跳过对照，测试用）；
        /// 用 ICollection&lt;string&gt; 而非 IReadOnlyCollection 是为了走实例 Contains（只读接口会被 span 扩展方法截胡）。
        /// </summary>
        public static PersonaEntryMappingResult Map(List<Dictionary<string, string>> rows, ICollection<string> comparatorIds)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("词条表没有任何数据行。");
                return new PersonaEntryMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaEntryEntry>();
            var seenEntryIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var entryId = Get(row, PersonaEntryTableContract.ColEntryId);
                var label = $"第 {rowIndex + 2} 行「{entryId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 词条_ID：必填、唯一
                if (string.IsNullOrEmpty(entryId))
                {
                    errors.Add($"{label}：「词条_ID」为空（必填）。");
                    continue;
                }
                if (!seenEntryIds.Add(entryId))
                {
                    errors.Add($"{label}：「词条_ID」重复，必须唯一。");
                    continue;
                }

                // 触发条件描述：必填（显示文本）
                var description = Get(row, PersonaEntryTableContract.ColDescription);
                if (string.IsNullOrWhiteSpace(description))
                {
                    errors.Add($"{label}：「触发条件描述」为空（必填）。");
                    continue;
                }

                // 条件类型：必填，原文存储（B7 接线时解析）
                var conditionType = Get(row, PersonaEntryTableContract.ColConditionType);
                if (string.IsNullOrEmpty(conditionType))
                {
                    errors.Add($"{label}：「条件类型」为空（必填）。");
                    continue;
                }

                // 比较符：必填；不在比较符定义表 → 警告不阻塞（比较符表可能未同步）
                var comparator = Get(row, PersonaEntryTableContract.ColComparator);
                if (string.IsNullOrEmpty(comparator))
                {
                    errors.Add($"{label}：「比较符」为空（必填）。");
                    continue;
                }
                if (comparatorIds != null && !comparatorIds.Contains(comparator))
                {
                    warnings.Add($"{label}：「比较符」值「{comparator}」不在比较符定义表中（比较符表可能未同步，请策划确认）。");
                }

                // 条件参数：原文存储（数值 2/4/0 与枚举 NORMAL/RARE 混写；允许空）
                var conditionParam = Get(row, PersonaEntryTableContract.ColConditionParam);

                entries.Add(new PersonaEntryEntry
                {
                    entryId = entryId,
                    description = description,
                    conditionType = conditionType,
                    comparator = comparator,
                    conditionParam = conditionParam
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaEntryMappingResult(false, null, errors, warnings);
            }

            // 按词条_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.entryId, right.entryId));

            return new PersonaEntryMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
