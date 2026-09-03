namespace PersonaCards.Cards
{
    /// <summary>
    /// 卡牌增强。旧值 0~3 顺序与语义永久不变（存档兼容：SavedPlayingCard.enhancement 按 int 序列化），
    /// 新值 4~7 为 UI 重排第二批商店单卡强化服务（SHOP_SERVICE_001~004），只增不改。
    /// </summary>
    public enum CardEnhancement
    {
        None = 0,
        ChipBoost = 1,
        MultBoost = 2,
        WildSuit = 3,
        /// <summary>筹码强化（商店服务）：计分 +5 筹码。</summary>
        ChipPlus = 4,
        /// <summary>倍率强化（商店服务）：计分 +0.5 倍率。</summary>
        MultPlus = 5,
        /// <summary>金币强化（商店服务）：计分无效果，胜利结算按牌库张数 ×2 入账（见 JourneyDeckState.CoinBonusIncome）。</summary>
        CoinBonus = 6,
        /// <summary>独立乘区强化（商店服务）：最终得分 ×1.03（每张独立乘区牌叠乘）。</summary>
        IndependentMult = 7
    }
}
