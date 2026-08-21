using UnityEngine.InputSystem;

namespace PersonaCards.UI
{
    /// <summary>
    /// 设置门面（模式同 GlobalConfig）：唯一生效状态 + 全量校验的整体替换。
    /// 实际应用副作用（AudioListener.volume、dim 层透明度、画面震动）由 PrototypeFlowController 读取后执行，
    /// 本类保持纯数据逻辑（EditMode 可测，不依赖场景）。
    /// </summary>
    public static class GameSettings
    {
        /// <summary>当前生效的设置。引用只读；字段修改统一走 TryApply / ApplyDefault（校验后才整体替换）。</summary>
        public static GameSettingsData Current { get; } = new GameSettingsData();

        /// <summary>「恢复默认」的目标数据：字段默认值即拍板默认（见 GameSettingsData 注释）。</summary>
        public static GameSettingsData Default() => new GameSettingsData();

        /// <summary>整体替换当前设置。任一项非法（范围/枚举/键位互异）则拒绝并保持旧状态，返回 false。</summary>
        public static bool TryApply(GameSettingsData data)
        {
            if (data == null) return false;

            // 亮度与主音量必须是 0~1 的有限值（NaN 与无穷的比较均为 false，天然被拒）
            if (!IsUnitInterval(data.brightness)) return false;
            if (!IsUnitInterval(data.masterVolume)) return false;

            // 三键必须是已定义的非 None 键，且互不重复（重复会导致按键语义歧义）
            var play = (Key)data.playKey;
            var discard = (Key)data.discardKey;
            var settings = (Key)data.settingsKey;
            if (!IsValidKey(play) || !IsValidKey(discard) || !IsValidKey(settings)) return false;
            if (play == discard || play == settings || discard == settings) return false;

            // 校验全部通过：整体替换，保证状态不会出现「改一半」的中间态
            Current.schemaVersion = data.schemaVersion;
            Current.brightness = data.brightness;
            Current.uiAnimation = data.uiAnimation;
            Current.screenShake = data.screenShake;
            Current.masterVolume = data.masterVolume;
            Current.playKey = data.playKey;
            Current.discardKey = data.discardKey;
            Current.settingsKey = data.settingsKey;
            return true;
        }

        /// <summary>恢复默认：Apply 默认数据（恒成功，12.6「恢复默认覆盖本地设置」由调用方负责立即落盘）。</summary>
        public static void ApplyDefault() => TryApply(Default());

        // —— 便捷只读属性：消费方（FlowController / BattlePrototypeController）经此读取，不直接摸 Current ——

        /// <summary>画面亮度 0~1。</summary>
        public static float Brightness => Current.brightness;

        /// <summary>主音量 0~1。</summary>
        public static float MasterVolume => Current.masterVolume;

        /// <summary>界面动效是否开启（动效统一归口：出牌/结算时长，未来手牌与画面动效同源）。</summary>
        public static bool AnimationsEnabled => Current.uiAnimation;

        /// <summary>屏幕震动是否开启。</summary>
        public static bool ScreenShakeEnabled => Current.screenShake;

        /// <summary>出牌快捷键（Key 枚举）。</summary>
        public static Key PlayKey => (Key)Current.playKey;

        /// <summary>弃牌快捷键（Key 枚举）。</summary>
        public static Key DiscardKey => (Key)Current.discardKey;

        /// <summary>打开/关闭设置界面的快捷键（Key 枚举）。</summary>
        public static Key SettingsKey => (Key)Current.settingsKey;

        /// <summary>0~1 区间校验：NaN/Infinity 与任何比较均为 false，无需单独判浮点合法性。</summary>
        private static bool IsUnitInterval(float value) => value >= 0f && value <= 1f;

        /// <summary>键位合法：已定义的枚举值且非 None（None 表示未绑定，快捷键无意义）。</summary>
        private static bool IsValidKey(Key key) => key != Key.None && System.Enum.IsDefined(typeof(Key), key);
    }
}
