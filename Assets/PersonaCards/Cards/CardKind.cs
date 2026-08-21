namespace PersonaCards.Cards
{
    /// <summary>
    /// 卡牌类型（P0-1D 数据驱动）：配表「卡牌类型」列的枚举化。
    /// 当前仅「手牌」（标准 52 张）；将来策划新增特殊牌类型时在此追加，
    /// 并同步 CardConfigTableContract 的类型校验与 CardConfigTableMapper 的映射。
    /// </summary>
    public enum CardKind
    {
        Hand = 1
    }
}
