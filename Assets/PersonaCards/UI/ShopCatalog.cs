using System;
using System.Collections.Generic;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店配置静态门面（P0-7）：状态机与控制器只通过这里读商店配置。
    /// 数据来自 P0-1J 资产化的 3 个资产（ShopProduct/ShopPoolRefresh/ShopSlotRefresh，ShopForge 归铸牌口径另行接线）；
    /// 资产缺失或校验失败时由控制器回落 Configure(null, null, null)（= 空列表，商店位全部「无货」），保证任何情况下流程可跑。
    /// 条目列表由控制器从资产 entries 提取（与 GlobalConfig.Configure 同惯例）。
    /// </summary>
    public static class ShopCatalog
    {
        private static IReadOnlyList<ShopProductEntry> _products = Array.Empty<ShopProductEntry>();
        private static IReadOnlyList<ShopPoolRefreshEntry> _poolRules = Array.Empty<ShopPoolRefreshEntry>();
        private static IReadOnlyList<ShopSlotRefreshEntry> _slotRules = Array.Empty<ShopSlotRefreshEntry>();

        /// <summary>当前生效的商品条目列表（资产注入或空）。</summary>
        public static IReadOnlyList<ShopProductEntry> Products => _products;

        /// <summary>当前生效的商品刷新规则列表（资产注入或空）。</summary>
        public static IReadOnlyList<ShopPoolRefreshEntry> PoolRules => _poolRules;

        /// <summary>当前生效的槽位刷新规则列表（资产注入或空）。</summary>
        public static IReadOnlyList<ShopSlotRefreshEntry> SlotRules => _slotRules;

        /// <summary>由控制器 Awake 注入三个商店条目列表；null 表示回落空配置（静默执行，商店位「无货」）。</summary>
        public static void Configure(IReadOnlyList<ShopProductEntry> products,
            IReadOnlyList<ShopPoolRefreshEntry> poolRules, IReadOnlyList<ShopSlotRefreshEntry> slotRules)
        {
            _products = products ?? Array.Empty<ShopProductEntry>();
            _poolRules = poolRules ?? Array.Empty<ShopPoolRefreshEntry>();
            _slotRules = slotRules ?? Array.Empty<ShopSlotRefreshEntry>();
        }
    }
}
