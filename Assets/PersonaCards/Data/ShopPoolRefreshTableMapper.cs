using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品刷新规则配表契约（P0-1J）：与策划表格「商店_商品刷新规则」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 商品池_ID 当前混用前缀 POLL_CARD_*（其余 POOL_*）——ID 按不透明字符串处理，两种前缀并存时发一条全局警告。
    /// </summary>
    public static class ShopPoolRefreshTableContract
    {
        /// <summary>工作表名（商品刷新规则数据）。</summary>
        public const string SheetName = "商店_商品刷新规则";

        /// <summary>列名：商品池_ID（POLL_CARD_xxx / POOL_PERSONA_xxx / POOL_SERVICE_xxx；行标识，不透明字符串）。</summary>
        public const string ColPoolId = "商品池_ID";

        /// <summary>列名：商品_ID（引用商品配置表；导入命令不 join，断链检查留给后续阶段）。</summary>
        public const string ColProductId = "商品_ID";

        /// <summary>列名：权重（必填，≥1；当前配表三档 1/10/20）。</summary>
        public const string ColWeight = "权重";
    }

    /// <summary>商店商品刷新规则映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class ShopPoolRefreshMappingResult
    {
        public ShopPoolRefreshMappingResult(bool succeeded, List<ShopPoolRefreshEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的刷新规则条目（按商品池_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<ShopPoolRefreshEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（前缀混用全局提示等）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 商店商品刷新规则配表映射器：把 XlsxTableReader 输出的行字典列表转成 ShopPoolRefreshEntry 列表。
    /// 规则：商品池_ID 必填唯一（不透明字符串，不假设连续/前缀）；商品_ID 必填（不 join 存在性）；权重必填 ≥1；
    /// POLL_ 与 POOL_ 两种前缀并存 → 一条全局警告（建议策划统一，不阻塞导入）。
    /// </summary>
    public static class ShopPoolRefreshTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static ShopPoolRefreshMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商店_商品刷新规则没有任何数据行。");
                return new ShopPoolRefreshMappingResult(false, null, errors, warnings);
            }

            var entries = new List<ShopPoolRefreshEntry>();
            var seenPoolIds = new HashSet<string>();
            var hasPollPrefix = false;
            var hasPoolPrefix = false;

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var poolId = Get(row, ShopPoolRefreshTableContract.ColPoolId);
                var label = $"第 {rowIndex + 2} 行「{poolId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 商品池_ID：必填、唯一（不透明字符串；前缀拼写不参与校验，混用时最后发一条全局警告）
                if (string.IsNullOrEmpty(poolId))
                {
                    errors.Add($"{label}：「商品池_ID」为空（必填）。");
                    continue;
                }
                if (!seenPoolIds.Add(poolId))
                {
                    errors.Add($"{label}：「商品池_ID」重复，必须唯一。");
                    continue;
                }
                if (poolId.StartsWith("POLL_")) hasPollPrefix = true;
                if (poolId.StartsWith("POOL_")) hasPoolPrefix = true;

                // 商品_ID：必填（引用商品配置表，导入命令不 join）
                var productId = Get(row, ShopPoolRefreshTableContract.ColProductId);
                if (string.IsNullOrEmpty(productId))
                {
                    errors.Add($"{label}：「商品_ID」为空（必填）。");
                    continue;
                }

                // 权重：必填 ≥1（当前配表三档 1/10/20）
                var weightText = Get(row, ShopPoolRefreshTableContract.ColWeight);
                if (!int.TryParse(weightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) || weight < 1)
                {
                    errors.Add($"{label}：「权重」值「{weightText}」不是正整数（必须 ≥1）。");
                    continue;
                }

                entries.Add(new ShopPoolRefreshEntry
                {
                    poolId = poolId,
                    productId = productId,
                    weight = weight
                });
            }

            if (errors.Count > 0)
            {
                return new ShopPoolRefreshMappingResult(false, null, errors, warnings);
            }

            // 前缀混用：POLL_CARD_* 与 POOL_* 并存 → 一条全局警告（ID 已按原文存储，建议策划统一为 POOL_）
            if (hasPollPrefix && hasPoolPrefix)
                warnings.Add("商品池_ID 前缀混用（POLL_ 与 POOL_ 并存），已按原文存储：建议策划统一为 POOL_。");

            // 按商品池_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.poolId, right.poolId));

            return new ShopPoolRefreshMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
