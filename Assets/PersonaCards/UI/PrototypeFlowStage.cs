namespace PersonaCards.UI
{
    public enum PrototypeFlowStage
    {
        MainMenu,
        PersonaSetup,
        Battle,
        Reward,
        Shop,
        BossReveal,
        RunReport,
        PersonaForge,
        FailureResult,
        /// <summary>人格牌生成节点（中局铸牌）：复用铸牌界面，确认获得后直接推进到下一节点，不回主菜单。追加在枚举末尾，旧存档 stage 值不变。</summary>
        PersonaGen
    }
}
