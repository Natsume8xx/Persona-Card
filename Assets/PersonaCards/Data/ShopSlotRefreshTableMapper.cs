using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品槽位刷新规则配表契约（P0-1J）：与策划表格「商店_商品槽位刷新规则」sheet 的表头与枚举值约定。
    /// 修改表格结构或枚举值必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 商品类型旧写法「人格」（商品表为「人格牌」）导入时归一为「人格牌」并发一条全局警告。
    /// </summary>
    public static class ShopSlotRefreshTableContract
    {
        /// <summary>工作表名（商品槽位刷新规则数据）。</summary>
        public const string SheetName = "商店_商品槽位刷新规则";

        /// <summary>列名：刷新_ID（REFRESH_xxx；行标识，不透明字符串——当前配表 REFRESH_004 跳号，不假设连续）。</summary>
        public const string ColRefreshId = "刷新_ID";

        /// <summary>列名：商店刷新节点（AI1/AI2/AI3，原文存储）。</summary>
        public const string ColNode = "商店刷新节点";

        /// <summary>列名：商品类型（卡牌/人格牌/服务；旧写法「人格」归一为「人格牌」）。</summary>
        public const string ColProductType = "商品类型";

        /// <summary>列名：出现数量（必填，非负整数）。</summary>
        public const string ColCount = "出现数量";

        /// <summary>列名：出现权重（必填，≥1；当前配表 20~45）。</summary>
        public const string ColWeight = "出现权重";

        /// <summary>商品类型旧写法：旧表用「人格」表示人格牌，导入时归一为「人格牌」并提示策划改名。</summary>
        public const string ProductTypeLegacyPersona = "人格";
    }

    /// <summary>商店商品槽位刷新规则映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class ShopSlotRefreshMappingResult
    {
        public ShopSlotRefreshMappingResult(bool succeeded, List<ShopSlotRefreshEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的槽位刷新规则条目（按刷新_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<ShopSlotRefreshEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（「人格」归一全局提示等）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 商店商品槽位刷新规则配表映射器：把 XlsxTableReader 输出的行字典列表转成 ShopSlotRefreshEntry 列表。
    /// 规则：刷新_ID 必填唯一（不透明字符串，跳号合法）；商店刷新节点必填原文；商品类型只认「卡牌/人格牌/服务」，
    /// 旧写法「人格」归一为「人格牌」（结束后一条全局警告）；出现数量必填非负整数；出现权重必填 ≥1。
    /// </summary>
    public static class ShopSlotRefreshTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static ShopSlotRefreshMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商店_商品槽位刷新规则没有任何数据行。");
                return new ShopSlotRefreshMappingResult(false, null, errors, warnings);
            }

            var entries = new List<ShopSlotRefreshEntry>();
            var seenRefreshIds = new HashSet<string>();
            var legacyPersonaRows = 0; // 「人格」旧写法归一计数（结束后合并为一条全局警告）

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var refreshId = Get(row, ShopSlotRefreshTableContract.ColRefreshId);
                var label = $"第 {rowIndex + 2} 行「{refreshId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 刷新_ID：必填、唯一（不透明字符串；当前配表 REFRESH_004 跳号，合法且不告警）
                if (string.IsNullOrEmpty(refreshId))
                {
                    errors.Add($"{label}：「刷新_ID」为空（必填）。");
                    continue;
                }
                if (!seenRefreshIds.Add(refreshId))
                {
                    errors.Add($"{label}：「刷新_ID」重复，必须唯一。");
                    continue;
                }

                // 商店刷新节点：必填原文（AI1/AI2/AI3）
                var node = Get(row, ShopSlotRefreshTableContract.ColNode);
                if (string.IsNullOrEmpty(node))
                {
                    errors.Add($"{label}：「商店刷新节点」为空（必填）。");
                    continue;
                }

                // 商品类型：只认三值；旧写法「人格」归一为「人格牌」
                var productType = Get(row, ShopSlotRefreshTableContract.ColProductType);
                if (productType == ShopSlotRefreshTableContract.ProductTypeLegacyPersona)
                {
                    productType = ShopProductTableContract.ProductTypePersona;
                    legacyPersonaRows++;
                }
                if (Array.IndexOf(ShopProductTableContract.ProductTypes, productType) < 0)
                {
                    errors.Add($"{label}：「商品类型」值「{Get(row, ShopSlotRefreshTableContract.ColProductType)}」无效，应为 {string.Join("/", ShopProductTableContract.ProductTypes)}。");
                    continue;
                }

                // 出现数量：必填非负整数
                var countText = Get(row, ShopSlotRefreshTableContract.ColCount);
                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 0)
                {
                    errors.Add($"{label}：「出现数量」值「{countText}」不是非负整数（必填）。");
                    continue;
                }

                // 出现权重：必填 ≥1
                var weightText = Get(row, ShopSlotRefreshTableContract.ColWeight);
                if (!int.TryParse(weightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) || weight < 1)
                {
                    errors.Add($"{label}：「出现权重」值「{weightText}」不是正整数（必须 ≥1）。");
                    continue;
                }

                entries.Add(new ShopSlotRefreshEntry
                {
                    refreshId = refreshId,
                    node = node,
                    productType = productType,
                    count = count,
                    weight = weight
                });
            }

            if (errors.Count > 0)
            {
                return new ShopSlotRefreshMappingResult(false, null, errors, warnings);
            }

            // 「人格」归一：合并为一条全局警告（本次导入共归一 N 行），建议策划改表为「人格牌」
            if (legacyPersonaRows > 0)
                warnings.Add($"商品类型「人格」是旧写法，共 {legacyPersonaRows} 行已按「人格牌」归一：建议策划改为「人格牌」。");

            // 按刷新_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.refreshId, right.refreshId));

            return new ShopSlotRefreshMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
