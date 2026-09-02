using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 牌型强化配表契约（P0-1J）：与策划表格「商品_牌型强化」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 牌型_ID 引用牌型配置表（HAND_01~11；导入命令不 join，断链检查留给后续阶段）。
    /// </summary>
    public static class HandUpTableContract
    {
        /// <summary>工作表名（牌型强化数据）。</summary>
        public const string SheetName = "商品_牌型强化";

        /// <summary>列名：牌型强化_ID（HAND_UP_xxx；行标识）。</summary>
        public const string ColHandUpId = "牌型强化_ID";

        /// <summary>列名：牌型_ID（HAND_xx；原文存储，不 join）。</summary>
        public const string ColHandId = "牌型_ID";

        /// <summary>列名：牌型名称（显示名）。</summary>
        public const string ColHandName = "牌型名称";

        /// <summary>列名：等级（Lv.1~Lv.4，原文存储，不假设格式）。</summary>
        public const string ColLevel = "等级";

        /// <summary>列名：基础筹码（必填，非负整数）。</summary>
        public const string ColBaseChips = "基础筹码";

        /// <summary>列名：基础倍率（原文存储；混写 1.1/3/3.25/11 等，语义解析留给后续阶段）。</summary>
        public const string ColBaseMult = "基础倍率";

        /// <summary>列名：价格（必填，非负整数）。</summary>
        public const string ColPrice = "价格";
    }

    /// <summary>牌型强化映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）。</summary>
    public sealed class HandUpMappingResult
    {
        public HandUpMappingResult(bool succeeded, List<HandUpEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的牌型强化条目（按牌型强化_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<HandUpEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与同阶段结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 牌型强化配表映射器：把 XlsxTableReader 输出的行字典列表转成 HandUpEntry 列表。
    /// 规则：牌型强化_ID 必填唯一；牌型_ID 必填原文（不 join）；牌型名称必填；等级必填原文；
    /// 基础筹码/价格必填非负整数；基础倍率必填原文存储（混写不解析）。
    /// </summary>
    public static class HandUpTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static HandUpMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商品_牌型强化没有任何数据行。");
                return new HandUpMappingResult(false, null, errors, warnings);
            }

            var entries = new List<HandUpEntry>();
            var seenHandUpIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var handUpId = Get(row, HandUpTableContract.ColHandUpId);
                var label = $"第 {rowIndex + 2} 行「{handUpId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 牌型强化_ID：必填、唯一
                if (string.IsNullOrEmpty(handUpId))
                {
                    errors.Add($"{label}：「牌型强化_ID」为空（必填）。");
                    continue;
                }
                if (!seenHandUpIds.Add(handUpId))
                {
                    errors.Add($"{label}：「牌型强化_ID」重复，必须唯一。");
                    continue;
                }

                // 牌型_ID：必填原文（引用牌型配置表 HAND_01~11，不 join）
                var handId = Get(row, HandUpTableContract.ColHandId);
                if (string.IsNullOrEmpty(handId))
                {
                    errors.Add($"{label}：「牌型_ID」为空（必填）。");
                    continue;
                }

                // 牌型名称：必填（显示名）
                var handName = Get(row, HandUpTableContract.ColHandName);
                if (string.IsNullOrWhiteSpace(handName))
                {
                    errors.Add($"{label}：「牌型名称」为空（必填）。");
                    continue;
                }

                // 等级：必填原文（Lv.1~Lv.4，不假设格式）
                var level = Get(row, HandUpTableContract.ColLevel);
                if (string.IsNullOrEmpty(level))
                {
                    errors.Add($"{label}：「等级」为空（必填）。");
                    continue;
                }

                // 基础筹码：必填非负整数
                var baseChipsText = Get(row, HandUpTableContract.ColBaseChips);
                if (!int.TryParse(baseChipsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseChips) || baseChips < 0)
                {
                    errors.Add($"{label}：「基础筹码」值「{baseChipsText}」不是非负整数（必填）。");
                    continue;
                }

                // 基础倍率：必填原文（混写 1.1/3/3.25/11 等，语义解析留给后续阶段）
                var baseMult = Get(row, HandUpTableContract.ColBaseMult);
                if (string.IsNullOrEmpty(baseMult))
                {
                    errors.Add($"{label}：「基础倍率」为空（必填）。");
                    continue;
                }

                // 价格：必填非负整数
                var priceText = Get(row, HandUpTableContract.ColPrice);
                if (!int.TryParse(priceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) || price < 0)
                {
                    errors.Add($"{label}：「价格」值「{priceText}」不是非负整数（必填）。");
                    continue;
                }

                entries.Add(new HandUpEntry
                {
                    handUpId = handUpId,
                    handId = handId,
                    handName = handName,
                    level = level,
                    baseChips = baseChips,
                    baseMult = baseMult,
                    price = price
                });
            }

            if (errors.Count > 0)
            {
                return new HandUpMappingResult(false, null, errors, warnings);
            }

            // 按牌型强化_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.handUpId, right.handUpId));

            return new HandUpMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
