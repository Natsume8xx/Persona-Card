using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 花色强化配表契约（P0-1J）：与策划表格「商品_花色强化」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 花色_ID 引用新 sheet「花色配置表」（SUIT_001~004；导入命令不 join，断链检查留给后续阶段）。
    /// </summary>
    public static class SuitUpTableContract
    {
        /// <summary>工作表名（花色强化数据）。</summary>
        public const string SheetName = "商品_花色强化";

        /// <summary>列名：花色强化_ID（SUIT_UP_xxx；行标识）。</summary>
        public const string ColSuitUpId = "花色强化_ID";

        /// <summary>列名：花色_ID（SUIT_xxx；原文存储，不 join）。</summary>
        public const string ColSuitId = "花色_ID";

        /// <summary>列名：花色名称（显示名）。</summary>
        public const string ColSuitName = "花色名称";

        /// <summary>列名：等级（Lv.1~Lv.4，原文存储，不假设格式）。</summary>
        public const string ColLevel = "等级";

        /// <summary>列名：额外筹码（必填，非负整数）。</summary>
        public const string ColExtraChips = "额外筹码";

        /// <summary>列名：价格（必填，非负整数）。</summary>
        public const string ColPrice = "价格";
    }

    /// <summary>花色强化映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）。</summary>
    public sealed class SuitUpMappingResult
    {
        public SuitUpMappingResult(bool succeeded, List<SuitUpEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的花色强化条目（按花色强化_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<SuitUpEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与同阶段结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 花色强化配表映射器：把 XlsxTableReader 输出的行字典列表转成 SuitUpEntry 列表。
    /// 规则：花色强化_ID 必填唯一；花色_ID 必填原文（不 join）；花色名称必填；等级必填原文；
    /// 额外筹码/价格必填非负整数。
    /// </summary>
    public static class SuitUpTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static SuitUpMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商品_花色强化没有任何数据行。");
                return new SuitUpMappingResult(false, null, errors, warnings);
            }

            var entries = new List<SuitUpEntry>();
            var seenSuitUpIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var suitUpId = Get(row, SuitUpTableContract.ColSuitUpId);
                var label = $"第 {rowIndex + 2} 行「{suitUpId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 花色强化_ID：必填、唯一
                if (string.IsNullOrEmpty(suitUpId))
                {
                    errors.Add($"{label}：「花色强化_ID」为空（必填）。");
                    continue;
                }
                if (!seenSuitUpIds.Add(suitUpId))
                {
                    errors.Add($"{label}：「花色强化_ID」重复，必须唯一。");
                    continue;
                }

                // 花色_ID：必填原文（引用新 sheet「花色配置表」，不 join）
                var suitId = Get(row, SuitUpTableContract.ColSuitId);
                if (string.IsNullOrEmpty(suitId))
                {
                    errors.Add($"{label}：「花色_ID」为空（必填）。");
                    continue;
                }

                // 花色名称：必填（显示名）
                var suitName = Get(row, SuitUpTableContract.ColSuitName);
                if (string.IsNullOrWhiteSpace(suitName))
                {
                    errors.Add($"{label}：「花色名称」为空（必填）。");
                    continue;
                }

                // 等级：必填原文（Lv.1~Lv.4，不假设格式）
                var level = Get(row, SuitUpTableContract.ColLevel);
                if (string.IsNullOrEmpty(level))
                {
                    errors.Add($"{label}：「等级」为空（必填）。");
                    continue;
                }

                // 额外筹码：必填非负整数
                var extraChipsText = Get(row, SuitUpTableContract.ColExtraChips);
                if (!int.TryParse(extraChipsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var extraChips) || extraChips < 0)
                {
                    errors.Add($"{label}：「额外筹码」值「{extraChipsText}」不是非负整数（必填）。");
                    continue;
                }

                // 价格：必填非负整数
                var priceText = Get(row, SuitUpTableContract.ColPrice);
                if (!int.TryParse(priceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) || price < 0)
                {
                    errors.Add($"{label}：「价格」值「{priceText}」不是非负整数（必填）。");
                    continue;
                }

                entries.Add(new SuitUpEntry
                {
                    suitUpId = suitUpId,
                    suitId = suitId,
                    suitName = suitName,
                    level = level,
                    extraChips = extraChips,
                    price = price
                });
            }

            if (errors.Count > 0)
            {
                return new SuitUpMappingResult(false, null, errors, warnings);
            }

            // 按花色强化_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.suitUpId, right.suitUpId));

            return new SuitUpMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
