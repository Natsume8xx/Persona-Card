using System;
using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.Data;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// ShopUiSession 商店主界面会话测试（UI 重排第二批）：商品页 4 行 + 服务区块 + 购买按钮状态 +
    /// 铸造页 8 人格列表/进度/副属性顺序解锁/真实扣款/满级态。目录用 2 人格合成夹具（真实资产值由冒烟覆盖）。
    /// </summary>
    public sealed class ShopUiSessionTests
    {
        private ShopUiSession _session;
        private JourneyDeckState _deck;
        private ShopState _shop;

        [SetUp]
        public void SetUp()
        {
            PersonaForgeCatalog.Configure(BuildCards(), BuildEntries(), BuildMains(), BuildSubs(), BuildForge());
            _deck = new JourneyDeckState(new[]
            {
                new PlayingCardInstance("c1", Suit.Hearts, Rank.Five),
                new PlayingCardInstance("c2", Suit.Spades, Rank.Seven),
                new PlayingCardInstance("c3", Suit.Clubs, Rank.Nine)
            }, 10);
            _shop = BuildShop(CardProduct("SHOP_CARD_001", "红桃5", ShopState.EffectAddCard, 2));
            _session = new ShopUiSession();
            _session.Configure(_shop, _deck, BuildLoadout(), new ForgeUnlockState(), 0);
        }

        // ---------- 构造夹具 ----------

        private static ShopProductEntry CardProduct(string id, string name, string effectType, int price)
        {
            return new ShopProductEntry
            {
                productId = id,
                productName = name,
                productType = ShopProductTableContract.ProductTypeCard,
                price = price,
                purchaseLimit = 1,
                effectType = effectType,
                effectParam1 = "",
                effectParam2 = ""
            };
        }

        private static ShopProductEntry ServiceProduct(string id, string name, string effectType, string param1)
        {
            return new ShopProductEntry
            {
                productId = id,
                productName = name,
                productType = ShopProductTableContract.ProductTypeService,
                price = 5,
                purchaseLimit = 1,
                effectType = effectType,
                effectParam1 = param1,
                effectParam2 = ""
            };
        }

        /// <summary>6 槽商店：卡牌 2（红桃5×2）+ 人格牌 2（无货）+ 服务 2（筹码强化×2）。</summary>
        private static ShopState BuildShop(params ShopProductEntry[] cardCandidates)
        {
            var products = new List<ShopProductEntry>(cardCandidates);
            products.Add(ServiceProduct("SHOP_SERVICE_001", "筹码强化", ShopState.EffectEnhanceCard, "基础筹码"));
            var pool = new List<ShopPoolRefreshEntry>();
            foreach (var product in products)
                pool.Add(new ShopPoolRefreshEntry { poolId = $"POOL_{product.productId}", productId = product.productId, weight = 1 });
            var slots = new List<ShopSlotRefreshEntry>
            {
                new ShopSlotRefreshEntry { refreshId = "REFRESH_1", node = ShopState.NodeAi1, productType = ShopProductTableContract.ProductTypeCard, count = 2, weight = 20 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_2", node = ShopState.NodeAi1, productType = ShopProductTableContract.ProductTypePersona, count = 2, weight = 20 },
                new ShopSlotRefreshEntry { refreshId = "REFRESH_3", node = ShopState.NodeAi1, productType = ShopProductTableContract.ProductTypeService, count = 2, weight = 20 }
            };
            return new ShopState(products, pool, slots, 0, 12345u);
        }

        private static PersonaLoadoutState BuildLoadout()
        {
            return new PersonaLoadoutState(new[]
            {
                new PersonaCardDefinition("persona.test.1", "测试·一", PersonaConditionKind.Always, HandType.Pair, PersonaEffectKind.AddChips, 10m),
                new PersonaCardDefinition("persona.test.2", "测试·二", PersonaConditionKind.Always, HandType.Pair, PersonaEffectKind.AddMultiplier, 1m),
                null,
                null
            });
        }

        private static PersonaCardAsset BuildCards()
        {
            var asset = ScriptableObject.CreateInstance<PersonaCardAsset>();
            asset.entries.Add(new PersonaCardEntry { personaId = "PER_001", personaName = "人格牌01", entryId = "ENTRY_001", mainAttrId = "MAIN_001", subAttrId = "SUB_001", maxAttrs = 3, maxSubAttrs = 2, subPoolSize = 5 });
            asset.entries.Add(new PersonaCardEntry { personaId = "PER_002", personaName = "人格牌02", entryId = "ENTRY_002", mainAttrId = "MAIN_002", subAttrId = "SUB_004", maxAttrs = 3, maxSubAttrs = 2, subPoolSize = 5 });
            return asset;
        }

        private static PersonaEntryAsset BuildEntries()
        {
            var asset = ScriptableObject.CreateInstance<PersonaEntryAsset>();
            asset.entries.Add(new PersonaEntryEntry { entryId = "ENTRY_001", description = "连续两次使用相同牌型", conditionType = "连续牌型", comparator = "EQ", conditionParam = "2" });
            asset.entries.Add(new PersonaEntryEntry { entryId = "ENTRY_002", description = "本次计分牌数量 ≥4", conditionType = "计分牌数量", comparator = "GTE", conditionParam = "4" });
            return asset;
        }

        private static PersonaMainAttrAsset BuildMains()
        {
            var asset = ScriptableObject.CreateInstance<PersonaMainAttrAsset>();
            asset.entries.Add(new PersonaMainAttrEntry { attrId = "MAIN_001", attrType = "基础筹码", param1 = "增加", param2 = "15", unlockNode = "默认" });
            asset.entries.Add(new PersonaMainAttrEntry { attrId = "MAIN_002", attrType = "基础倍率", param1 = "增加", param2 = "1", unlockNode = "默认" });
            return asset;
        }

        private static PersonaSubAttrAsset BuildSubs()
        {
            var asset = ScriptableObject.CreateInstance<PersonaSubAttrAsset>();
            asset.entries.Add(new PersonaSubAttrEntry { subAttrId = "SUB_001", ownerPersona = "人格牌01", weight = 40, attrType = "基础筹码", param1 = "增加", param2 = "8", unlockNode = "AI1" });
            asset.entries.Add(new PersonaSubAttrEntry { subAttrId = "SUB_002", ownerPersona = "人格牌01", weight = 25, attrType = "基础倍率", param1 = "增加", param2 = "0.3", unlockNode = "AI2" });
            asset.entries.Add(new PersonaSubAttrEntry { subAttrId = "SUB_003", ownerPersona = "人格牌01", weight = 20, attrType = "独立倍率", param1 = "增加", param2 = "0.03", unlockNode = "AI3" });
            asset.entries.Add(new PersonaSubAttrEntry { subAttrId = "SUB_004", ownerPersona = "人格牌02", weight = 40, attrType = "金币", param1 = "增加", param2 = "5", unlockNode = "AI1" });
            asset.entries.Add(new PersonaSubAttrEntry { subAttrId = "SUB_005", ownerPersona = "人格牌02", weight = 30, attrType = "出牌次数", param1 = "增加", param2 = "1", unlockNode = "AI2" });
            return asset;
        }

        private static ShopForgeAsset BuildForge()
        {
            var asset = ScriptableObject.CreateInstance<ShopForgeAsset>();
            asset.entries.Add(new ShopForgeEntry { forgeId = "FORGE_001", forgeName = "解锁第二词条", price = 5 });
            asset.entries.Add(new ShopForgeEntry { forgeId = "FORGE_002", forgeName = "解锁第三词条", price = 8 });
            return asset;
        }

        // ---------- 标签页与侧边栏 ----------

        [Test]
        public void Configure_默认商品页_侧边栏统计文案()
        {
            Assert.That(_session.IsForgeTab, Is.False);
            Assert.That(_session.Coins, Is.EqualTo(10));
            Assert.That(_session.DeckCount, Is.EqualTo(3));
            Assert.That(_session.EquippedPersonaCount, Is.EqualTo(2));
            Assert.That(_session.PersonaSlotCount, Is.EqualTo(4));
            Assert.That(_session.SidebarStatsText, Is.EqualTo("金币 10 · 牌库 3 张 · 人格 2/4"));
        }

        [Test]
        public void 标签切换_商品页与铸造页()
        {
            _session.ShowForge();
            Assert.That(_session.IsForgeTab, Is.True);
            _session.ShowProducts();
            Assert.That(_session.IsForgeTab, Is.False);
        }

        [Test]
        public void Configure_null依赖_抛异常_unlocks可空()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _session.Configure(null, _deck, BuildLoadout(), new ForgeUnlockState(), 0));
            Assert.Throws<ArgumentNullException>(() =>
                _session.Configure(_shop, null, BuildLoadout(), new ForgeUnlockState(), 0));
            Assert.DoesNotThrow(() =>
                _session.Configure(_shop, _deck, null, null, 0));
        }

        [Test]
        public void Shop_暴露当前注入的商店状态()
        {
            Assert.That(_session.Shop, Is.SameAs(_shop));
            var other = BuildShop(CardProduct("SHOP_CARD_003", "梅花2", ShopState.EffectAddCard, 3));
            _session.Configure(other, _deck, BuildLoadout(), new ForgeUnlockState(), 0);
            Assert.That(_session.Shop, Is.SameAs(other));
        }

        [Test]
        public void IsConfigured_未注入为假_注入后为真()
        {
            var fresh = new ShopUiSession();
            Assert.That(fresh.IsConfigured, Is.False); // 视图在会话未注入时 Refresh 应跳过（配置顺序防御）
            fresh.Configure(_shop, _deck, BuildLoadout(), new ForgeUnlockState(), 0);
            Assert.That(fresh.IsConfigured, Is.True);
        }

        // ---------- 商品页 ----------

        [Test]
        public void 商品行_卡牌位与无货位文案()
        {
            Assert.That(_session.ProductRowVisibleCount, Is.EqualTo(4));
            Assert.That(_session.ProductRowText(0), Is.EqualTo("红桃5 · 2金币"));
            Assert.That(_session.ProductRowText(1), Is.EqualTo("红桃5 · 2金币"));
            Assert.That(_session.ProductRowText(2), Is.EqualTo("无货"));
            Assert.That(_session.ProductRowText(3), Is.EqualTo("无货"));
        }

        [Test]
        public void 商品行_越界抛异常()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.ProductRowText(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.SelectProduct(4));
        }

        [Test]
        public void 选中商品_无货位忽略保持当前()
        {
            _session.SelectProduct(1);
            Assert.That(_session.SelectedProductIndex, Is.EqualTo(1));
            _session.SelectProduct(2); // 无货位
            Assert.That(_session.SelectedProductIndex, Is.EqualTo(1));
            Assert.That(_session.HasProduct(2), Is.False);
        }

        [Test]
        public void 商品详情_增加卡牌_卡牌名与花色符号()
        {
            Assert.That(_session.HasSelectedProduct, Is.True);
            Assert.That(_session.ProductNameText, Is.EqualTo("红桃5"));
            Assert.That(_session.ProductTypeText, Is.EqualTo("类型·卡牌"));
            Assert.That(_session.ProductDetailText, Is.EqualTo("获得 1 张 红桃5（♥）加入牌库"));
            Assert.That(_session.ProductPriceText, Is.EqualTo("2金币"));
        }

        [Test]
        public void 商品详情_移除卡牌_固定文案()
        {
            var shop = BuildShop(CardProduct("SHOP_CARD_002", "黑桃K", ShopState.EffectRemoveCard, 2));
            _session.Configure(shop, _deck, BuildLoadout(), new ForgeUnlockState(), 0);
            Assert.That(_session.ProductDetailText, Is.EqualTo("从牌库移除 1 张卡牌"));
        }

        [Test]
        public void 购买按钮_可购与金币不足()
        {
            Assert.That(_session.CanBuySelected, Is.True);
            Assert.That(_session.BuyButtonText, Is.EqualTo("购买商品（2金币）"));

            var poor = new JourneyDeckState(new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, 1);
            _session.Configure(_shop, poor, BuildLoadout(), new ForgeUnlockState(), 0);
            Assert.That(_session.CanBuySelected, Is.False);
            Assert.That(_session.BuyButtonText, Is.EqualTo("金币不足"));
        }

        [Test]
        public void 购买按钮_售罄态()
        {
            _shop.Slots[0].MarkSold();
            Assert.That(_session.ProductRowText(0), Is.EqualTo("红桃5 · 已售罄"));
            Assert.That(_session.ProductPriceText, Is.EqualTo("已售罄"));
            Assert.That(_session.CanBuySelected, Is.False);
            Assert.That(_session.BuyButtonText, Is.EqualTo("已售罄"));
        }

        [Test]
        public void 服务区块_行文案与可打开()
        {
            Assert.That(_session.ServiceRowCount, Is.EqualTo(2));
            Assert.That(_session.ServiceRowText(0), Is.EqualTo("筹码强化 · 5金币"));
            Assert.That(_session.ServiceRowText(1), Is.EqualTo("筹码强化 · 5金币"));
            Assert.That(_session.CanOpenService(0), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.ServiceRowText(2));
        }

        [Test]
        public void 服务区块_售罄后不可打开()
        {
            _shop.Slots[4].MarkSold();
            Assert.That(_session.CanOpenService(0), Is.False);
            Assert.That(_session.ServiceRowText(0), Is.EqualTo("筹码强化 · 已售罄"));
        }

        // ---------- 铸造页 ----------

        [Test]
        public void 铸造列表_行名与进度()
        {
            _session.ShowForge();
            Assert.That(_session.ForgeCount, Is.EqualTo(2));
            Assert.That(_session.ForgeRowName(0), Is.EqualTo("人格牌01"));
            Assert.That(_session.ForgeRowProgress(0), Is.EqualTo("0/2"));
            Assert.That(_session.ForgeRowProgress(1), Is.EqualTo("0/2"));
            Assert.That(_session.IsForgeRowMaxed(0), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.SelectForge(2));
        }

        [Test]
        public void 铸造详情_词条与主属性文案()
        {
            _session.ShowForge();
            Assert.That(_session.ForgeEntryText(0), Is.EqualTo("连续两次使用相同牌型"));
            Assert.That(_session.ForgeMainAttrText(0), Is.EqualTo("基础筹码 +15"));
            Assert.That(_session.ForgeMainAttrType(0), Is.EqualTo("基础筹码"));
            Assert.That(_session.ForgeMainAttrText(1), Is.EqualTo("基础倍率 +1"));
        }

        [Test]
        public void 副属性槽位_初始未解锁_节点文案()
        {
            _session.ShowForge();
            Assert.That(_session.SubAttrSlotCount(0), Is.EqualTo(2));
            Assert.That(_session.IsSubAttrUnlocked(0, 0), Is.False);
            Assert.That(_session.SubAttrStatusText(0, 0), Is.EqualTo("未解锁"));
            Assert.That(_session.SubAttrNodeText(0, 0), Is.EqualTo("解锁节点：第一章 · 已到达"));
            Assert.That(_session.SubAttrNodeText(0, 1), Is.EqualTo("解锁节点：第二章 · 未到达"));
        }

        [Test]
        public void 副属性槽位_越界抛异常()
        {
            _session.ShowForge();
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.IsSubAttrUnlocked(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.SubAttrStatusText(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _session.UnlockButtonText(0, -1));
        }

        [Test]
        public void 解锁按钮_顺序钳制文案()
        {
            _session.ShowForge();
            Assert.That(_session.CanUnlockSubAttr(0, 0), Is.True);
            Assert.That(_session.CanUnlockSubAttr(0, 1), Is.False); // 顺序未到
            Assert.That(_session.UnlockButtonText(0, 0), Is.EqualTo("解锁 · 5金币"));
            Assert.That(_session.UnlockButtonText(0, 1), Is.EqualTo("未解锁"));
        }

        [Test]
        public void 解锁_成功扣款进度推进_第二槽8金可解锁()
        {
            var rich = new JourneyDeckState(new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, 15);
            _session.Configure(_shop, rich, BuildLoadout(), new ForgeUnlockState(), 0);
            _session.ShowForge();
            Assert.That(_session.TryUnlockSubAttr(0, 0), Is.True);
            Assert.That(_session.Coins, Is.EqualTo(10));
            Assert.That(_session.ForgeRowProgress(0), Is.EqualTo("1/2"));
            Assert.That(_session.IsSubAttrUnlocked(0, 0), Is.True);
            Assert.That(_session.SubAttrStatusText(0, 0), Is.EqualTo("基础筹码 +8"));
            Assert.That(_session.UnlockButtonText(0, 0), Is.EqualTo("已解锁"));
            Assert.That(_session.CanUnlockSubAttr(0, 1), Is.True);
            Assert.That(_session.UnlockButtonText(0, 1), Is.EqualTo("解锁 · 8金币"));
        }

        [Test]
        public void 解锁_金币不足_无副作用()
        {
            var poor = new JourneyDeckState(new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, 4);
            _session.Configure(_shop, poor, BuildLoadout(), new ForgeUnlockState(), 0);
            _session.ShowForge();
            Assert.That(_session.CanUnlockSubAttr(0, 0), Is.False);
            Assert.That(_session.UnlockButtonText(0, 0), Is.EqualTo("金币不足"));
            Assert.That(_session.TryUnlockSubAttr(0, 0), Is.False);
            Assert.That(_session.Coins, Is.EqualTo(4));
            Assert.That(_session.ForgeRowProgress(0), Is.EqualTo("0/2"));
        }

        [Test]
        public void 解锁_满级后无按钮()
        {
            var rich = new JourneyDeckState(new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, 20);
            _session.Configure(_shop, rich, BuildLoadout(), new ForgeUnlockState(), 0);
            _session.ShowForge();
            Assert.That(_session.TryUnlockSubAttr(0, 0), Is.True);
            Assert.That(_session.TryUnlockSubAttr(0, 1), Is.True);
            Assert.That(_session.Coins, Is.EqualTo(7));
            Assert.That(_session.ForgeRowProgress(0), Is.EqualTo("2/2"));
            Assert.That(_session.IsForgeRowMaxed(0), Is.True);
            Assert.That(_session.CanUnlockSubAttr(0, 0), Is.False);
            Assert.That(_session.CanUnlockSubAttr(0, 1), Is.False);
            Assert.That(_session.UnlockButtonText(0, 0), Is.EqualTo("已解锁"));
            Assert.That(_session.UnlockButtonText(0, 1), Is.EqualTo("已解锁"));
            Assert.That(_session.SubAttrStatusText(0, 1), Is.EqualTo("基础倍率 +0.3"));
        }

        [Test]
        public void 解锁_多人格进度独立()
        {
            _session.ShowForge();
            _session.TryUnlockSubAttr(0, 0);
            Assert.That(_session.ForgeRowProgress(0), Is.EqualTo("1/2"));
            Assert.That(_session.ForgeRowProgress(1), Is.EqualTo("0/2"));
            Assert.That(_session.CanUnlockSubAttr(1, 0), Is.True);
        }

        [Test]
        public void LeaveLabel_默认与自定义去向()
        {
            Assert.That(_session.LeaveLabel, Is.EqualTo("离开商店"));
            _session.LeaveLabel = "离开商店 · 前往 Boss";
            Assert.That(_session.LeaveLabel, Is.EqualTo("离开商店 · 前往 Boss"));
        }
    }
}
