using System;
using System.IO;
using NUnit.Framework;
using PersonaCards.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// GameSettingsStore 测试（P0-1H）：
    /// 注入临时路径（不碰玩家 persistentDataPath）；Save/Load 往返；
    /// 缺失/损坏/schemaVersion 不符均返回 false（异常回落默认，12.6）；损坏文件备份 .damaged-时间戳。
    /// </summary>
    public class GameSettingsStoreTests
    {
        private string _directory;
        private GameSettingsStore _store;

        [SetUp]
        public void SetUp()
        {
            // 每个测试独立临时目录，防止用例间文件互相污染
            _directory = Path.Combine(Path.GetTempPath(), "PersonaCards.SettingsStore." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _store = new GameSettingsStore(Path.Combine(_directory, "settings.json"));
        }

        [TearDown]
        public void TearDown()
        {
            // 清空临时目录（含 damaged 备份）
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        public void SaveLoadRoundTripPreservesAllFields()
        {
            var data = new GameSettingsData
            {
                brightness = 0.35f,
                uiAnimation = false,
                screenShake = false,
                masterVolume = 0.15f,
                playKey = (int)Key.K,
                discardKey = (int)Key.X,
                settingsKey = (int)Key.Tab
            };

            Assert.That(_store.TrySave(data), Is.True);
            Assert.That(_store.Exists, Is.True);
            Assert.That(_store.TryLoad(out var loaded), Is.True);

            Assert.That(loaded.brightness, Is.EqualTo(0.35f));
            Assert.That(loaded.uiAnimation, Is.False);
            Assert.That(loaded.screenShake, Is.False);
            Assert.That(loaded.masterVolume, Is.EqualTo(0.15f));
            Assert.That(loaded.playKey, Is.EqualTo((int)Key.K));
            Assert.That(loaded.discardKey, Is.EqualTo((int)Key.X));
            Assert.That(loaded.settingsKey, Is.EqualTo((int)Key.Tab));
        }

        [Test]
        public void LoadMissingFileReturnsFalse()
        {
            // 首次启动：无存档文件 → false（调用方回落默认），不抛异常
            Assert.That(_store.TryLoad(out var data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void LoadCorruptJsonBacksUpDamagedFileAndReturnsFalse()
        {
            File.WriteAllText(_store.SavePath, "{ 这不是合法的 JSON");

            Assert.That(_store.TryLoad(out var data), Is.False);
            Assert.That(data, Is.Null);

            // 损坏文件已备份为 .damaged-时间戳，原文件仍在原位（可人工恢复）
            var damaged = Directory.GetFiles(_directory, "settings.json.damaged-*");
            Assert.That(damaged.Length, Is.EqualTo(1));
            Assert.That(File.Exists(_store.SavePath), Is.True);
        }

        [Test]
        public void LoadBadSchemaVersionReturnsFalse()
        {
            // 合法 JSON 但版本号与当前实现（1）不符 → 整体回落默认，不做迁移
            var future = new GameSettingsData { schemaVersion = 99 };
            File.WriteAllText(_store.SavePath, JsonUtility.ToJson(future));

            Assert.That(_store.TryLoad(out var data), Is.False);
            Assert.That(data, Is.Null);
        }

        [Test]
        public void SaveOverwritesExistingFile()
        {
            var first = new GameSettingsData { brightness = 0.1f };
            var second = new GameSettingsData { brightness = 0.9f };
            Assert.That(_store.TrySave(first), Is.True);
            Assert.That(_store.TrySave(second), Is.True);

            Assert.That(_store.TryLoad(out var loaded), Is.True);
            Assert.That(loaded.brightness, Is.EqualTo(0.9f));
        }

        [Test]
        public void SaveFailureReturnsFalseInsteadOfThrowing()
        {
            // 路径的父级被同名文件占用 → 建目录/写入失败 → 返回 false 不抛异常（保存失败不阻断游戏）
            var blocker = Path.Combine(_directory, "occupied");
            File.WriteAllText(blocker, "占位文件");
            var doomed = new GameSettingsStore(Path.Combine(blocker, "sub", "settings.json"));

            Assert.That(doomed.TrySave(new GameSettingsData()), Is.False);
        }
    }
}
