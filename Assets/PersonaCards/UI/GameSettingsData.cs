using System;
using UnityEngine.InputSystem;

namespace PersonaCards.UI
{
    /// <summary>
    /// 设置数据 POCO：JsonUtility 可序列化，字段即持久化格式（策划 12.7 表结构为空，schema 自定，见 Docs/KF/P0-1H.md）。
    /// 字段默认值即「恢复默认」的目标值（拍板：亮度 0.8 / 动效开 / 震动开 / 音量 0.8 / 出牌 Space / 弃牌 D / 设置 Escape）。
    /// </summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        /// <summary>存储格式版本：与当前实现（1）不匹配时读取方整体回落默认。</summary>
        public int schemaVersion = 1;

        /// <summary>画面亮度 0~1（1 = 最亮，全局 dim 层全透明）。</summary>
        public float brightness = 0.8f;

        /// <summary>界面动效总开关：当前控制出牌/结算展示时长，未来手牌与整体画面动效统一归口此处（用户 2026-08-21 确认动效系统后续加入）。</summary>
        public bool uiAnimation = true;

        /// <summary>屏幕震动：出牌结算时的画面抖动。</summary>
        public bool screenShake = true;

        /// <summary>主音量 0~1（作用于 AudioListener.volume；当前项目暂无音频文件，设置生效但无感）。</summary>
        public float masterVolume = 0.8f;

        /// <summary>出牌快捷键（Input System Key 枚举值）。</summary>
        public int playKey = (int)Key.Space;

        /// <summary>弃牌快捷键（Input System Key 枚举值）。</summary>
        public int discardKey = (int)Key.D;

        /// <summary>打开/关闭设置界面的快捷键（Input System Key 枚举值）。</summary>
        public int settingsKey = (int)Key.Escape;

        /// <summary>全字段拷贝（设置界面「取消」回滚的快照用；不共享引用，防快照被后续修改连带变更）。</summary>
        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                schemaVersion = schemaVersion,
                brightness = brightness,
                uiAnimation = uiAnimation,
                screenShake = screenShake,
                masterVolume = masterVolume,
                playKey = playKey,
                discardKey = discardKey,
                settingsKey = settingsKey
            };
        }
    }
}
