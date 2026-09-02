namespace PersonaCards.Core
{
    /// <summary>
    /// 牌型品质（P0-1J 新增）：来自配表「牌型品质定义表」（NORMAL=普通/1、RARE=稀有/2）。
    /// 品质判定（词条 ENTRY_003/008「打出牌型，品质为普通/稀有」）在运行时按此枚举比对；
    /// 枚举值 = 品质等级，新品质只追加不重排（资产按 int 序列化）。
    /// </summary>
    public enum HandQuality
    {
        NORMAL = 1,
        RARE = 2
    }
}
