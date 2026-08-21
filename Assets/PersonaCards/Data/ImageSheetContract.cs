namespace PersonaCards.Data
{
    /// <summary>
    /// 「图片配置」sheet 共享契约（P0-1D 提取）：供卡牌/将来人格牌等导入命令读取绑定 ID 集合做对照校验。
    /// （HandTypeTableContract 已含同名常量、P0-1C 已完结不重构，新契约从此处引用。）
    /// </summary>
    public static class ImageSheetContract
    {
        /// <summary>工作表名（图片配置）。</summary>
        public const string SheetName = "图片配置";

        /// <summary>绑定 ID 列（各配置 sheet 的 ID 列值对照此列校验，如 CARD_xxx/HAND_xx/PER_xxx）。</summary>
        public const string ColBindingId = "绑定ID";
    }
}
