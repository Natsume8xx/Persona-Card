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
using PersonaCards.Core;

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
        // P0-1I 战斗体验对齐：手牌显示排序切换按钮 + 得分进度条填充图
        [SerializeField] private Button handSortButton;
        [SerializeField] private Image scoreProgressFill;
        private RectTransform _scoreProgressFillRect; // 进度条填充 rect：anchorMax.x 驱动宽度（从左向右增长）
        private float _progressFillLeftX = 0.02f;     // 填充条左右缘（Awake 时从场景 rect 读取，锚点改布局无需改代码）
        private float _progressFillRightX = 0.98f;
        [SerializeField] private bool externalFlowManaged;

        private BattleStateMachine _battle;
        private bool _completionRaised;
        private Font _runtimeFont;
        private readonly Dictionary<string, BattleCardView> _cardViews = new Dictionary<string, BattleCardView>();
        private bool _modalOpen;
        private int _deckViewerPage;
        private DeckViewerZone _deckViewerZone;

        /// <summary>牌库查看器各卡槽自带的卡底 Image（懒缓存：首次打开查看器时收集，场景零改动）。</summary>
        private Image[] _deckViewerBackdrops;

        /// <summary>各卡槽卡底的原始颜色（美术贴图缺失时恢复旧样式纯色底）。</summary>
        private Color[] _deckViewerBackdropColors;

        /// <summary>整卡贴图 → Sprite 缓存（卡槽 Image 只能渲染 Sprite，按贴图名缓存避免每次翻页重建）。</summary>
        private readonly Dictionary<string, Sprite> _deckViewerFaceSprites = new Dictionary<string, Sprite>();
        // P0-1I：手牌显示排序模式（战斗内临时状态，不落档）；当前金币（由 FlowController 注入）与本场获得金币（预留 P0-6，恒 0）
        private HandSortMode _handSortMode = HandSortMode.RankFirst;
        private int _journeyCoins;
        private int _battleCoinsGained;

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
            Text[] handReferenceRowTexts,
            Button handSort,
            Image scoreProgress)
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
            handSortButton = handSort;
            scoreProgressFill = scoreProgress;
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
            handSortButton.onClick.AddListener(ToggleHandSortMode);
            // P0-1I 进度条：记录填充条 rect 与左右缘（anchorMax.x 驱动宽度，避免 Filled+9-slice 从中间起步）
            if (scoreProgressFill != null)
            {
                _scoreProgressFillRect = scoreProgressFill.rectTransform;
                _progressFillLeftX = _scoreProgressFillRect.anchorMin.x;
                _progressFillRightX = _scoreProgressFillRect.anchorMax.x;
            }
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

            // 音效：战斗界面静态按钮统一挂点击音效（手牌按钮由 BattleCardView.Configure 单独挂）
            MusicManager.AttachClickSound(playButton, discardButton, deckViewerButton, handReferenceButton,
                deckViewerCloseButton, handReferenceCloseButton, deckViewerPreviousButton, deckViewerNextButton,
                handSortButton, newBattleButton);
            MusicManager.AttachClickSound(deckViewerZoneButtons);
        }

        public void BeginBattle()
        {
            BeginBattle(350, 20260811u);
        }

        public void BeginBattle(long targetScore, uint seed, IEnumerable<PlayingCardInstance> cards = null,
            PersonaLoadout personaLoadout = null, BossEncounterRuntime bossEncounter = null,
            int playsLimit = BattleStateMachine.StartingPlays, int discardsLimit = BattleStateMachine.StartingDiscards,
            int journeyCoins = 0)
        {
            _battle = new BattleStateMachine(cards ?? StandardDeckFactory.Create(), seed, targetScore,
                personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout(), bossEncounter: bossEncounter,
                playsLimit: playsLimit, discardsLimit: discardsLimit);
            _completionRaised = false;
            _modalOpen = false;
            // P0-1I：新战斗开局重置手牌显示排序为默认「大小」（排序偏好不落档），并刷新按钮文案
            _handSortMode = HandSortMode.RankFirst;
            RefreshHandSortButtonLabel();
            // P0-1I：当前金币由 FlowController 注入（来源 JourneyDeckState）；本场获得金币预留 P0-6 接入
            _journeyCoins = journeyCoins;
            _battleCoinsGained = 0;
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            resultOverlay.SetActive(false);
            scoringLogText.text = "等待出牌";
            messageText.text = "选择 1—5 张牌，然后出牌或弃牌";
            RefreshAll();
            RefreshBossRule();
        }

        public void RestoreBattle(BattleStateSnapshot snapshot, PersonaLoadout personaLoadout, int journeyCoins = 0)
        {
            _battle = new BattleStateMachine(snapshot, personaLoadout ?? InitialPersonaCatalog.CreateDefaultLoadout());
            _completionRaised = false;
            _modalOpen = false;
            resultOverlay.SetActive(false);
            deckViewerOverlay.SetActive(false);
            handReferenceOverlay.SetActive(false);
            // P0-1I：读档恢复默认「大小」排序；金币与 BeginBattle 同源注入
            _handSortMode = HandSortMode.RankFirst;
            RefreshHandSortButtonLabel();
            _journeyCoins = journeyCoins;
            _battleCoinsGained = 0;
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
            // P0-1I 进度条（策划案 3.3.9）：当前得分与目标分数的完成比例，防御除零。
            // 用 anchorMax.x 驱动宽度（从左向右增长）：Filled 类型挂带 border 的 sprite 会因 9-slice 边界从中间起步，弃用
            if (_scoreProgressFillRect != null)
            {
                var progressRatio = _battle.TargetScore > 0
                    ? Mathf.Clamp01((float)_battle.TotalScore / _battle.TargetScore)
                    : 0f;
                _scoreProgressFillRect.anchorMax = new Vector2(
                    Mathf.Lerp(_progressFillLeftX, _progressFillRightX, progressRatio),
                    _scoreProgressFillRect.anchorMax.y);
            }
            // P0-1I 金币两行（3.3.9）：获得金币=本场累计（P0-6 接入前恒 +0），当前金币=旅程持有总量
            resourceText.text = $"剩余出牌：{_battle.PlaysRemaining} / {_battle.PlaysLimit}\n\n剩余弃牌：{_battle.DiscardsRemaining} / {_battle.DiscardsLimit}\n\n牌堆：{_battle.Deck.DrawPile.Count}\n\n获得金币：+{_battleCoinsGained}\n\n当前金币：{_journeyCoins}";
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

            // P0-1I 手牌排序（策划案 3.3.10）：只按显示顺序渲染，Deck.Hand 本身不动——
            // 抽牌顺序、选中状态（按 card.Id 查询）均不受影响，index 只决定扇形排布位置
            var cards = HandDisplaySorter.Sort(_battle.Deck.Hand, _handSortMode);
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var view = Instantiate(cardPrefab, handRoot);
                view.Configure(card, _battle.SelectedCardIds.Contains(card.Id), index, cards.Count, _runtimeFont, OnCardClicked);
                view.SetInteractable(!_battle.IsPresentationLocked && !_modalOpen);
                _cardViews[card.Id] = view;
            }
        }

        /// <summary>P0-1I 手牌排序切换：大小 ↔ 花色 轮换，只改显示顺序（3.3.10）。</summary>
        private void ToggleHandSortMode()
        {
            _handSortMode = _handSortMode == HandSortMode.RankFirst ? HandSortMode.SuitGrouped : HandSortMode.RankFirst;
            RefreshHandSortButtonLabel();
            RefreshHand();
        }

        /// <summary>同步排序切换按钮文案，反映当前显示模式。</summary>
        private void RefreshHandSortButtonLabel()
        {
            if (handSortButton == null) return;
            var label = handSortButton.transform.Find("Label");
            if (label == null) return;
            var text = label.GetComponent<Text>();
            if (text != null)
            {
                text.text = _handSortMode == HandSortMode.RankFirst ? "排序：大小" : "排序：花色";
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

        /// <summary>出牌入口：UI 按钮与快捷键（P0-1H 空格，可改键）共用此方法。</summary>
        public void OnPlay()
        {
            if (!_battle.IsPresentationLocked && !_modalOpen)
            {
                StartCoroutine(PlaySequence());
            }
        }

        /// <summary>弃牌入口：UI 按钮与快捷键（P0-1H D，可改键）共用此方法。</summary>
        public void OnDiscard()
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
            MusicManager.Instance.PlaySfx(MusicCatalog.SfxDiscard); // 音效：弃牌

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
            MusicManager.Instance.PlaySfx(MusicCatalog.SfxScoreCount); // 音效：分数计算（结算事件演示）
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
            if (newCardIds.Count > 0)
            {
                MusicManager.Instance.PlaySfx(MusicCatalog.SfxDraw); // 音效：出牌/弃牌后补牌
            }
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
            // P0-1H：动效统一归口到设置系统的「界面动效」开关（GameSettings.AnimationsEnabled），
            // 战斗屏不再自带「减少动效」Toggle；未来手牌与整体画面动效同样读此门面。
            return GameSettings.AnimationsEnabled ? normalDuration : 0f;
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
            // P0-1I 牌库查看页签对齐（UI-04「总数 / 当前可选数」）：
            // 可选数 = 还能打出的牌（手牌+抽牌堆），只对 全部牌 / 抽牌堆 页签有意义；空页签显示「（空）」
            var available = _deckViewerZone is DeckViewerZone.All or DeckViewerZone.DrawPile
                ? _battle.Deck.DrawPile.Count + _battle.Deck.Hand.Count
                : 0;
            deckViewerSummaryText.text = cards.Count == 0
                ? "（空）"
                : $"总数 {cards.Count} · 可选 {available}";
            deckViewerPageText.text = cards.Count == 0
                ? $"{ZoneName(_deckViewerZone)} · （空）"
                : $"{ZoneName(_deckViewerZone)}  ·  第 {_deckViewerPage + 1} / {maximumPage + 1} 页";
            deckViewerPreviousButton.interactable = _deckViewerPage > 0;
            deckViewerNextButton.interactable = _deckViewerPage < maximumPage;
            for (var index = 0; index < deckViewerZoneButtons.Length; index++)
            {
                deckViewerZoneButtons[index].targetGraphic.color = index == (int)_deckViewerZone
                    ? new Color32(112, 84, 39, 255)
                    : new Color32(32, 32, 31, 248);
            }
            EnsureDeckViewerBackdropCache(); // 首次渲染前记录各卡槽卡底原始色（无贴图时的回退底色）
            for (var visibleIndex = 0; visibleIndex < pageSize; visibleIndex++)
            {
                var cardIndex = _deckViewerPage * pageSize + visibleIndex;
                var hasCard = cardIndex < cards.Count;
                deckViewerCardTexts[visibleIndex].transform.parent.gameObject.SetActive(hasCard);
                if (!hasCard) continue;
                var card = cards[cardIndex];
                // 美术牌面接入（与手牌 BattleCardView 同模式）：有贴图 → 卡槽自带卡底 Image 换整卡 Sprite
                //（点数由美术自带），文本降级为只显示增强标记；无贴图 → 恢复原始纯色卡底 + 旧文本样式
                var face = CardFaceCatalog.FaceFor(card.Suit, card.Rank);
                var hasFace = face != null;
                var backdrop = _deckViewerBackdrops[visibleIndex];
                backdrop.sprite = hasFace ? SpriteForFace(face) : null; // null 回退原始纯色卡底
                backdrop.color = hasFace ? Color.white : _deckViewerBackdropColors[visibleIndex];
                var enhancement = EnhancementName(card.Enhancement).Replace("\n", string.Empty);
                deckViewerCardTexts[visibleIndex].gameObject.SetActive(!hasFace || enhancement.Length > 0);
                deckViewerCardTexts[visibleIndex].text = hasFace
                    ? enhancement
                    : $"{RankName(card.Rank)}\n{SuitName(card.Suit)}{EnhancementName(card.Enhancement)}";
                deckViewerCardTexts[visibleIndex].color = hasFace
                    ? new Color32(116, 77, 24, 255) // 增强标记棕色（与手牌增强徽记同色）
                    : card.Suit is Suit.Hearts or Suit.Diamonds
                        ? new Color32(180, 91, 72, 255)
                        : new Color32(226, 214, 184, 255);
            }
        }

        /// <summary>
        /// 懒收集各卡槽自带的卡底 Image 与原始颜色（首次打开查看器时调用一次）。
        /// 原始色在首次被贴图覆盖前记录，之后作为无贴图卡的回退底色。
        /// </summary>
        private void EnsureDeckViewerBackdropCache()
        {
            if (_deckViewerBackdrops != null) return;
            _deckViewerBackdrops = new Image[deckViewerCardTexts.Length];
            _deckViewerBackdropColors = new Color[deckViewerCardTexts.Length];
            for (var index = 0; index < deckViewerCardTexts.Length; index++)
            {
                var backdrop = deckViewerCardTexts[index].transform.parent.GetComponent<Image>();
                _deckViewerBackdrops[index] = backdrop;
                _deckViewerBackdropColors[index] = backdrop.color;
            }
        }

        /// <summary>整卡贴图 → Sprite（卡槽 Image 只能渲染 Sprite；按贴图名缓存，52 张每张只转一次）。</summary>
        private Sprite SpriteForFace(Texture2D face)
        {
            if (_deckViewerFaceSprites.TryGetValue(face.name, out var cached)) return cached;
            var sprite = Sprite.Create(face, new Rect(0f, 0f, face.width, face.height), new Vector2(0.5f, 0.5f));
            sprite.name = face.name; // 与贴图同名，调试/日志可读
            _deckViewerFaceSprites[face.name] = sprite;
            return sprite;
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
            // All 已按显示顺序（配表「显示顺序」列）排列，直接按序渲染，不再按枚举序
            var definitions = HandTypeCatalog.All.ToArray();
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
