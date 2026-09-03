using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店配置静态门面（P0-7）：状态机与控制器只通过这里读商店配置。
    /// 数据来自 P0-1J 资产化的 3 个资产（ShopProduct/ShopPoolRefresh/ShopSlotRefresh，ShopForge 归铸牌口径另行接线）；
    /// 资产缺失或校验失败时由控制器回落 Configure(null, null, null)（= 空列表，商店位全部「无货」），保证任何情况下流程可跑。
    /// 条目列表由控制器从资产 entries 提取（与 GlobalConfig.Configure 同惯例）。
    /// P0-11：Configure 末尾为 3 个强化服务（SHOP_SERVICE_006~008）合成服务池规则（临时口径，策划补表后删除合成逻辑）——
    /// 合成条件 = 商品表有对应效果类型的服务 且 强化配表已注入（EnhancementConfig.HasTables，play build 无 Editor 注入则缺席）。
    /// </summary>
    public static class ShopCatalog
    {
        /// <summary>强化服务合成池权重：对齐服务池现有档位（20），与 POOL_SERVICE_001~005 同档竞争。</summary>
        public const int EnhancementSyntheticWeight = 20;

        private static IReadOnlyList<ShopProductEntry> _products = Array.Empty<ShopProductEntry>();
        private static IReadOnlyList<ShopPoolRefreshEntry> _poolRules = Array.Empty<ShopPoolRefreshEntry>();
        private static IReadOnlyList<ShopSlotRefreshEntry> _slotRules = Array.Empty<ShopSlotRefreshEntry>();

        /// <summary>当前生效的商品条目列表（资产注入或空）。</summary>
        public static IReadOnlyList<ShopProductEntry> Products => _products;

        /// <summary>当前生效的商品刷新规则列表（资产注入或空，含 P0-11 强化服务合成行）。</summary>
        public static IReadOnlyList<ShopPoolRefreshEntry> PoolRules => _poolRules;

        /// <summary>当前生效的槽位刷新规则列表（资产注入或空）。</summary>
        public static IReadOnlyList<ShopSlotRefreshEntry> SlotRules => _slotRules;

        /// <summary>由控制器 Awake 注入三个商店条目列表；null 表示回落空配置（静默执行，商店位「无货」）。
        /// 注入顺序依赖：必须先 EnhanceTableBootstrap.Load()（EnhancementConfig.HasTables）再调用本方法。</summary>
        public static void Configure(IReadOnlyList<ShopProductEntry> products,
            IReadOnlyList<ShopPoolRefreshEntry> poolRules, IReadOnlyList<ShopSlotRefreshEntry> slotRules)
        {
            _products = products ?? Array.Empty<ShopProductEntry>();
            _poolRules = MergeEnhancementPoolRules(poolRules ?? Array.Empty<ShopPoolRefreshEntry>());
            _slotRules = slotRules ?? Array.Empty<ShopSlotRefreshEntry>();
        }

        /// <summary>
        /// P0-11：合成强化服务池规则。商品表有对应效果类型的服务、强化配表已注入、且策划尚未补该商品的池规则时，
        /// 追加一条 {poolId=POOL_SERVICE_006~008, weight=20}；任一条件不满足不合成（强化服务不上架，功能缺席不崩溃）。
        /// </summary>
        private static IReadOnlyList<ShopPoolRefreshEntry> MergeEnhancementPoolRules(IReadOnlyList<ShopPoolRefreshEntry> poolRules)
        {
            if (!EnhancementConfig.HasTables) return poolRules;
            var merged = new List<ShopPoolRefreshEntry>(poolRules);
            var syntheticTargets = new[]
            {
                new KeyValuePair<string, string>(ShopState.EffectEnhancePersona, "POOL_SERVICE_006"),
                new KeyValuePair<string, string>(ShopState.EffectEnhanceSuit, "POOL_SERVICE_007"),
                new KeyValuePair<string, string>(ShopState.EffectEnhanceHand, "POOL_SERVICE_008")
            };
            foreach (var target in syntheticTargets)
            {
                ShopProductEntry product = null;
                foreach (var entry in _products)
                {
                    if (entry != null && string.Equals(entry.effectType, target.Key, StringComparison.Ordinal))
                    {
                        product = entry;
                        break;
                    }
                }
                if (product == null) continue; // 商品表缺该服务 → 不合成
                var alreadyConfigured = false;
                foreach (var rule in merged)
                {
                    if (rule != null && string.Equals(rule.productId, product.productId, StringComparison.Ordinal))
                    {
                        alreadyConfigured = true;
                        break;
                    }
                }
                if (alreadyConfigured) continue; // 策划已补池规则 → 不重复合成
                merged.Add(new ShopPoolRefreshEntry
                {
                    poolId = target.Value,
                    productId = product.productId,
                    weight = EnhancementSyntheticWeight
                });
            }
            return merged;
        }
    }
}
