namespace PersonaCards.Cards
{
    /// <summary>
    /// 卡牌参数类型（P0-1D 数据驱动）：配表「参数类型」列的枚举化。
    /// 当前仅「筹码」（参数1 = 牌面筹码值，PlayingCardRules.GetFaceChipValue 消费）；
    /// 将来策划新增参数类型（如「倍率」）时在此追加，并同步契约校验与门面查询逻辑。
    /// </summary>
    public enum CardParamType
    {
        Chips = 1
    }
}
