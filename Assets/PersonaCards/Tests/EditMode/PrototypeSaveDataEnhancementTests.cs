using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Core;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 三线强化存档层测试（P0-11）：3 列表 round-trip、空列表、旧档缺字段 null-guard、占位行跳过、等级钳制。
    /// schemaVersion 保持 3：旧档缺字段 → JsonUtility 不跑字段初始化器 → 列表为 null，EnhancementSaveCodec.Restore 兜底全 0 级。
    /// </summary>
    public sealed class PrototypeSaveDataEnhancementTests
    {
        [Test]
        public void RoundTripPreservesPersonaSuitHandLevels()
        {
            var data = new PrototypeSaveData
            {
                personaLevels = new List<SavedPersonaLevel>
                {
                    new SavedPersonaLevel { isEmpty = false, templateId = "PER_001", level = 2 },
                    new SavedPersonaLevel { isEmpty = false, templateId = "PER_004", level = 4 }
                },
                suitLevels = new List<SavedSuitLevel>
                {
                    new SavedSuitLevel { isEmpty = false, suit = (int)Suit.Spades, level = 3 },
                    new SavedSuitLevel { isEmpty = false, suit = (int)Suit.Hearts, level = 1 }
                },
                handLevels = new List<SavedHandLevel>
                {
                    new SavedHandLevel { isEmpty = false, handType = (int)HandType.RoyalFlush, level = 2 },
                    new SavedHandLevel { isEmpty = false, handType = (int)HandType.Flush, level = 4 }
                }
            };

            var restored = JsonUtility.FromJson<PrototypeSaveData>(JsonUtility.ToJson(data));

            Assert.That(restored.schemaVersion, Is.EqualTo(3)); // P0-11 不升 schema
            Assert.That(restored.personaLevels, Has.Count.EqualTo(2));
            Assert.That(restored.personaLevels[0].templateId, Is.EqualTo("PER_001"));
            Assert.That(restored.personaLevels[0].level, Is.EqualTo(2));
            Assert.That(restored.personaLevels[1].templateId, Is.EqualTo("PER_004"));
            Assert.That(restored.personaLevels[1].level, Is.EqualTo(4));
            Assert.That(restored.suitLevels, Has.Count.EqualTo(2));
            Assert.That(restored.suitLevels[0].suit, Is.EqualTo((int)Suit.Spades));
            Assert.That(restored.suitLevels[0].level, Is.EqualTo(3));
            Assert.That(restored.suitLevels[1].suit, Is.EqualTo((int)Suit.Hearts));
            Assert.That(restored.handLevels, Has.Count.EqualTo(2));
            Assert.That(restored.handLevels[0].handType, Is.EqualTo((int)HandType.RoyalFlush));
            Assert.That(restored.handLevels[1].handType, Is.EqualTo((int)HandType.Flush));
            Assert.That(restored.handLevels[1].level, Is.EqualTo(4));
        }

        [Test]
        public void RoundTripWithEmptyListsYieldsEmptyState()
        {
            // 空列表序列化后 FromJson 回来是 null（JsonUtility 省略空集合）或空列表——两种都须经 Restore 兜底为全 0 级
            var restored = JsonUtility.FromJson<PrototypeSaveData>(JsonUtility.ToJson(new PrototypeSaveData()));

            Assert.That(restored.personaLevels == null || restored.personaLevels.Count == 0, Is.True);
            Assert.That(restored.suitLevels == null || restored.suitLevels.Count == 0, Is.True);
            Assert.That(restored.handLevels == null || restored.handLevels.Count == 0, Is.True);

            var state = EnhancementSaveCodec.Restore(restored);
            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(0));
            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(0));
            Assert.That(state.HandLevelOf(HandType.RoyalFlush), Is.EqualTo(0));
            Assert.That(state.PersonaLevels, Is.Empty);
            Assert.That(state.SuitLevels, Is.Empty);
            Assert.That(state.HandLevels, Is.Empty);
        }

        [Test]
        public void NullLevelListsFromOldSavesRestoreAsEmpty()
        {
            // 手工构造旧档 JSON：完全不含三线强化字段 → FromJson 后 3 列表为 null 或空列表（引擎差异）→ Restore 全 0 级不崩溃
            const string oldSaveJson =
                "{\"schemaVersion\":3,\"hasActiveRun\":true,\"stage\":4,\"battleNumber\":3,\"runSeed\":123," +
                "\"coins\":25,\"selectedJourneyCardIndex\":-1,\"rewardClaimed\":false,\"deck\":[],\"collection\":[],\"equipped\":[]}";

            var data = JsonUtility.FromJson<PrototypeSaveData>(oldSaveJson);
            Assert.That(data.personaLevels == null || data.personaLevels.Count == 0, Is.True);
            Assert.That(data.suitLevels == null || data.suitLevels.Count == 0, Is.True);
            Assert.That(data.handLevels == null || data.handLevels.Count == 0, Is.True);
            Assert.That(data.coins, Is.EqualTo(25)); // 其余字段不受影响

            var state = EnhancementSaveCodec.Restore(data);
            Assert.That(state.PersonaLevels, Is.Empty);
            Assert.That(state.SuitLevels, Is.Empty);
            Assert.That(state.HandLevels, Is.Empty);
        }

        [Test]
        public void RestoreSkipsEmptyPlaceholderRowsAndNullEntries()
        {
            var data = new PrototypeSaveData
            {
                personaLevels = new List<SavedPersonaLevel>
                {
                    new SavedPersonaLevel { isEmpty = true, templateId = "PER_001", level = 3 }, // 空占位行跳过
                    null, // null 条目跳过
                    new SavedPersonaLevel { isEmpty = false, templateId = "", level = 3 }, // 空键跳过
                    new SavedPersonaLevel { isEmpty = false, templateId = "PER_002", level = 2 }
                },
                suitLevels = new List<SavedSuitLevel>
                {
                    new SavedSuitLevel { isEmpty = true, suit = (int)Suit.Spades, level = 3 },
                    null,
                    new SavedSuitLevel { isEmpty = false, suit = (int)Suit.Clubs, level = 4 }
                },
                handLevels = new List<SavedHandLevel>
                {
                    new SavedHandLevel { isEmpty = true, handType = (int)HandType.Flush, level = 3 },
                    null,
                    new SavedHandLevel { isEmpty = false, handType = (int)HandType.Pair, level = 1 }
                }
            };

            var state = EnhancementSaveCodec.Restore(data);

            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(0));
            Assert.That(state.PersonaLevelOf("PER_002"), Is.EqualTo(2));
            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(0));
            Assert.That(state.SuitLevelOf(Suit.Clubs), Is.EqualTo(4));
            Assert.That(state.HandLevelOf(HandType.Flush), Is.EqualTo(0));
            Assert.That(state.HandLevelOf(HandType.Pair), Is.EqualTo(1));
        }

        [Test]
        public void RestoreClampsOutOfRangeLevels()
        {
            var data = new PrototypeSaveData
            {
                personaLevels = new List<SavedPersonaLevel>
                {
                    new SavedPersonaLevel { isEmpty = false, templateId = "PER_001", level = 99 }, // → 4
                    new SavedPersonaLevel { isEmpty = false, templateId = "PER_002", level = -5 }  // → 0
                },
                suitLevels = new List<SavedSuitLevel>
                {
                    new SavedSuitLevel { isEmpty = false, suit = (int)Suit.Spades, level = 99 } // → 4
                },
                handLevels = new List<SavedHandLevel>
                {
                    new SavedHandLevel { isEmpty = false, handType = (int)HandType.Flush, level = -1 } // → 0
                }
            };

            var state = EnhancementSaveCodec.Restore(data);

            Assert.That(state.PersonaLevelOf("PER_001"), Is.EqualTo(EnhancementState.PersonaMaxLevel));
            Assert.That(state.PersonaLevelOf("PER_002"), Is.EqualTo(0));
            Assert.That(state.SuitLevelOf(Suit.Spades), Is.EqualTo(EnhancementState.SuitMaxLevel));
            Assert.That(state.HandLevelOf(HandType.Flush), Is.EqualTo(0));
        }
    }
}
