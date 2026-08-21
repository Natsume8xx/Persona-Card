namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 人格牌品质（配置数据面，P0-1E 新增）：基础/进阶/稀有/异质。
    /// 与运行时结算枚举（PersonaConditionKind/PersonaEffectKind）分离，供 PersonaCardTemplate 使用。
    /// 「异质」= 代策划确认 A1 拍板的统一名（配表旧写法「特殊」由 Data 层 Mapper 规范化）。
    /// 显式赋值从 1 起：防序列化（JsonUtility）把缺省值 0 误读为合法枚举。
    /// </summary>
    public enum PersonaQuality
    {
        /// <summary>基础。</summary>
        Basic = 1,

        /// <summary>进阶。</summary>
        Advanced = 2,

        /// <summary>稀有。</summary>
        Rare = 3,

        /// <summary>异质（A1 拍板统一名，旧写法「特殊」）。</summary>
        Mutant = 4
    }
}
