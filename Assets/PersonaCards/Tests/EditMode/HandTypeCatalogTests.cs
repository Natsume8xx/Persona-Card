using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>牌型目录门面测试：Configure 注入/回落语义与排序。目录是静态门面，[TearDown] 必须回落白盒防测试间泄漏。</summary>
    public sealed class HandTypeCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
            HandTypeCatalog.Configure(null); // 每个测试结束后回到白盒，防静态状态泄漏到其他测试
        }

        [Test]
        public void ConfigureWithValidEntriesOverridesFallback()
        {
            var entries = new List<HandTypeEntry>
            {
                new HandTypeEntry(HandType.HighCard, "高牌", 999, 1m, 1, "HAND_01")
            };

            HandTypeCatalog.Configure(entries);

            Assert.That(HandTypeCatalog.Get(HandType.HighCard).BaseChips, Is.EqualTo(999));
            // 未注入的牌型回落白盒占位
            Assert.That(HandTypeCatalog.Get(HandType.FiveOfAKind).BaseChips, Is.EqualTo(100));
            Assert.That(HandTypeCatalog.LastConfiguredSummary, Does.Contain("1 个来自配置").And.Contain("11 个回落白盒"));
        }

        [Test]
        public void ConfigureWithMissingSpecialTypesFillsFallback()
        {
            // 与真实配表一致的场景：10 行数据（缺五条/同花五条），两处回落白盒
            HandTypeCatalog.Configure(HandTypeEntry.CreateFallbackList().Take(10).ToList());

            Assert.That(HandTypeCatalog.Get(HandType.FiveOfAKind).BaseMultiplier, Is.EqualTo(8m));
            Assert.That(HandTypeCatalog.Get(HandType.FlushFive).BaseMultiplier, Is.EqualTo(8m));
            Assert.That(HandTypeCatalog.All, Has.Count.EqualTo(12));
            Assert.That(HandTypeCatalog.LastConfiguredSummary, Does.Contain("10 个来自配置").And.Contain("2 个回落白盒"));
        }

        [Test]
        public void ConfigureWithDuplicateEntriesFallsBackToWhitelist()
        {
            var entries = new List<HandTypeEntry>
            {
                new HandTypeEntry(HandType.HighCard, "高牌", 55, 1m, 1, "HAND_01"),
                new HandTypeEntry(HandType.HighCard, "重复高牌", 999, 1m, 1, "HAND_01")
            };

            HandTypeCatalog.Configure(entries);

            Assert.That(HandTypeCatalog.Get(HandType.HighCard).BaseChips, Is.EqualTo(55)); // 白盒值
            Assert.That(HandTypeCatalog.LastConfiguredSummary, Is.Empty); // 回落白盒时摘要清空
        }

        [Test]
        public void ConfigureWithEmptyOrNullFallsBack()
        {
            HandTypeCatalog.Configure(new List<HandTypeEntry>());
            Assert.That(HandTypeCatalog.Get(HandType.Pair).BaseChips, Is.EqualTo(48));

            HandTypeCatalog.Configure(null);
            Assert.That(HandTypeCatalog.Get(HandType.Pair).BaseChips, Is.EqualTo(48));
            Assert.That(HandTypeCatalog.LastConfiguredSummary, Is.Empty);
        }

        [Test]
        public void AllOrdersByDisplayOrder()
        {
            // 乱序注入：All 必须按显示顺序（再按枚举序兜底）呈现；未注入的牌型白盒补齐后按各自显示顺序接在后面
            var entries = new List<HandTypeEntry>
            {
                new HandTypeEntry(HandType.Straight, "顺子", 60, 4m, 5, "HAND_05"),
                new HandTypeEntry(HandType.HighCard, "高牌", 55, 1m, 1, "HAND_01"),
                new HandTypeEntry(HandType.Pair, "对子", 48, 2m, 2, "HAND_02")
            };

            HandTypeCatalog.Configure(entries);

            // 注入顺序故意打乱（Straight/HighCard/Pair）：All 必须按显示顺序呈现（高牌1 → 对子2 → 顺子5），白盒补齐的牌型按各自显示顺序就位
            var orderedTypes = HandTypeCatalog.All.Select(definition => definition.HandType).ToList();
            Assert.That(orderedTypes, Has.Count.EqualTo(12));
            Assert.That(orderedTypes[0], Is.EqualTo(HandType.HighCard)); // 显示顺序 1
            Assert.That(orderedTypes[1], Is.EqualTo(HandType.Pair));     // 显示顺序 2
            Assert.That(orderedTypes[4], Is.EqualTo(HandType.Straight)); // 显示顺序 5
        }

        [Test]
        public void AssetFallbackEntriesMatchCatalogFallback()
        {
            // 防漂移交叉校验：资产白盒工厂（decimal→double→decimal 往返）与目录白盒回落（decimal 直达）必须逐字段一致。
            // 数值唯一源是 HandTypeEntry.CreateFallbackList，此测试锁死"有人改一边"的回归。
            var asset = ScriptableObject.CreateInstance<HandTypeAsset>();
            asset.entries = HandTypeAsset.CreateFallbackEntries();
            Assert.That(asset.Validate(out var error), Is.True, error);

            HandTypeCatalog.Configure(asset.BuildEntries());
            var catalogAfterConfigure = HandTypeCatalog.All.ToList();

            HandTypeCatalog.Configure(null); // 目录白盒
            var catalogFallback = HandTypeCatalog.All.ToList();

            Assert.That(catalogAfterConfigure, Has.Count.EqualTo(12));
            Assert.That(catalogFallback, Has.Count.EqualTo(12));
            for (var index = 0; index < 12; index++)
            {
                var configured = catalogAfterConfigure[index];
                var fallback = catalogFallback[index];
                Assert.That(configured.HandType, Is.EqualTo(fallback.HandType), $"条目 {index} 牌型漂移");
                Assert.That(configured.DisplayName, Is.EqualTo(fallback.DisplayName), $"条目 {index} 名称漂移");
                Assert.That(configured.BaseChips, Is.EqualTo(fallback.BaseChips), $"条目 {index} 筹码漂移");
                Assert.That(configured.BaseMultiplier, Is.EqualTo(fallback.BaseMultiplier), $"条目 {index} 倍率漂移");
                Assert.That(configured.DisplayOrder, Is.EqualTo(fallback.DisplayOrder), $"条目 {index} 顺序漂移");
                Assert.That(configured.CardId, Is.EqualTo(fallback.CardId), $"条目 {index} card_id 漂移");
            }

            ScriptableObject.DestroyImmediate(asset);
        }
    }
}
