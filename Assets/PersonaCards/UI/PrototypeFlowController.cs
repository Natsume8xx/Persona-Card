using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PersonaCards.Battle;
using PersonaCards.Battle.Bosses;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Battle.Personas;
using PersonaCards.Cards;
using PersonaCards.Cards.Hands;
using PersonaCards.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PersonaCards.Core;

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
        /// <summary>牌型配置资产（P0-1C 数据驱动）：Awake 时注入 HandTypeCatalog；未配置或校验失败时回落白盒（= 配表当前初值）。</summary>
        [SerializeField] private HandTypeAsset handTypes;
        /// <summary>卡牌配置资产（P0-1D 数据驱动）：Awake 时注入 PlayingCardRules；未配置或校验失败时回落白盒（= 配表当前初值）。</summary>
        [SerializeField] private CardConfigAsset cardConfig;
        /// <summary>人格牌配置资产（P0-1E 数据驱动）：Awake 时注入 InitialPersonaCatalog；未配置或校验失败时回落白盒（= 空模板目录，教学 3 张）。</summary>
        [SerializeField] private PersonaConfigAsset personaConfig;
        /// <summary>全局配置资产（P0-1F 数据驱动）：Awake 时注入 GlobalConfig；未配置或校验失败时回落白盒（= 空配置，出牌/弃牌回落 4/3）。</summary>
        [SerializeField] private GlobalConfigAsset globalConfig;
        /// <summary>商店商品资产（P0-7 数据驱动）：Awake 时注入 ShopCatalog；未配置时回落空列表（商店位全部「无货」）。</summary>
        [SerializeField] private ShopProductAsset shopProducts;
        /// <summary>商店商品刷新规则资产（P0-7 数据驱动）：同上，空配置回落空列表。</summary>
        [SerializeField] private ShopPoolRefreshAsset shopPools;
        /// <summary>商店商品槽位刷新规则资产（P0-7 数据驱动）：同上，空配置回落空列表。</summary>
        [SerializeField] private ShopSlotRefreshAsset shopSlots;
        /// <summary>教程遮罩根（P0-1G，Battle 屏子对象）：仅教程激活时显示，遮罩 Image 拦截下层战斗按钮点击。</summary>
        [SerializeField] private GameObject tutorialOverlay;
        /// <summary>教程面板内：步骤文本（右上「教学 n / 5」）/标题/正文。</summary>
        [SerializeField] private Text tutorialStepText;
        [SerializeField] private Text tutorialTitleText;
        [SerializeField] private Text tutorialBodyText;
        /// <summary>教程面板内按钮：下一步（末步文案「完成」）/跳过教学。</summary>
        [SerializeField] private Button tutorialNextButton;
        [SerializeField] private Button tutorialSkipButton;
        /// <summary>下一步按钮的文案标签（按钮子对象 Label）：末步显示「完成」。</summary>
        [SerializeField] private Text tutorialNextLabel;
        /// <summary>主菜单重播入口：按钮 + 文案标签（点击后文案变「✓ 下次战斗播放教学」作为即时反馈）。</summary>
        [SerializeField] private Button tutorialReplayButton;
        [SerializeField] private Text tutorialReplayLabel;
        // —— P0-1H 设置系统（ConfigureSettings 注入）——
        /// <summary>主菜单卡片内的「设置」入口按钮：点击打开设置界面（仅主菜单阶段）。</summary>
        [SerializeField] private Button settingsEntryButton;
        // —— P0-1I 主菜单四入口（ConfigureCompendium 注入）——
        /// <summary>图鉴占位屏根（Canvas 下独立屏）：主菜单覆盖屏，内容待策划确认（B5）。</summary>
        [SerializeField] private GameObject compendiumScreen;
        /// <summary>主菜单卡片内的「图鉴」入口按钮。</summary>
        [SerializeField] private Button compendiumEntryButton;
        /// <summary>图鉴屏「← 返回」按钮。</summary>
        [SerializeField] private Button compendiumBackButton;
        /// <summary>主菜单卡片内的「退出游戏」按钮。</summary>
        [SerializeField] private Button quitGameButton;
        /// <summary>设置界面根（Canvas 下独立屏）：主菜单 overlay，遮罩拦截下层点击。</summary>
        [SerializeField] private GameObject settingsOverlay;
        /// <summary>亮度/主音量滑条与百分比文本（策划 12.3.1：百分比立即预览）。</summary>
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Text brightnessValueText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        /// <summary>界面动效/屏幕震动开关（Toggle）。</summary>
        [SerializeField] private Toggle animationToggle;
        [SerializeField] private Toggle shakeToggle;
        /// <summary>三个快捷键按钮与文案标签（点击进入改键态，文案显示当前键）。</summary>
        [SerializeField] private Button playKeyButton;
        [SerializeField] private Text playKeyLabel;
        [SerializeField] private Button discardKeyButton;
        [SerializeField] private Text discardKeyLabel;
        [SerializeField] private Button settingsKeyButton;
        [SerializeField] private Text settingsKeyLabel;
        /// <summary>设置界面内的「重新查看战斗教学」入口：与主菜单按钮共用 toggle 逻辑（P0-1G 移交）。</summary>
        [SerializeField] private Button settingsTutorialReplayButton;
        [SerializeField] private Text settingsTutorialReplayLabel;
        /// <summary>顶部返回 / 底部「返回主界面」「恢复默认」「取消」「保存」按钮。</summary>
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button settingsReturnButton;
        [SerializeField] private Button settingsRestoreDefaultsButton;
        [SerializeField] private Button settingsCancelButton;
        [SerializeField] private Button settingsSaveButton;
        /// <summary>全局亮度 dim 层（Canvas 根最上层，raycastTarget=false）：alpha = 1 - 亮度。</summary>
        [SerializeField] private Image dimImage;

        private readonly PrototypeFlowStateMachine _flow = new PrototypeFlowStateMachine();
        private JourneyDeckState _journeyDeck;
        private PersonaLoadoutState _personaLoadout;
        /// <summary>三线强化等级（P0-11）：新局重置、读档还原、战斗/商店共用同一实例。</summary>
        private EnhancementState _enhancements = new EnhancementState();

        // 槽位视觉配色（与场景构建器 BattlePrototypeSceneBuilder 的原始值一致）：
        // 有卡 = 金棕底 + PaleGold 名 + 白规则；空槽 = 灰底 + 灰文本
        private static readonly Color SlotNameGold = new Color32(232, 214, 173, 255); // PaleGold
        private static readonly Color32 SetupSlotBackground = new Color32(58, 49, 35, 250);
        private static readonly Color32 SetupEmptySlotBackground = new Color32(37, 37, 39, 245);
        private static readonly Color32 BattleSlotBackground = new Color32(54, 47, 36, 245);
        private static readonly Color32 BattleEmptySlotBackground = new Color32(35, 35, 36, 210);
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
        /// <summary>商店继续按钮的文案标签（按钮子对象，延迟获取并缓存）。</summary>
        private Text _shopContinueLabel;
        /// <summary>商店运行时状态（P0-7）：进入商店时按节点种子生成商品位，读档同种子重建可复现；售罄态不随存档走（P0-8 入快照）。</summary>
        private ShopState _shopState;
        /// <summary>商店三个服务按钮（P0-7 起复用为商品位，顺序 = ShopState 槽位序：卡牌/人格牌/服务）。</summary>
        private Button[] _shopSlotButtons;
        /// <summary>三个商品位按钮的文案标签（按钮子对象，延迟获取并缓存）。</summary>
        private readonly Text[] _shopSlotLabels = new Text[3];
        // P0-11：强化选择模式——点强化服务槽位进入，slot0=确认/slot1=取消/slot2=服务名，轮换复用 Prev/Next；离开商店强制回 Normal
        private ShopScreenMode _shopMode = ShopScreenMode.Normal;
        /// <summary>强化选择会话（仅 EnhancementSelect 模式非 null；瞬态不入存档，读档回 Normal）。</summary>
        private ShopEnhancementSession _shopEnhancement;
        /// <summary>被选择的强化服务槽位（确认成功后标记售罄）。</summary>
        private int _shopEnhancementSlotIndex;
        /// <summary>获得新人格牌弹窗会话（UI 重排第一批）：弹窗打开期间非 null，关闭后置 null；瞬态不入存档（读档回铸牌屏重铸）。</summary>
        private PersonaEquipPromptSession _equipPromptSession;
        /// <summary>获得新人格牌弹窗视图实例（Resources/Prefabs/PersonaEquipPopup 的运行时实例，Canvas 最末兄弟节点覆盖全屏）。</summary>
        private PersonaEquipPopupView _equipPrompt;
        /// <summary>弹窗 prefab（Awake 缓存；缺失时确认铸造降级为不装备直接推进，收藏已入不受损）。</summary>
        private PersonaEquipPopupView _equipPromptPrefab;

        /// <summary>商店屏模式（P0-11）：Normal = 普通商品位；EnhancementSelect = 强化目标选择（零新节点，纯语义切换）。</summary>
        private enum ShopScreenMode
        {
            Normal,
            EnhancementSelect
        }
        // P0-9：揭示屏 Boss 名牌/台词（场景静态文本按名查找缓存；Find 失败后不再重试，显示场景默认值）
        private Text _bossRevealNameText;
        private Text _bossRevealLineText;
        private bool _bossRevealTextsResolved;
        // P0-6：奖励屏说明文本（Reward Rule）与金币结算行（按名查找缓存；基础句取场景静态原文，Find 失败后不追加金币行）
        private Text _rewardRuleText;
        private string _rewardRuleBaseText;
        private bool _rewardRuleResolved;
        /// <summary>教程序列状态（P0-1G）：五步推进逻辑在 TutorialSequence 纯类中，本类只做 UI 接线。</summary>
        private readonly TutorialSequence _tutorial = new TutorialSequence();
        /// <summary>重播请求标志：主菜单「战斗教学」按钮置位，下一次进入战斗时消耗并自动播放。</summary>
        private bool _tutorialReplayRequested;
        /// <summary>教程已看 PlayerPrefs 键（P0-1G）：首次进战斗自动播放判定；重播请求不读写此标志。</summary>
        private const string TutorialSeenKey = "PersonaCards.TutorialSeen";
        /// <summary>设置界面是否打开（P0-1H）：主菜单 overlay，不进入流程状态机。</summary>
        private bool _settingsOpen;
        /// <summary>图鉴占位屏是否打开（P0-1I）：主菜单覆盖屏，与设置互斥（ESC 优先关图鉴）。</summary>
        private bool _compendiumOpen;
        /// <summary>进入设置时的快照：「取消/返回主界面」回滚到此（12.3.1「未保存修改不生效」）。</summary>
        private GameSettingsData _settingsSnapshot;
        /// <summary>正在改键的目标（None = 未在改键；再点同一条目退出改键态）。</summary>
        private RebindingTarget _rebinding;
        /// <summary>刷新设置 UI 时置位：防止 Slider/Toggle 值回写触发 onValueChanged 递归。</summary>
        private bool _refreshingSettingsUi;
        /// <summary>设置存档（P0-1H）：独立 JSON 文件，与战役存档（PrototypeSaveStore）互不影响。</summary>
        private GameSettingsStore _settingsStore;

        /// <summary>改键目标：对应设置界面操作区的三个快捷键按钮。</summary>
        private enum RebindingTarget { None, Play, Discard, Settings }

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
            BattlePrototypeController battlePrototype, RunRouteAsset routeAsset, HandTypeAsset handTypeAsset,
            CardConfigAsset cardConfigAsset, PersonaConfigAsset personaConfigAsset, GlobalConfigAsset globalConfigAsset,
            GameObject tutorialOverlayRoot, Text tutorialStep, Text tutorialTitle, Text tutorialBody,
            Button tutorialNext, Button tutorialSkip, Text tutorialNextButtonLabel, Button tutorialReplay, Text tutorialReplayLabelText)
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
            handTypes = handTypeAsset;
            cardConfig = cardConfigAsset;
            personaConfig = personaConfigAsset;
            globalConfig = globalConfigAsset;
            tutorialOverlay = tutorialOverlayRoot;
            tutorialStepText = tutorialStep;
            tutorialTitleText = tutorialTitle;
            tutorialBodyText = tutorialBody;
            tutorialNextButton = tutorialNext;
            tutorialSkipButton = tutorialSkip;
            tutorialNextLabel = tutorialNextButtonLabel;
            tutorialReplayButton = tutorialReplay;
            tutorialReplayLabel = tutorialReplayLabelText;
        }

        /// <summary>
        /// 设置界面引用注入（P0-1H）：独立于主 Configure 签名（后者已近 40 参，不再膨胀）。
        /// SceneBuilder 构建 Settings Screen 后调用；字段名同样进 journey validation 校验列表。
        /// </summary>
        public void ConfigureSettings(Button mainMenuEntry, GameObject overlay,
            Slider brightness, Text brightnessValue, Slider volume, Text volumeValue,
            Toggle animation, Toggle shake,
            Button playKey, Text playKeyText, Button discardKey, Text discardKeyText,
            Button settingsKey, Text settingsKeyText,
            Button tutorialReplay, Text tutorialReplayText,
            Button back, Button returnMain, Button restoreDefaults, Button cancel, Button save,
            Image dim)
        {
            settingsEntryButton = mainMenuEntry;
            settingsOverlay = overlay;
            brightnessSlider = brightness;
            brightnessValueText = brightnessValue;
            volumeSlider = volume;
            volumeValueText = volumeValue;
            animationToggle = animation;
            shakeToggle = shake;
            playKeyButton = playKey;
            playKeyLabel = playKeyText;
            discardKeyButton = discardKey;
            discardKeyLabel = discardKeyText;
            settingsKeyButton = settingsKey;
            settingsKeyLabel = settingsKeyText;
            settingsTutorialReplayButton = tutorialReplay;
            settingsTutorialReplayLabel = tutorialReplayText;
            settingsBackButton = back;
            settingsReturnButton = returnMain;
            settingsRestoreDefaultsButton = restoreDefaults;
            settingsCancelButton = cancel;
            settingsSaveButton = save;
            dimImage = dim;
        }

        /// <summary>
        /// 图鉴占位与退出按钮引用注入（P0-1I）：同 ConfigureSettings 惯例独立注入，不膨胀主 Configure 签名。
        /// SceneBuilder 构建 Compendium Screen 后调用；字段名同样进 journey validation 校验列表。
        /// </summary>
        public void ConfigureCompendium(GameObject overlay, Button mainMenuEntry, Button back, Button quitGame)
        {
            compendiumScreen = overlay;
            compendiumEntryButton = mainMenuEntry;
            compendiumBackButton = back;
            quitGameButton = quitGame;
        }

        /// <summary>
        /// 商店三资产引用注入（P0-7）：同 ConfigureSettings 惯例独立注入，不膨胀主 Configure 签名。
        /// SceneBuilder 在 Configure 后调用；null 时 ShopCatalog 回落空配置（商店位全部「无货」）。
        /// </summary>
        public void ConfigureShop(ShopProductAsset products, ShopPoolRefreshAsset pools, ShopSlotRefreshAsset slots)
        {
            shopProducts = products;
            shopPools = pools;
            shopSlots = slots;
        }

        private void Awake()
        {
            // 路线资产注入：null 时 RunRoute 回落内置默认路线，流程仍可跑（P0-1 数据驱动）
            if (runRoute == null)
                Debug.LogWarning("[Flow] runRoute 路线资产未配置：使用内置默认路线（13 阶段白盒）。");
            RunRoute.Configure(runRoute);

            // 牌型目录注入（P0-1C 数据驱动）：null 或校验失败 → 目录回落白盒（= 配表当前初值），判定层始终可用
            if (handTypes == null)
            {
                Debug.LogWarning("[HandType] handTypes 牌型资产未配置：使用白盒牌型配置（12 个牌型）。");
                HandTypeCatalog.Configure(null);
            }
            else if (!handTypes.Validate(out var handTypeError))
            {
                Debug.LogWarning($"[HandType] handTypes 牌型资产校验失败（{handTypeError}）：使用白盒牌型配置。");
                HandTypeCatalog.Configure(null);
            }
            else
            {
                HandTypeCatalog.Configure(handTypes.BuildEntries());
                var summary = HandTypeCatalog.LastConfiguredSummary;
                if (!string.IsNullOrEmpty(summary))
                    Debug.Log($"[HandType] 牌型目录已注入：{summary}。");
            }

            // 卡牌规则注入（P0-1D 数据驱动）：null 或校验失败 → 门面回落白盒（= 配表当前初值），计分始终可用
            if (cardConfig == null)
            {
                Debug.LogWarning("[Card] cardConfig 卡牌配置资产未配置：使用白盒卡牌配置（52 张）。");
                PlayingCardRules.Configure(null);
            }
            else if (!cardConfig.Validate(out var cardConfigError))
            {
                Debug.LogWarning($"[Card] cardConfig 卡牌配置资产校验失败（{cardConfigError}）：使用白盒卡牌配置。");
                PlayingCardRules.Configure(null);
            }
            else
            {
                PlayingCardRules.Configure(cardConfig.BuildEntries());
                var summary = PlayingCardRules.LastConfiguredSummary;
                if (!string.IsNullOrEmpty(summary))
                    Debug.Log($"[Card] 卡牌规则已注入：{summary}。");
            }
            // 人格牌模板目录注入（P0-1E 数据驱动）：null 或校验失败 → 门面回落空模板目录（教学 3 张白盒，行为零差异）
            if (personaConfig == null)
            {
                Debug.LogWarning("[Persona] personaConfig 人格牌配置资产未配置：使用教学白盒（3 张）。");
                InitialPersonaCatalog.Configure(null);
            }
            else if (!personaConfig.Validate(out var personaConfigError))
            {
                Debug.LogWarning($"[Persona] personaConfig 人格牌配置资产校验失败（{personaConfigError}）：使用教学白盒（3 张）。");
                InitialPersonaCatalog.Configure(null);
            }
            else
            {
                InitialPersonaCatalog.Configure(personaConfig.entries);
                var summary = InitialPersonaCatalog.LastConfiguredSummary;
                if (!string.IsNullOrEmpty(summary))
                    Debug.Log($"[Persona] 人格牌模板目录已注入：{summary}");
            }
            // 全局配置注入（P0-1F 数据驱动）：null 或校验失败 → 门面回落空配置（出牌/弃牌回落 4/3，行为零差异）
            if (globalConfig == null)
            {
                Debug.LogWarning("[Global] globalConfig 全局配置资产未配置：使用白盒（出牌 4 / 弃牌 3）。");
                GlobalConfig.Configure(null);
            }
            else if (!globalConfig.Validate(out var globalConfigError))
            {
                Debug.LogWarning($"[Global] globalConfig 全局配置资产校验失败（{globalConfigError}）：使用白盒（出牌 4 / 弃牌 3）。");
                GlobalConfig.Configure(null);
            }
            else
            {
                GlobalConfig.Configure(globalConfig.entries);
                var summary = GlobalConfig.LastConfiguredSummary;
                if (!string.IsNullOrEmpty(summary))
                    Debug.Log($"[Global] 全局配置已注入：{summary}");
            }
            EnhancementTableBootstrap.Load(); // P0-11：强化配表注入必须先于 ShopCatalog.Configure（合成强化池规则依赖 HasTables）
            // 商店三资产注入（P0-7 数据驱动）：null 或校验失败 → 门面回落空列表（商店位全部「无货」），流程仍可跑
            if (shopProducts == null || shopPools == null || shopSlots == null)
            {
                Debug.LogWarning("[Shop] 商店三资产未配置完整：商品/池/槽位刷新规则回落空配置（商店位「无货」）。");
                ShopCatalog.Configure(null, null, null);
            }
            else
            {
                var productValid = shopProducts.Validate(out var shopProductError);
                var poolValid = shopPools.Validate(out var shopPoolError);
                var slotValid = shopSlots.Validate(out var shopSlotError);
                if (!productValid || !poolValid || !slotValid)
                {
                    Debug.LogWarning($"[Shop] 商店资产校验失败（{shopProductError ?? shopPoolError ?? shopSlotError}）：回落空配置（商店位「无货」）。");
                    ShopCatalog.Configure(null, null, null);
                }
                else
                {
                    ShopCatalog.Configure(shopProducts.entries, shopPools.entries, shopSlots.entries);
                    Debug.Log($"[Shop] 商店配置已注入：商品 {shopProducts.entries.Count} 条、池规则 {shopPools.entries.Count} 条、槽位规则 {shopSlots.entries.Count} 条。");
                }
            }
            _shopSlotButtons = new[] { shopDeleteButton, shopReforgeButton, shopEnhanceButton }; // 三按钮复用为商品位（顺序 = 槽位序）
            // 获得新人格牌弹窗 prefab 缓存（UI 重排第一批）：缺失时 LogError，确认铸造降级为不装备直接推进
            _equipPromptPrefab = Resources.Load<PersonaEquipPopupView>("Prefabs/PersonaEquipPopup");
            if (_equipPromptPrefab == null)
                Debug.LogError("[EquipPrompt] 弹窗 prefab 缺失（Assets/PersonaCards/Resources/Prefabs/PersonaEquipPopup.prefab）：确认铸造后不装备直接推进。");
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
            // P0-11：商店屏 Prev/Next 改路由——选择模式轮换强化目标，普通模式沿用选牌语义（与奖励屏同函数，无副作用）
            shopPreviousButton.onClick.AddListener(() => OnShopDirectionButton(-1));
            shopNextButton.onClick.AddListener(() => OnShopDirectionButton(1));
            // P0-7：三个服务按钮改为商品位按钮，点击按槽位购买（槽位序 0/1/2 = 卡牌/人格牌/服务）
            shopDeleteButton.onClick.AddListener(() => PurchaseShopSlot(0));
            shopReforgeButton.onClick.AddListener(() => PurchaseShopSlot(1));
            shopEnhanceButton.onClick.AddListener(() => PurchaseShopSlot(2));
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
            // 教程按钮绑定（P0-1G）：面板内下一步/跳过 + 主菜单重播请求
            tutorialNextButton.onClick.AddListener(TutorialNext);
            tutorialSkipButton.onClick.AddListener(TutorialSkip);
            tutorialReplayButton.onClick.AddListener(ToggleTutorialReplay);
            // 设置系统绑定（P0-1H）：存档读取（异常回落默认）→ 滑条/开关/按钮绑定 → 应用当前设置
            _settingsStore = new GameSettingsStore();
            if (_settingsStore.TryLoad(out var loadedSettings) && GameSettings.TryApply(loadedSettings))
                Debug.Log("[Settings] 设置已从存档读取。");
            else
                Debug.Log("[Settings] 设置存档缺失或异常：使用默认设置。");
            settingsEntryButton.onClick.AddListener(OpenSettings);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            animationToggle.onValueChanged.AddListener(OnAnimationToggled);
            shakeToggle.onValueChanged.AddListener(OnShakeToggled);
            playKeyButton.onClick.AddListener(() => BeginRebinding(RebindingTarget.Play));
            discardKeyButton.onClick.AddListener(() => BeginRebinding(RebindingTarget.Discard));
            settingsKeyButton.onClick.AddListener(() => BeginRebinding(RebindingTarget.Settings));
            settingsTutorialReplayButton.onClick.AddListener(ToggleTutorialReplay);
            settingsBackButton.onClick.AddListener(CancelAndCloseSettings);
            settingsReturnButton.onClick.AddListener(CancelAndCloseSettings);
            settingsRestoreDefaultsButton.onClick.AddListener(RestoreDefaults);
            settingsCancelButton.onClick.AddListener(CancelAndCloseSettings);
            settingsSaveButton.onClick.AddListener(SaveSettings);
            // P0-1I 主菜单四入口绑定：图鉴占位屏开关 + 退出游戏
            compendiumEntryButton.onClick.AddListener(OpenCompendium);
            compendiumBackButton.onClick.AddListener(CloseCompendium);
            quitGameButton.onClick.AddListener(QuitApplication);
            compendiumScreen.SetActive(false);
            RefreshAppliedSettings();
            RefreshSettingsUi();
            battleController.BattleCompleted += OnBattleCompleted;
            battleController.HandPlayed += OnHandPlayed;
            battleController.HandDiscarded += OnHandDiscarded;
            battleController.StableStateChanged += SaveActiveRun;
            continueButton.interactable = _saveStore.TryLoad(out var initialSave) && initialSave.hasActiveRun;

            // 音效：全部静态按钮统一挂点击音效（叠加监听不影响业务逻辑；手牌按钮由 BattleCardView.Configure 单独挂）
            MusicManager.AttachClickSound(startButton, continueButton, collectionButton, collectionBackButton,
                collectionPreviousButton, collectionNextButton, collectionUnequipButton,
                confirmPersonaButton, beginBattleButton, resultReturnButton,
                rewardContinueButton, shopContinueButton, reportReturnButton, personaBackButton, bossBackButton,
                rewardPreviousButton, rewardNextButton, shopPreviousButton, shopNextButton,
                shopDeleteButton, shopReforgeButton, shopEnhanceButton, forgeConfirmButton,
                tutorialNextButton, tutorialSkipButton, tutorialReplayButton,
                settingsEntryButton, playKeyButton, discardKeyButton, settingsKeyButton, settingsTutorialReplayButton,
                settingsBackButton, settingsReturnButton, settingsRestoreDefaultsButton, settingsCancelButton, settingsSaveButton,
                compendiumEntryButton, compendiumBackButton, quitGameButton);
            MusicManager.AttachClickSound(collectionCardButtons);
            MusicManager.AttachClickSound(collectionEquipmentButtons);
            MusicManager.AttachClickSound(personaSlotButtons);
            MusicManager.AttachClickSound(forgeCandidateButtons);

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
                var definition = hasDefinition ? _personaCollection[collectionIndex] : null;
                collectionCardButtons[visibleIndex].interactable = hasDefinition;
                // 收藏条目样式对齐准备屏（02）：左侧立绘 + 名称（金色）+ 效果（白色）两行；
                // 立绘缺失（美术未到货/未知键）时 ApplyPortrait 置空隐藏，文本仍完整可读
                collectionCardTexts[visibleIndex].text = hasDefinition ? definition.DisplayName : "空";
                ApplyPortrait(collectionScreen,
                    $"Persona Collection Card/Collection List Panel/Collection Card {visibleIndex + 1}/Portrait Artwork",
                    definition == null ? null : PersonaArtCatalog.PortraitFor(definition.TemplateId));
                var ruleText = CollectionCardRuleText(visibleIndex);
                if (ruleText != null) ruleText.text = hasDefinition ? PersonaRule(definition) : string.Empty;
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

        /// <summary>取收藏条目右侧的「效果」文本节点（对齐准备屏样式新增的场景节点；缺失时返回 null 静默跳过）。</summary>
        private Text CollectionCardRuleText(int visibleIndex)
        {
            var node = collectionScreen.transform.Find(
                $"Persona Collection Card/Collection List Panel/Collection Card {visibleIndex + 1}/Rule");
            return node == null ? null : node.GetComponent<Text>();
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
            _enhancements = new EnhancementState(); // P0-11：新局三线强化全 0 级
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
            else if (_flow.Stage == PrototypeFlowStage.PersonaGen)
            {
                BeginMidRunForge(); // 节点 0 是生成节点：初始化铸牌并保存
                SaveActiveRun();
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

        /// <summary>领取奖励强化：所选牌获得筹码强化与本场金币奖励（P0-6 同时入账，_rewardClaimed 防重复领取），按节点配置进商店或直接推进到下一节点。</summary>
        private void ContinueFromReward()
        {
            if (_journeyDeck == null || _rewardClaimed || !_journeyDeck.GrantRewardEnhancement(SelectedJourneyCard.Id)) return;
            _rewardClaimed = true;
            var coinReward = RunRoute.CoinsRewardOf(_flow.NodeIndex);
            if (coinReward > 0)
            {
                _journeyDeck.AddCoins(coinReward);
                MusicManager.Instance.PlaySfx(MusicCatalog.SfxCoin); // coin 音效接线：金币奖励入账
            }
            if (!_flow.ContinueFromReward()) return;
            Debug.Log($"[Flow] 已领取节点 {_flow.NodeIndex} 的奖励强化（{SelectedJourneyCard.Id}）{(coinReward > 0 ? $"与金币 +{coinReward}" : "")}，去向 {_flow.Stage}。");
            HandleStageEntry(); // 商店 / Boss 揭示 / 铸牌 / 直接开战，按去向分别处理
            Render();
        }

        /// <summary>离开商店：推进到下一节点；普通战直接开战，Boss 战先进入揭示界面，生成节点进铸牌。</summary>
        private void ContinueFromShop()
        {
            if (!_flow.ContinueFromShop()) return;
            CancelEnhancementSelection(); // P0-11：离开商店强制回 Normal（防下次进店残留选择模式）
            HandleStageEntry();
            Render();
        }

        /// <summary>节点推进后的入场处理：普通战直接开战；Boss 战停留在揭示界面；生成节点初始化铸牌；商店生成商品位。</summary>
        private void HandleStageEntry()
        {
            if (_flow.Stage == PrototypeFlowStage.Battle)
            {
                StartCurrentBattle(); // 内部会存档
                return;
            }
            if (_flow.Stage == PrototypeFlowStage.PersonaGen)
            {
                BeginMidRunForge();
            }
            else if (_flow.Stage == PrototypeFlowStage.Shop)
            {
                EnterShop(); // P0-7：按节点派生种子生成商品位（同节点同种子必得同商品）
            }
            SaveActiveRun(); // Boss 揭示 / 铸牌 / 商店：战斗未开始，仅保存流程位置与牌库
        }

        /// <summary>
        /// 进入商店（P0-7）：商品位种子 = 局种子 + 节点序号 + 2000（与战斗 +1、铸牌 +1000 错开）；
        /// 存档恢复同节点重建（同种子必得同商品），售罄态不随存档走（P0-8 入快照）。
        /// </summary>
        private void EnterShop()
        {
            var node = RunRoute.GetNode(_flow.NodeIndex);
            var generationCount = RunRoute.GenerationNodeCountBefore(_flow.NodeIndex);
            var seed = unchecked(_runSeed + (uint)(node.Index + 2000));
            _shopState = new ShopState(ShopCatalog.Products, ShopCatalog.PoolRules, ShopCatalog.SlotRules, generationCount, seed);
            CancelEnhancementSelection(); // P0-11：进入/读档回商店时选择模式强制回 Normal（会话瞬态不入存档）
            Debug.Log($"[Flow] 进入商店：节点 {_flow.NodeIndex}（AI 分组 {ShopState.GroupNameOf(generationCount)}），商品位 {_shopState.Slots.Count} 个。");
        }

        /// <summary>进入人格牌生成节点：按节点派生种子初始化铸牌状态（+1000 与战斗种子错开；存档恢复后同种子重建，骰值可复现）。</summary>
        private void BeginMidRunForge()
        {
            var node = RunRoute.GetNode(_flow.NodeIndex);
            var seed = unchecked(_runSeed + (uint)(node.Index + 1000));
            _forgeState = new PersonaForgeState(_behaviorTracker.CreateReport(), seed);
            _selectedForgeCandidate = -1;
            RefreshForge();
            Debug.Log($"[Flow] 人格牌生成节点 {node.Index}：铸牌候选已生成，派生种子 {seed}。");
        }

        /// <summary>按路线表启动当前节点战斗：目标分、出牌/弃牌限制、场次种子、Boss 均来自 RunRoute 与局种子。</summary>
        private void StartCurrentBattle()
        {
            var node = RunRoute.GetNode(_flow.NodeIndex);
            var playsLimit = RunRoute.PlaysLimitOf(_flow.NodeIndex); // 每关独立出牌/弃牌限制（配表驱动，0 回落默认）
            var discardsLimit = RunRoute.DiscardsLimitOf(_flow.NodeIndex);
            var seed = unchecked(_runSeed + (uint)(node.Index + 1)); // 场次种子由局种子派生，保证同局可复现
            var boss = node.kind == RunNodeKind.BossBattle
                ? BossEncounterCatalog.CreateFromPool(node.bossPoolId, seed) // P0-3：按池抽取（与揭示界面同种子，必得同一 Boss）
                : null;
            battleController.BeginBattle(node.targetScore, seed, _journeyDeck.CreateBattleDeck(), _personaLoadout.CreateLoadout(), boss,
                playsLimit, discardsLimit, selectionLimit: GlobalConfig.SelectionLimit, journeyCoins: _journeyDeck.Coins,
                coinsReward: RunRoute.CoinsRewardOf(_flow.NodeIndex), enhancements: _enhancements); // P0-1I：当前金币注入战斗信息显示（3.3.9）；P0-6：本场金币奖励同源注入；P0-11：三线强化等级注入战斗计分
            battleProgressText.text = $"旅程 {RunRoute.BattleOrdinalOf(_flow.NodeIndex)} / {RunRoute.BattleCount}"; // 进度只计战斗，生成节点不计入
            Debug.Log($"[Flow] 开始节点 {node.Index}（{node.kind}）：目标分 {node.targetScore}，出牌 {playsLimit} 弃牌 {discardsLimit}，场次种子 {seed}" +
                      (boss == null ? "，无 Boss。" : $"，Boss：{boss.Definition.EncounterId}。"));
            SaveActiveRun();
            TryAutoPlayTutorial(); // P0-1G：战斗已进入 Battle 屏，此时可安全弹出教学遮罩
        }

        /// <summary>教程是否正在展示（P0-1G）：供外部（如快捷键拦截）查询。</summary>
        public bool TutorialActive => _tutorial.IsActive;

        /// <summary>是否已看过教学（PlayerPrefs 持久标志；未写 = 0 = 未看过）。</summary>
        private static bool HasSeenTutorial => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

        /// <summary>写入已看标志（首次自动播放后调用；重播请求不读写此标志）。</summary>
        private static void MarkTutorialSeen()
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 战斗开始后尝试自动播放教学（P0-1G）：重播请求优先；否则仅首次进战斗（未看过）自动播放。
        /// 播放即写已看标志，之后同一安装不再自动弹（策划 11.3.1「第一次进入战斗自动播放」）。
        /// </summary>
        private void TryAutoPlayTutorial()
        {
            if (!TutorialSequence.ShouldAutoPlay(_tutorialReplayRequested, HasSeenTutorial)) return;
            _tutorial.Start();
            _tutorialReplayRequested = false; // 重播请求一次性消费
            MarkTutorialSeen();
            Debug.Log("[Flow] 开始战斗教学（5 步）。");
            Render();
        }

        /// <summary>教学面板「下一步」：末步之后结束（P0-1G）。</summary>
        private void TutorialNext()
        {
            if (!_tutorial.IsActive) return;
            _tutorial.Next();
            if (!_tutorial.IsActive)
                Debug.Log("[Flow] 战斗教学结束。");
            Render();
        }

        /// <summary>教学面板「跳过教学」：直接结束（P0-1G）。</summary>
        private void TutorialSkip()
        {
            if (!_tutorial.IsActive) return;
            _tutorial.Skip();
            Debug.Log("[Flow] 战斗教学已跳过。");
            Render();
        }

        /// <summary>主菜单「战斗教学」按钮：切换重播请求——已标记 ↔ 未标记（再点取消），下一次进入战斗按标记决定是否播放（P0-1G；P0-1H 设置界面同款入口共用此逻辑）。</summary>
        private void ToggleTutorialReplay()
        {
            _tutorialReplayRequested = !_tutorialReplayRequested;
            RefreshTutorialReplayLabels();
            Debug.Log(_tutorialReplayRequested
                ? "[Flow] 已标记：下次进入战斗时播放教学。"
                : "[Flow] 已取消标记：下次进入战斗不再播放教学。");
        }

        /// <summary>同步主菜单与设置界面两处「战斗教学」按钮文案（即时反馈当前标记状态，P0-1H）。</summary>
        private void RefreshTutorialReplayLabels()
        {
            var label = _tutorialReplayRequested ? "✓ 下次战斗播放教学" : "战斗教学";
            if (tutorialReplayLabel != null) tutorialReplayLabel.text = label;
            if (settingsTutorialReplayLabel != null) settingsTutorialReplayLabel.text = label;
        }

        /// <summary>
        /// 每帧按键处理（P0-1H 快捷键统一入口）：改键态捕获一切按键；教学激活吞掉一切快捷键（P0-1G 拦截点就此兑现）；
        /// 设置键在主菜单/设置界面切换；出牌/弃牌键仅战斗阶段生效（且由 BattlePrototypeController 自身守卫演示锁与弹窗）。
        /// 项目已切换 Input System（Player Settings），故用 Keyboard.current 而非旧 UnityEngine.Input。
        /// </summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return; // 无键盘设备（触屏等）时全部快捷键不可用，仅按钮操作

            // 改键态优先：一切按键（含 ESC）被捕获为候选新键；再点同一条目可退出改键态
            if (_rebinding != RebindingTarget.None)
            {
                foreach (var keyControl in keyboard.allKeys)
                {
                    if (keyControl.wasPressedThisFrame)
                    {
                        TryFinishRebinding(keyControl.keyCode);
                        break;
                    }
                }
                return;
            }

            // 教学激活：吞掉全部快捷键（策划 11.3.1「拦截空格、弃牌快捷键，防止操作穿透」）
            if (_tutorial.IsActive) return;

            // 设置键：设置界面 ↔ 主菜单；其余阶段（含战斗）不响应 ESC（原型无暂停系统，见 P0-1H 拍板）
            // P0-1I：图鉴占位屏打开时 ESC 优先关图鉴（与设置互斥，图鉴覆盖屏在最上层）
            if (keyboard[GameSettings.SettingsKey].wasPressedThisFrame)
            {
                if (_compendiumOpen) CloseCompendium();
                else if (_settingsOpen) CloseSettings();
                else if (_flow.Stage == PrototypeFlowStage.MainMenu) OpenSettings();
            }

            // 出牌/弃牌键：仅战斗阶段响应（按钮同入口，内部守卫锁定态）
            if (_flow.Stage == PrototypeFlowStage.Battle)
            {
                if (keyboard[GameSettings.PlayKey].wasPressedThisFrame) battleController.OnPlay();
                if (keyboard[GameSettings.DiscardKey].wasPressedThisFrame) battleController.OnDiscard();
            }
        }

        // —— P0-1H 设置系统方法 ——

        /// <summary>打开设置界面（ESC 或主菜单「设置」按钮）：快照当前设置供取消回滚。</summary>
        private void OpenSettings()
        {
            if (_flow.Stage != PrototypeFlowStage.MainMenu || _settingsOpen) return;
            _compendiumOpen = false; // P0-1I 防御：设置与图鉴互斥，同屏不可双开
            _settingsOpen = true;
            _settingsSnapshot = GameSettings.Current.Clone(); // 回滚基线（12.3.1「取消：返回修改前状态」）
            _rebinding = RebindingTarget.None;
            Render();
            RefreshSettingsUi();
        }

        // —— P0-1I 主菜单四入口：图鉴占位屏 + 退出游戏 ——

        /// <summary>打开图鉴占位屏（仅主菜单阶段；内容待策划确认 B5 后替换）。</summary>
        private void OpenCompendium()
        {
            if (_flow.Stage != PrototypeFlowStage.MainMenu || _compendiumOpen) return;
            _compendiumOpen = true;
            Render();
        }

        /// <summary>关闭图鉴占位屏（返回主菜单）。</summary>
        private void CloseCompendium()
        {
            _compendiumOpen = false;
            Render();
        }

        /// <summary>
        /// 退出游戏：编辑器 Play 模式下 Application.Quit 无效，故反射 EditorApplication.isPlaying = false
        /// （UI 程序集不引用 UnityEditor，反射避免 asmdef 改动）；真机走 Application.Quit()。
        /// </summary>
        private void QuitApplication()
        {
            var editorApplication = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            var isPlaying = editorApplication?.GetProperty(
                "isPlaying", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (isPlaying != null && (bool)isPlaying.GetValue(null, null))
            {
                isPlaying.SetValue(null, false, null);
                return;
            }
            Application.Quit();
        }

        /// <summary>关闭设置界面（仅隐藏；修改回滚由 CancelAndCloseSettings 负责）。</summary>
        private void CloseSettings()
        {
            _settingsOpen = false;
            _rebinding = RebindingTarget.None;
            Render();
        }

        /// <summary>「保存设置」：当前值落盘并关闭（12.3.1「保存后下次进游戏自动读取」）。保存失败不关闭界面、不丢修改。</summary>
        private void SaveSettings()
        {
            if (_settingsStore.TrySave(GameSettings.Current))
            {
                Debug.Log("[Settings] 设置已保存。");
                CloseSettings();
            }
            else
            {
                Debug.LogWarning("[Settings] 设置保存失败：请检查磁盘空间后重试。");
            }
        }

        /// <summary>「取消」「返回主界面」「← 返回」：回滚进入设置时的快照并关闭（未保存修改不生效）。</summary>
        private void CancelAndCloseSettings()
        {
            GameSettings.TryApply(_settingsSnapshot); // 快照必合法，恒成功
            RefreshAppliedSettings();
            CloseSettings();
        }

        /// <summary>「恢复默认」：默认值立即生效并落盘覆盖本地（12.6「恢复默认覆盖本地设置」）。</summary>
        private void RestoreDefaults()
        {
            GameSettings.ApplyDefault();
            RefreshAppliedSettings();
            RefreshSettingsUi();
            if (_settingsStore.TrySave(GameSettings.Current))
                Debug.Log("[Settings] 已恢复默认设置并保存。");
            else
                Debug.LogWarning("[Settings] 恢复默认设置保存失败。");
        }

        /// <summary>亮度滑条：修改即生效（草稿不落盘，12.6 调和拍板）；百分比文本即时预览。</summary>
        private void OnBrightnessSliderChanged(float value)
        {
            if (_refreshingSettingsUi || !_settingsOpen) return;
            var updated = GameSettings.Current.Clone();
            updated.brightness = value;
            if (GameSettings.TryApply(updated)) // 滑条只给 0~1，恒成功
            {
                RefreshAppliedSettings();
                brightnessValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        /// <summary>主音量滑条：修改即生效（AudioListener.volume）；百分比文本即时预览。</summary>
        private void OnVolumeSliderChanged(float value)
        {
            if (_refreshingSettingsUi || !_settingsOpen) return;
            var updated = GameSettings.Current.Clone();
            updated.masterVolume = value;
            if (GameSettings.TryApply(updated))
            {
                RefreshAppliedSettings();
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        /// <summary>界面动效开关：修改即生效（PresentationDuration 每帧读门面，无需额外应用）。</summary>
        private void OnAnimationToggled(bool isOn)
        {
            if (_refreshingSettingsUi || !_settingsOpen) return;
            var updated = GameSettings.Current.Clone();
            updated.uiAnimation = isOn;
            GameSettings.TryApply(updated);
        }

        /// <summary>屏幕震动开关：修改即生效（出牌结算时读门面）。</summary>
        private void OnShakeToggled(bool isOn)
        {
            if (_refreshingSettingsUi || !_settingsOpen) return;
            var updated = GameSettings.Current.Clone();
            updated.screenShake = isOn;
            GameSettings.TryApply(updated);
        }

        /// <summary>点击键位按钮：进入改键态；再点同一条目退出（12.3.1「点击进入改键状态」）。</summary>
        private void BeginRebinding(RebindingTarget target)
        {
            if (!_settingsOpen) return;
            _rebinding = _rebinding == target ? RebindingTarget.None : target;
            RefreshSettingsUi();
        }

        /// <summary>改键态按下的键替换对应快捷键：与已有键重复或非法则拒绝并保持改键态（日志提示）。</summary>
        private void TryFinishRebinding(Key key)
        {
            var updated = GameSettings.Current.Clone();
            switch (_rebinding)
            {
                case RebindingTarget.Play: updated.playKey = (int)key; break;
                case RebindingTarget.Discard: updated.discardKey = (int)key; break;
                case RebindingTarget.Settings: updated.settingsKey = (int)key; break;
                default: return;
            }
            if (!GameSettings.TryApply(updated))
            {
                Debug.LogWarning("[Settings] 改键失败：按键与已有快捷键重复或非法。");
                return; // 保持改键态，等待玩家按其他键或再点按钮退出
            }
            Debug.Log($"[Settings] 已改键：{_rebinding} → {key}。");
            _rebinding = RebindingTarget.None;
            RefreshSettingsUi();
        }

        /// <summary>把门面当前值应用到引擎侧：主音量 → AudioListener；亮度 → 全局 dim 层 alpha（1 - 亮度）。</summary>
        private void RefreshAppliedSettings()
        {
            AudioListener.volume = GameSettings.MasterVolume;
            if (dimImage != null)
            {
                var color = dimImage.color;
                color.a = 1f - GameSettings.Brightness;
                dimImage.color = color;
            }
        }

        /// <summary>刷新设置界面全部控件（滑条/开关/百分比/键位文案/教学按钮）：_refreshingSettingsUi 防值回写递归。</summary>
        private void RefreshSettingsUi()
        {
            _refreshingSettingsUi = true;
            try
            {
                brightnessSlider.value = GameSettings.Brightness;
                volumeSlider.value = GameSettings.MasterVolume;
                animationToggle.isOn = GameSettings.AnimationsEnabled;
                shakeToggle.isOn = GameSettings.ScreenShakeEnabled;
                brightnessValueText.text = $"{Mathf.RoundToInt(GameSettings.Brightness * 100f)}%";
                volumeValueText.text = $"{Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
                playKeyLabel.text = KeyButtonLabel(RebindingTarget.Play, GameSettings.PlayKey);
                discardKeyLabel.text = KeyButtonLabel(RebindingTarget.Discard, GameSettings.DiscardKey);
                settingsKeyLabel.text = KeyButtonLabel(RebindingTarget.Settings, GameSettings.SettingsKey);
                RefreshTutorialReplayLabels();
            }
            finally
            {
                _refreshingSettingsUi = false;
            }
        }

        /// <summary>键位按钮文案：改键态提示「请按新按键…」，否则显示当前键（空格/ESC 用中文名，其余枚举名）。</summary>
        private string KeyButtonLabel(RebindingTarget target, Key key)
        {
            var name = target == RebindingTarget.Play ? "出牌" : target == RebindingTarget.Discard ? "弃牌" : "设置与返回";
            if (_rebinding == target) return $"{name}：请按新按键…";
            return $"{name}：{KeyDisplayName(key)}";
        }

        /// <summary>按键显示名：空格/ESC 用中文名（策划 12.3.1 文案风格），其余键用枚举名。</summary>
        private static string KeyDisplayName(Key key)
        {
            switch (key)
            {
                case Key.Space: return "空格";
                case Key.Escape: return "ESC";
                default: return key.ToString();
            }
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

        private void OnHandPlayed(HandType handType, int cardCount, long score)
        {
            _behaviorTracker?.RecordPlay(handType, cardCount, score);
            // 屏幕震动（P0-1H）：出牌结算时战斗屏短促抖动，开关读设置门面
            if (GameSettings.ScreenShakeEnabled)
                StartCoroutine(ShakeScreen());
        }

        /// <summary>屏幕震动协程：战斗屏 ±6px 抖动 0.25 秒，幅度线性衰减复位（结束后精确还原原位置）。</summary>
        private System.Collections.IEnumerator ShakeScreen()
        {
            if (battleScreen == null) yield break;
            var rect = battleScreen.GetComponent<RectTransform>();
            var duration = 0.25f;
            var elapsed = 0f;
            var basePosition = rect.anchoredPosition;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var amplitude = 6f * (1f - elapsed / duration);
                rect.anchoredPosition = basePosition + new Vector2(
                    UnityEngine.Random.Range(-amplitude, amplitude),
                    UnityEngine.Random.Range(-amplitude, amplitude));
                yield return null;
            }
            rect.anchoredPosition = basePosition;
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
            forgeStatusText.text = $"已获得 {candidate.DisplayName}";
            // 弹窗（UI 重排第一批）：中局与局终均弹；确认/暂不替换回调闭环推进；prefab 缺失时降级为不装备直接推进
            _equipPromptSession = new PersonaEquipPromptSession(candidate, _personaCollection.Count, _personaLoadout.Slots);
            if (_equipPromptPrefab == null)
            {
                Debug.LogError($"[EquipPrompt] 弹窗 prefab 缺失：{candidate.DisplayName} 已入收藏，跳过装备确认直接推进。");
                AdvanceAfterForgeConfirm();
                return;
            }
            var canvas = personaForgeScreen.GetComponentInParent<Canvas>();
            _equipPrompt = Instantiate(_equipPromptPrefab,
                canvas != null ? canvas.transform : personaForgeScreen.transform.parent);
            _equipPrompt.gameObject.SetActive(true);
            _equipPrompt.Configure(_equipPromptSession, OnEquipPromptDecline, OnEquipPromptConfirm);
        }

        /// <summary>弹窗「暂不替换」：新牌只入收藏不装备，关闭弹窗后推进流程。</summary>
        private void OnEquipPromptDecline()
        {
            CloseEquipPrompt();
            AdvanceAfterForgeConfirm();
        }

        /// <summary>弹窗「替换并继续」：执行替换（EquipAt 语义：同 TemplateId 在其他槽则两槽互换）→ 刷装备 → 关闭弹窗推进流程。</summary>
        private void OnEquipPromptConfirm()
        {
            if (_equipPromptSession != null)
            {
                _equipPromptSession.ExecuteReplace(_personaLoadout);
                RefreshPersonaLoadout();
            }
            CloseEquipPrompt();
            AdvanceAfterForgeConfirm();
        }

        /// <summary>销毁弹窗实例并清空瞬态会话。</summary>
        private void CloseEquipPrompt()
        {
            if (_equipPrompt != null)
            {
                Destroy(_equipPrompt.gameObject);
                _equipPrompt = null;
            }
            _equipPromptSession = null;
        }

        /// <summary>弹窗闭环后的流程推进：中局铸牌 → 推进下一节点（HandleStageEntry 内部落盘）；局终铸造 → 回主菜单（不再自动打开收藏页）。</summary>
        private void AdvanceAfterForgeConfirm()
        {
            if (_flow.Stage == PrototypeFlowStage.PersonaGen)
            {
                if (!_flow.CompletePersonaGen()) return;
                Debug.Log($"[Flow] 中局铸牌确认：获得 {_personaCollection[_personaCollection.Count - 1].DisplayName}，推进到节点 {_flow.NodeIndex}。");
                HandleStageEntry();
                Render();
                return;
            }
            ReturnToMainMenu();
        }

        private void OnBattleCompleted(BattleStatus status, long score, long target)
        {
            var won = status == BattleStatus.Won;
            if (!_flow.CompleteBattle(won)) return;
            Debug.Log($"[Flow] 战斗结算：{(won ? "胜利" : "失败")}，得分 {score} / 目标 {target}，进入阶段 {_flow.Stage}。");
            MusicManager.Instance.PlaySfx(won ? MusicCatalog.SfxVictory : MusicCatalog.SfxDefeat); // 音效：战斗胜利/失败结算

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
                SyncPersonaPortrait(index, definition);
                SyncPersonaSlotVisual(index, definition != null);
            }
        }

        /// <summary>
        /// 槽位视觉状态同步（美术接入）：有卡 = 金棕底 + 金名 + 白规则，空槽 = 灰底 + 灰文本。
        /// 配色与场景构建器原始值一致（02 屏 58,49,35 / 战斗屏 54,47,36；空槽 37,37,39 / 35,35,36），
        /// 每帧刷新以覆盖槽位 Button 的颜色过渡副作用；面板节点缺失时静默跳过。
        /// </summary>
        private void SyncPersonaSlotVisual(int slotIndex, bool equipped)
        {
            var nameColor = equipped ? SlotNameGold : Color.gray;
            var ruleColor = equipped ? Color.white : Color.gray;
            personaSlotNameTexts[slotIndex].color = nameColor;
            personaSlotRuleTexts[slotIndex].color = ruleColor;
            battlePersonaNameTexts[slotIndex].color = nameColor;
            battlePersonaRuleTexts[slotIndex].color = ruleColor;
            ColorSlotPanel(personaSetupScreen, $"Persona Setup Card/Loadout Slot {slotIndex + 1}",
                equipped ? SetupSlotBackground : SetupEmptySlotBackground);
            ColorSlotPanel(battleScreen, $"Left - Persona Slots/Persona Slot {slotIndex + 1}",
                equipped ? BattleSlotBackground : BattleEmptySlotBackground);
        }

        /// <summary>给指定屏幕根下的面板 Image 刷底色；节点缺失时静默跳过。</summary>
        private static void ColorSlotPanel(GameObject screenRoot, string relativePath, Color color)
        {
            if (screenRoot == null) return;
            var panel = screenRoot.transform.Find(relativePath);
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        /// <summary>
        /// 按 TemplateId 同步 02 准备屏与战斗左栏的立绘（美术接入）。
        /// 仅当槽位有卡且目录能给出立绘时覆盖 Image.sprite；空槽/未知键/手动改版场景缺少节点时一律保持现状，静默跳过。
        /// </summary>
        private void SyncPersonaPortrait(int slotIndex, PersonaCardDefinition definition)
        {
            if (definition == null) return;
            var sprite = PersonaArtCatalog.PortraitFor(definition.TemplateId);
            if (sprite == null) return;
            ApplyPortrait(personaSetupScreen, $"Persona Setup Card/Loadout Slot {slotIndex + 1}/Portrait Artwork", sprite);
            ApplyPortrait(battleScreen, $"Left - Persona Slots/Persona Slot {slotIndex + 1}/Persona Portrait", sprite);
        }

        /// <summary>
        /// 在指定屏幕根下按相对路径找到立绘节点并换图；节点不存在时静默跳过（不打扰手动维护的场景）。
        /// 立绘节点可能是 Image（代码生成）或 RawImage（手动贴图时替换过组件），两者都支持。
        /// </summary>
        private static void ApplyPortrait(GameObject screenRoot, string relativePath, Sprite sprite)
        {
            if (screenRoot == null) return;
            var child = screenRoot.transform.Find(relativePath);
            if (child == null) return;
            var image = child.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                return;
            }
            var rawImage = child.GetComponent<RawImage>();
            if (rawImage != null && sprite != null)
            {
                rawImage.texture = sprite.texture;
                rawImage.color = Color.white; // 空槽期节点被设为透明隐藏，换图时恢复可见
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
                SyncForgePortrait(index, candidate);
            }
            forgeStatusText.text = _selectedForgeCandidate < 0
                ? "请选择一张人格牌；另两张将在确认后消失"
                : $"已选择：{_forgeState.Candidates[_selectedForgeCandidate].DisplayName}，请再次确认";
            forgeConfirmButton.interactable = _selectedForgeCandidate >= 0;
        }

        /// <summary>
        /// 铸造候选立绘同步（美术接入）：候选块 "Portrait" 节点若已是 RawImage（场景换装后）则按 TemplateId 换图；
        /// 目录未收录、节点仍是旧 Text 结构或资源缺失时静默跳过。
        /// </summary>
        private void SyncForgePortrait(int index, PersonaCardDefinition candidate)
        {
            if (forgeCandidateButtons == null || index >= forgeCandidateButtons.Length) return;
            var portrait = forgeCandidateButtons[index].transform.Find("Portrait");
            if (portrait == null) return;
            var rawImage = portrait.GetComponent<RawImage>();
            if (rawImage == null) return;
            var sprite = PersonaArtCatalog.PortraitFor(candidate.TemplateId);
            if (sprite == null) return;
            rawImage.texture = sprite.texture;
            rawImage.color = Color.white;
        }

        /// <summary>规则文案单源委托（UI 重排第一批）：输出与弹窗会话逐字一致，由 PersonaEquipPromptSessionTests 锁定零漂移。</summary>
        private static string ForgeRule(PersonaCardDefinition definition) => PersonaEquipPromptSession.RuleTextOf(definition);

        /// <summary>牌型中文名单源委托（UI 重排第一批）：输出由 PersonaEquipPromptSessionTests 锁定。</summary>
        private static string HandName(HandType handType) => PersonaEquipPromptSession.HandNameOf(handType);

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

        /// <summary>
        /// 商品位购买（P0-7，策划案 10.6 语义）：无货/已售罄/金币不足不生效；效果应用失败（如该牌已在牌组）不扣款不售罄；
        /// 成功才扣款并标记售罄（限购 1 = 即买即售罄）。售罄态不随存档走，读档后商店重置（P0-8 入快照）。
        /// P0-11 强化服务拦截：普通模式点强化服务 → 进入选择模式（不扣款不售罄）；选择模式下 slot0=确认/slot1·slot2=取消。
        /// </summary>
        private void PurchaseShopSlot(int slotIndex)
        {
            if (_shopState == null || slotIndex < 0 || slotIndex >= _shopState.Slots.Count) return;
            if (_shopMode == ShopScreenMode.EnhancementSelect)
            {
                if (slotIndex == 0) ConfirmEnhancement();
                else CancelEnhancementSelection();
                return;
            }
            var slot = _shopState.Slots[slotIndex];
            var product = slot.Product;
            if (product != null && !slot.SoldOut && ShopState.IsEnhancementEffect(product.effectType))
            {
                EnterEnhancementSelection(slotIndex, product);
                return;
            }
            if (product == null || slot.SoldOut)
            {
                shopStatusText.text = "该商品位已售罄";
                return;
            }
            if (_journeyDeck.Coins < product.price)
            {
                shopStatusText.text = "购买失败：金币不足";
                return;
            }
            if (!ApplyProductEffect(product))
            {
                shopStatusText.text = product.effectType == ShopState.EffectAddCard
                    ? "购买失败：该牌已在牌组（先移除才能买回）"
                    : "购买失败：牌组不可再删（已达手牌下限）";
                return;
            }
            slot.MarkSold();
            shopStatusText.text = $"已购买 {product.productName}，剩余金币 {_journeyDeck.Coins}";
            RefreshJourneyCardText();
            Render(); // 商品位刷新：已购位变「已售罄」，其余位按新金币余额重算灰态
            SaveActiveRun();
        }

        /// <summary>
        /// 进入强化选择模式（P0-11）：候选 = 服务类型对应线的未满级目标；全满级/无候选 → 提示「无可强化对象」且不进入
        /// （商品位保持可点，再次点击重复提示）。价格按目标当前等级动态定价，展示价（商品价 8）只作普通显示。
        /// </summary>
        private void EnterEnhancementSelection(int slotIndex, ShopProductEntry product)
        {
            var session = ShopEnhancementSession.TryCreate(product, _personaLoadout, _enhancements);
            if (session == null)
            {
                shopStatusText.text = "无可强化对象";
                return;
            }
            _shopMode = ShopScreenMode.EnhancementSelect;
            _shopEnhancement = session;
            _shopEnhancementSlotIndex = slotIndex;
            RefreshShopSlots();
        }

        /// <summary>
        /// 确认升级（P0-11）：真实价格扣款 + 升 1 级（失败无副作用，留在模式内提示金币不足）；成功后商品位仅标记售罄
        /// （TryMarkSold 不校验余额——真实价格 ≠ 商品展示价），回 Normal 并立即存档（等级与金币入档）。
        /// </summary>
        private void ConfirmEnhancement()
        {
            if (_shopEnhancement == null) return;
            if (!_shopEnhancement.TryConfirm(_journeyDeck))
            {
                shopStatusText.text = "购买失败：金币不足";
                return;
            }
            _shopState.TryMarkSold(_shopEnhancementSlotIndex);
            shopStatusText.text = $"已升级 {_shopEnhancement.Current.DisplayName} 至 Lv{_shopEnhancement.Current.Level + 1}，剩余金币 {_journeyDeck.Coins}";
            CancelEnhancementSelection();
            Render();
            SaveActiveRun();
        }

        /// <summary>退出选择模式回普通商品位渲染（P0-11）。</summary>
        private void CancelEnhancementSelection()
        {
            _shopMode = ShopScreenMode.Normal;
            _shopEnhancement = null;
            RefreshShopSlots();
        }

        /// <summary>商店屏 Prev/Next 路由（P0-11）：选择模式轮换强化目标；普通模式沿用原选牌语义（与奖励屏同函数）。</summary>
        private void OnShopDirectionButton(int delta)
        {
            if (_flow.Stage != PrototypeFlowStage.Shop) return;
            if (_shopMode == ShopScreenMode.EnhancementSelect && _shopEnhancement != null)
            {
                _shopEnhancement.Cycle(delta);
                RefreshEnhancementSelection();
                return;
            }
            if (delta < 0) SelectPreviousJourneyCard();
            else SelectNextJourneyCard();
        }

        /// <summary>轮换后刷新选择模式文案（P0-11）：状态/细节文本 + slot2 服务名跟随当前目标。</summary>
        private void RefreshEnhancementSelection()
        {
            if (_shopEnhancement == null) return;
            shopStatusText.text = _shopEnhancement.StatusText;
            shopCardText.text = _shopEnhancement.DetailText;
            var label = ShopSlotLabel(2);
            if (label != null) label.text = $"{_shopEnhancement.Current.DisplayName} · 升级";
        }

        /// <summary>商品效果应用（P0-7 白名单分派）：成功时内部完成扣款；失败返回 false 且不扣款（策划案 10.6 扣款失败不生效）。</summary>
        private bool ApplyProductEffect(ShopProductEntry product)
        {
            switch (product.effectType)
            {
                case ShopState.EffectAddCard:
                {
                    // 商品配置无 id 列：按商品名解析花色点数（临时口径，待策划确认）；牌组已有同 id 牌时 AddCard 拒绝
                    if (!ShopState.TryParseCardName(product.productName, out var suit, out var rank)) return false;
                    var card = new PlayingCardInstance(StandardDeckFactory.CreateId(suit, rank), suit, rank);
                    if (!_journeyDeck.AddCard(card)) return false;
                    return _journeyDeck.TrySpend(product.price); // 购买校验已保证余额足够，此处必成功
                }
                case ShopState.EffectRemoveCard:
                {
                    var card = SelectedJourneyCard;
                    if (!_journeyDeck.TryPurchase(JourneyDeckAction.Delete, card.Id, product.price)) return false;
                    _selectedJourneyCardIndex = Mathf.Clamp(_selectedJourneyCardIndex, 0, _journeyDeck.Cards.Count - 1);
                    return true;
                }
                default:
                    return false; // 白名单外效果不可能上架（PickProduct 已过滤），防御性拒绝
            }
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
                _enhancements = EnhancementSaveCodec.Restore(data); // P0-11：旧档缺字段 → 全 0 级（null-guard 在辅助方法内）
                _runSeed = data.runSeed; // 旧档无该字段时为 0：场次种子退化为 节点序号+1，仍可复现
                var stage = (PrototypeFlowStage)data.stage;
                _flow.Restore(stage, data.battleNumber, data.personaSetupReturnsToBossReveal); // JSON 字段名沿用 battleNumber，语义为节点索引（P0-8 重命名）
                var nodeKind = RunRoute.GetNode(data.battleNumber).kind;
                if (nodeKind == RunNodeKind.PersonaGen &&
                    (stage == PrototypeFlowStage.Battle || stage == PrototypeFlowStage.BossReveal ||
                     (stage == PrototypeFlowStage.PersonaSetup && data.personaSetupReturnsToBossReveal)))
                {
                    // 旧档守卫：新路线下该节点序号落在人格牌生成节点上，战斗/揭示/装备回程语义不适用，改按铸牌阶段恢复
                    Debug.LogWarning($"[Flow] 旧档节点 {data.battleNumber} 在新路线中是人格牌生成节点：改为铸牌阶段恢复。");
                    stage = PrototypeFlowStage.PersonaGen;
                    _flow.Restore(PrototypeFlowStage.PersonaGen, data.battleNumber);
                }
                Debug.Log($"[Flow] 恢复存档：阶段 {stage}，节点 {data.battleNumber}，局种子 {_runSeed}，金币 {_journeyDeck.Coins}。");
                if (stage == PrototypeFlowStage.PersonaForge)
                    _forgeState = new PersonaForgeState(_behaviorTracker.CreateReport(), 20260820u);
                else if (stage == PrototypeFlowStage.PersonaGen)
                    BeginMidRunForge(); // 同种子派生重建铸牌：骰值与退出前一致
                else if (stage == PrototypeFlowStage.Shop)
                    EnterShop(); // P0-7：同种子重建商品位（售罄态不随存档走，P0-8 入快照）
                if (stage == PrototypeFlowStage.Battle && data.battle != null && data.battle.hasSnapshot)
                {
                    battleController.RestoreBattle(FromSavedBattle(data.battle), _personaLoadout.CreateLoadout(), journeyCoins: _journeyDeck.Coins,
                        coinsReward: RunRoute.CoinsRewardOf(_flow.NodeIndex), enhancements: _enhancements); // P0-1I：读档同步当前金币显示；P0-6：本场金币奖励同源注入；P0-11：三线强化等级注入战斗计分
                    // 快照恢复不经过 StartCurrentBattle，进度文案需在此显式刷新（走查反馈修复：否则残留场景默认文案）
                    battleProgressText.text = $"旅程 {RunRoute.BattleOrdinalOf(_flow.NodeIndex)} / {RunRoute.BattleCount}";
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
                behavior = ToSavedBehavior(_behaviorTracker),
                personaLevels = _enhancements.PersonaLevels.Select(pair => new SavedPersonaLevel
                {
                    isEmpty = false,
                    templateId = pair.Key,
                    level = pair.Value
                }).ToList(),
                suitLevels = _enhancements.SuitLevels.Select(pair => new SavedSuitLevel
                {
                    isEmpty = false,
                    suit = (int)pair.Key,
                    level = pair.Value
                }).ToList(),
                handLevels = _enhancements.HandLevels.Select(pair => new SavedHandLevel
                {
                    isEmpty = false,
                    handType = (int)pair.Key,
                    level = pair.Value
                }).ToList() // P0-11：三线强化等级随活动局存档（空字典 → 空列表，非 null）
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
            playsLimit = battle.PlaysLimit,
            discardsLimit = battle.DiscardsLimit,
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
                battle.playsRemaining, battle.discardsRemaining, (BattleStatus)battle.status, bossSnapshot,
                // JsonUtility 旧档缺字段为 0（非默认值），必须显式回落默认（经 GlobalConfig 门面：读档时用当前全局配置，P0-1F）
                playsLimit: battle.playsLimit > 0 ? battle.playsLimit : GlobalConfig.StartingPlays,
                discardsLimit: battle.discardsLimit > 0 ? battle.discardsLimit : GlobalConfig.StartingDiscards);
        }

        private void RefreshJourneyCardText()
        {
            if (_journeyDeck == null) return;
            var card = SelectedJourneyCard;
            var description = $"{RankName(card.Rank)}{SuitName(card.Suit)}\n强化：{EnhancementName(card.Enhancement)}\n牌组 {_journeyDeck.Cards.Count} 张";
            rewardCardText.text = description;
            shopCardText.text = description + $"\n金币：{_journeyDeck.Coins}";
            shopCoinsText.text = $"当前金币：{_journeyDeck.Coins}";
        }

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

        /// <summary>商店继续按钮的文案标签（按钮子对象，延迟获取并缓存）。</summary>
        private Text ShopContinueLabel => _shopContinueLabel ??= shopContinueButton.GetComponentInChildren<Text>(true);

        /// <summary>商品位按钮的文案标签（按钮子对象，延迟获取并缓存；Find 失败保持 null，静默不刷新）。</summary>
        private Text ShopSlotLabel(int slotIndex)
        {
            if (_shopSlotLabels[slotIndex] == null && _shopSlotButtons != null && slotIndex < _shopSlotButtons.Length)
                _shopSlotLabels[slotIndex] = _shopSlotButtons[slotIndex].GetComponentInChildren<Text>(true);
            return _shopSlotLabels[slotIndex];
        }

        /// <summary>商品位渲染（P0-7）：文本 = 商品名 + 价格；无货/已售罄灰态；金币不足灰态。槽位多于按钮时只渲染前 3 位。
        /// P0-11：选择模式下 slot0=「确认升级」/slot1=「取消」/slot2=服务名（点击=取消），状态与细节文本跟随当前目标。</summary>
        private void RefreshShopSlots()
        {
            if (_shopMode == ShopScreenMode.EnhancementSelect)
            {
                RefreshEnhancementSlots();
                return;
            }
            for (var i = 0; i < _shopSlotButtons.Length; i++)
            {
                var button = _shopSlotButtons[i];
                var label = ShopSlotLabel(i);
                var slot = _shopState != null && i < _shopState.Slots.Count ? _shopState.Slots[i] : null;
                if (slot == null || slot.Product == null)
                {
                    if (label != null) label.text = "无货";
                    button.interactable = false;
                }
                else if (slot.SoldOut)
                {
                    if (label != null) label.text = $"{slot.Product.productName} · 已售罄";
                    button.interactable = false;
                }
                else
                {
                    if (label != null) label.text = $"{slot.Product.productName} · 费用 {slot.Product.price}";
                    button.interactable = _journeyDeck != null && _journeyDeck.Coins >= slot.Product.price;
                }
            }
        }

        /// <summary>选择模式槽位渲染（P0-11）：三个商品位按钮语义切换为 确认/取消/取消；状态文案写 shopStatusText、细节写 shopCardText。</summary>
        private void RefreshEnhancementSlots()
        {
            for (var i = 0; i < _shopSlotButtons.Length; i++)
            {
                var button = _shopSlotButtons[i];
                var label = ShopSlotLabel(i);
                button.interactable = i <= 2; // 只有前 3 个槽位按钮参与选择模式
                if (label == null) continue;
                if (i == 0) label.text = "确认升级";
                else if (i == 1) label.text = "取消";
                else if (i == 2) label.text = _shopEnhancement != null ? $"{_shopEnhancement.Current.DisplayName} · 升级" : "—";
                else label.text = "";
            }
            if (_shopEnhancement != null) RefreshEnhancementSelection();
        }

        private void Render()
        {
            mainMenuScreen.SetActive(_flow.Stage == PrototypeFlowStage.MainMenu && !_collectionOpen);
            collectionScreen.SetActive(_flow.Stage == PrototypeFlowStage.MainMenu && _collectionOpen);
            settingsOverlay.SetActive(_settingsOpen && _flow.Stage == PrototypeFlowStage.MainMenu); // P0-1H：设置 overlay 仅主菜单阶段
            compendiumScreen.SetActive(_compendiumOpen && _flow.Stage == PrototypeFlowStage.MainMenu); // P0-1I：图鉴覆盖屏仅主菜单阶段（主菜单屏保持可见）
            personaSetupScreen.SetActive(_flow.Stage == PrototypeFlowStage.PersonaSetup);
            bossRevealScreen.SetActive(_flow.Stage == PrototypeFlowStage.BossReveal);
            if (_flow.Stage == PrototypeFlowStage.BossReveal && bossRevealRuleText != null)
            {
                var node = RunRoute.GetNode(_flow.NodeIndex);
                // P0-3：与开战同种子抽取（局种子 + 节点序号 + 1），保证揭示展示的 Boss 就是即将开战的 Boss。
                var seed = unchecked(_runSeed + (uint)(node.Index + 1));
                var encounter = BossEncounterCatalog.CreateFromPool(node.bossPoolId, seed).Definition;
                // P0-9：Boss 名牌与台词按定义覆盖场景静态文本（按名查找；美术重排后 Find 失败则静默跳过，显示场景默认值）
                if (!_bossRevealTextsResolved)
                {
                    _bossRevealNameText = bossRevealScreen.transform.Find("Boss Reveal Card/Boss Name")?.GetComponent<Text>();
                    _bossRevealLineText = bossRevealScreen.transform.Find("Boss Reveal Card/Boss Line")?.GetComponent<Text>();
                    _bossRevealTextsResolved = true;
                }
                if (_bossRevealNameText != null) _bossRevealNameText.text = encounter.DisplayName;
                if (_bossRevealLineText != null) _bossRevealLineText.text = encounter.RevealLine;
                // 出牌/弃牌限制按节点配置展示（配表驱动）
                bossRevealRuleText.text = $"主规则 · {encounter.RuleName}\n{encounter.RuleDescription}\n\n介入事件 · {encounter.InterventionName}\n{encounter.InterventionDescription}\n\n目标分数：{node.targetScore}　出牌：{RunRoute.PlaysLimitOf(_flow.NodeIndex)}　弃牌：{RunRoute.DiscardsLimitOf(_flow.NodeIndex)}";
            }
            battleScreen.SetActive(_flow.Stage == PrototypeFlowStage.Battle);
            rewardScreen.SetActive(_flow.Stage == PrototypeFlowStage.Reward);
            if (_flow.Stage == PrototypeFlowStage.Reward)
            {
                // P0-6：奖励屏金币结算行——基础句取场景静态原文，运行时追加本场金币奖励；Find 失败静默跳过（美术重排容错）
                if (!_rewardRuleResolved)
                {
                    _rewardRuleText = rewardScreen.transform.Find("Reward Card/Reward Rule")?.GetComponent<Text>();
                    _rewardRuleBaseText = _rewardRuleText != null ? _rewardRuleText.text : null;
                    _rewardRuleResolved = true;
                }
                if (_rewardRuleText != null)
                {
                    var coinReward = RunRoute.CoinsRewardOf(_flow.NodeIndex);
                    _rewardRuleText.text = coinReward > 0
                        ? _rewardRuleBaseText + $"\n本场金币奖励 +{coinReward}"
                        : _rewardRuleBaseText;
                }
            }
            shopScreen.SetActive(_flow.Stage == PrototypeFlowStage.Shop);
            if (_flow.Stage == PrototypeFlowStage.Shop)
            {
                if (ShopContinueLabel != null)
                {
                    // 商店继续按钮文案按下一节点类型提示去向（配表可指定任意后续节点）
                    var nextKind = RunRoute.NextNodeKindOf(_flow.NodeIndex);
                    ShopContinueLabel.text = nextKind == RunNodeKind.BossBattle ? "离开商店 · 前往 Boss"
                        : nextKind == RunNodeKind.PersonaGen ? "离开商店 · 前往铸牌"
                        : "离开商店 · 继续旅程";
                }
                RefreshShopSlots(); // P0-7：商品位渲染（无货/售罄/金币不足灰态）
            }
            runReportScreen.SetActive(_flow.Stage == PrototypeFlowStage.RunReport);
            personaForgeScreen.SetActive(_flow.Stage == PrototypeFlowStage.PersonaForge || _flow.Stage == PrototypeFlowStage.PersonaGen); // 中局铸牌复用铸牌界面
            failureResultScreen.SetActive(_flow.Stage == PrototypeFlowStage.FailureResult);
            if (_flow.Stage == PrototypeFlowStage.Reward || _flow.Stage == PrototypeFlowStage.Shop)
            {
                RefreshJourneyCardText();
            }
            if (_flow.Stage == PrototypeFlowStage.PersonaSetup) RefreshPersonaLoadout();
            // 教程遮罩（P0-1G）：仅教程激活时显示（Battle 屏子对象，随战斗屏隐藏）；激活时同步刷新面板文案
            tutorialOverlay.SetActive(_tutorial.IsActive);
            if (_tutorial.IsActive)
            {
                var step = _tutorial.CurrentStep;
                tutorialStepText.text = $"教学 {step + 1} / {TutorialSequence.StepCount}";
                tutorialTitleText.text = TutorialSequence.GetTitle(step);
                tutorialBodyText.text = TutorialSequence.GetBody(step);
                if (tutorialNextLabel != null)
                    tutorialNextLabel.text = step == TutorialSequence.StepCount - 1 ? "完成" : "下一步";
            }
            // 音乐系统：每帧按阶段同步 BGM（同曲幂等；Battle 阶段同一枚举承载普通战与 Boss 战，按节点类型区分）
            var isBossBattle = _flow.Stage == PrototypeFlowStage.Battle
                && RunRoute.GetNode(_flow.NodeIndex).kind == RunNodeKind.BossBattle;
            MusicManager.Instance.SyncStage(_flow.Stage, isBossBattle);
        }
    }
}
