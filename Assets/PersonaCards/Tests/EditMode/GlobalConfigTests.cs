using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// GlobalConfig 门面测试（P0-1F）：
    /// 白盒回落 = 空配置（出牌/弃牌回落 Battle 编译期常量 4/3，行为与 P0-1F 前零差异）；
    /// Configure 注入 17 条目后 StartingPlays/StartingDiscards/TryGetInt/TryGetDecimal/商店槽位/兑换属性生效；
    /// 坏条目整体回落防半状态；[TearDown] Configure(null) 防静态泄漏。
    /// </summary>
    public class GlobalConfigTests
    {
        [TearDown]
        public void TearDown()
        {
            // 防静态状态泄漏到其他测试（与 InitialPersonaCatalogTests 同模式）
            GlobalConfig.Configure(null);
        }

        /// <summary>真实配表 17 条夹具（RULE_001~017 真实值；出牌/弃牌可覆盖，供本类与 RunRouteTests 共用）。</summary>
        internal static List<GlobalConfigEntry> BuildTableEntries(int startingPlays = 4, int startingDiscards = 3)
        {
            var entries = new List<GlobalConfigEntry>();
            void Add(string ruleId, string ruleName, string valueType, string valueText) => entries.Add(
                new GlobalConfigEntry { ruleId = ruleId, ruleName = ruleName, valueType = valueType, valueText = valueText });

            Add("RULE_001", "每关基础出牌次数", "整数", startingPlays.ToString());
            Add("RULE_002", "每关基础弃牌次数", "整数", startingDiscards.ToString());
            Add("RULE_003", "人格生效槽位", "整数", "4");
            Add("RULE_004", "基础人格数量", "整数", "8");
            Add("RULE_005", "商店商品槽数量", "整数", "4");
            Add("RULE_006", "每局AI人格生成总量", "整数", "3");
            Add("RULE_007", "每局AI人格可带出数量", "整数", "1");
            Add("RULE_008", "人格库存上限", "整数", "99");
            Add("RULE_009", "人格融合消耗数量", "整数", "3");
            Add("RULE_010", "人格融合生成数量", "整数", "1");
            Add("RULE_011", "最近3关行为权重", "小数", "0.65");
            Add("RULE_012", "本局累计行为权重", "小数", "0.35");
            Add("RULE_013", "雷同人格生成降重", "小数", "0.15");
            Add("RULE_014", "剩余出牌兑换单位", "整数", "1");
            Add("RULE_015", "剩余出牌奖励金币", "整数", "1");
            Add("RULE_016", "剩余弃牌兑换单位", "整数", "1");
            Add("RULE_017", "剩余弃牌奖励金币", "整数", "1");
            return entries;
        }

        [Test]
        public void WhiteBoxFallsBackToBattleConstants()
        {
            GlobalConfig.Configure(null);

            // 白盒回落：出牌/弃牌 = Battle 编译期常量 4/3（P0-1F 前行为零差异）
            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4));
            Assert.That(GlobalConfig.StartingDiscards, Is.EqualTo(3));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.Null);
            Assert.That(GlobalConfig.TryGetInt("RULE_003", out _), Is.False);
            Assert.That(GlobalConfig.TryGetDecimal("RULE_011", out _), Is.False);
        }

        [Test]
        public void Configure17EntriesLoadsStartingLimitsAndSummary()
        {
            GlobalConfig.Configure(BuildTableEntries(startingPlays: 5, startingDiscards: 2));

            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(5));
            Assert.That(GlobalConfig.StartingDiscards, Is.EqualTo(2));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.EqualTo("17 条全局配置已加载。"));
        }

        [Test]
        public void ShopSlotsReadsRule005WithDefault4()
        {
            // 白盒回落：无配置 → 4（配表默认值）
            Assert.That(GlobalConfig.ShopSlots, Is.EqualTo(4));

            // 配表注入 RULE_005=4 → 命中
            GlobalConfig.Configure(BuildTableEntries());
            Assert.That(GlobalConfig.ShopSlots, Is.EqualTo(4));

            // 覆盖 RULE_005=6 → 门面值随配表走
            var entries = BuildTableEntries();
            entries[4].valueText = "6";
            GlobalConfig.Configure(entries);
            Assert.That(GlobalConfig.ShopSlots, Is.EqualTo(6));
        }

        [Test]
        public void SelectionLimitReadsRule018WithDefault5()
        {
            // P0-2：白盒回落——无配置 → 5（Battle 编译期默认；RULE_018 为预留扩展位，当前 17 条夹具不含此行）
            Assert.That(GlobalConfig.SelectionLimit, Is.EqualTo(5));

            // 配表注入 17 条（无 RULE_018）→ 回落 5
            GlobalConfig.Configure(BuildTableEntries());
            Assert.That(GlobalConfig.SelectionLimit, Is.EqualTo(5));

            // 追加 RULE_018=7 → 门面值随配表走（Mapper 齐全校验只要求 RULE_001~017，多出允许）
            var entries = BuildTableEntries();
            entries.Add(new GlobalConfigEntry
            {
                ruleId = "RULE_018",
                ruleName = "选牌上限",
                valueType = "整数",
                valueText = "7"
            });
            GlobalConfig.Configure(entries);
            Assert.That(GlobalConfig.SelectionLimit, Is.EqualTo(7));
        }

        [Test]
        public void ExchangePropertiesReadRules014To017WithDefault1()
        {
            // 白盒回落：无配置 → 4 个兑换属性全 1（配表默认值）
            Assert.That(GlobalConfig.ExchangePlaysUnit, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangePlaysCoins, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsUnit, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsCoins, Is.EqualTo(1));

            // 配表注入 RULE_014~017 全 1 → 命中
            GlobalConfig.Configure(BuildTableEntries());
            Assert.That(GlobalConfig.ExchangePlaysUnit, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangePlaysCoins, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsUnit, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsCoins, Is.EqualTo(1));

            // 覆盖 RULE_014=2 → 门面值随配表走，其余不变
            var entries = BuildTableEntries();
            entries[13].valueText = "2";
            GlobalConfig.Configure(entries);
            Assert.That(GlobalConfig.ExchangePlaysUnit, Is.EqualTo(2));
            Assert.That(GlobalConfig.ExchangePlaysCoins, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsUnit, Is.EqualTo(1));
            Assert.That(GlobalConfig.ExchangeDiscardsCoins, Is.EqualTo(1));
        }

        [Test]
        public void TryGetIntHitsAndTypeMismatchFails()
        {
            GlobalConfig.Configure(BuildTableEntries());

            // 整数规则命中
            Assert.That(GlobalConfig.TryGetInt("RULE_003", out var slots), Is.True);
            Assert.That(slots, Is.EqualTo(4));
            Assert.That(GlobalConfig.TryGetInt("RULE_008", out var inventory), Is.True);
            Assert.That(inventory, Is.EqualTo(99));

            // 类型不匹配：小数规则走 TryGetInt 失败、整数规则走 TryGetDecimal 失败
            Assert.That(GlobalConfig.TryGetInt("RULE_011", out _), Is.False);
            Assert.That(GlobalConfig.TryGetDecimal("RULE_003", out _), Is.False);

            // 未知规则_ID 失败
            Assert.That(GlobalConfig.TryGetInt("RULE_999", out _), Is.False);
            Assert.That(GlobalConfig.TryGetInt("", out _), Is.False);
            Assert.That(GlobalConfig.TryGetInt(null, out _), Is.False);
        }

        [Test]
        public void TryGetDecimalPreservesPrecision()
        {
            GlobalConfig.Configure(BuildTableEntries());

            // decimal 原文精确保存（0.65/0.35/0.15 与配表一致）
            Assert.That(GlobalConfig.TryGetDecimal("RULE_011", out var recent), Is.True);
            Assert.That(recent, Is.EqualTo(0.65m));
            Assert.That(GlobalConfig.TryGetDecimal("RULE_012", out var cumulative), Is.True);
            Assert.That(cumulative, Is.EqualTo(0.35m));
            Assert.That(GlobalConfig.TryGetDecimal("RULE_013", out var duplicate), Is.True);
            Assert.That(duplicate, Is.EqualTo(0.15m));
        }

        [Test]
        public void BadEntryFallsBackWithoutPartialState()
        {
            var entries = BuildTableEntries();
            entries[4].valueType = "百分比"; // 第 5 条未知数值类型（资产 Validate 会拦，门面防御性兜底）

            GlobalConfig.Configure(entries);

            // 整体回落白盒，不是 11 条半状态
            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.Null);
            Assert.That(GlobalConfig.TryGetInt("RULE_003", out _), Is.False);
        }

        [Test]
        public void ConfigureRejectsDuplicateRuleId()
        {
            var entries = BuildTableEntries();
            entries.Add(entries[0]);

            GlobalConfig.Configure(entries);

            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void ConfigureRejectsNegativeValue()
        {
            var entries = BuildTableEntries();
            entries[0].valueText = "-1";

            GlobalConfig.Configure(entries);

            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void ConfigureNullClearsPreviousConfig()
        {
            GlobalConfig.Configure(BuildTableEntries(startingPlays: 5));
            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(5));

            GlobalConfig.Configure(null);

            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4));
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void PartialEntriesOnlyAffectListedRules()
        {
            // 部分条目合法（不强制 17 条齐全——齐全校验在 Mapper 导入层）：只配 RULE_002=2
            GlobalConfig.Configure(new List<GlobalConfigEntry>
            {
                new GlobalConfigEntry
                {
                    ruleId = "RULE_002",
                    ruleName = "每关基础弃牌次数",
                    valueType = "整数",
                    valueText = "2"
                }
            });

            Assert.That(GlobalConfig.StartingDiscards, Is.EqualTo(2));
            Assert.That(GlobalConfig.StartingPlays, Is.EqualTo(4)); // RULE_001 未配 → 回落白盒
            Assert.That(GlobalConfig.LastConfiguredSummary, Is.EqualTo("1 条全局配置已加载。"));
        }
    }
}
