namespace PersonaCards.UI
{
    /// <summary>
    /// 战斗教程序列（P0-1G）：五步教学文案与步骤推进的纯数据逻辑，与策划案 11.3.1 五主题一一对应。
    /// 纯 C# 类（不依赖 UnityEngine），供 EditMode 测试与 PrototypeFlowController 共用。
    /// </summary>
    public sealed class TutorialSequence
    {
        /// <summary>教学总步数（策划 11.3.1 五步教学）。</summary>
        public const int StepCount = 5;

        /// <summary>各步标题（与 StepBodies 一一对应，顺序即播放顺序）。</summary>
        public static readonly string[] StepTitles =
        {
            "得分与目标",
            "手牌与选牌",
            "计分预览",
            "人格牌区",
            "首领协议区",
        };

        /// <summary>各步正文（与 11.3.1 五主题对应；第五步泛化不写死 Boss 名，代策划确认 C4）。</summary>
        public static readonly string[] StepBodies =
        {
            "左上角显示本场当前得分，右上角是本场目标分。在出牌次数用完之前达到目标分即获胜；用尽仍未达标则失败。",
            "手牌区选牌：点击要出的牌选中它（最多 5 张），再次点击可取消。点「出牌」打出选中的牌；点「弃牌」弃掉选中的牌并重抽。出牌与弃牌次数均有限。",
            "选牌时预览区会实时显示这手牌的牌型与预计得分。牌型越强（如顺子、同花、四条），得分越高。凑出大牌型是获胜的关键。",
            "人格牌在满足条件时自动触发，按槽位从左到右结算，为得分增加筹码或倍率。装备合适的人格能让手牌发挥更大价值。",
            "首领战斗会展示首领协议与介入事件，改变本场规则。阅读协议文本，围绕它调整出牌与弃牌策略。",
        };

        /// <summary>当前步骤：-1 = 未激活/已结束，0~StepCount-1 = 正在展示的教学步。</summary>
        public int CurrentStep { get; private set; } = -1;

        /// <summary>教学是否正在展示。</summary>
        public bool IsActive => CurrentStep >= 0;

        /// <summary>从第 0 步开始播放。</summary>
        public void Start() => CurrentStep = 0;

        /// <summary>推进到下一步：最后一步之后结束（CurrentStep = -1）；结束时再调保持结束（幂等）。</summary>
        public void Next() => CurrentStep = CurrentStep >= 0 && CurrentStep < StepCount - 1 ? CurrentStep + 1 : -1;

        /// <summary>跳过：直接结束。</summary>
        public void Skip() => CurrentStep = -1;

        /// <summary>取步骤标题：越界（-1 或 >= StepCount）返回空串，UI 需容忍。</summary>
        public static string GetTitle(int step) => step >= 0 && step < StepCount ? StepTitles[step] : string.Empty;

        /// <summary>取步骤正文：越界返回空串。</summary>
        public static string GetBody(int step) => step >= 0 && step < StepCount ? StepBodies[step] : string.Empty;

        /// <summary>
        /// 是否应自动播放：重播请求优先；未看过教学的玩家首次进战斗自动播放。
        /// 纯函数——PlayerPrefs 的读写由调用方（PrototypeFlowController）完成，测试无需依赖 PlayerPrefs。
        /// </summary>
        public static bool ShouldAutoPlay(bool replayRequested, bool hasSeen) => replayRequested || !hasSeen;
    }
}
