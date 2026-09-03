using System;
using System.Collections.Generic;
using PersonaCards.Cards;
using PersonaCards.Core.Random;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店运行时状态（P0-7）：纯 C# 类，无引擎引用，EditMode 可直接测试。
    /// 语义以策划案 10.6 为准：进入商店按当前节点（AI 分组）与商品池生成商品位；限购；购买校验；货币不足不生效。
    /// 商品抽取种子 = 局种子 + 节点序号 + 2000（与战斗 +1、铸牌 +1000 错开）；同节点同种子必得同商品，存档恢复可复现。
    /// 售罄态（限购 1 = 即买即售罄）不随存档走，读档后商店重置——售罄态入快照随 P0-8 存档 schema v4 落地。
    /// </summary>
    public sealed class ShopState
    {
        /// <summary>效果类型原文常量（配表「效果类型」列）：只引用本轮已接线效果。</summary>
        public const string EffectAddCard = "增加卡牌";
        public const string EffectRemoveCard = "移除卡牌";

        /// <summary>P0-11 三线强化服务效果类型（配表原文：强化人格 / 强化花色 / 强化牌型）。</summary>
        public const string EffectEnhancePersona = "强化人格";
        public const string EffectEnhanceSuit = "强化花色";
        public const string EffectEnhanceHand = "强化牌型";

        /// <summary>本轮已实现接线效果的白名单：增加卡牌 / 移除卡牌 / 三线强化。未实装效果不进商品位（策划案商品池按效果过滤）。
        /// 增加人格牌待「模板→运行时定义」转换（B7 行为→词条映射）落地后放开；强化类服务经 P0-11 三线强化接线。
        /// 强化服务商品（SHOP_SERVICE_006~008）能否上架还取决于强化配表注入（ShopCatalog 合成池规则时过滤）。</summary>
        private static readonly string[] ImplementedEffects =
            { EffectAddCard, EffectRemoveCard, EffectEnhancePersona, EffectEnhanceSuit, EffectEnhanceHand };

        /// <summary>是否为三线强化服务效果（P0-11）：购买走选择模式（目标按当前等级动态定价），不走普通商品购买流程。</summary>
        public static bool IsEnhancementEffect(string effectType)
        {
            return string.Equals(effectType, EffectEnhancePersona, StringComparison.Ordinal)
                || string.Equals(effectType, EffectEnhanceSuit, StringComparison.Ordinal)
                || string.Equals(effectType, EffectEnhanceHand, StringComparison.Ordinal);
        }

        /// <summary>槽位刷新节点分组（配表「商店刷新节点」列原文）。</summary>
        public const string NodeAi1 = "AI1";
        public const string NodeAi2 = "AI2";
        public const string NodeAi3 = "AI3";

        /// <summary>单个商品位：商品（可为 null = 无货）+ 售罄态（限购 1 = 即买即售罄）。</summary>
        public sealed class ShopSlot
        {
            public ShopSlot(ShopProductEntry product)
            {
                Product = product;
            }

            /// <summary>位内商品；null 表示该位无货（候选池为空或全被白名单过滤）。</summary>
            public ShopProductEntry Product { get; }
            public bool SoldOut { get; private set; }

            public void MarkSold() => SoldOut = true;
        }

        private readonly List<ShopSlot> _slots = new List<ShopSlot>();

        /// <summary>
        /// 生成商品位：按槽位刷新规则（分组 = 已过生成节点数映射，类型序 = 配表商品类型序 卡牌/人格牌/服务）逐个槽位
        /// 加权抽取商品；每槽位种子 = seed + 槽位序号（槽位间错开）。规则缺失的类型或抽取无候选的槽位 → 无货位。
        /// </summary>
        public ShopState(IEnumerable<ShopProductEntry> products, IEnumerable<ShopPoolRefreshEntry> poolRules,
            IEnumerable<ShopSlotRefreshEntry> slotRules, int generationNodeCount, uint seed)
        {
            var group = GroupNameOf(generationNodeCount);
            var slotIndex = 0;
            foreach (var productType in ShopProductTableContract.ProductTypes)
            {
                var count = SlotCountOf(slotRules, group, productType);
                for (var i = 0; i < count; i++)
                {
                    var slotSeed = unchecked(seed + (uint)slotIndex);
                    _slots.Add(new ShopSlot(PickProduct(products, poolRules, productType, slotSeed)));
                    slotIndex++;
                }
            }
        }

        public IReadOnlyList<ShopSlot> Slots => _slots.AsReadOnly();

        /// <summary>购买校验（策划案 10.6）：槽位存在、商品非空、未售罄、金币足够。通过才标记售罄并返回 true；任何一步不满足不生效。</summary>
        public bool TryPurchase(int slotIndex, int coins)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;
            var slot = _slots[slotIndex];
            if (slot.Product == null || slot.SoldOut) return false;
            if (coins < slot.Product.price) return false;
            slot.MarkSold();
            return true;
        }

        /// <summary>
        /// 仅标记售罄不校验余额（P0-11 强化服务专用）：真实扣款发生在目标选择确认时（价格按目标当前等级动态定价，
        /// 商品位价格仅为展示价），确认成功后此处只落限购 1 = 即买即售罄语义。
        /// </summary>
        public bool TryMarkSold(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return false;
            var slot = _slots[slotIndex];
            if (slot.Product == null || slot.SoldOut) return false;
            slot.MarkSold();
            return true;
        }

        /// <summary>
        /// AI 分组映射（临时口径，待策划确认）：已过生成节点数 0→AI1、1→AI2、≥2→AI3。
        /// 配表槽位刷新规则只有 AI1~AI3 三个分组；关卡 3 后的商店前已过 1 个生成节点 → AI2，关卡 11 后的商店 → AI3。
        /// </summary>
        public static string GroupNameOf(int generationNodeCount)
        {
            return generationNodeCount <= 0 ? NodeAi1 : generationNodeCount == 1 ? NodeAi2 : NodeAi3;
        }

        /// <summary>
        /// 加权抽取单件商品（静态纯函数）：类型过滤 → 效果白名单 → 商品池规则 join（池里没有的商品不上架）→ 权重滚动抽取。
        /// 无候选（池为空/全被过滤）返回 null。权重必须 ≥1（资产校验已保证，防御性回落 1）。
        /// </summary>
        public static ShopProductEntry PickProduct(IEnumerable<ShopProductEntry> products,
            IEnumerable<ShopPoolRefreshEntry> poolRules, string productType, uint seed)
        {
            // 类型 + 效果白名单过滤，按商品_ID 建候选映射
            var candidates = new Dictionary<string, ShopProductEntry>(StringComparer.Ordinal);
            if (products != null)
            {
                foreach (var product in products)
                {
                    if (product == null || !string.Equals(product.productType, productType, StringComparison.Ordinal)) continue;
                    if (Array.IndexOf(ImplementedEffects, product.effectType) < 0) continue;
                    candidates[product.productId] = product;
                }
            }
            if (candidates.Count == 0) return null;

            // 商品池 join：池规则里的商品_ID 必须在候选内，权重即抽取权重（同一商品多行视为加权重复，无副作用）
            var weighted = new List<KeyValuePair<ShopProductEntry, int>>();
            if (poolRules != null)
            {
                foreach (var rule in poolRules)
                {
                    if (rule == null || rule.weight < 1) continue;
                    if (candidates.TryGetValue(rule.productId, out var product))
                        weighted.Add(new KeyValuePair<ShopProductEntry, int>(product, rule.weight));
                }
            }
            if (weighted.Count == 0) return null;

            var totalWeight = 0;
            foreach (var pair in weighted) totalWeight += pair.Value;
            var rng = new XorShift32Rng(seed);
            var roll = rng.NextInt(totalWeight);
            foreach (var pair in weighted)
            {
                roll -= pair.Value;
                if (roll < 0) return pair.Key;
            }
            return weighted[weighted.Count - 1].Key; // 防御：浮点不可达路径
        }

        /// <summary>槽位数量（分组 + 类型匹配的规则行 count；无匹配行 → 0，即该类型不设位）。</summary>
        private static int SlotCountOf(IEnumerable<ShopSlotRefreshEntry> slotRules, string group, string productType)
        {
            var count = 0;
            if (slotRules == null) return 0;
            foreach (var rule in slotRules)
            {
                if (rule == null) continue;
                if (!string.Equals(rule.node, group, StringComparison.Ordinal)) continue;
                if (!string.Equals(rule.productType, productType, StringComparison.Ordinal)) continue;
                count += Math.Max(0, rule.count);
            }
            return count;
        }

        /// <summary>卡商品名解析（临时口径，待策划确认）：「黑桃A」→ 花色 + 点数。商品配置无 id 列，按商品名尾段解析；解析失败返回 false。</summary>
        public static bool TryParseCardName(string productName, out Suit suit, out Rank rank)
        {
            suit = Suit.Clubs;
            rank = Rank.Two;
            if (string.IsNullOrWhiteSpace(productName) || productName.Length < 2) return false;
            var suitText = productName.Substring(0, Math.Min(2, productName.Length));
            var rankText = productName.Substring(suitText.Length);
            switch (suitText)
            {
                case "黑桃": suit = Suit.Spades; break;
                case "红桃": suit = Suit.Hearts; break;
                case "梅花": suit = Suit.Clubs; break;
                case "方片": suit = Suit.Diamonds; break;
                default: return false;
            }
            switch (rankText)
            {
                case "A": rank = Rank.Ace; break;
                case "J": rank = Rank.Jack; break;
                case "Q": rank = Rank.Queen; break;
                case "K": rank = Rank.King; break;
                default:
                {
                    if (!int.TryParse(rankText, out var value) || value < 2 || value > 10) return false;
                    rank = (Rank)value;
                    break;
                }
            }
            return true;
        }
    }
}
