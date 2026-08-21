using System;
using System.Collections.Generic;
using System.Globalization;
using PersonaCards.Cards;

namespace PersonaCards.Data
{
    /// <summary>
    /// 卡牌配置配表契约：与策划表格「卡牌配置」sheet 的表头与枚举值约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1D.md 记录）。
    /// </summary>
    public static class CardConfigTableContract
    {
        /// <summary>工作表名（卡牌数据）。</summary>
        public const string SheetName = "卡牌配置";

        /// <summary>列名：卡牌_ID（CARD_xxx，美术绑定 ID；存值并对照图片配置「绑定ID」警告，不是行为键）。</summary>
        public const string ColCardId = "卡牌_ID";

        /// <summary>列名：卡牌名称（显示名）。</summary>
        public const string ColName = "卡牌名称";

        /// <summary>列名：卡牌类型（当前仅「手牌」）。</summary>
        public const string ColCardKind = "卡牌类型";

        /// <summary>列名：花色（黑桃/红桃/梅花/方块，固定映射 Suit 枚举）。</summary>
        public const string ColSuit = "花色";

        /// <summary>列名：点数（A/2~10/J/Q/K，固定映射 Rank 枚举）。</summary>
        public const string ColRank = "点数";

        /// <summary>列名：参数类型（当前仅「筹码」）。</summary>
        public const string ColParamType = "参数类型";

        /// <summary>列名：参数1（筹码类型时 = 牌面筹码值，非负整数）。</summary>
        public const string ColParamValue = "参数1";

        /// <summary>枚举值：卡牌类型「手牌」。</summary>
        public const string KindHand = "手牌";

        /// <summary>枚举值：参数类型「筹码」。</summary>
        public const string ParamChips = "筹码";
    }

    /// <summary>卡牌配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class CardMappingResult
    {
        public CardMappingResult(bool succeeded, List<CardConfigEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（Cards 纯数据形态，按卡牌_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<CardConfigEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（卡牌_ID 不在图片配置绑定 ID 集合等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 卡牌配置配表映射器：把 XlsxTableReader 输出的行字典列表转成 CardConfigEntry（Cards 纯数据）列表。
    /// 规则：行为键 = 花色+点数组合（4×13=52 必须齐全、无重复，防策划误删）；卡牌_ID 存值（美术绑定 ID）并对照
    /// 图片配置绑定 ID 警告容错；卡牌类型/参数类型当前只认「手牌」/「筹码」，其他值 = 错误（防静默丢数据）。
    /// </summary>
    public static class CardConfigTableMapper
    {
        /// <summary>
        /// 映射行字典列表（XlsxTableReader.ReadTable 的输出）。
        /// imageBindingIds = 图片配置 sheet 的绑定 ID 集合（null 表示跳过对照，测试用）；
        /// 用 ICollection&lt;string&gt; 而非 IReadOnlyCollection 是为了走实例 Contains（只读接口会被 span 扩展方法截胡）。
        /// </summary>
        public static CardMappingResult Map(List<Dictionary<string, string>> rows, ICollection<string> imageBindingIds)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("卡牌配置表没有任何数据行。");
                return new CardMappingResult(false, null, errors, warnings);
            }

            var entries = new List<CardConfigEntry>();
            var seenCardIds = new HashSet<string>();
            var seenSuitRanks = new HashSet<(Suit, Rank)>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var cardId = Get(row, CardConfigTableContract.ColCardId);
                var label = $"第 {rowIndex + 2} 行「{cardId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 卡牌_ID：非空、唯一（行标识与美术绑定 ID；不在图片配置绑定 ID 集合 → 警告容错）
                if (string.IsNullOrEmpty(cardId))
                {
                    errors.Add($"{label}：「卡牌_ID」为空（必填）。");
                    continue;
                }
                if (!seenCardIds.Add(cardId))
                {
                    errors.Add($"{label}：「卡牌_ID」重复，必须唯一。");
                    continue;
                }

                // 花色：固定映射黑桃/红桃/梅花/方块
                var suitText = Get(row, CardConfigTableContract.ColSuit);
                if (!CardConfigEntry.TryMapSuit(suitText, out var suit))
                {
                    errors.Add($"{label}：「花色」值「{suitText}」无效，应为黑桃/红桃/梅花/方块。");
                    continue;
                }

                // 点数：固定映射 A/2~10/J/Q/K
                var rankText = Get(row, CardConfigTableContract.ColRank);
                if (!CardConfigEntry.TryMapRank(rankText, out var rank))
                {
                    errors.Add($"{label}：「点数」值「{rankText}」无效，应为 A/2~10/J/Q/K。");
                    continue;
                }

                // 行为键 = (花色,点数)：重复 = 错误
                if (!seenSuitRanks.Add((suit, rank)))
                {
                    errors.Add($"{label}：「花色」{suitText} + 「点数」{rankText} 与已有行重复（行为键必须唯一）。");
                    continue;
                }

                // 卡牌名称必填
                var name = Get(row, CardConfigTableContract.ColName);
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{label}：「卡牌名称」为空。");
                    continue;
                }

                // 卡牌类型：当前仅支持「手牌」，其他值报错防静默丢数据（扩类型时同步 CardKind 枚举与契约）
                var kindText = Get(row, CardConfigTableContract.ColCardKind);
                if (kindText != CardConfigTableContract.KindHand)
                {
                    errors.Add($"{label}：「卡牌类型」值「{kindText}」无效，当前仅支持「手牌」。");
                    continue;
                }

                // 参数类型：当前仅支持「筹码」，其他值报错（扩类型时同步 CardParamType 枚举与契约）
                var paramTypeText = Get(row, CardConfigTableContract.ColParamType);
                if (paramTypeText != CardConfigTableContract.ParamChips)
                {
                    errors.Add($"{label}：「参数类型」值「{paramTypeText}」无效，当前仅支持「筹码」。");
                    continue;
                }

                // 参数1：非负整数（筹码类型时 = 牌面筹码值）
                var paramValueText = Get(row, CardConfigTableContract.ColParamValue);
                if (!int.TryParse(paramValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var paramValue)
                    || paramValue < 0)
                {
                    errors.Add($"{label}：「参数1」值「{paramValueText}」不是非负整数。");
                    continue;
                }

                // 卡牌_ID 对照图片配置「绑定ID」列：不在集合 = 警告容错（策划改 ID 只需同步图片配置）
                if (imageBindingIds != null && !imageBindingIds.Contains(cardId))
                {
                    warnings.Add($"{label}：「卡牌_ID」值「{cardId}」不在图片配置「绑定ID」列中（卡图可能未同步，程序已存值容错）。");
                }

                entries.Add(new CardConfigEntry(cardId, name, CardKind.Hand, suit, rank, CardParamType.Chips, paramValue));
            }

            if (errors.Count > 0)
            {
                return new CardMappingResult(false, null, errors, warnings);
            }

            // 52 组合齐全检查（防策划误删行）：4 花色 × 13 点数必须全在；缺任一 = 错误
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var rank = (Rank)rankValue;
                    if (!seenSuitRanks.Contains((suit, rank)))
                    {
                        errors.Add($"卡牌配置表缺少花色 {suit} + 点数 {rank} 的行（标准 52 张应齐全）：请确认该行未被误删。");
                    }
                }
            }
            if (errors.Count > 0)
            {
                return new CardMappingResult(false, null, errors, warnings);
            }

            // 按卡牌_ID 升序排列条目（资产 Inspector 与日志的可读性；门面 Configure 不依赖顺序）
            entries.Sort((left, right) => string.CompareOrdinal(left.CardId, right.CardId));

            return new CardMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
