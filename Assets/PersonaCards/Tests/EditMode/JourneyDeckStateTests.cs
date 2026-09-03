using System.Linq;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.UI;
using PersonaCards.Core;

namespace PersonaCards.Tests.EditMode
{
    public sealed class JourneyDeckStateTests
    {
        [Test]
        public void RewardEnhancementPersistsIntoBattleDeckCopy()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());
            var target = journey.Cards.First();

            Assert.That(journey.GrantRewardEnhancement(target.Id), Is.True);
            Assert.That(journey.CreateBattleDeck().Single(card => card.Id == target.Id).Enhancement,
                Is.EqualTo(CardEnhancement.ChipBoost));
        }

        [Test]
        public void PurchaseConsumesCoinsAndMutatesSelectedCardOnly()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());
            var target = journey.Cards.First();

            Assert.That(journey.TryPurchase(JourneyDeckAction.Enhance, target.Id), Is.True);
            Assert.That(journey.Coins, Is.EqualTo(1));
            Assert.That(journey.Cards.Single(card => card.Id == target.Id).Enhancement,
                Is.EqualTo(CardEnhancement.MultBoost));
            Assert.That(journey.Cards.Count(card => card.Enhancement != CardEnhancement.None), Is.EqualTo(1));
        }

        [Test]
        public void CannotPurchaseWithoutEnoughCoins()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 1);
            var target = journey.Cards.First();

            Assert.That(journey.TryPurchase(JourneyDeckAction.Delete, target.Id), Is.False);
            Assert.That(journey.Coins, Is.EqualTo(1));
            Assert.That(journey.Cards.Count, Is.EqualTo(52));
        }

        [Test]
        public void PurchaseWithCustomPriceChargesThatPrice()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 10);
            var target = journey.Cards.First();

            Assert.That(journey.TryPurchase(JourneyDeckAction.Enhance, target.Id, 5), Is.True); // 按商品价 5 扣款
            Assert.That(journey.Coins, Is.EqualTo(5));

            Assert.That(journey.TryPurchase(JourneyDeckAction.Delete, target.Id, 6), Is.False); // 余额不足不扣款不生效
            Assert.That(journey.Coins, Is.EqualTo(5));
            Assert.That(journey.Cards.Count, Is.EqualTo(52));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => journey.TryPurchase(JourneyDeckAction.Delete, target.Id, -1));
        }

        [Test]
        public void ApplyCardEnhancementReplacesEnhancementOnlyAndDoesNotCharge()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());
            var target = journey.Cards.First();

            Assert.That(journey.ApplyCardEnhancement(target.Id, CardEnhancement.CoinBonus), Is.True);
            var updated = journey.Cards.Single(card => card.Id == target.Id);
            Assert.That(updated.Enhancement, Is.EqualTo(CardEnhancement.CoinBonus));
            Assert.That(updated.Suit, Is.EqualTo(target.Suit));
            Assert.That(updated.Rank, Is.EqualTo(target.Rank));
            Assert.That(journey.Coins, Is.EqualTo(3)); // 不扣款：扣款由 TrySpend 先行（选择确认流程）

            Assert.That(journey.ApplyCardEnhancement("不存在的牌", CardEnhancement.ChipPlus), Is.False);
        }

        [Test]
        public void CoinBonusIncomeCountsCoinBonusCardsOnly()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create());

            Assert.That(journey.CoinBonusIncome(), Is.EqualTo(0)); // 无金币强化牌 → 0

            journey.ApplyCardEnhancement(journey.Cards[0].Id, CardEnhancement.CoinBonus);
            journey.ApplyCardEnhancement(journey.Cards[1].Id, CardEnhancement.CoinBonus);
            journey.ApplyCardEnhancement(journey.Cards[2].Id, CardEnhancement.ChipPlus); // 其他增强不计入

            Assert.That(journey.CoinBonusIncome(), Is.EqualTo(4)); // 2 张 × 2
            Assert.That(journey.CoinBonusIncome(3), Is.EqualTo(6)); // 自定义单价
            Assert.That(journey.Coins, Is.EqualTo(3)); // 纯查询不改状态
        }

        [Test]
        public void TrySpendDeductsAndRejectsInsufficientOrNegative()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 10);

            Assert.That(journey.TrySpend(7), Is.True);
            Assert.That(journey.Coins, Is.EqualTo(3));

            Assert.That(journey.TrySpend(4), Is.False); // 余额不足不扣款
            Assert.That(journey.Coins, Is.EqualTo(3));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => journey.TrySpend(-1));
        }

        [Test]
        public void AddCardAddsCardAndRejectsDuplicateOrNull()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 10);
            var removed = journey.Cards.First();
            Assert.That(journey.TryPurchase(JourneyDeckAction.Delete, removed.Id, 5), Is.True); // 先移除一张

            var boughtBack = new PlayingCardInstance(removed.Id, removed.Suit, removed.Rank);
            Assert.That(journey.AddCard(boughtBack), Is.True); // 买回：同 id 牌不在牌组时可加入
            Assert.That(journey.Cards.Count, Is.EqualTo(52));
            Assert.That(journey.Cards.Any(card => card.Id == boughtBack.Id), Is.True);

            Assert.That(journey.AddCard(boughtBack), Is.False); // 同 id 不可重复持有
            Assert.That(journey.AddCard(journey.Cards.First()), Is.False); // 已在牌组的牌拒绝
            Assert.Throws<System.ArgumentNullException>(() => journey.AddCard(null));
        }

        [Test]
        public void AddCoinsIncreasesBalanceAndAllowsZero()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 3);

            journey.AddCoins(3);
            Assert.That(journey.Coins, Is.EqualTo(6));

            journey.AddCoins(0); // 零额发放为幂等空操作
            Assert.That(journey.Coins, Is.EqualTo(6));
        }

        [Test]
        public void AddCoinsRejectsNegativeAmounts()
        {
            var journey = new JourneyDeckState(StandardDeckFactory.Create(), 3);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => journey.AddCoins(-1));
            Assert.That(journey.Coins, Is.EqualTo(3)); // 非法调用不改变余额
        }

        [Test]
        public void PersonaSlotCycleSwapsDefinitionsAndPreservesUniqueLoadout()
        {
            var state = new PersonaLoadoutState();

            state.CycleSlot(0);

            Assert.That(state.Slots[0], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Executor));
            Assert.That(state.Slots[1], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator));
            Assert.That(state.CreateLoadout().Slots.Count, Is.EqualTo(4));
            Assert.That(state.Slots.Where(definition => definition != null).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void EquippingOwnedPersonaToAnOccupiedSlotMovesExistingCopyInsteadOfDuplicatingIt()
        {
            var state = new PersonaLoadoutState();

            state.EquipAt(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator, 2);

            Assert.That(state.Slots[2], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator));
            Assert.That(state.Slots[0], Is.EqualTo(PersonaCards.Battle.Personas.InitialPersonaCatalog.Ambitious));
            Assert.That(state.Slots.Count(definition => definition == PersonaCards.Battle.Personas.InitialPersonaCatalog.Accumulator), Is.EqualTo(1));
        }

        [Test]
        public void PersonaSlotCanBeUnequipped()
        {
            var state = new PersonaLoadoutState();

            state.Unequip(1);

            Assert.That(state.Slots[1], Is.Null);
            Assert.That(state.CreateLoadout().Slots.Count, Is.EqualTo(4));
        }

        [Test]
        public void AutomaticEquipNeverOverwritesAFullLoadout()
        {
            var state = new PersonaLoadoutState();
            var forged = new PersonaCards.Battle.Personas.PersonaCardDefinition(
                "persona.test.forged", "测试人格",
                PersonaCards.Battle.Personas.PersonaConditionKind.Always, HandType.HighCard,
                PersonaCards.Battle.Personas.PersonaEffectKind.AddChips, 20m);
            var before = state.Slots.ToArray();

            var result = state.Equip(forged);

            Assert.That(result, Is.EqualTo(3));
            Assert.That(state.Slots[3], Is.EqualTo(forged));
            state.EquipAt(PersonaCards.Battle.Personas.InitialPersonaCatalog.Ambitious, 3);
            var fullLoadout = state.Slots.ToArray();
            var anotherForged = new PersonaCards.Battle.Personas.PersonaCardDefinition(
                "persona.test.another", "另一测试人格",
                PersonaCards.Battle.Personas.PersonaConditionKind.Always, HandType.HighCard,
                PersonaCards.Battle.Personas.PersonaEffectKind.AddChips, 30m);
            Assert.That(state.Equip(anotherForged), Is.EqualTo(-1));
            Assert.That(state.Slots, Is.EqualTo(fullLoadout));
            Assert.That(before[0], Is.EqualTo(state.Slots[0]));
        }

        [Test]
        public void BehaviorReportUsesRecordedActionsAndForgeProducesThreeDifferentCandidates()
        {
            var tracker = new RunBehaviorTracker();
            tracker.RecordPlay(HandType.Pair, 2, 120);
            tracker.RecordPlay(HandType.Pair, 2, 180);
            tracker.RecordDiscard(3);

            var report = tracker.CreateReport();
            var forge = new PersonaForgeState(report, 42u);

            Assert.That(report.Plays, Is.EqualTo(2));
            Assert.That(report.Discards, Is.EqualTo(1));
            Assert.That(report.DominantHand, Is.EqualTo(HandType.Pair));
            Assert.That(forge.Rolls, Has.Count.EqualTo(3));
            Assert.That(forge.Rolls.All(roll => roll >= 1 && roll <= 20), Is.True);
            Assert.That(forge.Candidates.Select(candidate => candidate.EffectKind).Distinct().Count(), Is.EqualTo(3));
        }
    }
}
