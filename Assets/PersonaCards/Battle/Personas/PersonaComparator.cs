namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌比较符（配置数据面，P0-1E 新增）：等于/大于等于/小于/小于等于。
    /// 显式赋值从 1 起：防序列化把缺省值 0 误读为合法枚举。
    /// </summary>
    public enum PersonaComparator
    {
        /// <summary>等于（=）。</summary>
        Equal = 1,

        /// <summary>大于等于（&gt;=）。</summary>
        GreaterOrEqual = 2,

        /// <summary>小于（&lt;）。</summary>
        Less = 3,

        /// <summary>小于等于（&lt;=）。</summary>
        LessOrEqual = 4
    }
}
