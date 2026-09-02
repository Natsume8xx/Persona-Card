using System;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using PersonaCards.Core.Random;
using PersonaCards.Core;

namespace PersonaCards.Battle
{
    public sealed class BattleStateMachine
    {
        public const int HandLimit = 8;
        /// <summary>选牌上限编译期默认（白盒回落；运行时以构造参数/全局配置 RULE_018 为准，P0-2）。</summary>
        public const int DefaultSelectionLimit = 5;
        public const int StartingPlays = 4;
        public const int StartingDiscards = 3;

        private readonly HashSet<string> _selectedCardIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly ScoringPipeline _scoringPipeline;
        private readonly PersonaLoadout _personaLoadout;
        private readonly BossEncounterRuntime _bossEncounter;

        public BattleStateMachine(
            IEnumerable<PlayingCardInstance> cards,
            uint seed,
            long targetScore,
            PersonaLoadout personaLoadout = null,
            ScoringPipeline scoringPipeline = null,
            BossEncounterRuntime bossEncounter = null,
            int playsLimit = StartingPlays,
            int discardsLimit = StartingDiscards,
            int selectionLimit = DefaultSelectionLimit)
        {
            if (targetScore < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetScore));
            }
            if (playsLimit < 1 || discardsLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playsLimit), "出牌/弃牌上限必须至少为 1（配置为 0 表示使用默认值，调用方应已解析）。");
            }
            if (selectionLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionLimit), "选牌上限必须至少为 1。");
            }

            Deck = new DeckState(cards ?? throw new ArgumentNullException(nameof(cards)));
            TargetScore = targetScore;
            PlaysLimit = playsLimit;      // 本场限制由路线节点配置（0 已在 RunRoute 门面解析为默认值）
            DiscardsLimit = discardsLimit;
            SelectionLimit = selectionLimit; // 选牌上限由全局配置派生（调用方解析；默认回落编译期常量）
            PlaysRemaining = playsLimit;
            DiscardsRemaining = discardsLimit;
            Status = BattleStatus.PlayerTurn;
            _personaLoadout = personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout();
            _scoringPipeline = scoringPipeline ?? new ScoringPipeline();
            _bossEncounter = bossEncounter;

            Deck.ShuffleDrawPile(new XorShift32Rng(seed));
            DrawToHandLimit();
        }

        public BattleStateMachine(BattleStateSnapshot snapshot, PersonaLoadout personaLoadout = null,
            ScoringPipeline scoringPipeline = null, int selectionLimit = DefaultSelectionLimit)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.TargetScore < 1) throw new ArgumentOutOfRangeException(nameof(snapshot));
            if (snapshot.PlaysLimit < 1 || snapshot.DiscardsLimit < 1 ||
                snapshot.TotalScore < 0 || snapshot.PlaysRemaining < 0 || snapshot.PlaysRemaining > snapshot.PlaysLimit ||
                snapshot.DiscardsRemaining < 0 || snapshot.DiscardsRemaining > snapshot.DiscardsLimit)
                throw new ArgumentOutOfRangeException(nameof(snapshot), "Battle resources are invalid.");
            if (!Enum.IsDefined(typeof(BattleStatus), snapshot.Status))
                throw new ArgumentOutOfRangeException(nameof(snapshot), "Battle status is invalid.");
            if (selectionLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(selectionLimit), "选牌上限必须至少为 1。");

            Deck = new DeckState(snapshot.DrawPile, snapshot.Hand, snapshot.Played, snapshot.Discarded);
            TargetScore = snapshot.TargetScore;
            TotalScore = snapshot.TotalScore;
            PlaysLimit = snapshot.PlaysLimit;        // 恢复时以快照记录的场次上限为准（旧档缺字段回落默认值）
            DiscardsLimit = snapshot.DiscardsLimit;
            PlaysRemaining = snapshot.PlaysRemaining;
            DiscardsRemaining = snapshot.DiscardsRemaining;
            Status = snapshot.Status;
            SelectionLimit = selectionLimit; // 选牌上限为全局配置派生，不随快照（P0-8 存档时全局配置重载兜底）
            _personaLoadout = personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout();
            _scoringPipeline = scoringPipeline ?? new ScoringPipeline();
            _bossEncounter = BossEncounterCatalog.Restore(snapshot.BossEncounter);
            foreach (var cardId in snapshot.SelectedCardIds ?? Array.Empty<string>())
            {
                if (!Deck.Hand.Any(card => string.Equals(card.Id, cardId, StringComparison.Ordinal)) ||
                    !_selectedCardIds.Add(cardId))
                    throw new ArgumentException("Saved selection is invalid.", nameof(snapshot));
            }
            if (_selectedCardIds.Count > SelectionLimit)
                throw new ArgumentException("Saved selection exceeds the selection limit.", nameof(snapshot));
        }

        public DeckState Deck { get; }
        public long TargetScore { get; }
        public long TotalScore { get; private set; }
        /// <summary>本场出牌次数上限（由路线节点配置；未指定时为 StartingPlays）。</summary>
        public int PlaysLimit { get; }
        /// <summary>本场弃牌次数上限（由路线节点配置；未指定时为 StartingDiscards）。</summary>
        public int DiscardsLimit { get; }
        /// <summary>本场选牌上限（由全局配置 RULE_018 派生；未配置时为 DefaultSelectionLimit）。</summary>
        public int SelectionLimit { get; }
        public int PlaysRemaining { get; private set; }
        public int DiscardsRemaining { get; private set; }
        public BattleStatus Status { get; private set; }
        public bool IsPresentationLocked { get; private set; }
        public IReadOnlyCollection<string> SelectedCardIds => _selectedCardIds;
        public BossEncounterRuntime BossEncounter => _bossEncounter;

        public void SetPresentationLock(bool isLocked)
        {
            IsPresentationLocked = isLocked;
        }

        public BattleCommandResult TryToggleSelection(string cardId)
        {
            if (IsPresentationLocked)
            {
                return BattleCommandResult.Rejected(BattleCommandFailure.PresentationInProgress);
            }

            if (Status != BattleStatus.PlayerTurn)
            {
                return BattleCommandResult.Rejected(BattleCommandFailure.BattleFinished);
            }

            if (!Deck.Hand.Any(card => string.Equals(card.Id, cardId, StringComparison.Ordinal)))
            {
                return BattleCommandResult.Rejected(BattleCommandFailure.CardNotInHand);
            }

            if (_selectedCardIds.Remove(cardId))
            {
                return BattleCommandResult.Success();
            }

            if (_selectedCardIds.Count >= SelectionLimit)
            {
                return BattleCommandResult.Rejected(BattleCommandFailure.SelectionLimitReached);
            }

            _selectedCardIds.Add(cardId);
            return BattleCommandResult.Success();
        }

        public ScoringResult PreviewSelected()
        {
            if (_selectedCardIds.Count == 0)
            {
                return null;
            }

            return _scoringPipeline.Score(GetSelectedCardsInHandOrder(), CreateScoringEffects());
        }

        public BattleCommandResult TryPlaySelected()
        {
            var validation = ValidateAction(PlaysRemaining, BattleCommandFailure.NoPlaysRemaining);
            if (validation != BattleCommandFailure.None)
            {
                return BattleCommandResult.Rejected(validation);
            }

            var selectedCards = GetSelectedCardsInHandOrder();
            var evaluation = new HandEvaluator().Evaluate(selectedCards);
            var scoringResult = _scoringPipeline.Score(selectedCards, CreateScoringEffects());
            Deck.MoveCards(selectedCards.Select(card => card.Id), CardZone.Hand, CardZone.Played);
            PlaysRemaining--;
            TotalScore += scoringResult.FinalScore;
            _bossEncounter?.CommitHand(evaluation.HandType);
            _selectedCardIds.Clear();

            if (TotalScore >= TargetScore)
            {
                Status = BattleStatus.Won;
                return BattleCommandResult.Success(scoringResult);
            }

            DrawToHandLimit();
            UpdateLossState();
            return BattleCommandResult.Success(scoringResult);
        }

        public BattleCommandResult TryDiscardSelected()
        {
            var validation = ValidateAction(DiscardsRemaining, BattleCommandFailure.NoDiscardsRemaining);
            if (validation != BattleCommandFailure.None)
            {
                return BattleCommandResult.Rejected(validation);
            }

            var selectedCards = GetSelectedCardsInHandOrder();
            Deck.MoveCards(selectedCards.Select(card => card.Id), CardZone.Hand, CardZone.Discarded);
            DiscardsRemaining--;
            _selectedCardIds.Clear();
            DrawToHandLimit();
            UpdateLossState();
            return BattleCommandResult.Success();
        }

        private BattleCommandFailure ValidateAction(int remaining, BattleCommandFailure exhaustedFailure)
        {
            if (IsPresentationLocked)
            {
                return BattleCommandFailure.PresentationInProgress;
            }

            if (Status != BattleStatus.PlayerTurn)
            {
                return BattleCommandFailure.BattleFinished;
            }

            if (_selectedCardIds.Count == 0)
            {
                return BattleCommandFailure.NoCardsSelected;
            }

            return remaining <= 0 ? exhaustedFailure : BattleCommandFailure.None;
        }

        private PlayingCardInstance[] GetSelectedCardsInHandOrder()
        {
            return Deck.Hand.Where(card => _selectedCardIds.Contains(card.Id)).ToArray();
        }

        private IReadOnlyList<IScoringEffect> CreateScoringEffects()
        {
            var effects = new List<IScoringEffect>(_personaLoadout.CreateScoringEffects());
            if (_bossEncounter != null) effects.AddRange(_bossEncounter.CreateScoringEffects());
            return effects;
        }

        private void DrawToHandLimit()
        {
            Deck.Draw(Math.Max(0, HandLimit - Deck.Hand.Count));
            Deck.ValidateCardConservation();
        }

        private void UpdateLossState()
        {
            if (TotalScore < TargetScore &&
                (PlaysRemaining == 0 || (Deck.Hand.Count == 0 && Deck.DrawPile.Count == 0)))
            {
                Status = BattleStatus.Lost;
            }
        }
    }

    public sealed class BattleStateSnapshot
    {
        public BattleStateSnapshot(IEnumerable<PlayingCardInstance> drawPile, IEnumerable<PlayingCardInstance> hand,
            IEnumerable<PlayingCardInstance> played, IEnumerable<PlayingCardInstance> discarded,
            IEnumerable<string> selectedCardIds, long targetScore, long totalScore, int playsRemaining,
            int discardsRemaining, BattleStatus status, BossEncounterSnapshot bossEncounter = null,
            int playsLimit = BattleStateMachine.StartingPlays, int discardsLimit = BattleStateMachine.StartingDiscards)
        {
            DrawPile = (drawPile ?? throw new ArgumentNullException(nameof(drawPile))).ToArray();
            Hand = (hand ?? throw new ArgumentNullException(nameof(hand))).ToArray();
            Played = (played ?? throw new ArgumentNullException(nameof(played))).ToArray();
            Discarded = (discarded ?? throw new ArgumentNullException(nameof(discarded))).ToArray();
            SelectedCardIds = (selectedCardIds ?? throw new ArgumentNullException(nameof(selectedCardIds))).ToArray();
            TargetScore = targetScore;
            TotalScore = totalScore;
            PlaysRemaining = playsRemaining;
            DiscardsRemaining = discardsRemaining;
            Status = status;
            BossEncounter = bossEncounter;
            PlaysLimit = playsLimit;          // 快照自记本场上限，恢复构造器据此校验（旧档缺字段回落默认值）
            DiscardsLimit = discardsLimit;
        }

        public IReadOnlyList<PlayingCardInstance> DrawPile { get; }
        public IReadOnlyList<PlayingCardInstance> Hand { get; }
        public IReadOnlyList<PlayingCardInstance> Played { get; }
        public IReadOnlyList<PlayingCardInstance> Discarded { get; }
        public IReadOnlyList<string> SelectedCardIds { get; }
        public long TargetScore { get; }
        public long TotalScore { get; }
        public int PlaysRemaining { get; }
        public int DiscardsRemaining { get; }
        /// <summary>本场出牌次数上限（快照自记；供恢复构造器校验与 HUD 显示）。</summary>
        public int PlaysLimit { get; }
        /// <summary>本场弃牌次数上限（快照自记；同上）。</summary>
        public int DiscardsLimit { get; }
        public BattleStatus Status { get; }
        public BossEncounterSnapshot BossEncounter { get; }
    }
}
