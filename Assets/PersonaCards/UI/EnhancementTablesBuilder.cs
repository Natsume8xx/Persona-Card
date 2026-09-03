using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>三线强化配表翻译结果：Tables 永远非 null（解析失败的行被跳过并记录警告，运行时尽力加载）。</summary>
    public sealed class EnhancementTablesBuildResult
    {
        public EnhancementTablesBuildResult(EnhancementTables tables, List<string> warnings)
        {
            Tables = tables;
            Warnings = warnings;
        }

        /// <summary>翻译出的数值容器（部分内容缺失时其余线仍生效）。</summary>
        public EnhancementTables Tables { get; }

        /// <summary>全部警告（跳过的行、价格不一致、底值低于 Lv0 等，供运行时日志）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 三线强化配表条目 → Battle 纯 C# 数值容器翻译器（P0-11）。
    /// Battle 是 noEngineReferences 程序集不能直接消费 Data 资产条目，本类在 UI 程序集做翻译。
    /// 解析规则：Lv.N 提取数字；baseMult 用 InvariantCulture 解析后 Round(6) 抹浮点尾巴；
    /// 独立倍率型百分比 ÷100（+10% → +0.1，暂定口径见代策划确认）；牌型增量 = 表内绝对底值 − HandTypeCatalog Lv0 底值。
    /// 单行解析失败 → 跳过该行 + 警告，不 fail-fast（资产导入时已校验，此处只做运行时兜底）。
    /// </summary>
    public static class EnhancementTablesBuilder
    {
        public static EnhancementTablesBuildResult Build(
            IReadOnlyList<PersonaUpRuleEntry> personaRules,
            IReadOnlyList<SuitUpEntry> suitUps,
            IReadOnlyList<HandUpEntry> handUps)
        {
            var tables = new EnhancementTables();
            var warnings = new List<string>();

            BuildPersonaRules(personaRules, tables, warnings);
            BuildSuitUps(suitUps, tables, warnings);
            BuildHandUps(handUps, tables, warnings);

            return new EnhancementTablesBuildResult(tables, warnings);
        }

        private static void BuildPersonaRules(
            IReadOnlyList<PersonaUpRuleEntry> personaRules,
            EnhancementTables tables,
            List<string> warnings)
        {
            if (personaRules == null || personaRules.Count == 0)
            {
                warnings.Add("人格牌强化规则表为空：人格线不生效。");
                return;
            }

            var basePrices = new HashSet<int>();
            var levelPriceSteps = new HashSet<int>();
            var firstBasePrice = 0;
            var firstStep = 0;
            var haveFirst = false;

            for (var index = 0; index < personaRules.Count; index++)
            {
                var entry = personaRules[index];
                if (entry == null)
                {
                    warnings.Add($"人格牌强化规则第 {index} 条为 null，已跳过。");
                    continue;
                }
                if (!TryMapEffectKind(entry.mainAttrType, out var kind))
                {
                    warnings.Add($"人格牌强化规则「{entry.ruleId}」的主属性类型「{entry.mainAttrType}」无法映射到效果类型，已跳过。");
                    continue;
                }
                if (!TryParseIncrease(entry.perLevelIncrease, out var increase))
                {
                    warnings.Add($"人格牌强化规则「{entry.ruleId}」的每级增加「{entry.perLevelIncrease}」无法解析，已跳过。");
                    continue;
                }
                if (tables.PersonaPerLevelIncrease.ContainsKey(kind))
                {
                    warnings.Add($"人格牌强化规则「{entry.ruleId}」的效果类型 {kind} 已被先行规则占用，本行被忽略。");
                    continue;
                }

                tables.PersonaPerLevelIncrease[kind] = increase;
                basePrices.Add(entry.basePrice);
                levelPriceSteps.Add(entry.levelPriceStep);
                if (!haveFirst)
                {
                    haveFirst = true;
                    firstBasePrice = entry.basePrice;
                    firstStep = entry.levelPriceStep;
                }
            }

            tables.PersonaBasePrice = firstBasePrice;
            tables.PersonaLevelPriceStep = firstStep;
            if (basePrices.Count > 1)
            {
                warnings.Add($"人格牌强化规则的基础价格不一致（{string.Join("/", basePrices)}），已取第一条 {tables.PersonaBasePrice}，请策划确认。");
            }
            if (levelPriceSteps.Count > 1)
            {
                warnings.Add($"人格牌强化规则的每级涨价不一致（{string.Join("/", levelPriceSteps)}），已取第一条 {tables.PersonaLevelPriceStep}，请策划确认。");
            }
        }

        private static void BuildSuitUps(
            IReadOnlyList<SuitUpEntry> suitUps,
            EnhancementTables tables,
            List<string> warnings)
        {
            if (suitUps == null || suitUps.Count == 0)
            {
                warnings.Add("花色强化表为空：花色线不生效。");
                return;
            }

            for (var index = 0; index < suitUps.Count; index++)
            {
                var entry = suitUps[index];
                if (entry == null)
                {
                    warnings.Add($"花色强化第 {index} 条为 null，已跳过。");
                    continue;
                }
                if (!CardConfigEntry.TryMapSuit(entry.suitId, out var suit))
                {
                    warnings.Add($"花色强化「{entry.suitUpId}」的花色_ID「{entry.suitId}」无法映射，已跳过。");
                    continue;
                }
                if (!TryParseLevel(entry.level, out var level) || level < 1 || level > EnhancementState.SuitMaxLevel)
                {
                    warnings.Add($"花色强化「{entry.suitUpId}」的等级「{entry.level}」无法解析或超出 1~4，已跳过。");
                    continue;
                }
                if (entry.extraChips < 0)
                {
                    warnings.Add($"花色强化「{entry.suitUpId}」的额外筹码「{entry.extraChips}」为负数，已跳过。");
                    continue;
                }

                GetOrAdd(tables.SuitChips, suit)[level - 1] = entry.extraChips;
                GetOrAdd(tables.SuitPrices, suit)[level - 1] = entry.price;
                if (!string.IsNullOrEmpty(entry.suitName)) tables.SuitNames[suit] = entry.suitName;
            }
        }

        private static void BuildHandUps(
            IReadOnlyList<HandUpEntry> handUps,
            EnhancementTables tables,
            List<string> warnings)
        {
            if (handUps == null || handUps.Count == 0)
            {
                warnings.Add("牌型强化表为空：牌型线不生效。");
                return;
            }

            for (var index = 0; index < handUps.Count; index++)
            {
                var entry = handUps[index];
                if (entry == null)
                {
                    warnings.Add($"牌型强化第 {index} 条为 null，已跳过。");
                    continue;
                }
                if (!HandTypeTableMapper.TryMapHandId(entry.handId, out var handType))
                {
                    warnings.Add($"牌型强化「{entry.handUpId}」的牌型_ID「{entry.handId}」无法映射，已跳过。");
                    continue;
                }
                if (!TryParseLevel(entry.level, out var level) || level < 1 || level > EnhancementState.HandMaxLevel)
                {
                    warnings.Add($"牌型强化「{entry.handUpId}」的等级「{entry.level}」无法解析或超出 1~4，已跳过。");
                    continue;
                }
                if (!decimal.TryParse(entry.baseMult, NumberStyles.Number, CultureInfo.InvariantCulture, out var tableMult))
                {
                    warnings.Add($"牌型强化「{entry.handUpId}」的基础倍率「{entry.baseMult}」无法解析，已跳过。");
                    continue;
                }

                var catalog = HandTypeCatalog.Get(handType);
                var chipDelta = entry.baseChips - catalog.BaseChips;
                var multDelta = decimal.Round(tableMult - catalog.BaseMultiplier, 6); // 抹「1.1000000000000001」类浮点尾巴
                if (chipDelta < 0 || multDelta < 0m)
                {
                    warnings.Add($"牌型强化「{entry.handUpId}」的 Lv{level} 底值低于 Lv0（筹码差 {chipDelta}/倍率差 {multDelta}），表数据疑似异常，请策划确认。");
                }

                GetOrAdd(tables.HandChipDeltas, handType)[level - 1] = chipDelta;
                GetOrAdd(tables.HandMultDeltas, handType)[level - 1] = multDelta;
                GetOrAdd(tables.HandPrices, handType)[level - 1] = entry.price;
                if (!string.IsNullOrEmpty(entry.handName)) tables.HandNames[handType] = entry.handName;
                if (!tables.HandTargets.Contains(handType)) tables.HandTargets.Add(handType); // 保序去重 = 商店轮换顺序
            }
        }

        /// <summary>主属性类型 → 效果类型（PersonaUpRuleTableContract 常量对照，配表 3 行恰好一一覆盖）。</summary>
        private static bool TryMapEffectKind(string mainAttrType, out PersonaEffectKind kind)
        {
            switch (mainAttrType)
            {
                case PersonaUpRuleTableContract.MainAttrTypeChips: kind = PersonaEffectKind.AddChips; return true;
                case PersonaUpRuleTableContract.MainAttrTypeMult: kind = PersonaEffectKind.AddMultiplier; return true;
                case PersonaUpRuleTableContract.MainAttrTypeXMult: kind = PersonaEffectKind.MultiplyFinal; return true;
                default:
                    kind = default;
                    return false;
            }
        }

        /// <summary>「+10筹码」「+0.3倍率」「+10%独立倍率」→ 10 / 0.3 / 0.1（百分比紧跟数字后时 ÷100）。</summary>
        private static bool TryParseIncrease(string text, out decimal increase)
        {
            increase = 0m;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var trimmed = text.Trim();
            if (trimmed[0] == '+') trimmed = trimmed.Substring(1);
            if (trimmed.Length == 0) return false;

            var buffer = new StringBuilder();
            var hasDigit = false;
            var index = 0;
            while (index < trimmed.Length && (char.IsDigit(trimmed[index]) || trimmed[index] == '.'))
            {
                buffer.Append(trimmed[index]);
                if (char.IsDigit(trimmed[index])) hasDigit = true;
                index++;
            }

            if (!hasDigit) return false;
            if (!decimal.TryParse(buffer.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out increase)) return false;
            if (index < trimmed.Length && trimmed[index] == '%') increase = increase / 100m;
            return true;
        }

        /// <summary>「Lv.1」~「Lv.4」→ 提取第一个连续数字段；无数字返回 false。</summary>
        private static bool TryParseLevel(string levelText, out int level)
        {
            level = 0;
            if (string.IsNullOrWhiteSpace(levelText)) return false;
            var index = 0;
            while (index < levelText.Length && !char.IsDigit(levelText[index])) index++;
            if (index >= levelText.Length) return false;
            var start = index;
            while (index < levelText.Length && char.IsDigit(levelText[index])) index++;
            return int.TryParse(levelText.Substring(start, index - start), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out level);
        }

        private static int[] GetOrAdd(Dictionary<Suit, int[]> map, Suit suit)
        {
            if (!map.TryGetValue(suit, out var array))
            {
                array = new int[4];
                map[suit] = array;
            }
            return array;
        }

        private static int[] GetOrAdd(Dictionary<HandType, int[]> map, HandType handType)
        {
            if (!map.TryGetValue(handType, out var array))
            {
                array = new int[4];
                map[handType] = array;
            }
            return array;
        }

        private static decimal[] GetOrAdd(Dictionary<HandType, decimal[]> map, HandType handType)
        {
            if (!map.TryGetValue(handType, out var array))
            {
                array = new decimal[4];
                map[handType] = array;
            }
            return array;
        }
    }
}
