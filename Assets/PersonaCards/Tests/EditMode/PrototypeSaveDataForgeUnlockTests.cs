using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// PrototypeSaveData.forgeUnlocks 存档出入测试（UI 重排第二批）：schemaVersion 保持 3、
    /// JsonUtility 序列化往返、旧档缺字段容忍（实测为空列表而非 null，断言容忍两种）、行级防御。
    /// </summary>
    public sealed class PrototypeSaveDataForgeUnlockTests
    {
        private static JourneyDeckState Deck(int coins)
        {
            return new JourneyDeckState(
                new[] { new PlayingCardInstance("c1", Suit.Hearts, Rank.Five) }, coins);
        }

        [Test]
        public void Save_空状态_返回空列表()
        {
            var rows = ForgeUnlockSaveCodec.Save(new ForgeUnlockState());
            Assert.That(rows, Is.Not.Null);
            Assert.That(rows.Count, Is.EqualTo(0));
        }

        [Test]
        public void Save_null状态_抛异常()
        {
            Assert.Throws<System.ArgumentNullException>(() => ForgeUnlockSaveCodec.Save(null));
        }

        [Test]
        public void Save_RoundTrip_解锁数保留()
        {
            var state = new ForgeUnlockState();
            var deck = Deck(20);
            state.TryUnlock("PER_001", 2, 5, deck);
            state.TryUnlock("PER_001", 2, 8, deck);
            state.TryUnlock("PER_005", 2, 5, deck);

            var rows = ForgeUnlockSaveCodec.Save(state);
            Assert.That(rows.Count, Is.EqualTo(2));
            var restored = ForgeUnlockSaveCodec.Restore(rows);
            Assert.That(restored.UnlockedCountOf("PER_001"), Is.EqualTo(2));
            Assert.That(restored.UnlockedCountOf("PER_005"), Is.EqualTo(1));
            Assert.That(restored.UnlockedCountOf("PER_002"), Is.EqualTo(0));
        }

        [Test]
        public void Restore_null_返回全未解锁状态()
        {
            var state = ForgeUnlockSaveCodec.Restore(null);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(0));
        }

        [Test]
        public void Restore_防御行_空占位与空id跳过()
        {
            var rows = new List<SavedForgeUnlock>
            {
                new SavedForgeUnlock { isEmpty = true, personaId = "PER_001", unlockedCount = 1 },
                new SavedForgeUnlock { isEmpty = false, personaId = "", unlockedCount = 1 },
                new SavedForgeUnlock { isEmpty = false, personaId = "PER_003", unlockedCount = 2 },
                null
            };
            var state = ForgeUnlockSaveCodec.Restore(rows);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(0));
            Assert.That(state.UnlockedCountOf("PER_003"), Is.EqualTo(2));
        }

        [Test]
        public void Restore_计数下限1钳制()
        {
            var rows = new List<SavedForgeUnlock>
            {
                new SavedForgeUnlock { isEmpty = false, personaId = "PER_001", unlockedCount = 0 }
            };
            var state = ForgeUnlockSaveCodec.Restore(rows);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(1));
        }

        [Test]
        public void JsonUtility_序列化往返_forgeUnlocks保留()
        {
            var data = new PrototypeSaveData { hasActiveRun = true };
            data.forgeUnlocks.Add(new SavedForgeUnlock { isEmpty = false, personaId = "PER_001", unlockedCount = 1 });
            data.forgeUnlocks.Add(new SavedForgeUnlock { isEmpty = false, personaId = "PER_008", unlockedCount = 2 });

            var json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<PrototypeSaveData>(json);
            Assert.That(restored.forgeUnlocks.Count, Is.EqualTo(2));
            Assert.That(restored.forgeUnlocks[0].personaId, Is.EqualTo("PER_001"));
            Assert.That(restored.forgeUnlocks[1].unlockedCount, Is.EqualTo(2));
        }

        [Test]
        public void JsonUtility_旧档缺字段_读回空或空列表_schemaVersion保持3()
        {
            // 旧档 JSON 无 forgeUnlocks 字段：实测读回为空列表（Unity 6），断言容忍 null 与空列表两种
            var json = "{\"schemaVersion\":3,\"hasActiveRun\":true,\"deck\":[],\"collection\":[],\"equipped\":[]}";
            var restored = JsonUtility.FromJson<PrototypeSaveData>(json);
            Assert.That(restored.schemaVersion, Is.EqualTo(3));
            Assert.That(restored.forgeUnlocks == null || restored.forgeUnlocks.Count == 0, Is.True);
            var state = ForgeUnlockSaveCodec.Restore(restored.forgeUnlocks);
            Assert.That(state.UnlockedCountOf("PER_001"), Is.EqualTo(0));
        }
    }
}
