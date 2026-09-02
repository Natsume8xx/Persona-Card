namespace PersonaCards.Core
{
    /// <summary>
    /// 牌型枚举（判定强度来源）：枚举值 = 判定强度 Priority，人格牌"最低牌型"条件依赖此序。
    /// P0-1C 起搬移至 Core（无依赖的叶子程序集）：Data 层配置资产（HandTypeAsset 等）需要引用牌型枚举，
    /// 而 Data 不能反向引用 Cards（会形成循环依赖）。枚举值与语义与搬移前完全一致，
    /// 旧存档与旧配置均按整数值序列化，切勿改序或插值。
    /// </summary>
    public enum HandType
    {
        HighCard = 1,
        Pair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraightFlush = 9,
        FiveOfAKind = 10,
        FlushHouse = 11,
        FlushFive = 12,
        // P0-1J：皇家同花顺（配表 HAND_11），末尾追加不改旧值（旧存档按 int 序列化）
        RoyalFlush = 13
    }
}
