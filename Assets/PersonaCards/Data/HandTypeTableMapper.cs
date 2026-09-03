using System;
using System.Collections.Generic;
using System.Globalization;
using PersonaCards.Core;

namespace PersonaCards.Data
{
    /// <summary>
    /// 牌型配置配表契约：与策划表格「牌型配置」sheet 的表头与枚举值约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1C.md 记录）。
    /// </summary>
    public static class HandTypeTableContract
    {
        /// <summary>工作表名（牌型数据）。</summary>
        public const string SheetName = "牌型配置";

        /// <summary>工作表名（牌型品质定义表，用于品质对照校验）。</summary>
        public const string QualitySheetName = "牌型品质定义表";

        /// <summary>列名：牌型_ID（权威键，HAND_01~12 固定映射 HandType 枚举；P0-1J 表头由「手牌_ID」改名）。</summary>
        public const string ColHandId = "牌型_ID";

        /// <summary>列名：牌型名称（显示名）。</summary>
        public const string ColName = "牌型名称";

        /// <summary>列名：计分牌数（A4 拍板：不导入，仅对照「显示顺序」列不一致时警告）。</summary>
        public const string ColScoringCount = "计分牌数";

        /// <summary>列名：基础筹码（非负整数）。</summary>
        public const string ColChips = "基础筹码";

        /// <summary>列名：基础倍率（≥1，允许 2.5 等小数；解析后直接定型 decimal）。</summary>
        public const string ColMultiplier = "基础倍率";

        /// <summary>列名：显示顺序（非负整数，1 起；0 = 回落枚举序）。</summary>
        public const string ColDisplayOrder = "显示顺序";

        /// <summary>列名：牌型品质_ID（引用牌型品质定义表，NORMAL/RARE）。</summary>
        public const string ColQuality = "牌型品质_ID";
    }

    /// <summary>牌型配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class HandTypeMappingResult
    {
        public HandTypeMappingResult(bool succeeded, List<HandTypeEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（Core 纯数据形态，按显示顺序排列；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<HandTypeEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（计分牌数/显示顺序不一致、显示顺序重复等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 牌型配置配表映射器：把 XlsxTableReader 输出的行字典列表转成 HandTypeEntry（Core 纯数据）列表。
    /// 规则：权威键 = 「牌型_ID」列（HAND_01~12 固定映射枚举）；行级全量校验，任一行出错整体失败；
    /// 五条/同花五条不在表 → 容错（目录 Configure 白盒补齐，已拍板）；「计分牌数」列不导入（已拍板）。
    /// P0-1J：加「牌型品质_ID」列（对照牌型品质定义表校验）。
    /// </summary>
    public static class HandTypeTableMapper
    {
        /// <summary>
        /// 映射行字典列表（XlsxTableReader.ReadTable 的输出）。
        /// qualityIds = 牌型品质定义表的品质 ID 集合（null 表示跳过品质对照，测试用）；
        /// 用 ICollection&lt;string&gt; 而非 IReadOnlyCollection 是为了走实例 Contains（只读接口会被 span 扩展方法截胡）。
        /// </summary>
        public static HandTypeMappingResult Map(List<Dictionary<string, string>> rows, ICollection<string> qualityIds)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("牌型配置表没有任何数据行。");
                return new HandTypeMappingResult(false, null, errors, warnings);
            }

            var entries = new List<HandTypeEntry>();
            var seenHandIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var handId = Get(row, HandTypeTableContract.ColHandId);
                var name = Get(row, HandTypeTableContract.ColName);
                var label = $"第 {rowIndex + 2} 行「{handId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 权威键：手牌_ID 必填、唯一、且必须在固定映射内
                if (string.IsNullOrEmpty(handId))
                {
                    errors.Add($"{label}：「手牌_ID」为空（权威键必填）。");
                    continue;
                }
                if (!seenHandIds.Add(handId))
                {
                    errors.Add($"{label}：「手牌_ID」重复，必须唯一。");
                    continue;
                }
                if (!TryMapHandId(handId, out var handType))
                {
                    errors.Add($"{label}：「手牌_ID」值无效，应为 HAND_01 ~ HAND_12。");
                    continue;
                }

                // 牌型名称必填
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{label}：「牌型名称」为空。");
                    continue;
                }

                // 基础筹码：非负整数
                var chipsText = Get(row, HandTypeTableContract.ColChips);
                if (!int.TryParse(chipsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var chips) || chips < 0)
                {
                    errors.Add($"{label}：「基础筹码」值「{chipsText}」不是非负整数。");
                    continue;
                }

                // 基础倍率：≥1 的有限数值，且不超出 decimal 上界（解析后直接定型 decimal，全程无 double）
                var multiplierText = Get(row, HandTypeTableContract.ColMultiplier);
                if (!double.TryParse(multiplierText, NumberStyles.Float, CultureInfo.InvariantCulture, out var multiplier)
                    || double.IsNaN(multiplier) || double.IsInfinity(multiplier)
                    || multiplier < 1 || multiplier > (double)decimal.MaxValue)
                {
                    errors.Add($"{label}：「基础倍率」值「{multiplierText}」无效，必须为 ≥1 的有限数值。");
                    continue;
                }

                // 显示顺序：空 = 0（回落枚举序）；非负整数
                var orderText = Get(row, HandTypeTableContract.ColDisplayOrder);
                var displayOrder = 0;
                if (!string.IsNullOrEmpty(orderText))
                {
                    if (!int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out displayOrder) || displayOrder < 0)
                    {
                        errors.Add($"{label}：「显示顺序」值「{orderText}」不是非负整数。");
                        continue;
                    }
                }

