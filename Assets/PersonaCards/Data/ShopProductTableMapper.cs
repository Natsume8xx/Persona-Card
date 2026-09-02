using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 商店商品配表契约（P0-1J）：与策划表格「商品_商品配置表」sheet 的表头与枚举值约定。
    /// 修改表格结构或枚举值必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 「商店_商品刷新规则」的商品_ID 列引用本表 ID（导入命令不 join，断链检查留给后续阶段）。
    /// </summary>
    public static class ShopProductTableContract
    {
        /// <summary>工作表名（商品数据）。</summary>
        public const string SheetName = "商品_商品配置表";

        /// <summary>列名：商品_ID（SHOP_CARD_xxx / SHOP_PER_xxx / SHOP_SERVICE_xxx；行标识）。</summary>
        public const string ColProductId = "商品_ID";

        /// <summary>列名：商品名称（仅存值，Inspector/日志可读）。</summary>
        public const string ColProductName = "商品名称";

        /// <summary>列名：商品类型（卡牌/人格牌/服务）。</summary>
        public const string ColProductType = "商品类型";

        /// <summary>列名：价格（必填，非负整数）。</summary>
        public const string ColPrice = "价格";

        /// <summary>列名：购买次数限制（空 = 0；非负整数）。</summary>
        public const string ColPurchaseLimit = "购买次数限制";

        /// <summary>列名：效果类型（已知集合外警告照存——效果类型由策划扩展）。</summary>
        public const string ColEffectType = "效果类型";

        /// <summary>列名：效果参数1（原文存储，允许空；混写 1/基础筹码/人格_ID 等，语义解析留给后续阶段）。</summary>
        public const string ColEffectParam1 = "效果参数1";

        /// <summary>列名：效果参数2（原文存储，允许空；混写 5/2/0.5/0.03/Lv+1 等，语义解析留给后续阶段）。</summary>
        public const string ColEffectParam2 = "效果参数2";

        /// <summary>商品类型枚举值：卡牌。</summary>
        public const string ProductTypeCard = "卡牌";

        /// <summary>商品类型枚举值：人格牌。</summary>
        public const string ProductTypePersona = "人格牌";

        /// <summary>商品类型枚举值：服务。</summary>
        public const string ProductTypeService = "服务";

        /// <summary>商品类型合法值集合。</summary>
        public static readonly string[] ProductTypes = { ProductTypeCard, ProductTypePersona, ProductTypeService };

        /// <summary>效果类型已知集合（当前配表 7 种）。</summary>
        public static readonly string[] KnownEffectTypes =
        {
            "增加卡牌", "增加人格牌", "强化卡牌", "移除卡牌", "强化人格", "强化花色", "强化牌型"
        };
    }

    /// <summary>商店商品配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class ShopProductMappingResult
    {
        public ShopProductMappingResult(bool succeeded, List<ShopProductEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的商品条目（按商品_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<ShopProductEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（效果类型未知等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 商店商品配表映射器：把 XlsxTableReader 输出的行字典列表转成 ShopProductEntry 列表。
    /// 规则：商品_ID 必填唯一；商品类型只认「卡牌/人格牌/服务」；价格必填非负整数；
    /// 购买次数限制空 = 0；效果参数 1/2 一律原文 string 存储（混写不解析）；效果类型已知集合外警告照存。
    /// </summary>
    public static class ShopProductTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static ShopProductMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商品_商品配置表没有任何数据行。");
                return new ShopProductMappingResult(false, null, errors, warnings);
            }

            var entries = new List<ShopProductEntry>();
            var seenProductIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var productId = Get(row, ShopProductTableContract.ColProductId);
                var label = $"第 {rowIndex + 2} 行「{productId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 商品_ID：必填、唯一
                if (string.IsNullOrEmpty(productId))
                {
                    errors.Add($"{label}：「商品_ID」为空（必填）。");
                    continue;
                }
                if (!seenProductIds.Add(productId))
                {
                    errors.Add($"{label}：「商品_ID」重复，必须唯一。");
                    continue;
                }

                // 商品名称：必填（显示名）
                var productName = Get(row, ShopProductTableContract.ColProductName);
                if (string.IsNullOrWhiteSpace(productName))
                {
                    errors.Add($"{label}：「商品名称」为空（必填）。");
                    continue;
                }

                // 商品类型：只认三值
                var productType = Get(row, ShopProductTableContract.ColProductType);
                if (Array.IndexOf(ShopProductTableContract.ProductTypes, productType) < 0)
                {
                    errors.Add($"{label}：「商品类型」值「{productType}」无效，应为 {string.Join("/", ShopProductTableContract.ProductTypes)}。");
                    continue;
                }

                // 价格：必填非负整数
                var priceText = Get(row, ShopProductTableContract.ColPrice);
                if (!int.TryParse(priceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var price) || price < 0)
                {
                    errors.Add($"{label}：「价格」值「{priceText}」不是非负整数（必填）。");
                    continue;
                }

                // 购买次数限制：空 = 0；非负整数
                var purchaseLimit = ParseNonNegativeInt(Get(row, ShopProductTableContract.ColPurchaseLimit));
                if (purchaseLimit < 0)
                {
                    errors.Add($"{label}：「购买次数限制」值「{Get(row, ShopProductTableContract.ColPurchaseLimit)}」不是非负整数。");
                    continue;
                }

                // 效果类型：必填；已知集合外警告照存（策划可扩展）
                var effectType = Get(row, ShopProductTableContract.ColEffectType);
                if (string.IsNullOrEmpty(effectType))
                {
                    errors.Add($"{label}：「效果类型」为空（必填）。");
                    continue;
                }
                if (Array.IndexOf(ShopProductTableContract.KnownEffectTypes, effectType) < 0)
                    warnings.Add($"{label}：效果类型「{effectType}」不在已知集合内，已按原文存储。");

                // 效果参数 1/2：原文存储（允许空；混写 1/基础筹码/Lv+1/0.03 等，语义解析留给后续阶段）
                var effectParam1 = Get(row, ShopProductTableContract.ColEffectParam1);
                var effectParam2 = Get(row, ShopProductTableContract.ColEffectParam2);

                entries.Add(new ShopProductEntry
                {
                    productId = productId,
                    productName = productName,
                    productType = productType,
                    price = price,
                    purchaseLimit = purchaseLimit,
                    effectType = effectType,
                    effectParam1 = effectParam1,
                    effectParam2 = effectParam2
                });
            }

            if (errors.Count > 0)
            {
                return new ShopProductMappingResult(false, null, errors, warnings);
            }

            // 按商品_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.productId, right.productId));

            return new ShopProductMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";

        /// <summary>解析非负整数：空串 → 0（= 使用默认值）；非整数或负数 → -1（调用方判错）。</summary>
        private static int ParseNonNegativeInt(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : -1;
        }
    }
}
