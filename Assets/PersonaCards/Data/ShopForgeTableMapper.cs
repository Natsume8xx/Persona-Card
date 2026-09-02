using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店人格铸造配表契约（P0-1J）：与策划表格「商店_人格铸造」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// </summary>
    public static class ShopForgeTableContract
    {
        /// <summary>工作表名（人格铸造数据）。</summary>
        public const string SheetName = "商店_人格铸造";

        /// <summary>列名：功能_ID（FORGE_xxx；行标识）。</summary>
        public const string ColForgeId = "功能_ID";

        /// <summary>列名：功能名称（解锁第二词条/解锁第三词条…）。</summary>
        public const string ColForgeName = "功能名称";

        /// <summary>列名：价格（必填，非负整数）。</summary>
        public const string ColPrice = "价格";
    }

    /// <summary>商店人格铸造映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class ShopForgeMappingResult
    {
        public ShopForgeMappingResult(bool succeeded, List<ShopForgeEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的铸造功能条目（按功能_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<ShopForgeEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与同阶段结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 商店人格铸造配表映射器：把 XlsxTableReader 输出的行字典列表转成 ShopForgeEntry 列表。
    /// 规则：功能_ID 必填唯一；功能名称必填；价格必填非负整数。
    /// </summary>
    public static class ShopForgeTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static ShopForgeMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商店_人格铸造没有任何数据行。");
                return new ShopForgeMappingResult(false, null, errors, warnings);
            }

            var entries = new List<ShopForgeEntry>();
            var seenForgeIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var forgeId = Get(row, ShopForgeTableContract.ColForgeId);
                var label = $"第 {rowIndex + 2} 行「{forgeId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 功能_ID：必填、唯一
                if (string.IsNullOrEmpty(forgeId))
                {
                    errors.Add($"{label}：「功能_ID」为空（必填）。");
                    continue;
                }
                if (!seenForgeIds.Add(forgeId))
                {
                    errors.Add($"{label}：「功能_ID」重复，必须唯一。");
                    continue;
                }

                // 功能名称：必填（显示名）
                var forgeName = Get(row, ShopForgeTableContract.ColForgeName);
                if (string.IsNullOrWhiteSpace(forgeName))
                {
                    errors.Add($"{label}：「功能名称」为空（必填）。");
                    continue;
                }

                // 价格：必填非负整数
                var priceText = Get(row, ShopForgeTableContract.ColPrice);
                if (!int.TryParse(priceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) || price < 0)
                {
                    errors.Add($"{label}：「价格」值「{priceText}」不是非负整数（必填）。");
                    continue;
                }

                entries.Add(new ShopForgeEntry
                {
                    forgeId = forgeId,
                    forgeName = forgeName,
                    price = price
                });
            }

            if (errors.Count > 0)
            {
                return new ShopForgeMappingResult(false, null, errors, warnings);
            }

            // 按功能_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.forgeId, right.forgeId));

            return new ShopForgeMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
