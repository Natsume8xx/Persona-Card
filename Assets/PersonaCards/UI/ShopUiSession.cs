using System;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店主界面会话（UI 重排第二批）：纯 C# 无引擎依赖，可单测。
    /// 双标签：商品页（4 商品位 = 卡牌 2 + 人格牌 2，槽 0~3）+ 服务区块（槽 4+，点击由视图委托 FlowController 打开对应界面）
    /// 与铸造页（PersonaForgeCatalog 8 人格列表 + 副属性解锁 5 金→8 金顺序解锁，进度「0/2」真实入档）。
    /// 本会话只做状态投影与选择/解锁动作：商品购买由视图委托 FlowController.PurchaseShopSlot（旧流程复用）；
    /// 副属性解锁 = ForgeUnlockState.TryUnlock（真实扣款）。离开按钮文案（去向后缀）由 FlowController 写入 LeaveLabel。
    /// </summary>
    public sealed class ShopUiSession
    {
        /// <summary>商品位行数（槽 0~3：卡牌 2 + 人格牌 2；服务槽恒在 4 之后，由 ShopState 构造器按类型序生成）。</summary>
        public const int ProductRowCount = 4;

        private ShopState _shop;
        private JourneyDeckState _deck;
        private PersonaLoadoutState _loadout;
        private ForgeUnlockState _unlocks;
        private int _generationNodeCount;
        private bool _forgeTab;
        private int _selectedProductIndex;
        private int _selectedForgeIndex;

        /// <summary>离开按钮文案（默认「离开商店」；FlowController 写入「离开商店 · 前往 Boss」等去向后缀）。</summary>
        public string LeaveLabel { get; set; } = "离开商店";

        /// <summary>
        /// 注入依赖：shop/deck 必填（null 抛）；loadout 可空（侧边栏人格统计回落 0/0）；unlocks 可空（按全未解锁）。
        /// 重置标签回商品页、选中回第一行。
        /// </summary>
        public void Configure(ShopState shop, JourneyDeckState deck, PersonaLoadoutState loadout,
            ForgeUnlockState unlocks, int generationNodeCount)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _loadout = loadout;
            _unlocks = unlocks ?? new ForgeUnlockState();
            _generationNodeCount = generationNodeCount;
            _forgeTab = false;
            _selectedProductIndex = 0;
            _selectedForgeIndex = 0;
        }

        // ---------- 标签页 ----------

        /// <summary>当前注入的商店状态（FlowController 判断是否需要重新 Configure）。</summary>
        public ShopState Shop => _shop;

        /// <summary>当前是否铸造标签页（false = 商品页）。</summary>
        public bool IsForgeTab => _forgeTab;

        public void ShowProducts() => _forgeTab = false;

        public void ShowForge() => _forgeTab = true;

        // ---------- 左侧玩家信息侧边栏 ----------

        public int Coins => _deck.Coins;

        public int DeckCount => _deck.Cards.Count;

        /// <summary>已装备人格牌数（loadout 未注入 → 0）。</summary>
        public int EquippedPersonaCount
        {
            get
            {
                if (_loadout == null) return 0;
                var count = 0;
                foreach (var slot in _loadout.Slots)
                    if (slot != null) count++;
                return count;
            }
        }

        public int PersonaSlotCount => _loadout != null ? _loadout.Slots.Count : 0;

        /// <summary>侧边栏统计文案：「金币 3 · 牌库 10 张 · 人格 2/4」。</summary>
        public string SidebarStatsText => $"金币 {Coins} · 牌库 {DeckCount} 张 · 人格 {EquippedPersonaCount}/{PersonaSlotCount}";

        // ---------- 商品页：商品位行（槽 0~3） ----------

        /// <summary>商品位行数（= min(4, 槽位数)；防御旧槽位数据不足 4）。</summary>
        public int ProductRowVisibleCount => Math.Min(ProductRowCount, _shop.Slots.Count);

        /// <summary>商品位行文案：「黑桃A · 2金币」/「黑桃A · 已售罄」/「无货」；越界抛 ArgumentOutOfRangeException。</summary>
        public string ProductRowText(int rowIndex)
        {
            var slot = ProductSlotAt(rowIndex);
            if (slot.Product == null) return "无货";
            return slot.SoldOut ? $"{slot.Product.productName} · 已售罄" : $"{slot.Product.productName} · {slot.Product.price}金币";
        }

        /// <summary>该商品位是否有商品（无货位不可选）。</summary>
        public bool HasProduct(int rowIndex) => ProductSlotAt(rowIndex).Product != null;

        /// <summary>该商品位是否已售罄。</summary>
        public bool IsProductSoldOut(int rowIndex) => ProductSlotAt(rowIndex).SoldOut;

        /// <summary>切换选中商品位；无货位忽略（保持当前选中）；越界抛 ArgumentOutOfRangeException。</summary>
        public void SelectProduct(int rowIndex)
        {
            ProductSlotAt(rowIndex); // 越界校验
            if (ProductSlotAt(rowIndex).Product == null) return;
            _selectedProductIndex = rowIndex;
        }

        public int SelectedProductIndex => _selectedProductIndex;

        // ---------- 商品页：右列商品详情 ----------

        public bool HasSelectedProduct => ProductSlotAt(_selectedProductIndex).Product != null;

        public string ProductNameText =>
            HasSelectedProduct ? ProductSlotAt(_selectedProductIndex).Product.productName : "无货位";

        /// <summary>类型标签：「类型·卡牌」/「类型·人格牌」/「类型·服务」（无货位 →「类型·--」）。</summary>
        public string ProductTypeText =>
            HasSelectedProduct ? $"类型·{ProductSlotAt(_selectedProductIndex).Product.productType}" : "类型·--";

        /// <summary>效果描述：增加卡牌 → 卡牌名解析 + 花色符号；移除卡牌 → 固定文案；其余 → 效果类型原文（参数1 非空追加）。</summary>
        public string ProductDetailText
        {
            get
            {
                var product = ProductSlotAt(_selectedProductIndex).Product;
                if (product == null) return "该商品位无货。";
                switch (product.effectType)
                {
                    case ShopState.EffectAddCard:
                        return ShopState.TryParseCardName(product.productName, out var suit, out var rank)
                            ? $"获得 1 张 {PersonaShopText.CardTextOf(suit, rank)}（{PersonaShopText.CardSymbolOf(suit)}）加入牌库"
                            : $"获得 1 张 {product.productName} 加入牌库";
                    case ShopState.EffectRemoveCard:
                        return "从牌库移除 1 张卡牌";
                    default:
                        return string.IsNullOrWhiteSpace(product.effectParam1)
                            ? product.effectType
                            : $"{product.effectType}：{product.effectParam1}";
                }
            }
        }

        /// <summary>价格文案：「2金币」/「已售罄」/「无货」。</summary>
        public string ProductPriceText
        {
            get
            {
                var product = ProductSlotAt(_selectedProductIndex).Product;
                if (product == null) return "无货";
                return IsProductSoldOut(_selectedProductIndex) ? "已售罄" : $"{product.price}金币";
            }
        }

        /// <summary>可购买：有商品、未售罄、金币足够。购买执行委托 FlowController.PurchaseShopSlot。</summary>
        public bool CanBuySelected
        {
            get
            {
                var product = ProductSlotAt(_selectedProductIndex).Product;
                return product != null && !IsProductSoldOut(_selectedProductIndex) && Coins >= product.price;
            }
        }

        /// <summary>购买按钮文案：「购买商品（2金币）」/「金币不足」/「已售罄」/「无货」。</summary>
        public string BuyButtonText
        {
            get
            {
                var product = ProductSlotAt(_selectedProductIndex).Product;
                if (product == null) return "无货";
                if (IsProductSoldOut(_selectedProductIndex)) return "已售罄";
                return Coins < product.price ? "金币不足" : $"购买商品（{product.price}金币）";
            }
        }

        // ---------- 商品页：服务区块（槽 4+） ----------

        public int ServiceRowCount => Math.Max(0, _shop.Slots.Count - ProductRowCount);

        /// <summary>服务行文案：「筹码强化 · 5金币」/「筹码强化 · 已售罄」/「无货」；越界抛 ArgumentOutOfRangeException。</summary>
        public string ServiceRowText(int rowIndex)
        {
            var slot = ServiceSlotAt(rowIndex);
            if (slot.Product == null) return "无货";
            return slot.SoldOut ? $"{slot.Product.productName} · 已售罄" : $"{slot.Product.productName} · {slot.Product.price}金币";
        }

        /// <summary>服务行可点击打开对应界面：有商品且未售罄。</summary>
        public bool CanOpenService(int rowIndex)
        {
            var slot = ServiceSlotAt(rowIndex);
            return slot.Product != null && !slot.SoldOut;
        }

        // ---------- 铸造页：人格列表 ----------

        public int ForgeCount => PersonaForgeCatalog.CardCount;

        public int SelectedForgeIndex => _selectedForgeIndex;

        /// <summary>切换选中人格；越界抛 ArgumentOutOfRangeException。</summary>
        public void SelectForge(int index)
        {
            if (index < 0 || index >= ForgeCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            _selectedForgeIndex = index;
        }

        public PersonaCardEntry ForgeCardAt(int index) => PersonaForgeCatalog.CardAt(index);

        public string ForgeRowName(int index) => ForgeCardAt(index).personaName;

        /// <summary>解锁进度：「0/2」「1/2」「2/2」（已拍板口径：真实进度入档）。</summary>
        public string ForgeRowProgress(int index)
        {
            var card = ForgeCardAt(index);
            var unlocked = Math.Min(UnlockedCountOf(card.personaId), Math.Max(0, card.maxSubAttrs));
            return $"{unlocked}/{Math.Max(0, card.maxSubAttrs)}";
        }

        /// <summary>该人格副属性是否已全部解锁。</summary>
        public bool IsForgeRowMaxed(int index)
        {
            var card = ForgeCardAt(index);
            return UnlockedCountOf(card.personaId) >= Math.Max(0, card.maxSubAttrs);
        }

        /// <summary>词条触发条件描述（「连续两次使用相同牌型」；词条未收录 → 空串）。</summary>
        public string ForgeEntryText(int index) => PersonaForgeCatalog.EntryDescriptionOf(ForgeCardAt(index).entryId);

        /// <summary>主属性效果描述（「基础筹码 +15」）。</summary>
        public string ForgeMainAttrText(int index) => PersonaForgeCatalog.MainAttrEffectTextOf(ForgeCardAt(index));

        /// <summary>主属性类型名（「基础筹码」；视图「主属性 · {type}」标签用）。</summary>
        public string ForgeMainAttrType(int index) => PersonaForgeCatalog.MainAttrTypeOf(ForgeCardAt(index));

        // ---------- 铸造页：副属性槽位 ----------

        /// <summary>副属性槽位数（= maxSubAttrs，配表恒 2）。</summary>
        public int SubAttrSlotCount(int forgeIndex) => Math.Max(0, ForgeCardAt(forgeIndex).maxSubAttrs);

        /// <summary>槽位是否已解锁（解锁数 &gt; slotIndex）。</summary>
        public bool IsSubAttrUnlocked(int forgeIndex, int slotIndex)
        {
            ValidateSubAttrSlot(forgeIndex, slotIndex);
            return UnlockedCountOf(ForgeCardAt(forgeIndex).personaId) > slotIndex;
        }

        /// <summary>槽位状态文案：已解锁 → 池内行效果描述（「基础筹码 +8」）；未解锁 →「未解锁」；池行缺失 →「未解锁」。</summary>
        public string SubAttrStatusText(int forgeIndex, int slotIndex)
        {
            ValidateSubAttrSlot(forgeIndex, slotIndex);
            if (!IsSubAttrUnlocked(forgeIndex, slotIndex)) return "未解锁";
            var sub = PersonaForgeCatalog.SubAttrAt(ForgeCardAt(forgeIndex), slotIndex);
            return sub != null ? PersonaShopText.EffectTextOf(sub.attrType, sub.param1, sub.param2) : "未解锁";
        }

        /// <summary>槽位解锁节点文案：「解锁节点：第一章 · 已到达」；池行缺失返回空串。</summary>
        public string SubAttrNodeText(int forgeIndex, int slotIndex)
        {
            ValidateSubAttrSlot(forgeIndex, slotIndex);
            var sub = PersonaForgeCatalog.SubAttrAt(ForgeCardAt(forgeIndex), slotIndex);
            if (sub == null) return "";
            var rank = PersonaShopText.UnlockRankOf(sub.unlockNode);
            var reached = PersonaShopText.IsNodeReached(sub.unlockNode, _generationNodeCount);
            return $"解锁节点：{rank} · {(reached ? "已到达" : "未到达")}";
        }

        /// <summary>可解锁：未解锁、顺序到位（已解锁数 == slotIndex）、价格行存在、金币足够。</summary>
        public bool CanUnlockSubAttr(int forgeIndex, int slotIndex)
        {
            if (!IsSubAttrSlotValid(forgeIndex, slotIndex)) return false;
            if (IsSubAttrUnlocked(forgeIndex, slotIndex)) return false;
            if (UnlockedCountOf(ForgeCardAt(forgeIndex).personaId) != slotIndex) return false;
            var price = PersonaForgeCatalog.ForgePriceAt(slotIndex);
            return price >= 0 && Coins >= price;
        }

        /// <summary>解锁按钮文案：「解锁 · 5金币」/「金币不足」/「已解锁」/「未解锁」（顺序未到时）。</summary>
        public string UnlockButtonText(int forgeIndex, int slotIndex)
        {
            ValidateSubAttrSlot(forgeIndex, slotIndex);
            if (IsSubAttrUnlocked(forgeIndex, slotIndex)) return "已解锁";
            if (UnlockedCountOf(ForgeCardAt(forgeIndex).personaId) != slotIndex) return "未解锁";
            var price = PersonaForgeCatalog.ForgePriceAt(slotIndex);
            if (price < 0) return "未解锁";
            return Coins >= price ? $"解锁 · {price}金币" : "金币不足";
        }

        /// <summary>尝试解锁副属性槽位：顺序钳制 + 上限钳制 + 真实扣款（全部委托 ForgeUnlockState.TryUnlock）；失败无副作用。</summary>
        public bool TryUnlockSubAttr(int forgeIndex, int slotIndex)
        {
            if (!CanUnlockSubAttr(forgeIndex, slotIndex)) return false;
            var card = ForgeCardAt(forgeIndex);
            var price = PersonaForgeCatalog.ForgePriceAt(slotIndex);
            return _unlocks.TryUnlock(card.personaId, Math.Max(0, card.maxSubAttrs), price, _deck);
        }

        // ---------- 内部 ----------

        private int UnlockedCountOf(string personaId) => _unlocks.UnlockedCountOf(personaId);

        private ShopState.ShopSlot ProductSlotAt(int rowIndex)
        {
            // 商品位访问器只认槽 0~3（服务槽 4+ 走 ServiceSlotAt）；槽位数不足时同样越界
            if (rowIndex < 0 || rowIndex >= ProductRowCount || rowIndex >= _shop.Slots.Count)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            return _shop.Slots[rowIndex];
        }

        private ShopState.ShopSlot ServiceSlotAt(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= ServiceRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            return _shop.Slots[ProductRowCount + rowIndex];
        }

        private bool IsSubAttrSlotValid(int forgeIndex, int slotIndex)
        {
            return forgeIndex >= 0 && forgeIndex < ForgeCount &&
                slotIndex >= 0 && slotIndex < SubAttrSlotCount(forgeIndex);
        }

        private void ValidateSubAttrSlot(int forgeIndex, int slotIndex)
        {
            if (forgeIndex < 0 || forgeIndex >= ForgeCount)
                throw new ArgumentOutOfRangeException(nameof(forgeIndex));
            if (slotIndex < 0 || slotIndex >= SubAttrSlotCount(forgeIndex))
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}
