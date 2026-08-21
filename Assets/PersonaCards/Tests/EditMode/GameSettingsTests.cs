using NUnit.Framework;
using PersonaCards.UI;
using UnityEngine.InputSystem;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// GameSettings 门面测试（P0-1H）：
    /// 默认值即拍板默认（亮度 0.8/动效开/震动开/音量 0.8/Space/D/Escape）；
    /// TryApply 全量校验（范围/枚举/键位互异）整体替换，非法拒绝保持旧状态；
    /// [TearDown] ApplyDefault 防静态状态泄漏（与 GlobalConfigTests 同模式）。
    /// </summary>
    public class GameSettingsTests
    {
        [TearDown]
        public void TearDown()
        {
            // 防静态状态泄漏到其他测试
            GameSettings.ApplyDefault();
        }

        [Test]
        public void DefaultsAreAsSpecified()
        {
            // 默认值与 P0-1H 拍板一致（12.7 表结构为空，默认值自定）
            Assert.That(GameSettings.Brightness, Is.EqualTo(0.8f));
            Assert.That(GameSettings.AnimationsEnabled, Is.True);
            Assert.That(GameSettings.ScreenShakeEnabled, Is.True);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.8f));
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.Space));
            Assert.That(GameSettings.DiscardKey, Is.EqualTo(Key.D));
            Assert.That(GameSettings.SettingsKey, Is.EqualTo(Key.Escape));
        }

        [Test]
        public void TryApplyValidDataUpdatesAllFields()
        {
            var data = new GameSettingsData
            {
                brightness = 0.5f,
                uiAnimation = false,
                screenShake = false,
                masterVolume = 0.2f,
                playKey = (int)Key.K,
                discardKey = (int)Key.X,
                settingsKey = (int)Key.Tab
            };

            Assert.That(GameSettings.TryApply(data), Is.True);

            Assert.That(GameSettings.Brightness, Is.EqualTo(0.5f));
            Assert.That(GameSettings.AnimationsEnabled, Is.False);
            Assert.That(GameSettings.ScreenShakeEnabled, Is.False);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.2f));
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.K));
            Assert.That(GameSettings.DiscardKey, Is.EqualTo(Key.X));
            Assert.That(GameSettings.SettingsKey, Is.EqualTo(Key.Tab));
        }

        [Test]
        public void ApplyRejectsBrightnessOutOfRangeAndKeepsOldState()
        {
            // 先写入一个合法状态作为「旧状态」
            var valid = new GameSettingsData { brightness = 0.5f };
            Assert.That(GameSettings.TryApply(valid), Is.True);

            // 三种非法亮度（下越界/上越界/NaN）逐一拒绝，且保持旧状态
            var below = new GameSettingsData { brightness = -0.01f };
            Assert.That(GameSettings.TryApply(below), Is.False);
            var above = new GameSettingsData { brightness = 1.01f };
            Assert.That(GameSettings.TryApply(above), Is.False);
            var nan = new GameSettingsData { brightness = float.NaN };
            Assert.That(GameSettings.TryApply(nan), Is.False);

            Assert.That(GameSettings.Brightness, Is.EqualTo(0.5f));
        }

        [Test]
        public void ApplyRejectsVolumeOutOfRangeAndKeepsOldState()
        {
            var valid = new GameSettingsData { masterVolume = 0.3f };
            Assert.That(GameSettings.TryApply(valid), Is.True);

            Assert.That(GameSettings.TryApply(new GameSettingsData { masterVolume = -0.01f }), Is.False);
            Assert.That(GameSettings.TryApply(new GameSettingsData { masterVolume = 1.01f }), Is.False);
            Assert.That(GameSettings.TryApply(new GameSettingsData { masterVolume = float.NaN }), Is.False);

            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.3f));
        }

        [Test]
        public void ApplyRejectsUndefinedKeyValue()
        {
            var valid = new GameSettingsData { playKey = (int)Key.K };
            Assert.That(GameSettings.TryApply(valid), Is.True);

            // 未定义枚举值（int 9999 不是任何 Key）拒绝且保持旧状态
            Assert.That(GameSettings.TryApply(new GameSettingsData { playKey = 9999 }), Is.False);
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.K));
        }

        [Test]
        public void ApplyRejectsKeyNone()
        {
            Assert.That(GameSettings.TryApply(new GameSettingsData { playKey = (int)Key.None }), Is.False);
            Assert.That(GameSettings.TryApply(new GameSettingsData { discardKey = (int)Key.None }), Is.False);
            Assert.That(GameSettings.TryApply(new GameSettingsData { settingsKey = (int)Key.None }), Is.False);

            // 全部拒绝：仍是默认键位
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.Space));
        }

        [Test]
        public void ApplyRejectsDuplicateKeys()
        {
            var valid = new GameSettingsData { playKey = (int)Key.K };
            Assert.That(GameSettings.TryApply(valid), Is.True);

            // 出牌=弃牌 / 出牌=设置 均拒绝（按键语义歧义）
            var sameAsDiscard = new GameSettingsData { playKey = (int)Key.K, discardKey = (int)Key.K };
            Assert.That(GameSettings.TryApply(sameAsDiscard), Is.False);
            var sameAsSettings = new GameSettingsData { playKey = (int)Key.K, settingsKey = (int)Key.K };
            Assert.That(GameSettings.TryApply(sameAsSettings), Is.False);

            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.K));
            Assert.That(GameSettings.DiscardKey, Is.EqualTo(Key.D));
        }

        [Test]
        public void ApplyRejectsNull()
        {
            Assert.That(GameSettings.TryApply(null), Is.False);
            Assert.That(GameSettings.Brightness, Is.EqualTo(0.8f)); // 状态未动
        }

        [Test]
        public void ApplyDefaultRestoresSpec()
        {
            var custom = new GameSettingsData
            {
                brightness = 0.1f,
                uiAnimation = false,
                screenShake = false,
                masterVolume = 0.1f,
                playKey = (int)Key.Z
            };
            Assert.That(GameSettings.TryApply(custom), Is.True);
            Assert.That(GameSettings.Brightness, Is.EqualTo(0.1f));

            // 「恢复默认」目标 = 拍板默认
            GameSettings.ApplyDefault();

            Assert.That(GameSettings.Brightness, Is.EqualTo(0.8f));
            Assert.That(GameSettings.AnimationsEnabled, Is.True);
            Assert.That(GameSettings.ScreenShakeEnabled, Is.True);
            Assert.That(GameSettings.MasterVolume, Is.EqualTo(0.8f));
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.Space));
            Assert.That(GameSettings.DiscardKey, Is.EqualTo(Key.D));
            Assert.That(GameSettings.SettingsKey, Is.EqualTo(Key.Escape));
        }

        [Test]
        public void SnapshotRestoreWorkflow()
        {
            // 设置界面的取消语义：进入时快照 → 修改 → 回滚快照
            var snapshot = GameSettings.Default();
            Assert.That(GameSettings.TryApply(new GameSettingsData { brightness = 0.2f, screenShake = false }), Is.True);
            Assert.That(GameSettings.Brightness, Is.EqualTo(0.2f));

            Assert.That(GameSettings.TryApply(snapshot), Is.True);

            Assert.That(GameSettings.Brightness, Is.EqualTo(0.8f));
            Assert.That(GameSettings.ScreenShakeEnabled, Is.True);
        }

        [Test]
        public void KeyPropertiesParseEnumFromFields()
        {
            var data = new GameSettingsData
            {
                playKey = (int)Key.Enter,
                discardKey = (int)Key.Backspace,
                settingsKey = (int)Key.F1
            };
            Assert.That(GameSettings.TryApply(data), Is.True);

            // 便捷属性把 int 字段解析回 Key 枚举（快捷键 Update 用 wasPressedThisFrame 读键）
            Assert.That(GameSettings.PlayKey, Is.EqualTo(Key.Enter));
            Assert.That(GameSettings.DiscardKey, Is.EqualTo(Key.Backspace));
            Assert.That(GameSettings.SettingsKey, Is.EqualTo(Key.F1));
        }
    }
}
