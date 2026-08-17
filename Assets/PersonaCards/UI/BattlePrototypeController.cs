using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PersonaCards.Battle;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Cards.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    public sealed class BattlePrototypeController : MonoBehaviour
    {
        [Header("Scene UI References")]
        [SerializeField] private RectTransform handRoot;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text bossRuleText;
        [SerializeField] private Text previewText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private Text resultText;
        [SerializeField] private Button newBattleButton;
        [SerializeField] private BattleCardView cardPrefab;
        [SerializeField] private RectTransform playedSlotsRoot;
        [SerializeField] private Text scoringLogText;
        [SerializeField] private Toggle reduceMotionToggle;
        [SerializeField] private Button deckViewerButton;
        [SerializeField] private Button handReferenceButton;
        [SerializeField] private GameObject deckViewerOverlay;
        [SerializeField] private Button deckViewerCloseButton;
        [SerializeField] private Button deckViewerPreviousButton;
        [SerializeField] private Button deckViewerNextButton;
        [SerializeField] private Button[] deckViewerZoneButtons;
        [SerializeField] private Text[] deckViewerCardTexts;
        [SerializeField] private Text deckViewerSummaryText;
        [SerializeField] private Text deckViewerPageText;
        [SerializeField] private GameObject handReferenceOverlay;
        [SerializeField] private Button handReferenceCloseButton;
        [SerializeField] private Text[] handReferenceRows;
        [SerializeField] private bool externalFlowManaged;

        private BattleStateMachine _battle;
        private bool _completionRaised;
        private Font _runtimeFont;
        private readonly Dictionary<string, BattleCardView> _cardViews = new Dictionary<string, BattleCardView>();
        private bool _modalOpen;
        private int _deckViewerPage;
        private DeckViewerZone _deckViewerZone;

        public event Action<BattleStatus, long, long> BattleCompleted;
        public event Action<HandType, int, long> HandPlayed;
        public event Action<int> HandDiscarded;
        public event Action StableStateChanged;

        public BattleStateMachine Battle => _battle;

        public void SetExternalFlowManaged(bool managed)
        {
            externalFlowManaged = managed;
        }

        public void ConfigureScene(
            RectTransform handArea,
            Text score,
            Text resources,
            Text bossRule,
            Text preview,
            Text message,
            Button play,
            Button discard,
            GameObject resultPanel,
            Text result,
            Button newBattle,
            BattleCardView cardViewPrefab,
            RectTransform playedArea,
            Text scoringLog,
            Toggle reduceMotion,
            Button openDeckViewer,
            Button openHandReference,
            GameObject deckViewer,
            Button deckViewerClose,
            Button deckViewerPrevious,
            Button deckViewerNext,
            Button[] deckViewerZones,
            Text[] deckViewerCards,
            Text deckViewerSummary,
            Text deckViewerPageLabel,
            GameObject handReference,
            Button handReferenceClose,
            Text[] handReferenceRowTexts)
        {
            handRoot = handArea;
            scoreText = score;
            resourceText = resources;
            bossRuleText = bossRule;
            previewText = preview;
            messageText = message;
            playButton = play;
            discardButton = discard;
            resultOverlay = resultPanel;
            resultText = result;
            newBattleButton = newBattle;
            cardPrefab = cardViewPrefab;
            playedSlotsRoot = playedArea;
            scoringLogText = scoringLog;
            reduceMotionToggle = reduceMotion;
            deckViewerButton = openDeckViewer;
            handReferenceButton = openHandReference;
            deckViewerOverlay = deckViewer;
            deckViewerCloseButton = deckViewerClose;
            deckViewerPreviousButton = deckViewerPrevious;
            deckViewerNextButton = deckViewerNext;
            deckViewerZoneButtons = deckViewerZones;
            deckViewerCardTexts = deckViewerCards;
            deckViewerSummaryText = deckViewerSummary;
            deckViewerPageText = deckViewerPageLabel;
            handReferenceOverlay = handReference;
            handReferenceCloseButton = handReferenceClose;
            handReferenceRows = handReferenceRowTexts;
        }

        private void Awake()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 28);
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                text.font = _runtimeFont;
            }

            playButton.onClick.AddListener(OnPlay);
            discardButton.onClick.AddListener(OnDiscard);
            deckViewerButton.onClick.AddListener(OpenDeckViewer);
            handReferenceButton.onClick.AddListener(OpenHandReference);
            deckViewerCloseButton.onClick.AddListener(CloseModal);
            handReferenceCloseButton.onClick.AddListener(CloseModal);
            deckViewerPreviousButton.onClick.AddListener(() => ChangeDeckViewerPage(-1));
            deckViewerNextButton.onClick.AddListener(() => ChangeDeckViewerPage(1));
            for (var index = 0; index < deckViewerZoneButtons.Length; index++)
            {
                var zoneIndex = index;
                deckViewerZoneButtons[index].onClick.AddListener(() => SelectDeckViewerZone((DeckViewerZone)zoneIndex));
            }
            if (!externalFlowManaged)
            {
                newBattleButton.onClick.AddListener(BeginBattle);
                BeginBattle();
            }
            else
            {
                resultOverlay.SetActive(false);
            }
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            RefreshHandReference();
        }

        public void BeginBattle()
        {
            BeginBattle(350, 20260811u);
        }

        public void BeginBattle(long targetScore, uint seed, IEnumerable<PlayingCardInstance> cards = null,
            PersonaLoadout personaLoadout = null, BossEncounterRuntime bossEncounter = null)
        {
            _battle = new BattleStateMachine(cards ?? StandardDeckFactory.Create(), seed, targetScore,
                personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout(), bossEncounter: bossEncounter);
            _completionRaised = false;
            _modalOpen = false;
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            resultOverlay.SetActive(false);
            scoringLogText.text = "等待出牌";
            messageText.text = "选择 1—5 张牌，然后出牌或弃牌";
            RefreshAll();
            RefreshBossRule();
        }

        public void RestoreBattle(BattleStateSnapshot snapshot, PersonaLoadout personaLoadout)
        {
            _battle = new BattleStateMachine(snapshot, personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout());
            _completionRaised = false;
            _modalOpen = false;
            resultOverlay.SetActive(false);
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            scoringLogText.text = "战斗已从存档恢复";
            messageText.text = _battle.SelectedCardIds.Count > 0 ? "已恢复上次选择" : "选择 1—5 张牌，然后出牌或弃牌";
            RefreshAll();
            RefreshBossRule();
        }

        private void RefreshBossRule()
        {
            if (bossRuleText == null) return;
            bossRuleText.text = _battle?.BossEncounter == null
                ? "观察者未施加特殊规则"
                : $"{_battle.BossEncounter.Definition.RuleName}\n{_battle.BossEncounter.Definition.RuleDescription}\n\n介入 · {_battle.BossEncounter.Definition.InterventionName}\n{_battle.BossEncounter.Definition.InterventionDescription}";
        }

        private void RefreshAll()
        {
            RefreshHand();
            var preview = _battle.PreviewSelected();
            if (preview == null)
            {
                previewText.text = "当前牌型：—\n预计得分：—";
            }
            else
            {
                var cards = _battle.Deck.Hand.Where(card => _battle.SelectedCardIds.Contains(card.Id));
                var hand = new HandEvaluator().Evaluate(cards);
                previewText.text = $"当前牌型：{GetHandName(hand.HandType)}    筹码 {preview.Chips:0} × 倍率 {preview.Multiplier * preview.FinalMultiplier:0.##}\n预计得分：{preview.FinalScore}";
            }

            scoreText.text = $"当前得分\n<size=46>{_battle.TotalScore}</size>\n\n目标分数\n<size=38>{_battle.TargetScore}</size>";
            resourceText.text = $"剩余出牌：{_battle.PlaysRemaining} / 4\n\n剩余弃牌：{_battle.DiscardsRemaining} / 3\n\n牌堆：{_battle.Deck.DrawPile.Count}";
            var playerCanInspect = _battle.Status == BattleStatus.PlayerTurn && !_battle.IsPresentationLocked && !_modalOpen;
            var canAct = playerCanInspect && _battle.SelectedCardIds.Count > 0;
            playButton.interactable = canAct && _battle.PlaysRemaining > 0;
            discardButton.interactable = canAct && _battle.DiscardsRemaining > 0;
            deckViewerButton.interactable = playerCanInspect;
            handReferenceButton.interactable = playerCanInspect;

            if (_battle.Status != BattleStatus.PlayerTurn)
            {
                resultOverlay.SetActive(!externalFlowManaged);
                resultText.text = _battle.Status == BattleStatus.Won
                    ? $"战斗胜利\n\n最终得分  {_battle.TotalScore}\n目标分数  {_battle.TargetScore}"
                    : $"战斗失败\n\n最终得分  {_battle.TotalScore}\n还差  {_battle.TargetScore - _battle.TotalScore}";

                if (!_completionRaised)
                {
                    _completionRaised = true;
                    BattleCompleted?.Invoke(_battle.Status, _battle.TotalScore, _battle.TargetScore);
                }
            }
        }

        private void RefreshHand()
        {
            for (var index = handRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(handRoot.GetChild(index).gameObject);
            }
            _cardViews.Clear();

            var cards = _battle.Deck.Hand;
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var view = Instantiate(cardPrefab, handRoot);
                view.Configure(card, _battle.SelectedCardIds.Contains(card.Id), index, cards.Count, _runtimeFont, OnCardClicked);
                view.SetInteractable(!_battle.IsPresentationLocked && !_modalOpen);
                _cardViews[card.Id] = view;
            }
        }

        private void OnCardClicked(string cardId)
        {
            if (_modalOpen) return;
            var result = _battle.TryToggleSelection(cardId);
            messageText.text = result.Succeeded ? "已更新选择" : FailureText(result.Failure);
            RefreshAll();
            if (result.Succeeded) StableStateChanged?.Invoke();
        }

        private void OnPlay()
        {
            if (!_battle.IsPresentationLocked && !_modalOpen)
            {
                StartCoroutine(PlaySequence());
            }
        }

        private void OnDiscard()
        {
            if (!_battle.IsPresentationLocked && !_modalOpen)
            {
                StartCoroutine(DiscardSequence());
            }
        }

        private IEnumerator PlaySequence()
        {
            var selectedCards = _battle.Deck.Hand.Where(card => _battle.SelectedCardIds.Contains(card.Id)).ToArray();
            var previousHandIds = new HashSet<string>(_battle.Deck.Hand.Select(card => card.Id));
            var selectedViews = selectedCards.Select(card => _cardViews[card.Id]).ToArray();
            var result = _battle.TryPlaySelected();
            if (!result.Succeeded)
            {
                messageText.text = FailureText(result.Failure);
                yield break;
            }
            var evaluation = new HandEvaluator().Evaluate(selectedCards);
            HandPlayed?.Invoke(evaluation.HandType, selectedCards.Length, result.ScoringResult.FinalScore);

            _battle.SetPresentationLock(true);
            SetCardInteraction(false);
            playButton.interactable = false;
            discardButton.interactable = false;
            messageText.text = "正在结算……";

            yield return AnimateCardsToPlayedSlots(selectedViews);
            yield return PresentScoringEvents(result.ScoringResult.Events);
            yield return WaitForPresentation(0.25f);
            ClearPlayedCards();

            var newCardIds = new HashSet<string>(_battle.Deck.Hand.Select(card => card.Id).Where(id => !previousHandIds.Contains(id)));
            RefreshAll();
            yield return AnimateNewCards(newCardIds);
            _battle.SetPresentationLock(false);
            messageText.text = $"本手获得 {result.ScoringResult.FinalScore} 分";
            RefreshAll();
            StableStateChanged?.Invoke();
        }

        private IEnumerator DiscardSequence()
        {
            var count = _battle.SelectedCardIds.Count;
            var previousHandIds = new HashSet<string>(_battle.Deck.Hand.Select(card => card.Id));
            var selectedViews = _battle.SelectedCardIds.Select(id => _cardViews[id]).ToArray();
            var result = _battle.TryDiscardSelected();
            if (!result.Succeeded)
            {
                messageText.text = FailureText(result.Failure);
                yield break;
            }
            HandDiscarded?.Invoke(count);

            _battle.SetPresentationLock(true);
            SetCardInteraction(false);
            playButton.interactable = false;
            discardButton.interactable = false;
            scoringLogText.text = $"弃置 {count} 张牌";
            yield return AnimateDiscard(selectedViews);

            var newCardIds = new HashSet<string>(_battle.Deck.Hand.Select(card => card.Id).Where(id => !previousHandIds.Contains(id)));
            RefreshAll();
            yield return AnimateNewCards(newCardIds);
            _battle.SetPresentationLock(false);
            messageText.text = $"已弃置 {count} 张牌";
            RefreshAll();
            StableStateChanged?.Invoke();
        }

        private IEnumerator AnimateCardsToPlayedSlots(IReadOnlyList<BattleCardView> views)
        {
            var slots = playedSlotsRoot.Cast<RectTransform>().Take(views.Count).ToArray();
            var starts = views.Select(view => view.transform.position).ToArray();
            foreach (var view in views)
            {
                view.transform.SetParent(playedSlotsRoot, true);
                view.SetInteractable(false);
            }

            var duration = PresentationDuration(0.28f);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var amount = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                for (var index = 0; index < views.Count; index++)
                {
                    views[index].transform.position = Vector3.Lerp(starts[index], slots[index].position, amount);
                }
                yield return null;
            }

            for (var index = 0; index < views.Count; index++)
            {
                views[index].transform.position = slots[index].position;
            }
        }

        private IEnumerator PresentScoringEvents(IEnumerable<ScoringEvent> events)
        {
            foreach (var scoringEvent in events)
            {
                scoringLogText.text = DescribeEvent(scoringEvent);
                yield return WaitForPresentation(0.16f);
            }
        }

        private IEnumerator AnimateDiscard(IEnumerable<BattleCardView> views)
        {
            var cards = views.ToArray();
            var starts = cards.Select(view => view.RectTransform.anchoredPosition).ToArray();
            var duration = PresentationDuration(0.24f);
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var amount = elapsed / duration;
                for (var index = 0; index < cards.Length; index++)
                {
                    cards[index].RectTransform.anchoredPosition = starts[index] + Vector2.down * 100f * amount;
                    cards[index].CanvasGroup.alpha = 1f - amount;
                }
                yield return null;
            }
        }

        private IEnumerator AnimateNewCards(ISet<string> newCardIds)
        {
            foreach (var card in _battle.Deck.Hand.Where(card => newCardIds.Contains(card.Id)))
            {
                var view = _cardViews[card.Id];
                var target = view.RectTransform.anchoredPosition;
                var start = target + Vector2.down * 70f;
                var duration = PresentationDuration(0.12f);
                view.CanvasGroup.alpha = duration <= 0f ? 1f : 0f;
                for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
                {
                    var amount = elapsed / duration;
                    view.RectTransform.anchoredPosition = Vector2.Lerp(start, target, amount);
                    view.CanvasGroup.alpha = amount;
                    yield return null;
                }
                view.RectTransform.anchoredPosition = target;
                view.CanvasGroup.alpha = 1f;
            }
        }

        private IEnumerator WaitForPresentation(float seconds)
        {
            var duration = PresentationDuration(seconds);
            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
        }

        private float PresentationDuration(float normalDuration)
        {
            return reduceMotionToggle != null && reduceMotionToggle.isOn ? 0f : normalDuration;
        }

        private void ClearPlayedCards()
        {
            foreach (var view in playedSlotsRoot.GetComponentsInChildren<BattleCardView>(true))
            {
                Destroy(view.gameObject);
            }
        }

        private void SetCardInteraction(bool interactable)
        {
            foreach (var view in _cardViews.Values)
            {
                view.SetInteractable(interactable);
            }
        }

        private void OpenDeckViewer()
        {
            if (!CanOpenModal()) return;
            _modalOpen = true;
            _deckViewerZone = DeckViewerZone.All;
            _deckViewerPage = 0;
            deckViewerOverlay.SetActive(true);
            handReferenceOverlay.SetActive(false);
            SetCardInteraction(false);
            playButton.interactable = false;
            discardButton.interactable = false;
            RefreshDeckViewer();
        }

        private void OpenHandReference()
        {
            if (!CanOpenModal()) return;
            _modalOpen = true;
            handReferenceOverlay.SetActive(true);
            deckViewerOverlay.SetActive(false);
            SetCardInteraction(false);
            playButton.interactable = false;
            discardButton.interactable = false;
            RefreshHandReference();
        }

        private bool CanOpenModal()
        {
            return _battle != null && !_modalOpen && _battle.Status == BattleStatus.PlayerTurn &&
                   !_battle.IsPresentationLocked;
        }

        private void CloseModal()
        {
            _modalOpen = false;
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            if (_battle != null) RefreshAll();
        }

        private void SelectDeckViewerZone(DeckViewerZone zone)
        {
            _deckViewerZone = zone;
            _deckViewerPage = 0;
            RefreshDeckViewer();
        }

        private void ChangeDeckViewerPage(int delta)
        {
            var cards = DeckViewerCards();
            var maximumPage = Math.Max(0, (cards.Count - 1) / deckViewerCardTexts.Length);
            _deckViewerPage = Mathf.Clamp(_deckViewerPage + delta, 0, maximumPage);
            RefreshDeckViewer();
        }

        private void RefreshDeckViewer()
        {
            if (_battle == null || !deckViewerOverlay.activeSelf) return;
            var cards = DeckViewerCards();
            var pageSize = deckViewerCardTexts.Length;
            var maximumPage = Math.Max(0, (cards.Count - 1) / pageSize);
            _deckViewerPage = Mathf.Clamp(_deckViewerPage, 0, maximumPage);
            deckViewerSummaryText.text = $"总数 {_battle.Deck.TotalCardCount} · 手牌 {_battle.Deck.Hand.Count} · 抽牌堆 {_battle.Deck.DrawPile.Count} · 已出 {_battle.Deck.Played.Count} · 已弃 {_battle.Deck.Discarded.Count}";
            deckViewerPageText.text = $"{ZoneName(_deckViewerZone)}  ·  第 {_deckViewerPage + 1} / {maximumPage + 1} 页";
            deckViewerPreviousButton.interactable = _deckViewerPage > 0;
            deckViewerNextButton.interactable = _deckViewerPage < maximumPage;
            for (var index = 0; index < deckViewerZoneButtons.Length; index++)
            {
                deckViewerZoneButtons[index].targetGraphic.color = index == (int)_deckViewerZone
                    ? new Color32(112, 84, 39, 255)
                    : new Color32(32, 32, 31, 248);
            }
            for (var visibleIndex = 0; visibleIndex < pageSize; visibleIndex++)
            {
                var cardIndex = _deckViewerPage * pageSize + visibleIndex;
                var hasCard = cardIndex < cards.Count;
                deckViewerCardTexts[visibleIndex].transform.parent.gameObject.SetActive(hasCard);
                if (!hasCard) continue;
                var card = cards[cardIndex];
                deckViewerCardTexts[visibleIndex].text = $"{RankName(card.Rank)}\n{SuitName(card.Suit)}{EnhancementName(card.Enhancement)}";
                deckViewerCardTexts[visibleIndex].color = card.Suit is Suit.Hearts or Suit.Diamonds
                    ? new Color32(180, 91, 72, 255)
                    : new Color32(226, 214, 184, 255);
            }
        }

        private IReadOnlyList<PlayingCardInstance> DeckViewerCards()
        {
            IEnumerable<PlayingCardInstance> cards = _deckViewerZone switch
            {
                DeckViewerZone.DrawPile => _battle.Deck.DrawPile,
                DeckViewerZone.Played => _battle.Deck.Played,
                DeckViewerZone.Discarded => _battle.Deck.Discarded,
                _ => _battle.Deck.DrawPile.Concat(_battle.Deck.Hand).Concat(_battle.Deck.Played).Concat(_battle.Deck.Discarded)
            };
            return cards.OrderBy(card => card.Suit).ThenByDescending(card => card.Rank).ToArray();
        }

        private void RefreshHandReference()
        {
            var definitions = HandTypeCatalog.All.OrderBy(definition => definition.HandType).ToArray();
            for (var index = 0; index < handReferenceRows.Length; index++)
            {
                var hasDefinition = index < definitions.Length;
                handReferenceRows[index].transform.parent.gameObject.SetActive(hasDefinition);
                if (!hasDefinition) continue;
                var definition = definitions[index];
                handReferenceRows[index].text = $"{index + 1:00}   {definition.DisplayName}    {HandCondition(definition.HandType)}    筹码 {definition.BaseChips}    ×{definition.BaseMultiplier}";
            }
        }

        private static string HandCondition(HandType type) => type switch
        {
            HandType.HighCard => "未组成其他牌型",
            HandType.Pair => "2 张同点数",
            HandType.TwoPair => "两组对子",
            HandType.ThreeOfAKind => "3 张同点数",
            HandType.Straight => "5 张连续点数",
            HandType.Flush => "5 张同花色",
            HandType.FullHouse => "三条 + 对子",
            HandType.FourOfAKind => "4 张同点数",
            HandType.StraightFlush => "同花色的顺子",
            HandType.FiveOfAKind => "5 张同点数",
            HandType.FlushHouse => "同花色的葫芦",
            HandType.FlushFive => "同花色的五条",
            _ => "—"
        };

        private static string ZoneName(DeckViewerZone zone) => zone switch
        {
            DeckViewerZone.DrawPile => "抽牌堆",
            DeckViewerZone.Played => "已出牌",
            DeckViewerZone.Discarded => "已弃牌",
            _ => "全部牌"
        };

        private static string RankName(Rank rank) => rank switch
        {
            Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J", _ => ((int)rank).ToString()
        };

        private static string SuitName(Suit suit) => suit switch
        {
            Suit.Clubs => "♣", Suit.Diamonds => "♦", Suit.Hearts => "♥", _ => "♠"
        };

        private static string EnhancementName(CardEnhancement enhancement) => enhancement switch
        {
            CardEnhancement.ChipBoost => "\n+筹码", CardEnhancement.MultBoost => "\n+倍率", _ => string.Empty
        };

        private enum DeckViewerZone
        {
            All,
            DrawPile,
            Played,
            Discarded
        }

        private static string DescribeEvent(ScoringEvent scoringEvent)
        {
            return scoringEvent.Operation switch
            {
                ScoringOperation.SetChips => $"牌型筹码：{scoringEvent.After:0}",
                ScoringOperation.SetMultiplier => $"牌型倍率：×{scoringEvent.After:0.##}",
                ScoringOperation.AddChips => $"{SourceName(scoringEvent)}  +{scoringEvent.Value:0} 筹码",
                ScoringOperation.AddMultiplier => $"{SourceName(scoringEvent)}  +{scoringEvent.Value:0.##} 倍率",
                ScoringOperation.MultiplyFinal => $"{SourceName(scoringEvent)}  最终 ×{scoringEvent.Value:0.##}",
                ScoringOperation.Skip => $"{SourceName(scoringEvent)}  未触发",
                ScoringOperation.CalculateRawScore => $"汇总得分：{scoringEvent.After:0.##}",
                ScoringOperation.RoundAndClamp => $"本手得分：{scoringEvent.After:0}",
                _ => scoringEvent.DisplayTextKey
            };
        }

        private static string SourceName(ScoringEvent scoringEvent)
        {
            return scoringEvent.SourceId switch
            {
                "persona.initial.accumulator" => "积累者",
                "persona.initial.executor" => "执行者",
                "persona.initial.ambitious" => "野心者",
                BossEncounterCatalog.RepeatedJudgmentRuleId => "重复审判",
                BossEncounterCatalog.FirstHandEncouragementId => "先手鼓励",
                _ => scoringEvent.SourceType == ScoringSourceType.PlayingCard ? "计分牌" : scoringEvent.SourceType.ToString()
            };
        }

        private static string FailureText(BattleCommandFailure failure)
        {
            return failure switch
            {
                BattleCommandFailure.SelectionLimitReached => "最多只能选择 5 张牌",
                BattleCommandFailure.PresentationInProgress => "结算动画进行中",
                BattleCommandFailure.NoCardsSelected => "请先选择至少 1 张牌",
                BattleCommandFailure.NoPlaysRemaining => "出牌次数已经用完",
                BattleCommandFailure.NoDiscardsRemaining => "弃牌次数已经用完",
                BattleCommandFailure.BattleFinished => "本场战斗已经结束",
                _ => "当前操作不可用"
            };
        }

        private static string GetHandName(HandType type)
        {
            return type switch
            {
                HandType.HighCard => "高牌", HandType.Pair => "对子", HandType.TwoPair => "两对",
                HandType.ThreeOfAKind => "三条", HandType.Straight => "顺子", HandType.Flush => "同花",
                HandType.FullHouse => "葫芦", HandType.FourOfAKind => "四条", HandType.StraightFlush => "同花顺",
                HandType.FiveOfAKind => "五条", HandType.FlushHouse => "同花葫芦", HandType.FlushFive => "同花五条",
                _ => "未知"
            };
        }
    }
}
