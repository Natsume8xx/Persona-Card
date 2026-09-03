using System.Collections.Generic;
using System.Linq;
using PersonaCards.Cards;

namespace PersonaCards.Data
{
    /// <summary>
    /// 花色配置配表契约：与策划表格「花色配置」sheet 的表头与枚举值约定（P0-11）。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-11.md 记录）。
    /// </summary>
    public static class SuitConfigTableContract
    {
        /// <summary>工作表名。</summary>
        public const string SheetName = "花色配置";

        /// <summary>列名：花色_ID（权威键，SUIT_001~004 固定映射 Suit 枚举，复用 CardConfigEntry.TryMapSuit）。</summary>
        public const string ColSuitId = "花色_ID";

        /// <summary>列名：花色名称（显示名）。</summary>
        public const string ColName = "花色名称";
    }

    /// <summary>花色配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）。</summary>
    public sealed class SuitConfigMappingResult
    {
        public SuitConfigMappingResult(bool succeeded, List<SuitConfigEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（Cards 纯数据形态，按花色_ID 顺序排列；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<SuitConfigEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前契约下无警告场景，保留容器对齐其余 mapper 形态）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 花色配置配表映射器：把 XlsxTableReader 输出的行字典列表转成 SuitConfigEntry 列表（P0-11）。
    /// 规则：权威键 = 「花色_ID」列（SUIT_001~004 固定映射，复用 CardConfigEntry.TryMapSuit）；
    /// 行级全量校验，任一行出错整体失败；4 花色必须齐全（防策划误删行）。
    /// </summary>
    public static class SuitConfigTableMapper
    {
        public static SuitConfigMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("花色配置表没有任何数据行。");
                return new SuitConfigMappingResult(false, null, errors, warnings);
            }

            var entries = new List<SuitConfigEntry>();
            var seenSuitIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var suitId = Get(row, SuitConfigTableContract.ColSuitId);
                var name = Get(row, SuitConfigTableContract.ColName);
                var label = $"第 {rowIndex + 2} 行「{suitId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 权威键：花色_ID 必填、唯一、且必须在固定映射内
                if (string.IsNullOrEmpty(suitId))
                {
                    errors.Add($"{label}：「花色_ID」为空（权威键必填）。");
                    continue;
                }
                if (!seenSuitIds.Add(suitId))
                {
                    errors.Add($"{label}：「花色_ID」重复，必须唯一。");
                    continue;
                }
                if (!CardConfigEntry.TryMapSuit(suitId, out var suit))
                {
                    errors.Add($"{label}：「花色_ID」值无效，应为 SUIT_001 ~ SUIT_004。");
                    continue;
                }

                // 花色名称必填
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{label}：「花色名称」为空。");
                    continue;
                }

                entries.Add(new SuitConfigEntry(suit, name));
            }

            if (errors.Count > 0)
            {
                return new SuitConfigMappingResult(false, null, errors, warnings);
            }

            // 4 花色齐全检查（防策划误删行）
            var presentSuits = new HashSet<Suit>(entries.Select(entry => entry.Suit));
            foreach (var required in RequiredSuits)
            {
                if (!presentSuits.Contains(required))
                    errors.Add($"花色配置表缺少花色 {required} 的行（对应 SUIT_001~SUIT_004 之一）：请确认该行未被误删。");
            }
            if (errors.Count > 0)
            {
                return new SuitConfigMappingResult(false, null, errors, warnings);
            }

            // 按花色_ID 升序排列条目（SUIT_001→SUIT_004 = 黑桃/红桃/梅花/方块；资产 Inspector 可读性，行序乱序不受影响）
            entries.Sort((left, right) => SuitOrder(left.Suit).CompareTo(SuitOrder(right.Suit)));

            return new SuitConfigMappingResult(true, entries, errors, warnings);
        }

        /// <summary>表必须包含的行：4 个花色全在（误删任一行报错）。</summary>
        private static readonly Suit[] RequiredSuits =
        {
            Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds
        };

        /// <summary>花色_ID 序号（SUIT_001~004 = 黑桃/红桃/梅花/方块）；Suit 枚举序（Clubs=0…）与 ID 序不一致，故单独映射。</summary>
        private static int SuitOrder(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spades: return 1;
                case Suit.Hearts: return 2;
                case Suit.Clubs: return 3;
                case Suit.Diamonds: return 4;
                default: return 99;
            }
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
