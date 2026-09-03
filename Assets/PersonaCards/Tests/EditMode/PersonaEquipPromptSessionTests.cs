using System;
using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Core;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 获得新人格牌弹窗会话测试（UI 重排第一批）：默认选中空槽/全满回落、切换与越界、统计/类型/条件/效果/
    /// 提示条/确认按钮文案精确格式、ExecuteReplace 委托 EquipAt（替换与同卡互换）、构造不修改装备（拒绝路径守卫）、
    /// RuleTextOf/HandNameOf 与旧 ForgeRule/HandName 输出逐字一致（锁定委托零漂移）。
    /// </summary>
    public sealed class PersonaEquipPromptSessionTests
    {
        [Test]
        public void DefaultSelectionPicksFirstEmptySlot()
        {
            var session = Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null);

            Assert.That(session.SelectedSlotIndex, Is.EqualTo(3));
            Assert.That(session.IsTargetEmpty, Is.True);
            Assert.That(session.Replaced, Is.Null);
        }

        [Test]
        public void DefaultSelectionFallsBackToSlotZeroWhenFull()
        {
            var session = Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, Candidate());

            Assert.That(session.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(session.IsTargetEmpty, Is.False);
            Assert.That(session.Replaced, Is.SameAs(InitialPersonaCatalog.Accumulator));
        }

        [Test]
        public void DefaultSelectionPicksLowestIndexAmongMultipleEmpties()
        {
            var session = Session(null, InitialPersonaCatalog.Accumulator, null, InitialPersonaCatalog.Executor);

            Assert.That(session.SelectedSlotIndex, Is.EqualTo(0));
            Assert.That(session.IsTargetEmpty, Is.True);
        }

        [Test]
        public void SelectSlotUpdatesSelectionAndInvalidIndexThrows()
        {
            var session = Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null);

            session.SelectSlot(2);
            Assert.That(session.SelectedSlotIndex, Is.EqualTo(2));
            Assert.That(session.IsTargetEmpty, Is.False);
            Assert.That(session.Replaced, Is.SameAs(InitialPersonaCatalog.Ambitious));

            Assert.Throws<ArgumentOutOfRangeException>(() => session.SelectSlot(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.SelectSlot(4));
        }

        [Test]
        public void StatsTextUsesExactFormat()
        {
            var session = new PersonaEquipPromptSession(Candidate(), 5, new[]
            {
                InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null
            });

            Assert.That(session.StatsText, Is.EqualTo("本次获得 1张 / 当前持有 5张 / 当前装备 3/4"));
        }

        [Test]
        public void TypeTagTextDerivesModeFromDisplayName()
        {
            Assert.That(Session().TypeTagText, Is.EqualTo("类型·映照"));
        }

        [Test]
        public void ConditionTextVariants()
        {
            Assert.That(Session().ConditionText, Is.EqualTo("对子或更高"));

            var always = new PersonaCardDefinition("persona.test.always", "常驻·测试",
                PersonaConditionKind.Always, HandType.HighCard, PersonaEffectKind.AddChips, 5m);
            var session = new PersonaEquipPromptSession(always, 1, new[]
            {
                InitialPersonaCatalog.Accumulator, null, null, null
            });
            Assert.That(session.ConditionText, Is.EqualTo("始终生效"));
        }

        [Test]
        public void EffectTextVariants()
        {
            Assert.That(Session().EffectText, Is.EqualTo("+10 筹码"));

            var multiplier = new PersonaCardDefinition("persona.forge.偏转.调律者", "偏转·调律者",
                PersonaConditionKind.MinimumHandPriority, HandType.Pair, PersonaEffectKind.AddMultiplier, 1.5m);
            Assert.That(Session(multiplier).EffectText, Is.EqualTo("+1.5 倍率"));

            var final = new PersonaCardDefinition("persona.forge.裂变.破局者", "裂变·破局者",
                PersonaConditionKind.MinimumHandPriority, HandType.Pair, PersonaEffectKind.MultiplyFinal, 1.07m);
            Assert.That(Session(final).EffectText, Is.EqualTo("最终 ×1.07"));
        }

        [Test]
        public void BarTextReplaceVariant()
        {
            var session = Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, Candidate());
            session.SelectSlot(0);

            Assert.That(session.BarText, Is.EqualTo("映照·洞察者 → 槽位01，替换 积累者（旧牌保留）"));
        }

        [Test]
        public void BarTextEmptyVariant()
        {
            var session = Session();

            Assert.That(session.BarText, Is.EqualTo("映照·洞察者 → 槽位04，装备至空槽"));
        }

        [Test]
        public void ConfirmButtonTextVariants()
        {
            var empty = Session();
            Assert.That(empty.ConfirmButtonText, Is.EqualTo("装备至 槽位04 并继续"));

            var full = Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, Candidate());
            full.SelectSlot(1);
            Assert.That(full.ConfirmButtonText, Is.EqualTo("替换 执行者 并继续"));
        }

        [Test]
        public void SlotNameAndStatusTexts()
        {
            var session = Session();

            Assert.That(session.SlotNameText(0), Is.EqualTo("01  积累者"));
            Assert.That(session.SlotNameText(3), Is.EqualTo("04  空槽"));
            Assert.That(session.SlotStatusText(3), Is.EqualTo("装备至此")); // 默认选中空槽
            Assert.That(session.SlotStatusText(0), Is.EqualTo("选择"));

            session.SelectSlot(1);
            Assert.That(session.SlotStatusText(1), Is.EqualTo("将替换"));
            Assert.That(session.SlotStatusText(3), Is.EqualTo("选择"));
        }

        [Test]
        public void ExecuteReplaceEquipsCandidateAtSelectedSlot()
        {
            var loadout = new PersonaLoadoutState(new[]
            {
                InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null
            });
            var session = new PersonaEquipPromptSession(Candidate(), 4, loadout.Slots);

            Assert.That(session.ExecuteReplace(loadout), Is.EqualTo(3));
            Assert.That(loadout.Slots[3], Is.SameAs(session.Candidate));
        }

        [Test]
        public void ExecuteReplaceSameTemplateSwapsSlots()
        {
            var candidate = Candidate();
            var loadout = new PersonaLoadoutState(new[]
            {
                InitialPersonaCatalog.Accumulator, candidate,
                InitialPersonaCatalog.Ambitious, null
            });
            var session = new PersonaEquipPromptSession(candidate, 4, loadout.Slots);
            session.SelectSlot(3);

            Assert.That(session.ExecuteReplace(loadout), Is.EqualTo(3));
            Assert.That(loadout.Slots[3], Is.SameAs(candidate));
            Assert.That(loadout.Slots[1], Is.Null); // 同卡已在槽 1 → 两槽互换
        }

        [Test]
        public void ConstructionDoesNotMutateLoadout()
        {
            var loadout = new PersonaLoadoutState(new[]
            {
                InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null
            });
            var before = (IReadOnlyList<PersonaCardDefinition>)loadout.Slots;

            var session = new PersonaEquipPromptSession(Candidate(), 4, loadout.Slots);
            session.SelectSlot(2); // 只动会话，不动 loadout

            for (var i = 0; i < 4; i++)
                Assert.That(loadout.Slots[i], Is.SameAs(before[i]));
        }

        [Test]
        public void NullCandidateThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new PersonaEquipPromptSession(null, 1,
                new PersonaCardDefinition[] { null, null, null, null }));
        }

        [Test]
        public void RuleTextOfMatchesLegacyForgeRule()
        {
            var chips = Candidate(); // AddChips 10 → 「对子或更高：+10 筹码」
            var multiplier = new PersonaCardDefinition("persona.forge.偏转.调律者", "偏转·调律者",
                PersonaConditionKind.MinimumHandPriority, HandType.Pair, PersonaEffectKind.AddMultiplier, 1.5m);
            var final = new PersonaCardDefinition("persona.forge.裂变.破局者", "裂变·破局者",
                PersonaConditionKind.MinimumHandPriority, HandType.Pair, PersonaEffectKind.MultiplyFinal, 1.07m);

            Assert.That(PersonaEquipPromptSession.RuleTextOf(chips), Is.EqualTo("对子或更高：+10 筹码"));
            Assert.That(PersonaEquipPromptSession.RuleTextOf(multiplier), Is.EqualTo("对子或更高：+1.5 倍率"));
            Assert.That(PersonaEquipPromptSession.RuleTextOf(final), Is.EqualTo("对子或更高：最终 ×1.07"));
        }

        [Test]
        public void HandNameOfMatchesLegacySwitch()
        {
            Assert.That(PersonaEquipPromptSession.HandNameOf(HandType.Pair), Is.EqualTo("对子"));
            Assert.That(PersonaEquipPromptSession.HandNameOf(HandType.StraightFlush), Is.EqualTo("同花顺"));
            Assert.That(PersonaEquipPromptSession.HandNameOf(HandType.HighCard), Is.EqualTo("高牌"));
            Assert.That(PersonaEquipPromptSession.HandNameOf(HandType.RoyalFlush), Is.EqualTo("高牌")); // 旧 switch 默认分支
        }

        /// <summary>默认夹具：初始 3 卡 + 1 空槽，候选「映照·洞察者」（AddChips 10，对子）。</summary>
        private static PersonaEquipPromptSession Session()
        {
            return Session(InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null);
        }

        private static PersonaEquipPromptSession Session(params PersonaCardDefinition[] slots)
        {
            return new PersonaEquipPromptSession(Candidate(), 4, slots);
        }

        private static PersonaEquipPromptSession Session(PersonaCardDefinition candidate)
        {
            return new PersonaEquipPromptSession(candidate, 4, new[]
            {
                InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor,
                InitialPersonaCatalog.Ambitious, null
            });
        }

        private static PersonaCardDefinition Candidate()
        {
            return new PersonaCardDefinition("persona.forge.映照.洞察者", "映照·洞察者",
                PersonaConditionKind.MinimumHandPriority, HandType.Pair, PersonaEffectKind.AddChips, 10m);
        }
    }
}
