namespace PersonaCards.UI
{
    /// <summary>
    /// 列表型强化界面会话抽象（UI 重排第二批）：牌型强化 / 人格主词条强化两个界面同构，
    /// 共用 EnhanceListPanelView 渲染；实现类为纯逻辑（可单测），视图只读文案与状态。
    /// 初始未选中（SelectedIndex = -1）：确认按钮禁用（网页版「未选禁用」）。
    /// </summary>
    public interface IEnhanceListSession
    {
        /// <summary>大标题（如「牌型强化」）。</summary>
        string Title { get; }

        /// <summary>标题下说明文案（小字）。</summary>
        string Description { get; }

        /// <summary>底部左侧提示（如「请选择目标」；无提示返回空串，此时左侧显示价格）。</summary>
        string Hint { get; }

        /// <summary>候选目标数。</summary>
        int Count { get; }

        /// <summary>当前选中索引；-1 = 未选择。</summary>
        int SelectedIndex { get; }

        /// <summary>选中候选；越界/负数忽略（不支持反选，只能换选）。</summary>
        void Select(int index);

        /// <summary>候选名称（亮金大字）。</summary>
        string NameText(int index);

        /// <summary>候选细节文案（暗金小字）。</summary>
        string DetailText(int index);

        /// <summary>右侧等级文案（如「Lv.0」）。</summary>
        string LevelText(int index);

        /// <summary>左下价格文案（如「本次价格：8 金币」）；未选中（index &lt; 0）给占位。</summary>
        string PriceText(int index);

        /// <summary>确认按钮可用（已选中即真；金币不足在 TryConfirm 时拒绝）。</summary>
        bool CanConfirm { get; }

        /// <summary>确认购买：按真实价扣款 + 升级；金币不足/失败无副作用。</summary>
        bool TryConfirm(JourneyDeckState deck);
    }
}
