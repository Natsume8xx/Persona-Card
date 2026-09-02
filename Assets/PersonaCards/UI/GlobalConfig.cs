using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PersonaCards.Battle;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 全局配置门面（P0-1F）：17 条规则（RULE_001~017）的运行时读取入口。
    /// RULE_001/002（每关基础出牌/弃牌次数）与战斗参数化归口：RunRoute 默认值链与读档兜底均经
    /// StartingPlays/StartingDiscards；RULE_005（商店商品槽数量）与 RULE_014~017（剩余行动兑换）已开便捷属性，
    /// 其余 9 条只落数据，玩法接入等对应 P0 任务经 TryGetInt/TryGetDecimal 读取。
    /// 白盒回落 = 空配置：Configure(null)/空资产/坏条目 → 出牌/弃牌回落 Battle 编译期常量 4/3（行为与 P0-1F 前零差异）。
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>规则_ID：每关基础出牌次数。</summary>
        public const string RuleStartingPlays = "RULE_001";

        /// <summary>规则_ID：每关基础弃牌次数。</summary>
        public const string RuleStartingDiscards = "RULE_002";

        /// <summary>规则_ID：商店商品槽数量。</summary>
        public const string RuleShopSlots = "RULE_005";

        /// <summary>规则_ID：剩余出牌兑换单位。</summary>
        public const string RuleExchangePlaysUnit = "RULE_014";

        /// <summary>规则_ID：剩余出牌奖励金币。</summary>
        public const string RuleExchangePlaysCoins = "RULE_015";

        /// <summary>规则_ID：剩余弃牌兑换单位。</summary>
        public const string RuleExchangeDiscardsUnit = "RULE_016";

        /// <summary>规则_ID：剩余弃牌奖励金币。</summary>
        public const string RuleExchangeDiscardsCoins = "RULE_017";

        private static Dictionary<string, GlobalConfigEntry> _entries; // null = 白盒空配置
        private static string _summary;

        /// <summary>最近一次 Configure 的摘要（成功时非 null；由调用方打 [Global] 日志）。</summary>
        public static string LastConfiguredSummary => _summary;

        /// <summary>每关基础出牌次数：RULE_001 命中则用之，否则回落 Battle 编译期常量（白盒零差异）。</summary>
        public static int StartingPlays =>
            TryGetInt(RuleStartingPlays, out var value) ? value : BattleStateMachine.StartingPlays;

        /// <summary>每关基础弃牌次数：RULE_002 命中则用之，否则回落 Battle 编译期常量（白盒零差异）。</summary>
        public static int StartingDiscards =>
            TryGetInt(RuleStartingDiscards, out var value) ? value : BattleStateMachine.StartingDiscards;

        /// <summary>商店商品槽数量：RULE_005 命中则用之，否则回落 4（与配表默认值一致，白盒零差异）。</summary>
        public static int ShopSlots =>
            TryGetInt(RuleShopSlots, out var value) ? value : 4;

        /// <summary>剩余出牌兑换单位：RULE_014 命中则用之，否则回落 1（与配表默认值一致，白盒零差异）。</summary>
        public static int ExchangePlaysUnit =>
            TryGetInt(RuleExchangePlaysUnit, out var value) ? value : 1;

        /// <summary>剩余出牌奖励金币：RULE_015 命中则用之，否则回落 1（与配表默认值一致，白盒零差异）。</summary>
        public static int ExchangePlaysCoins =>
            TryGetInt(RuleExchangePlaysCoins, out var value) ? value : 1;

        /// <summary>剩余弃牌兑换单位：RULE_016 命中则用之，否则回落 1（与配表默认值一致，白盒零差异）。</summary>
        public static int ExchangeDiscardsUnit =>
            TryGetInt(RuleExchangeDiscardsUnit, out var value) ? value : 1;

        /// <summary>剩余弃牌奖励金币：RULE_017 命中则用之，否则回落 1（与配表默认值一致，白盒零差异）。</summary>
        public static int ExchangeDiscardsCoins =>
            TryGetInt(RuleExchangeDiscardsCoins, out var value) ? value : 1;

        /// <summary>
        /// 注入条目列表：null/空列表 → 白盒空配置（summary 置 null）。逐条校验（格式/类型/数值/唯一/非负），
        /// 任一失败 → 整体回落白盒（防半状态，P0-1E 教训）；成功 → 整体替换 + 摘要。
        /// </summary>
        public static void Configure(IReadOnlyList<GlobalConfigEntry> entries)
        {
            // null/空 → 白盒空配置
            if (entries == null || entries.Count == 0)
            {
                _entries = null;
                _summary = null;
                return;
            }

            // 先全部校验再整体替换（防半状态）
            var converted = new Dictionary<string, GlobalConfigEntry>();
            foreach (var entry in entries)
            {
                if (entry == null
                    || string.IsNullOrEmpty(entry.ruleId)
                    || !Regex.IsMatch(entry.ruleId, GlobalConfigTableContract.RuleIdPattern)
                    || string.IsNullOrEmpty(entry.ruleName)
                    || Array.IndexOf(GlobalConfigTableContract.ValueTypes, entry.valueType) < 0
                    || !GlobalConfigTableMapper.TryParseValueText(entry.valueType, entry.valueText, out _)
                    || converted.ContainsKey(entry.ruleId))
                {
                    _entries = null;
                    _summary = null;
                    return;
                }
                converted.Add(entry.ruleId, entry);
            }

            _entries = converted;
            _summary = $"{converted.Count} 条全局配置已加载。";
        }

        /// <summary>按规则_ID 取整数（条目存在、数值类型=整数、可解析且非负时成功）。</summary>
        public static bool TryGetInt(string ruleId, out int value)
        {
            value = 0;
            if (_entries == null || string.IsNullOrEmpty(ruleId)) return false;
            if (!_entries.TryGetValue(ruleId, out var entry)) return false;
            if (entry.valueType != GlobalConfigTableContract.ValueTypeInteger) return false;
            if (!int.TryParse(entry.valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || value < 0)
            {
                value = 0;
                return false;
            }
            return true;
        }

        /// <summary>按规则_ID 取小数（条目存在、数值类型=小数、可解析且非负时成功；decimal 原文精确保存）。</summary>
        public static bool TryGetDecimal(string ruleId, out decimal value)
        {
            value = 0m;
            if (_entries == null || string.IsNullOrEmpty(ruleId)) return false;
            if (!_entries.TryGetValue(ruleId, out var entry)) return false;
            if (entry.valueType != GlobalConfigTableContract.ValueTypeDecimal) return false;
            if (!decimal.TryParse(entry.valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || value < 0m)
            {
                value = 0m;
                return false;
            }
            return true;
        }
    }
}