                // 计分牌数（A4 拍板：不导入）：与显示顺序列不一致时警告（策划案两列语义相同，以显示顺序为准）
                var scoringCountText = Get(row, HandTypeTableContract.ColScoringCount);
                if (!string.IsNullOrEmpty(scoringCountText)
                    && int.TryParse(scoringCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var scoringCount)
                    && scoringCount != displayOrder)
                {
                    warnings.Add($"{label}：「计分牌数」{scoringCount} 与「显示顺序」{displayOrder} 不一致（本列不导入，以显示顺序为准，请策划确认）。");
                }

                // 牌型品质（P0-1J）：必填、可解析为枚举、且在品质定义表内（qualityIds 为 null 跳过对照，测试用）
                var qualityText = Get(row, HandTypeTableContract.ColQuality);
                if (string.IsNullOrWhiteSpace(qualityText)
                    || !Enum.TryParse(qualityText, true, out HandQuality quality))
                {
                    errors.Add($"{label}：「牌型品质_ID」值「{qualityText}」无效，应为 NORMAL 或 RARE。");
                    continue;
                }
                if (qualityIds != null && !qualityIds.Contains(qualityText))
                {
                    errors.Add($"{label}：「牌型品质_ID」值「{qualityText}」不在牌型品质定义表中（品质表可能未同步，请策划确认）。");
                    continue;
                }

                entries.Add(new HandTypeEntry(handType, name, chips, (decimal)multiplier, displayOrder, quality));
            }

            if (errors.Count > 0)
            {
                return new HandTypeMappingResult(false, null, errors, warnings);
            }

            // 核心牌型齐全检查（防策划误删行）：HAND_01~HAND_11 必须全在（P0-1J 起皇家同花顺也是真实表行）；五条/同花五条缺失容错（目录白盒补齐，已拍板）
            var presentTypes = new HashSet<HandType>();
            foreach (var entry in entries)
                presentTypes.Add(entry.HandType);
            foreach (var required in RequiredCoreTypes)
            {
                if (!presentTypes.Contains(required))
                    errors.Add($"牌型配置表缺少核心牌型 {required} 的行（对应 HAND_01~HAND_11 之一）：请确认该行未被误删。");
            }
            if (errors.Count > 0)
            {
                return new HandTypeMappingResult(false, null, errors, warnings);
            }

            // 显示顺序重复 → 警告（目录 All 排序以枚举序兜底，不影响运行）
            var seenOrders = new Dictionary<int, string>();
            foreach (var entry in entries)
            {
                if (entry.DisplayOrder == 0) continue; // 0 = 回落枚举序，天然兜底
                if (seenOrders.TryGetValue(entry.DisplayOrder, out var previous))
                {
                    warnings.Add($"「显示顺序」{entry.DisplayOrder} 重复（{previous} 与 {entry.DisplayName}），排序时以枚举序兜底。");
                }
                else
                {
                    seenOrders[entry.DisplayOrder] = entry.DisplayName;
                }
            }

            // 按显示顺序排列条目（资产 Inspector 与日志的可读性；目录 Configure 不依赖顺序）
            entries.Sort((left, right) =>
            {
                var orderCompare = left.DisplayOrder.CompareTo(right.DisplayOrder);
                return orderCompare != 0 ? orderCompare : left.HandType.CompareTo(right.HandType);
            });

            return new HandTypeMappingResult(true, entries, errors, warnings);
        }

        /// <summary>核心牌型（表必须包含的行）：HAND_01~HAND_11 对应的枚举；五条/同花五条不在表是容错（目录白盒补齐）。</summary>
        private static readonly HandType[] RequiredCoreTypes =
        {
            HandType.HighCard, HandType.Pair, HandType.TwoPair, HandType.ThreeOfAKind, HandType.Straight,
            HandType.Flush, HandType.FullHouse, HandType.FourOfAKind, HandType.StraightFlush, HandType.FlushHouse,
            HandType.RoyalFlush
        };

        /// <summary>牌型_ID → HandType 固定映射：表行顺序改动、改 ID 都会在编译期锁定；
        /// P0-1J：HAND_11 改为皇家同花顺（配表定稿）；HAND_12 容错映射同花五条（表补行即生效）。
        /// P0-11 起公开：牌型强化表（HandUp）的 handId 解析复用同一映射。</summary>
        public static bool TryMapHandId(string handId, out HandType handType)
        {
            switch (handId)
            {
                case "HAND_01": handType = HandType.HighCard; return true;
                case "HAND_02": handType = HandType.Pair; return true;
                case "HAND_03": handType = HandType.TwoPair; return true;
                case "HAND_04": handType = HandType.ThreeOfAKind; return true;
                case "HAND_05": handType = HandType.Straight; return true;
                case "HAND_06": handType = HandType.Flush; return true;
                case "HAND_07": handType = HandType.FullHouse; return true;
                case "HAND_08": handType = HandType.FourOfAKind; return true;
                case "HAND_09": handType = HandType.StraightFlush; return true;
                case "HAND_10": handType = HandType.FlushHouse; return true;
                case "HAND_11": handType = HandType.RoyalFlush; return true;
                case "HAND_12": handType = HandType.FlushFive; return true;
                default:
                    handType = default;
                    return false;
            }
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
