using System.Collections.Generic;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 三线强化配表真实值夹具（P0-11）：与 PersonaUpRule.asset / SuitUp.asset / HandUp.asset 当前行一致。
    /// 供 EnhancementTablesBuilderTests / ShopEnhancementSessionTests / ShopCatalogTests 共用。
    /// </summary>
    internal static class EnhancementTestFixtures
    {
        /// <summary>与 PersonaUpRule.asset 当前 3 行一致的夹具。</summary>
        public static List<PersonaUpRuleEntry> RealPersonaRules()
        {
            return new List<PersonaUpRuleEntry>
            {
                new PersonaUpRuleEntry { ruleId = "PERSONA_UP_RULE_001", mainAttrType = PersonaUpRuleTableContract.MainAttrTypeChips, perLevelIncrease = "+10筹码", basePrice = 8, levelPriceStep = 3 },
                new PersonaUpRuleEntry { ruleId = "PERSONA_UP_RULE_002", mainAttrType = PersonaUpRuleTableContract.MainAttrTypeMult, perLevelIncrease = "+0.3倍率", basePrice = 8, levelPriceStep = 3 },
                new PersonaUpRuleEntry { ruleId = "PERSONA_UP_RULE_003", mainAttrType = PersonaUpRuleTableContract.MainAttrTypeXMult, perLevelIncrease = "+10%独立倍率", basePrice = 8, levelPriceStep = 3 }
            };
        }

        /// <summary>与 SuitUp.asset 当前 16 行一致的夹具（4 花色 × Lv1~4，筹码 5/10/15/20、价格 8/11/14/17）。</summary>
        public static List<SuitUpEntry> RealSuitUps()
        {
            var entries = new List<SuitUpEntry>();
            var suitIds = new[] { "SUIT_001", "SUIT_002", "SUIT_003", "SUIT_004" };
            var suitNames = new[] { "黑桃", "红桃", "梅花", "方块" };
            var chips = new[] { 5, 10, 15, 20 };
            var prices = new[] { 8, 11, 14, 17 };
            for (var suitIndex = 0; suitIndex < suitIds.Length; suitIndex++)
            {
                for (var level = 0; level < 4; level++)
                {
                    entries.Add(new SuitUpEntry
                    {
                        suitUpId = $"SUIT_UP_{suitIndex * 4 + level + 1:000}",
                        suitId = suitIds[suitIndex],
                        suitName = suitNames[suitIndex],
                        level = $"Lv.{level + 1}",
                        extraChips = chips[level],
                        price = prices[level]
                    });
                }
            }
            return entries;
        }

        /// <summary>
        /// 44 行夹具（11 牌型 × Lv1~4）：皇家同花顺 4 行与 HandUp.asset 真实值一致
        /// （110/120/130/140 与 13.2/14.4/15.6/16.8）；其余牌型按目录 Lv0 值 + 每级增量推导
        /// （差值断言只针对皇家，其余行仅用于 HandTargets 顺序锁定）。
        /// </summary>
        public static List<HandUpEntry> RealHandUps()
        {
            var entries = new List<HandUpEntry>();
            var rows = new[]
            {
                // handId, handName, 目录 Lv0 筹码, 目录 Lv0 倍率, 每级筹码增量, 每级倍率增量
                new[] { "HAND_01", "高牌", "55", "1", "10", "0.1" },
                new[] { "HAND_02", "对子", "48", "2", "10", "0.2" },
                new[] { "HAND_03", "两队", "52", "2.5", "10", "0.25" },
                new[] { "HAND_04", "三条", "57", "3", "10", "0.3" },
                new[] { "HAND_05", "顺子", "60", "4", "10", "0.4" },
                new[] { "HAND_06", "同花", "65", "4", "10", "0.4" },
                new[] { "HAND_07", "葫芦", "74", "5", "10", "0.5" },
                new[] { "HAND_08", "四条", "100", "6", "10", "0.6" },
                new[] { "HAND_09", "同花顺", "95", "10", "10", "1.0" },
                new[] { "HAND_10", "同花葫芦", "70", "12", "10", "1.2" },
                new[] { "HAND_11", "皇家同花顺", "100", "12", "10", "1.2" } // 真实值：110/13.2 → 140/16.8
            };
            var prices = new[] { 8, 11, 14, 17 };
            var rowIndex = 0;
            foreach (var row in rows)
            {
                var baseChips = int.Parse(row[2]);
                var baseMult = decimal.Parse(row[3], System.Globalization.CultureInfo.InvariantCulture);
                var chipStep = int.Parse(row[4]);
                var multStep = decimal.Parse(row[5], System.Globalization.CultureInfo.InvariantCulture);
                for (var level = 0; level < 4; level++)
                {
                    rowIndex++;
                    entries.Add(new HandUpEntry
                    {
                        handUpId = $"HAND_UP_{rowIndex:000}",
                        handId = row[0],
                        handName = row[1],
                        level = $"Lv.{level + 1}",
                        baseChips = baseChips + chipStep * (level + 1),
                        baseMult = (baseMult + multStep * (level + 1)).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        price = prices[level]
                    });
                }
            }
            return entries;
        }

        /// <summary>构造强化商品条目（服务类型；三线各一条，价格 8 = 展示价）。</summary>
        public static ShopProductEntry EnhancementProduct(string productId, string effectType)
        {
            return new ShopProductEntry
            {
                productId = productId,
                productName = effectType,
                productType = ShopProductTableContract.ProductTypeService,
                price = 8,
                purchaseLimit = 1,
                effectType = effectType,
                effectParam1 = "",
                effectParam2 = ""
            };
        }

        /// <summary>单行牌型强化条目构造（浮点尾巴/低于底值等异常夹具用）。</summary>
        public static HandUpEntry HandRow(string handUpId, string handId, string handName, string level, int baseChips, string baseMult, int price)
        {
            return new HandUpEntry
            {
                handUpId = handUpId,
                handId = handId,
                handName = handName,
                level = level,
                baseChips = baseChips,
                baseMult = baseMult,
                price = price
            };
        }
    }
}
