using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PersonaCards.Battle;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    public sealed class PrototypeFlowController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject collectionScreen;
        [SerializeField] private GameObject personaSetupScreen;
        [SerializeField] private GameObject bossRevealScreen;
        [SerializeField] private GameObject battleScreen;
        [SerializeField] private GameObject rewardScreen;
        [SerializeField] private GameObject shopScreen;
        [SerializeField] private GameObject runReportScreen;
        [SerializeField] private GameObject personaForgeScreen;
        [SerializeField] private GameObject failureResultScreen;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private Button collectionBackButton;
        [SerializeField] private Button collectionPreviousButton;
        [SerializeField] private Button collectionNextButton;
        [SerializeField] private Button collectionUnequipButton;
        [SerializeField] private Button[] collectionCardButtons;
        [SerializeField] private Text[] collectionCardTexts;
        [SerializeField] private Button[] collectionEquipmentButtons;
        [SerializeField] private Text[] collectionEquipmentTexts;
        [SerializeField] private Text collectionDetailText;
        [SerializeField] private Text collectionPageText;
        [SerializeField] private Button confirmPersonaButton;
        [SerializeField] private Button beginBattleButton;
        [SerializeField] private Button resultReturnButton;
        [SerializeField] private Button rewardContinueButton;
        [SerializeField] private Button shopContinueButton;
        [SerializeField] private Button reportReturnButton;
        [SerializeField] private Button personaBackButton;
        [SerializeField] private Button bossBackButton;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultSummaryText;
        [SerializeField] private Text battleProgressText;
        [SerializeField] private Text bossRevealRuleText;
        [SerializeField] private Text reportSummaryText;
        [SerializeField] private Text rewardCardText;
        [SerializeField] private Text shopCardText;
        [SerializeField] private Text shopCoinsText;
        [SerializeField] private Text shopStatusText;
        [SerializeField] private Button rewardPreviousButton;
        [SerializeField] private Button rewardNextButton;
        [SerializeField] private Button shopPreviousButton;
        [SerializeField] private Button shopNextButton;
        [SerializeField] private Button shopDeleteButton;
        [SerializeField] private Button shopReforgeButton;
        [SerializeField] private Button shopEnhanceButton;
        [SerializeField] private Button[] personaSlotButtons;
        [SerializeField] private Text[] personaSlotNameTexts;
        [SerializeField] private Text[] personaSlotRuleTexts;
        [SerializeField] private Text[] battlePersonaNameTexts;
        [SerializeField] private Text[] battlePersonaRuleTexts;
        [SerializeField] private Text forgeRollsText;
        [SerializeField] private Text forgeStatusText;
        [SerializeField] private Text[] forgeCandidateTexts;
        [SerializeField] private Button[] forgeCandidateButtons;
        [SerializeField] private Button forgeConfirmButton;
        [SerializeField] private BattlePrototypeController battleController;
        /// <summary>整局路线表资产（P0-1 数据驱动）：Awake 时注入 RunRoute；未配置时回落内置默认路线。</summary>
        [SerializeField] private RunRouteAsset runRoute;

        private readonly PrototypeFlowStateMachine _flow = new PrototypeFlowStateMachine();
        private JourneyDeckState _journeyDeck;
        private PersonaLoadoutState _personaLoadout;
        private RunBehaviorTracker _behaviorTracker;
        private PersonaForgeState _forgeState;
        private int _selectedForgeCandidate = -1;
        private readonly List<PersonaCardDefinition> _personaCollection = new List<PersonaCardDefinition>();
        private PrototypeSaveStore _saveStore;
        private bool _rewardClaimed;
        /// <summary>本局种子：场次种子 = _runSeed + 节点序号 + 1，保证同局存档恢复后手牌顺序一致。</summary>
        private uint _runSeed;
        private int _selectedJourneyCardIndex;
        private bool _collectionOpen;
        private int _collectionPage;
        private int _selectedCollectionIndex;
        private int _selectedEquipmentSlot;

        public PrototypeFlowStage Stage => _flow.Stage;

        public void Configure(GameObject mainMenu, GameObject collection, GameObject personaSetup, GameObject bossReveal,
            GameObject battle, GameObject reward, GameObject shop, GameObject runReport, GameObject personaForge,
            GameObject failureResult,
            Button start, Button continueGame, Button openCollection, Button collectionBack,
            Button collectionPrevious, Button collectionNext, Button collectionUnequip,
            Button[] collectionCards, Text[] collectionCardLabels,
            Button[] collectionEquipment, Text[] collectionEquipmentLabels,
            Text collectionDetail, Text collectionPageLabel,
            Button confirmPersona, Button beginBattle, Button resultReturn,
            Button rewardContinue, Button shopContinue, Button reportReturn, Button personaBack, Button bossBack,
            Text resultTitle, Text resultSummary, Text battleProgress, Text bossRevealRule, Text reportSummary,
            Text rewardCard, Text shopCard, Text shopCoins, Text shopStatus,
            Button rewardPrevious, Button rewardNext, Button shopPrevious, Button shopNext,
            Button shopDelete, Button shopReforge, Button shopEnhance,
            Button[] personaSlots, Text[] personaNames, Text[] personaRules,
            Text[] battlePersonaNames, Text[] battlePersonaRules,
            Text forgeRolls, Text forgeStatus, Text[] forgeCandidates, Button[] forgeCandidateButtonsValue,
            Button forgeConfirm,
            BattlePrototypeController battlePrototype, RunRouteAsset routeAsset)
        {
            mainMenuScreen = mainMenu;
            collectionScreen = collection;
            personaSetupScreen = personaSetup;
            bossRevealScreen = bossReveal;
            battleScreen = battle;
            rewardScreen = reward;
            shopScreen = shop;
            runReportScreen = runReport;
            personaForgeScreen = personaForge;
            failureResultScreen = failureResult;
            startButton = start;
            continueButton = continueGame;
            collectionButton = openCollection;
            collectionBackButton = collectionBack;
            collectionPreviousButton = collectionPrevious;
            collectionNextButton = collectionNext;
            collectionUnequipButton = collectionUnequip;
            collectionCardButtons = collectionCards;
            collectionCardTexts = collectionCardLabels;
            collectionEquipmentButtons = collectionEquipment;
            collectionEquipmentTexts = collectionEquipmentLabels;
            collectionDetailText = collectionDetail;
            collectionPageText = collectionPageLabel;
            confirmPersonaButton = confirmPersona;
            beginBattleButton = beginBattle;
            resultReturnButton = resultReturn;
            rewardContinueButton = rewardContinue;
            shopContinueButton = shopContinue;
            reportReturnButton = reportReturn;
            personaBackButton = personaBack;
            bossBackButton = bossBack;
            resultTitleText = resultTitle;
            resultSummaryText = resultSummary;
            battleProgressText = battleProgress;
            bossRevealRuleText = bossRevealRule;
            reportSummaryText = reportSummary;
            rewardCardText = rewardCard;
            shopCardText = shopCard;
            shopCoinsText = shopCoins;
            shopStatusText = shopStatus;
            rewardPreviousButton = rewardPrevious;
            rewardNextButton = rewardNext;
            shopPreviousButton = shopPrevious;
            shopNextButton = shopNext;
            shopDeleteButton = shopDelete;
            shopReforgeButton = shopReforge;
            shopEnhanceButton = shopEnhance;
            personaSlotButtons = personaSlots;
            personaSlotNameTexts = personaNames;
            personaSlotRuleTexts = personaRules;
            battlePersonaNameTexts = battlePersonaNames;
            battlePersonaRuleTexts = battlePersonaRules;
            forgeRollsText = forgeRolls;
            forgeStatusText = forgeStatus;
            forgeCandidateTexts = forgeCandidates;
            forgeCandidateButtons = forgeCandidateButtonsValue;
            forgeConfirmButton = forgeConfirm;
            battleController = battlePrototype;
            runRoute = routeAsset;
        }

        private void Awake()
        {
            // 路线资产注入：null 时 RunRoute 回落内置默认路线，流程仍可跑（P0-1 数据驱动）
            if (runRoute == null)
                Debug.LogWarning("[Flow] runRoute 路线资产未配置：使用内置默认路线（6 战 5 店）。");
            RunRoute.Configure(runRoute);
            _saveStore = new PrototypeSaveStore();
            LoadProfile();
            startButton.onClick.AddListener(StartNewRun);
            continueButton.onClick.AddListener(ContinueSavedRun);
            collectionButton.onClick.AddListener(OpenCollection);
            collectionBackButton.onClick.AddListener(CloseCollection);
            collectionPreviousButton.onClick.AddListener(() => ChangeCollectionPage(-1));
            collectionNextButton.onClick.AddListener(() => ChangeCollectionPage(1));
            collectionUnequipButton.onClick.AddListener(UnequipSelectedSlot);
            for (var index = 0; index < collectionCardButtons.Length; index++)
            {
                var visibleIndex = index;
                collectionCardButtons[index].onClick.AddListener(() => SelectCollectionCard(visibleIndex));
            }
            for (var index = 0; index < collectionEquipmentButtons.Length; index++)
            {
                var slotIndex = index;
                collectionEquipmentButtons[index].onClick.AddListener(() => SelectEquipmentSlot(slotIndex));
            }
            confirmPersonaButton.onClick.AddListener(ConfirmPersonaSetup);
            beginBattleButton.onClick.AddListener(BeginBattle);
            resultReturnButton.onClick.AddListener(ReturnToMainMenu);
            rewardContinueButton.onClick.AddListener(ContinueFromReward);
            shopContinueButton.onClick.AddListener(ContinueFromShop);
            reportReturnButton.onClick.AddListener(ContinueToForge);
            personaBackButton.onClick.AddListener(ReturnToMainMenu);
            bossBackButton.onClick.AddListener(ReturnToPersonaSetup);
            rewardPreviousButton.onClick.AddListener(SelectPreviousJourneyCard);
            rewardNextButton.onClick.AddListener(SelectNextJourneyCard);
            shopPreviousButton.onClick.AddListener(SelectPreviousJourneyCard);
            shopNextButton.onClick.AddListener(SelectNextJourneyCard);
            shopDeleteButton.onClick.AddListener(() => Purchase(JourneyDeckAction.Delete));
            shopReforgeButton.onClick.AddListener(() => Purchase(JourneyDeckAction.Reforge));
            shopEnhanceButton.onClick.AddListener(() => Purchase(JourneyDeckAction.Enhance));
            for (var index = 0; index < personaSlotButtons.Length; index++)
            {
                var slotIndex = index;
                personaSlotButtons[index].onClick.AddListener(() => CyclePersonaSlot(slotIndex));
            }
            for (var index = 0; index < forgeCandidateButtons.Length; index++)
            {
                var candidateIndex = index;
                forgeCandidateButtons[index].onClick.AddListener(() => SelectForgeCandidate(candidateIndex));
            }
            forgeConfirmButton.onClick.AddListener(ConfirmForgeCandidate);
            battleController.BattleCompleted += OnBattleCompleted;
            battleController.HandPlayed += OnHandPlayed;
            battleController.HandDiscarded += OnHandDiscarded;
            battleController.StableStateChanged += SaveActiveRun;
            continueButton.interactable = _saveStore.TryLoad(out var initialSave) && initialSave.hasActiveRun;
            Render();
        }

        private void OpenCollection()
        {
            if (_flow.Stage != PrototypeFlowStage.MainMenu) return;
            EnsureInitialCollection();
            _collectionOpen = true;
            _collectionPage = 0;
            _selectedCollectionIndex = _personaCollection.Count > 0 ? 0 : -1;
            _selectedEquipmentSlot = 0;
            Render();
            RefreshCollection();
        }

        private void CloseCollection()
        {
            _collectionOpen = false;
            SaveProfileOnly();
            Render();
        }

        private void ChangeCollectionPage(int delta)
        {
            var maximumPage = Math.Max(0, (_personaCollection.Count - 1) / collectionCardButtons.Length);
            _collectionPage = Mathf.Clamp(_collectionPage + delta, 0, maximumPage);
            _selectedCollectionIndex = Mathf.Clamp(_collectionPage * collectionCardButtons.Length,
                0, Math.Max(0, _personaCollection.Count - 1));
            RefreshCollection();
        }

        private void SelectCollectionCard(int visibleIndex)
        {
            var collectionIndex = _collectionPage * collectionCardButtons.Length + visibleIndex;
            if (collectionIndex < 0 || collectionIndex >= _personaCollection.Count) return;
            _selectedCollectionIndex = collectionIndex;
            RefreshCollection();
        }

        private void SelectEquipmentSlot(int slotIndex)
        {
            _selectedEquipmentSlot = slotIndex;
            if (_selectedCollectionIndex >= 0 && _selectedCollectionIndex < _personaCollection.Count)
            {
                _personaLoadout.EquipAt(_personaCollection[_selectedCollectionIndex], slotIndex);
                SaveProfileOnly();
                RefreshPersonaLoadout();
            }
            RefreshCollection();
        }

        private void UnequipSelectedSlot()
        {
            _personaLoadout.Unequip(_selectedEquipmentSlot);
            SaveProfileOnly();
            RefreshPersonaLoadout();
            RefreshCollection();
        }

        private void RefreshCollection()
        {
            if (collectionScreen == null || !_collectionOpen) return;
            var pageSize = collectionCardButtons.Length;
            var maximumPage = Math.Max(0, (_personaCollection.Count - 1) / pageSize);
            _collectionPage = Mathf.Clamp(_collectionPage, 0, maximumPage);
            collectionPageText.text = $"第 {_collectionPage + 1} / {maximumPage + 1} 页 · 已收藏 {_personaCollection.Count} 张";
            collectionPreviousButton.interactable = _collectionPage > 0;
            collectionNextButton.interactable = _collectionPage < maximumPage;

            for (var visibleIndex = 0; visibleIndex < pageSize; visibleIndex++)
            {
                var collectionIndex = _collectionPage * pageSize + visibleIndex;
                var hasDefinition = collectionIndex < _personaCollection.Count;
                collectionCardButtons[visibleIndex].interactable = hasDefinition;
                collectionCardTexts[visibleIndex].text = hasDefinition
                    ? $"{_personaCollection[collectionIndex].DisplayName}\n{PersonaRule(_personaCollection[collectionIndex])}"
                    : "空";
                collectionCardButtons[visibleIndex].targetGraphic.color = collectionIndex == _selectedCollectionIndex
                    ? new Color32(114, 86, 40, 255)
                    : new Color32(34, 34, 33, 248);
            }

            for (var slotIndex = 0; slotIndex < collectionEquipmentButtons.Length; slotIndex++)
            {
                var definition = _personaLoadout.Slots[slotIndex];
                collectionEquipmentTexts[slotIndex].text = definition == null
                    ? $"0{slotIndex + 1}  空槽\n点击选择此槽"
                    : $"0{slotIndex + 1}  {definition.DisplayName}\n{PersonaRule(definition)}";
                collectionEquipmentButtons[slotIndex].targetGraphic.color = slotIndex == _selectedEquipmentSlot
                    ? new Color32(92, 69, 34, 255)
                    : new Color32(28, 29, 28, 248);
            }

            var selected = _selectedCollectionIndex >= 0 && _selectedCollectionIndex < _personaCollection.Count
                ? _personaCollection[_selectedCollectionIndex]
                : null;
            collectionDetailText.text = selected == null
                ? "请选择一张已收藏的人格牌。"
                : $"已选择：{selected.DisplayName}\n{PersonaRule(selected)}\n\n点击右侧任意槽位即可装备；已装备的人格不会重复出现。";
            collectionUnequipButton.interactable = _personaLoadout.Slots[_selectedEquipmentSlot] != null;
        }

        private void OnDestroy()
        {
            if (battleController != null)
            {
                battleController.BattleCompleted -= OnBattleCompleted;
                battleController.HandPlayed -= OnHandPlayed;
                battleController.HandDiscarded -= OnHandDiscarded;
                battleController.StableStateChanged -= SaveActiveRun;
            }
        }

        /// <summary>开始新局：人格装备保留（_personaLoadout 沿用上局选择），其余旅程状态全部重建。</summary>
        private void StartNewRun()
        {
            if (!_flow.StartNewRun()) return;
            _personaLoadout ??= new PersonaLoadoutState();
            EnsureInitialCollection();
            InitializeRun();
            Render();
            SaveActiveRun();
        }

        /// <summary>初始化一局新旅程：重建 52 张牌库、重置金币/奖励状态、生成局种子。新开局与"返回准备"重开共用。</summary>
        private void InitializeRun()
        {
            _journeyDeck = new JourneyDeckState(StandardDeckFactory.Create());
            _behaviorTracker = new RunBehaviorTracker();
            _forgeState = null;
            _selectedForgeCandidate = -1;
            _rewardClaimed = false;
            _selectedJourneyCardIndex = 0;
            _runSeed = unchecked((uint)System.Environment.TickCount);
            Debug.Log($"[Flow] 新旅程初始化：牌库 {_journeyDeck.Cards.Count} 张，金币 {_journeyDeck.Coins}，局种子 {_runSeed}。");
        }

        private void ConfirmPersonaSetup()
        {
            if (!_flow.ConfirmPersonaSetup()) return;
            if (_flow.Stage == PrototypeFlowStage.Battle)
            {
                StartCurrentBattle(); // 新局：进第 0 节点（内部会存档）
            }
            else
            {
                SaveActiveRun(); // 装备检查完毕回 Boss 揭示：战斗未开始，仅保存装备变化
            }
            Render();
        }

        private void BeginBattle()
        {
            if (!_flow.BeginBossBattle()) return;
            StartCurrentBattle();
            Render();
        }

        /// <summary>领取奖励强化：所选牌获得筹码强化后进入商店（奖励之后固定接商店，不再直接开战）。</summary>
        private void ContinueFromReward()
        {
            if (_journeyDeck == null || _rewardClaimed || !_journeyDeck.GrantRewardEnhancement(SelectedJourneyCard.Id)) return;
            _rewardClaimed = true;
            if (!_flow.ContinueFromReward()) return;
            Debug.Log($"[Flow] 已领取节点 {_flow.NodeIndex} 的奖励强化（{SelectedJourneyCard.Id}），进入商店。");
            SaveActiveRun();
            Render();
        }

        /// <summary>离开商店：推进到下一节点；普通战直接开战，Boss 战先进入揭示界面。</summary>
        private void ContinueFromShop()
        {
            if (!_flow.ContinueFromShop()) return;
            if (_flow.Stage == PrototypeFlowStage.Battle)
            {
                StartCurrentBattle(); // 下一场是普通战：直接开战（内部会存档）
            }
            else
            {
                SaveActiveRun(); // 下一场是 Boss 战：停留在揭示界面
            }
            Render();
        }

        /// <summary>按路线表启动当前节点战斗：目标分、场次种子、Boss 均来自 RunRoute 与局种子。</summary>
        private void StartCurrentBattle()
        {
            var node = RunRoute.GetNode(_flow.NodeIndex);
            var seed = unchecked(_runSeed + (uint)(node.Index + 1)); // 场次种子由局种子派生，保证同局可复现
            var boss = node.kind == RunNodeKind.BossBattle
                ? BossEncounterCatalog.CreateFromPool(node.bossPoolId) // TODO(P0-3)：按池出 Boss，当前临时统一返回镜厅守门人
                : null;
            if (node.kind == RunNodeKind.BossBattle)
                Debug.LogWarning($"[Boss] 节点 {node.Index} 难度池 {node.bossPoolId} 尚未落地（TODO P0-3），临时返回镜厅守门人。");
            battleController.BeginBattle(node.targetScore, seed, _journeyDeck.CreateBattleDeck(), _personaLoadout.CreateLoadout(), boss);
            battleProgressText.text = $"旅程 {node.Index + 1} / {RunRoute.BattleCount}";
            Debug.Log($"[Flow] 开始节点 {node.Index}（{node.kind}）：目标分 {node.targetScore}，场次种子 {seed}" +
                      (boss == null ? "，无 Boss。" : $"，Boss：{boss.Definition.EncounterId}。"));
            SaveActiveRun();
        }

        private void ReturnToMainMenu()
        {
            _collectionOpen = false;
            _flow.ReturnToMainMenu();
            SaveInactiveProfile();
            Render();
        }

        /// <summary>Boss 揭示界面"返回检查装备"：保留本局（牌库/种子/节点不变），回装备界面；确认装备后回到揭示界面。</summary>
        private void ReturnToPersonaSetup()
        {
            if (!_flow.ReturnToPersonaSetup()) return;
            Debug.Log($"[Flow] 从 Boss 揭示返回装备检查：本局保留，节点 {_flow.NodeIndex}。");
            SaveActiveRun();
            Render();
        }

        private void OnHandPlayed(Cards.Hands.HandType handType, int cardCount, long score)
        {
            _behaviorTracker?.RecordPlay(handType, cardCount, score);
        }

        private void OnHandDiscarded(int cardCount)
        {
            _behaviorTracker?.RecordDiscard(cardCount);
        }

        private void ContinueToForge()
        {
            if (!_flow.ContinueToForge()) return;
            _forgeState = new PersonaForgeState(_behaviorTracker.CreateReport(), 20260820u);
            _selectedForgeCandidate = -1;
            RefreshForge();
            SaveActiveRun();
            Render();
        }

        private void SelectForgeCandidate(int candidateIndex)
        {
            _selectedForgeCandidate = candidateIndex;
            RefreshForge();
        }

        private void ConfirmForgeCandidate()
        {
            if (_selectedForgeCandidate < 0) return;
            var candidate = _forgeState.Candidates[_selectedForgeCandidate];
            _personaCollection.Add(candidate);
            var slotIndex = _personaLoadout.Equip(candidate);
            forgeStatusText.text = slotIndex >= 0
                ? $"已获得 {candidate.DisplayName}，装备至 0{slotIndex + 1} 槽"
                : $"已获得 {candidate.DisplayName}，装备已满，请选择替换槽位";
            RefreshPersonaLoadout();
            _flow.ReturnToMainMenu();
            _collectionOpen = true;
            _selectedCollectionIndex = _personaCollection.Count - 1;
            _collectionPage = _selectedCollectionIndex / collectionCardButtons.Length;
            _selectedEquipmentSlot = slotIndex >= 0 ? slotIndex : PersonaLoadout.SlotCount - 1;
            SaveInactiveProfile();
            Render();
            RefreshCollection();
        }

        private void OnBattleCompleted(BattleStatus status, long score, long target)
        {
            var won = status == BattleStatus.Won;
            if (!_flow.CompleteBattle(won)) return;
            Debug.Log($"[Flow] 战斗结算：{(won ? "胜利" : "失败")}，得分 {score} / 目标 {target}，进入阶段 {_flow.Stage}。");

            if (!won)
            {
                resultTitleText.text = "战斗失败";
                resultSummaryText.text = $"本局旅程已经结束。\n\n最终得分  {score}\n距离目标还差  {Math.Max(0L, target - score)}";
            }
            else if (_flow.Stage == PrototypeFlowStage.RunReport)
            {
                var report = _behaviorTracker.CreateReport();
                reportSummaryText.text = $"{report.Title}\n\n主导牌型：{HandName(report.DominantHand)}\n牌型集中度：{report.Focus}%    筛选倾向：{report.Restraint}%    得分效率：{report.Efficiency}%\n\n有效出牌 {report.Plays} 次 · 弃牌 {report.Discards} 次 · 累计得分 {report.Score}";
            }
            else
            {
                // 胜利且非最终节点 → 奖励阶段：重置选牌与领取状态（每场胜利都会经过这里，必须在每次进入时重置）
                _selectedJourneyCardIndex = 0;
                _rewardClaimed = false;
            }
            SaveActiveRun();
            Render();
        }

        private PlayingCardInstance SelectedJourneyCard => _journeyDeck.Cards[_selectedJourneyCardIndex];

        private void CyclePersonaSlot(int slotIndex)
        {
            _personaLoadout.CycleSlot(slotIndex);
            RefreshPersonaLoadout();
        }

        private void RefreshPersonaLoadout()
        {
            if (_personaLoadout == null) return;
            for (var index = 0; index < _personaLoadout.Slots.Count; index++)
            {
                var definition = _personaLoadout.Slots[index];
                personaSlotNameTexts[index].text = definition == null
                    ? $"0{index + 1}  空槽"
                    : $"0{index + 1}  {definition.DisplayName}";
                personaSlotRuleTexts[index].text = PersonaRule(definition);
                battlePersonaNameTexts[index].text = personaSlotNameTexts[index].text;
                battlePersonaRuleTexts[index].text = personaSlotRuleTexts[index].text;
            }
        }

        private static string PersonaRule(PersonaCardDefinition definition)
        {
            if (definition == null) return "点击其他槽位可交换空位";
            return definition.ConditionKind == PersonaConditionKind.Always
                ? definition.EffectKind switch
                {
                    PersonaEffectKind.AddChips => $"+{definition.EffectValue:0} 筹码",
                    PersonaEffectKind.AddMultiplier => $"+{definition.EffectValue:0.0} 倍率",
                    _ => $"最终 ×{definition.EffectValue:0.00}"
                }
                : ForgeRule(definition);
        }

        private void RefreshForge()
        {
            if (_forgeState == null) return;
            forgeRollsText.text = $"映照 D20：{_forgeState.Rolls[0]}     偏转 D20：{_forgeState.Rolls[1]}     裂变 D20：{_forgeState.Rolls[2]}";
            for (var index = 0; index < _forgeState.Candidates.Count; index++)
            {
                var candidate = _forgeState.Candidates[index];
                forgeCandidateTexts[index].text = $"{candidate.DisplayName}\n\n{ForgeRule(candidate)}";
                forgeCandidateButtons[index].targetGraphic.color = index == _selectedForgeCandidate
                    ? new Color32(91, 70, 34, 255)
                    : new Color32(55, 47, 35, 250);
            }
            forgeStatusText.text = _selectedForgeCandidate < 0
                ? "请选择一张人格牌；另两张将在确认后消失"
                : $"已选择：{_forgeState.Candidates[_selectedForgeCandidate].DisplayName}，请再次确认";
            forgeConfirmButton.interactable = _selectedForgeCandidate >= 0;
        }

        private static string ForgeRule(PersonaCardDefinition definition)
        {
            return definition.EffectKind switch
            {
                PersonaEffectKind.AddChips => $"{HandName(definition.MinimumHandType)}或更高：+{definition.EffectValue:0} 筹码",
                PersonaEffectKind.AddMultiplier => $"{HandName(definition.MinimumHandType)}或更高：+{definition.EffectValue:0.0} 倍率",
                _ => $"{HandName(definition.MinimumHandType)}或更高：最终 ×{definition.EffectValue:0.00}"
            };
        }

        private static string HandName(Cards.Hands.HandType handType) => handType switch
        {
            Cards.Hands.HandType.Pair => "对子",
            Cards.Hands.HandType.TwoPair => "两对",
            Cards.Hands.HandType.ThreeOfAKind => "三条",
            Cards.Hands.HandType.Straight => "顺子",
            Cards.Hands.HandType.Flush => "同花",
            Cards.Hands.HandType.FullHouse => "葫芦",
            Cards.Hands.HandType.FourOfAKind => "四条",
            Cards.Hands.HandType.StraightFlush => "同花顺",
            _ => "高牌"
        };

        private void SelectPreviousJourneyCard()
        {
            _selectedJourneyCardIndex = (_selectedJourneyCardIndex - 1 + _journeyDeck.Cards.Count) % _journeyDeck.Cards.Count;
            RefreshJourneyCardText();
        }

        private void SelectNextJourneyCard()
        {
            _selectedJourneyCardIndex = (_selectedJourneyCardIndex + 1) % _journeyDeck.Cards.Count;
            RefreshJourneyCardText();
        }

        private void Purchase(JourneyDeckAction action)
        {
            var card = SelectedJourneyCard;
            if (!_journeyDeck.TryPurchase(action, card.Id))
            {
                shopStatusText.text = "购买失败：金币不足或目标无效";
                return;
            }
            _selectedJourneyCardIndex = Mathf.Clamp(_selectedJourneyCardIndex, 0, _journeyDeck.Cards.Count - 1);
            shopStatusText.text = $"已完成{ActionName(action)}，剩余金币 {_journeyDeck.Coins}";
            RefreshJourneyCardText();
            SaveActiveRun();
        }

        private void ContinueSavedRun()
        {
            if (!_saveStore.TryLoad(out var data) || !data.hasActiveRun) return;
            try
            {
                _journeyDeck = new JourneyDeckState(data.deck.Select(FromSavedCard), Math.Max(0, data.coins));
                _personaCollection.Clear();
                _personaCollection.AddRange(data.collection.Where(item => !item.isEmpty).Select(FromSavedPersona));
                EnsureInitialCollection();
                var equipped = data.equipped.Select(item => item.isEmpty ? null : FromSavedPersona(item)).ToArray();
                if (equipped.Length != PersonaLoadout.SlotCount)
                    equipped = InitialPersonaCatalog.CreateDefaultLoadout().Slots.Select(slot => slot.Definition).ToArray();
                _personaLoadout = new PersonaLoadoutState(equipped);
                _behaviorTracker = new RunBehaviorTracker();
                var handCounts = data.behavior.handTypes.Zip(data.behavior.handCounts,
                    (type, count) => new KeyValuePair<HandType, int>((HandType)type, count));
                _behaviorTracker.Restore(data.behavior.plays, data.behavior.discards, data.behavior.cardsPlayed,
                    data.behavior.cardsDiscarded, data.behavior.score, handCounts);
                _selectedJourneyCardIndex = Mathf.Clamp(data.selectedJourneyCardIndex, 0, _journeyDeck.Cards.Count - 1);
                _rewardClaimed = data.rewardClaimed;
                _runSeed = data.runSeed; // 旧档无该字段时为 0：场次种子退化为 节点序号+1，仍可复现
                var stage = (PrototypeFlowStage)data.stage;
                _flow.Restore(stage, data.battleNumber, data.personaSetupReturnsToBossReveal); // JSON 字段名沿用 battleNumber，语义为节点索引（P0-8 重命名）
                Debug.Log($"[Flow] 恢复存档：阶段 {stage}，节点 {data.battleNumber}，局种子 {_runSeed}，金币 {_journeyDeck.Coins}。");
                if (stage == PrototypeFlowStage.PersonaForge)
                    _forgeState = new PersonaForgeState(_behaviorTracker.CreateReport(), 20260820u);
                if (stage == PrototypeFlowStage.Battle && data.battle != null && data.battle.hasSnapshot)
                {
                    battleController.RestoreBattle(FromSavedBattle(data.battle), _personaLoadout.CreateLoadout());
                    // 快照恢复不经过 StartCurrentBattle，进度文案需在此显式刷新（走查反馈修复：否则残留场景默认文案）
                    battleProgressText.text = $"旅程 {_flow.NodeIndex + 1} / {RunRoute.BattleCount}";
                }
                else if (stage == PrototypeFlowStage.Battle)
                {
                    StartCurrentBattle();
                }
                Render();
                RefreshPersonaLoadout();
                RefreshForge();
            }
            catch (Exception exception)
            {
                // 恢复失败：回到干净的主菜单，避免玩家卡在残缺界面；存档保留原样，新开一局会整体覆盖
                Debug.LogWarning($"活动旅程恢复失败：{exception.Message}");
                _flow.ReturnToMainMenu();
                continueButton.interactable = false;
                Render();
            }
        }

        private void LoadProfile()
        {
            if (!_saveStore.TryLoad(out var data))
            {
                _personaLoadout = new PersonaLoadoutState();
                EnsureInitialCollection();
                return;
            }
            try
            {
                _personaCollection.Clear();
                _personaCollection.AddRange(data.collection.Where(item => !item.isEmpty).Select(FromSavedPersona));
                var equipped = data.equipped.Select(item => item.isEmpty ? null : FromSavedPersona(item)).ToArray();
                _personaLoadout = equipped.Length == PersonaLoadout.SlotCount
                    ? new PersonaLoadoutState(equipped)
                    : new PersonaLoadoutState();
                EnsureInitialCollection();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"人格档案恢复失败：{exception.Message}");
                _personaLoadout = new PersonaLoadoutState();
                _personaCollection.Clear();
                EnsureInitialCollection();
            }
        }

        private void EnsureInitialCollection()
        {
            foreach (var definition in new[]
            {
                InitialPersonaCatalog.Accumulator, InitialPersonaCatalog.Executor, InitialPersonaCatalog.Ambitious
            })
            {
                if (_personaCollection.All(item => item.TemplateId != definition.TemplateId))
                    _personaCollection.Add(definition);
            }
        }

        private void SaveActiveRun() => Save(true);
        private void SaveInactiveProfile() => Save(false);

        private void SaveProfileOnly()
        {
            if (_saveStore == null || _personaLoadout == null) return;
            if (!_saveStore.TryLoad(out var data)) data = new PrototypeSaveData();
            data.schemaVersion = 3;
            data.collection = _personaCollection.Select(ToSavedPersona).ToList();
            data.equipped = _personaLoadout.Slots.Select(ToSavedPersona).ToList();
            _saveStore.Save(data);
            continueButton.interactable = data.hasActiveRun;
        }

        private void Save(bool hasActiveRun)
        {
            if (_saveStore == null || _journeyDeck == null || _personaLoadout == null || _behaviorTracker == null) return;
            var data = new PrototypeSaveData
            {
                hasActiveRun = hasActiveRun,
                stage = (int)_flow.Stage,
                battleNumber = _flow.NodeIndex, // JSON 字段名沿用 battleNumber（P0-8 升 schema v4 时重命名为 nodeIndex），语义为节点索引
                personaSetupReturnsToBossReveal = _flow.PersonaSetupReturnsToBossReveal,
                runSeed = _runSeed,
                coins = _journeyDeck.Coins,
                selectedJourneyCardIndex = _selectedJourneyCardIndex,
                rewardClaimed = _rewardClaimed,
                deck = _journeyDeck.Cards.Select(ToSavedCard).ToList(),
                collection = _personaCollection.Select(ToSavedPersona).ToList(),
                equipped = _personaLoadout.Slots.Select(ToSavedPersona).ToList(),
                behavior = ToSavedBehavior(_behaviorTracker)
            };
            if (_flow.Stage == PrototypeFlowStage.Battle && battleController.Battle != null &&
                !battleController.Battle.IsPresentationLocked)
                data.battle = ToSavedBattle(battleController.Battle);
            _saveStore.Save(data);
            continueButton.interactable = hasActiveRun;
        }

        private static SavedPlayingCard ToSavedCard(PlayingCardInstance card) => new SavedPlayingCard
        {
            id = card.Id, suit = (int)card.Suit, rank = (int)card.Rank, enhancement = (int)card.Enhancement
        };

        private static PlayingCardInstance FromSavedCard(SavedPlayingCard card) =>
            new PlayingCardInstance(card.id, (Suit)card.suit, (Rank)card.rank, (CardEnhancement)card.enhancement);

        private static SavedPersona ToSavedPersona(PersonaCardDefinition definition)
        {
            if (definition == null) return new SavedPersona { isEmpty = true };
            return new SavedPersona
            {
                templateId = definition.TemplateId,
                displayName = definition.DisplayName,
                conditionKind = (int)definition.ConditionKind,
                minimumHandType = (int)definition.MinimumHandType,
                effectKind = (int)definition.EffectKind,
                effectValue = definition.EffectValue.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static PersonaCardDefinition FromSavedPersona(SavedPersona persona) => new PersonaCardDefinition(
            persona.templateId, persona.displayName, (PersonaConditionKind)persona.conditionKind,
            (HandType)persona.minimumHandType, (PersonaEffectKind)persona.effectKind,
            decimal.Parse(persona.effectValue, CultureInfo.InvariantCulture));

        private static SavedBehavior ToSavedBehavior(RunBehaviorTracker tracker)
        {
            var behavior = new SavedBehavior
            {
                plays = tracker.Plays,
                discards = tracker.Discards,
                cardsPlayed = tracker.CardsPlayed,
                cardsDiscarded = tracker.CardsDiscarded,
                score = tracker.Score
            };
            foreach (var pair in tracker.HandCounts)
            {
                behavior.handTypes.Add((int)pair.Key);
                behavior.handCounts.Add(pair.Value);
            }
            return behavior;
        }

        private static SavedBattle ToSavedBattle(BattleStateMachine battle) => new SavedBattle
        {
            hasSnapshot = true,
            targetScore = battle.TargetScore,
            totalScore = battle.TotalScore,
            playsRemaining = battle.PlaysRemaining,
            discardsRemaining = battle.DiscardsRemaining,
            status = (int)battle.Status,
            drawPile = battle.Deck.DrawPile.Select(ToSavedCard).ToList(),
            hand = battle.Deck.Hand.Select(ToSavedCard).ToList(),
            played = battle.Deck.Played.Select(ToSavedCard).ToList(),
            discarded = battle.Deck.Discarded.Select(ToSavedCard).ToList(),
            selectedCardIds = battle.SelectedCardIds.ToList(),
            bossEncounterId = battle.BossEncounter?.Definition.EncounterId,
            bossHandsPlayed = battle.BossEncounter?.HandsPlayed ?? 0,
            bossHasPreviousHand = battle.BossEncounter?.PreviousHandType.HasValue ?? false,
            bossPreviousHandType = (int)(battle.BossEncounter?.PreviousHandType ?? 0)
        };

        private static BattleStateSnapshot FromSavedBattle(SavedBattle battle)
        {
            if (battle == null || !battle.hasSnapshot) throw new ArgumentException("Battle snapshot is missing.");
            BossEncounterSnapshot bossSnapshot = null;
            if (!string.IsNullOrWhiteSpace(battle.bossEncounterId))
            {
                HandType? previousHand = battle.bossHasPreviousHand ? (HandType)battle.bossPreviousHandType : null;
                bossSnapshot = new BossEncounterSnapshot(battle.bossEncounterId, battle.bossHandsPlayed, previousHand);
            }
            return new BattleStateSnapshot(battle.drawPile.Select(FromSavedCard), battle.hand.Select(FromSavedCard),
                battle.played.Select(FromSavedCard), battle.discarded.Select(FromSavedCard),
                battle.selectedCardIds ?? new List<string>(), battle.targetScore, battle.totalScore,
                battle.playsRemaining, battle.discardsRemaining, (BattleStatus)battle.status, bossSnapshot);
        }

        private void RefreshJourneyCardText()
        {
            if (_journeyDeck == null) return;
            var card = SelectedJourneyCard;
            var description = $"{RankName(card.Rank)}{SuitName(card.Suit)}\n强化：{EnhancementName(card.Enhancement)}\n牌组 {_journeyDeck.Cards.Count} 张";
            rewardCardText.text = description;
            shopCardText.text = description + $"\n金币：{_journeyDeck.Coins}";
            shopCoinsText.text = $"当前金币：{_journeyDeck.Coins}";
            var canPurchase = _journeyDeck.Coins >= 2;
            shopDeleteButton.interactable = canPurchase;
            shopReforgeButton.interactable = canPurchase;
            shopEnhanceButton.interactable = canPurchase;
        }

        private static string ActionName(JourneyDeckAction action) => action switch
        {
            JourneyDeckAction.Delete => "删除",
            JourneyDeckAction.Reforge => "重刻",
            _ => "强化"
        };

        private static string SuitName(Suit suit) => suit switch
        {
            Suit.Clubs => "梅花", Suit.Diamonds => "方片", Suit.Hearts => "红桃", _ => "黑桃"
        };

        private static string RankName(Rank rank) => rank switch
        {
            Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J", _ => ((int)rank).ToString()
        };

        private static string EnhancementName(CardEnhancement enhancement) => enhancement switch
        {
            CardEnhancement.ChipBoost => "筹码 +20",
            CardEnhancement.MultBoost => "倍率强化",
            _ => "无"
        };

        private void Render()
        {
            mainMenuScreen.SetActive(_flow.Stage == PrototypeFlowStage.MainMenu && !_collectionOpen);
            collectionScreen.SetActive(_flow.Stage == PrototypeFlowStage.MainMenu && _collectionOpen);
            personaSetupScreen.SetActive(_flow.Stage == PrototypeFlowStage.PersonaSetup);
            bossRevealScreen.SetActive(_flow.Stage == PrototypeFlowStage.BossReveal);
            if (_flow.Stage == PrototypeFlowStage.BossReveal && bossRevealRuleText != null)
            {
                var node = RunRoute.GetNode(_flow.NodeIndex);
                // TODO(P0-3/P0-9)：Boss 名称与规则文本应按难度池数据驱动；当前临时统一展示镜厅守门人。
                // TODO(P0-2)：出牌/弃牌次数将参数化，当前沿用固定值 4/3。
                var encounter = BossEncounterCatalog.CreateFromPool(node.bossPoolId).Definition;
                bossRevealRuleText.text = $"主规则 · {encounter.RuleName}\n{encounter.RuleDescription}\n\n介入事件 · {encounter.InterventionName}\n{encounter.InterventionDescription}\n\n目标分数：{node.targetScore}　出牌：4　弃牌：3";
            }
            battleScreen.SetActive(_flow.Stage == PrototypeFlowStage.Battle);
            rewardScreen.SetActive(_flow.Stage == PrototypeFlowStage.Reward);
            shopScreen.SetActive(_flow.Stage == PrototypeFlowStage.Shop);
            runReportScreen.SetActive(_flow.Stage == PrototypeFlowStage.RunReport);
            personaForgeScreen.SetActive(_flow.Stage == PrototypeFlowStage.PersonaForge);
            failureResultScreen.SetActive(_flow.Stage == PrototypeFlowStage.FailureResult);
            if (_flow.Stage == PrototypeFlowStage.Reward || _flow.Stage == PrototypeFlowStage.Shop)
            {
                RefreshJourneyCardText();
            }
            if (_flow.Stage == PrototypeFlowStage.PersonaSetup) RefreshPersonaLoadout();
        }
    }
}
