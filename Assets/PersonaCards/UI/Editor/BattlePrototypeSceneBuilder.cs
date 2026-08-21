using System.Collections.Generic;
using System.Linq;
using PersonaCards.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PersonaCards.UI.Editor
{
    public static class BattlePrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/BattlePrototype.unity";
        private const string PrefabFolder = "Assets/PersonaCards/UI/Prefabs";
        private const string CardPrefabPath = PrefabFolder + "/BattleCardView.prefab";
        private const string BattleBackgroundPath = "Assets/Art/Backgrounds/battle-table-mirror-hall-v1.png";
        private const string CardBackPath = "Assets/Art/Cards/card-back-occult-compass-v1.png";
        private const string CardFacePath = "Assets/Art/Cards/card-face-parchment-v1.png";
        private const string BossPortraitPath = "Assets/Art/Boss/boss-mirror-keeper-v1.png";
        private static readonly string[] PersonaPortraitPaths =
        {
            "Assets/Art/PersonaCards/persona-accumulator-v1.png",
            "Assets/Art/PersonaCards/persona-executor-v1.png",
            "Assets/Art/PersonaCards/persona-ambitious-v1.png"
        };

        private static readonly Color Panel = new Color32(14, 17, 17, 238);
        private static readonly Color Gold = new Color32(178, 139, 73, 255);
        private static readonly Color PaleGold = new Color32(232, 214, 173, 255);
        private static readonly Color PrimaryButton = new Color32(58, 47, 28, 248);
        private static readonly Color SecondaryButton = new Color32(31, 32, 31, 248);
        private static readonly Color SubtleGold = new Color32(112, 88, 49, 180);
        private static Font _font;

        [InitializeOnLoadMethod]
        private static void RebuildWhenPrototypeSchemaChanges()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath) return;
                if (GameObject.Find("Prototype Schema - Boss Rules Pass") == null) Build();
                ValidateRunRouteJourney();
            };
        }

        [MenuItem("Persona Cards/Rebuild Battle Prototype UI")]
        public static void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureFolders();
            var cardPrefab = CreateCardPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f, 1f);
            camera.orthographic = true;

            var canvasObject = new GameObject("Battle UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var battleScreen = CreateScreenRoot(canvasObject.transform, "04 Battle Screen");
            CreateBackground(battleScreen.transform);
            var left = CreatePanel(battleScreen.transform, "Left - Persona Slots", new Vector2(0.015f, 0.08f), new Vector2(0.215f, 0.95f), Panel);
            var battlePersonaReferences = BuildPersonaPanel(left.transform);

            var center = CreatePanel(battleScreen.transform, "Center - Table", new Vector2(0.23f, 0.05f), new Vector2(0.785f, 0.95f), new Color(0.04f, 0.045f, 0.05f, 0.60f));
            var centerReferences = BuildCenterPanel(center.transform);

            var right = CreatePanel(battleScreen.transform, "Right - Battle Info", new Vector2(0.80f, 0.08f), new Vector2(0.985f, 0.95f), Panel);
            var rightReferences = BuildRightPanel(right.transform);

            var resultReferences = BuildResultOverlay(battleScreen.transform);
            var modalReferences = BuildBattleModals(battleScreen.transform);

            var controllerObject = new GameObject("Battle Prototype Controller");
            var controller = controllerObject.AddComponent<BattlePrototypeController>();
            controller.ConfigureScene(
                centerReferences.HandRoot,
                rightReferences.Score,
                rightReferences.Resources,
                rightReferences.BossRule,
                centerReferences.Preview,
                centerReferences.Message,
                centerReferences.Play,
                centerReferences.Discard,
                resultReferences.Panel,
                resultReferences.Text,
                resultReferences.Button,
                cardPrefab,
                centerReferences.PlayedSlots,
                centerReferences.ScoringLog,
                rightReferences.DeckViewer,
                rightReferences.HandReference,
                modalReferences.DeckViewer,
                modalReferences.DeckViewerClose,
                modalReferences.DeckViewerPrevious,
                modalReferences.DeckViewerNext,
                modalReferences.DeckViewerZones,
                modalReferences.DeckViewerCards,
                modalReferences.DeckViewerSummary,
                modalReferences.DeckViewerPage,
                modalReferences.HandReference,
                modalReferences.HandReferenceClose,
                modalReferences.HandReferenceRows);
            controller.SetExternalFlowManaged(true);

            var flowReferences = BuildPrototypeFlow(canvasObject.transform, battleScreen.gameObject);
            var flowObject = new GameObject("Prototype Flow Controller");
            new GameObject("Prototype Schema - Boss Rules Pass");
            var flow = flowObject.AddComponent<PrototypeFlowController>();
            flow.Configure(
                flowReferences.MainMenu,
                flowReferences.Collection,
                flowReferences.PersonaSetup,
                flowReferences.BossReveal,
                battleScreen.gameObject,
                flowReferences.Reward,
                flowReferences.Shop,
                flowReferences.RunReport,
                flowReferences.PersonaForge,
                flowReferences.FailureResult,
                flowReferences.Start,
                flowReferences.Continue,
                flowReferences.CollectionButton,
                flowReferences.CollectionBack,
                flowReferences.CollectionPrevious,
                flowReferences.CollectionNext,
                flowReferences.CollectionUnequip,
                flowReferences.CollectionCards,
                flowReferences.CollectionCardTexts,
                flowReferences.CollectionEquipment,
                flowReferences.CollectionEquipmentTexts,
                flowReferences.CollectionDetail,
                flowReferences.CollectionPage,
                flowReferences.ConfirmPersona,
                flowReferences.BeginBattle,
                flowReferences.ResultReturn,
                flowReferences.RewardContinue,
                flowReferences.ShopContinue,
                flowReferences.ReportReturn,
                flowReferences.PersonaBack,
                flowReferences.BossBack,
                flowReferences.ResultTitle,
                flowReferences.ResultSummary,
                flowReferences.BattleProgress,
                flowReferences.BossRevealRule,
                flowReferences.ReportSummary,
                flowReferences.RewardCard,
                flowReferences.ShopCard,
                flowReferences.ShopCoins,
                flowReferences.ShopStatus,
                flowReferences.RewardPrevious,
                flowReferences.RewardNext,
                flowReferences.ShopPrevious,
                flowReferences.ShopNext,
                flowReferences.ShopDelete,
                flowReferences.ShopReforge,
                flowReferences.ShopEnhance,
                flowReferences.PersonaSlots,
                flowReferences.PersonaNames,
                flowReferences.PersonaRules,
                battlePersonaReferences.Names,
                battlePersonaReferences.Rules,
                flowReferences.ForgeRolls,
                flowReferences.ForgeStatus,
                flowReferences.ForgeCandidates,
                flowReferences.ForgeCandidateButtons,
                flowReferences.ForgeConfirm,
                controller,
                EnsureRunRouteAsset(),
                EnsureHandTypeAsset(),
                EnsureCardConfigAsset(),
                EnsurePersonaConfigAsset(),
                EnsureGlobalConfigAsset(),
                flowReferences.TutorialOverlay,
                flowReferences.TutorialStep,
                flowReferences.TutorialTitle,
                flowReferences.TutorialBody,
                flowReferences.TutorialNext,
                flowReferences.TutorialSkip,
                flowReferences.TutorialNextLabel,
                flowReferences.TutorialReplay,
                flowReferences.TutorialReplayLabel);
            // P0-1H 设置系统接线：22 个设置引用独立注入（Configure 签名已满）
            flow.ConfigureSettings(
                flowReferences.Settings.MainMenuEntry,
                flowReferences.Settings.Overlay,
                flowReferences.Settings.Brightness,
                flowReferences.Settings.BrightnessValue,
                flowReferences.Settings.Volume,
                flowReferences.Settings.VolumeValue,
                flowReferences.Settings.Animation,
                flowReferences.Settings.Shake,
                flowReferences.Settings.PlayKey,
                flowReferences.Settings.PlayKeyText,
                flowReferences.Settings.DiscardKey,
                flowReferences.Settings.DiscardKeyText,
                flowReferences.Settings.SettingsKey,
                flowReferences.Settings.SettingsKeyText,
                flowReferences.Settings.TutorialReplay,
                flowReferences.Settings.TutorialReplayText,
                flowReferences.Settings.Back,
                flowReferences.Settings.ReturnMain,
                flowReferences.Settings.RestoreDefaults,
                flowReferences.Settings.Cancel,
                flowReferences.Settings.Save,
                flowReferences.Settings.Dim);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetFirstBuildScene();
            Selection.activeGameObject = canvasObject;
            Debug.Log("BattlePrototype rebuilt with serialized UI hierarchy and card prefab.");
        }

        /// <summary>确保路线资产存在并返回引用：缺失时按 GDD 默认路线创建（场景重建时自动挂接）。</summary>
        private static RunRouteAsset EnsureRunRouteAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(RunRouteAssetGenerator.AssetPath);
            if (asset == null)
            {
                RunRouteAssetGenerator.CreateOrReset();
                asset = AssetDatabase.LoadAssetAtPath<RunRouteAsset>(RunRouteAssetGenerator.AssetPath);
                Debug.Log("[RunRoute] 场景重建时发现路线资产缺失，已按默认路线自动创建。");
            }
            return asset;
        }

        /// <summary>确保牌型配置资产存在并返回引用：缺失时按配表当前初值白盒创建（场景重建时自动挂接）。</summary>
        private static HandTypeAsset EnsureHandTypeAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<HandTypeAsset>(HandTypeImportCommand.AssetPath);
            if (asset == null)
            {
                HandTypeImportCommand.CreateOrReset();
                asset = AssetDatabase.LoadAssetAtPath<HandTypeAsset>(HandTypeImportCommand.AssetPath);
                Debug.Log("[HandType] 场景重建时发现牌型配置资产缺失，已按配表当前初值自动创建。");
            }
            return asset;
        }

        /// <summary>确保卡牌配置资产存在并返回引用：缺失时按配表当前初值白盒创建（场景重建时自动挂接）。</summary>
        private static CardConfigAsset EnsureCardConfigAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CardConfigAsset>(CardConfigImportCommand.AssetPath);
            if (asset == null)
            {
                CardConfigImportCommand.CreateOrReset();
                asset = AssetDatabase.LoadAssetAtPath<CardConfigAsset>(CardConfigImportCommand.AssetPath);
                Debug.Log("[Card] 场景重建时发现卡牌配置资产缺失，已按配表当前初值自动创建。");
            }
            return asset;
        }

        /// <summary>确保人格牌配置资产存在并返回引用：缺失时创建空条目资产（白盒 = 空模板目录，场景重建时自动挂接）。</summary>
        private static PersonaConfigAsset EnsurePersonaConfigAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<PersonaConfigAsset>(PersonaImportCommand.AssetPath);
            if (asset == null)
            {
                PersonaImportCommand.CreateOrReset();
                asset = AssetDatabase.LoadAssetAtPath<PersonaConfigAsset>(PersonaImportCommand.AssetPath);
                Debug.Log("[Persona] 场景重建时发现人格牌配置资产缺失，已创建空条目资产（白盒）。");
            }
            return asset;
        }

        /// <summary>确保全局配置资产存在并返回引用：缺失时创建空条目资产（白盒 = 空配置，出牌/弃牌回落 4/3，场景重建时自动挂接）。</summary>
        private static GlobalConfigAsset EnsureGlobalConfigAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GlobalConfigAsset>(GlobalConfigImportCommand.AssetPath);
            if (asset == null)
            {
                GlobalConfigImportCommand.CreateOrReset();
                asset = AssetDatabase.LoadAssetAtPath<GlobalConfigAsset>(GlobalConfigImportCommand.AssetPath);
                Debug.Log("[Global] 场景重建时发现全局配置资产缺失，已创建空条目资产（白盒）。");
            }
            return asset;
        }

        [MenuItem("Persona Cards/Validate Run Route Journey %#v")]
        public static void ValidateRunRouteJourney()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var requiredObjects = new[]
            {
                "01 Main Menu Screen", "10 Persona Collection Screen", "02 Persona Setup Screen", "03 Boss Reveal Screen",
                "04 Battle Screen", "05 Battle Reward Screen", "06 Shop Screen", "09 Persona Forge Screen",
                "08 Run Report Screen", "Prototype Flow Controller", "Battle Prototype Controller",
                "Start Game Button", "Collection Button", "Collection Back Button", "Collection Unequip Button",
                "Confirm Persona Button", "Reward Continue Button",
                "Shop Continue Button", "Begin Battle Button", "Report Return Button",
                "Selected Reward Card", "Reward Previous Button", "Reward Next Button",
                "Selected Shop Card", "Shop Status", "Shop Previous Button", "Shop Next Button",
                "Shop Delete Button", "Shop Reforge Button", "Shop Enhance Button",
                "Forge Rolls", "Forge Candidate 1", "Forge Candidate 2", "Forge Candidate 3",
                "Forge Confirm Button", "Collection Card 1", "Collection Equipment Slot 1"
                , "Deck Viewer Button", "Hand Reference Button", "Deck Viewer Overlay", "Hand Reference Overlay"
                , "Tutorial Overlay", "Tutorial Next Button", "Tutorial Skip Button", "Tutorial Replay Button"
                // P0-1H 设置系统：界面 + 遮罩 + dim 层 + 主菜单入口（其余设置控件由 serializedFlow 字段绑定校验兜底）
                , "05 Settings Screen", "Settings Card", "Settings Mask", "Screen Dim Layer",
                "Settings Button", "Settings Save Button", "Brightness Slider", "Master Volume Slider"
            };
            foreach (var objectName in requiredObjects)
            {
                if (FindInScene(scene, objectName) == null)
                {
                    throw new System.InvalidOperationException($"Journey validation failed: missing {objectName}.");
                }
            }

            var flowController = FindInScene(scene, "Prototype Flow Controller").GetComponent<PrototypeFlowController>();
            var serializedFlow = new SerializedObject(flowController);
            foreach (var propertyName in new[]
            {
                "mainMenuScreen", "collectionScreen", "personaSetupScreen", "bossRevealScreen", "battleScreen",
                "rewardScreen", "shopScreen", "runReportScreen", "personaForgeScreen", "failureResultScreen",
                "startButton", "collectionButton", "collectionBackButton", "collectionPreviousButton",
                "collectionNextButton", "collectionUnequipButton", "collectionDetailText", "collectionPageText",
                "confirmPersonaButton", "beginBattleButton", "resultReturnButton",
                "continueButton",
                "rewardContinueButton", "shopContinueButton", "reportReturnButton",
                "rewardCardText", "shopCardText", "shopCoinsText", "shopStatusText",
                "rewardPreviousButton", "rewardNextButton", "shopPreviousButton", "shopNextButton",
                "shopDeleteButton", "shopReforgeButton", "shopEnhanceButton",
                "forgeRollsText", "forgeStatusText", "forgeConfirmButton", "battleController", "runRoute",
                "handTypes", "cardConfig", "personaConfig", "globalConfig",
                "tutorialOverlay", "tutorialStepText", "tutorialTitleText", "tutorialBodyText",
                "tutorialNextButton", "tutorialSkipButton", "tutorialNextLabel", "tutorialReplayButton", "tutorialReplayLabel",
                // P0-1H 设置系统 22 个绑定字段
                "settingsEntryButton", "settingsOverlay", "brightnessSlider", "brightnessValueText", "volumeSlider", "volumeValueText",
                "animationToggle", "shakeToggle", "playKeyButton", "playKeyLabel", "discardKeyButton", "discardKeyLabel",
                "settingsKeyButton", "settingsKeyLabel", "settingsTutorialReplayButton", "settingsTutorialReplayLabel",
                "settingsBackButton", "settingsReturnButton", "settingsRestoreDefaultsButton", "settingsCancelButton",
                "settingsSaveButton", "dimImage"
            })
            {
                var property = serializedFlow.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                {
                    throw new System.InvalidOperationException($"Journey validation failed: unbound {propertyName}.");
                }
            }

            var battleController = FindInScene(scene, "Battle Prototype Controller").GetComponent<BattlePrototypeController>();
            var serializedBattle = new SerializedObject(battleController);
            foreach (var propertyName in new[]
            {
                "deckViewerButton", "handReferenceButton", "deckViewerOverlay", "deckViewerCloseButton",
                "deckViewerPreviousButton", "deckViewerNextButton", "deckViewerSummaryText", "deckViewerPageText",
                "handReferenceOverlay", "handReferenceCloseButton"
            })
            {
                var property = serializedBattle.FindProperty(propertyName);
                if (property == null || property.objectReferenceValue == null)
                    throw new System.InvalidOperationException($"Journey validation failed: unbound battle {propertyName}.");
            }
            foreach (var pair in new[] { ("deckViewerZoneButtons", 4), ("deckViewerCardTexts", 20), ("handReferenceRows", 12) })
            {
                var property = serializedBattle.FindProperty(pair.Item1);
                if (property == null || property.arraySize != pair.Item2)
                    throw new System.InvalidOperationException($"Journey validation failed: invalid battle {pair.Item1}.");
            }

            foreach (var arrayPropertyName in new[]
            {
                "collectionCardButtons", "collectionCardTexts", "collectionEquipmentButtons", "collectionEquipmentTexts",
                "personaSlotButtons", "personaSlotNameTexts", "personaSlotRuleTexts",
                "battlePersonaNameTexts", "battlePersonaRuleTexts",
                "forgeCandidateTexts", "forgeCandidateButtons"
            })
            {
                var property = serializedFlow.FindProperty(arrayPropertyName);
                var expectedSize = arrayPropertyName.StartsWith("forge") ? 3
                    : arrayPropertyName.StartsWith("collectionCard") ? 6 : 4;
                if (property == null || property.arraySize != expectedSize)
                    throw new System.InvalidOperationException($"Journey validation failed: invalid {arrayPropertyName}.");
                for (var index = 0; index < property.arraySize; index++)
                {
                    if (property.GetArrayElementAtIndex(index).objectReferenceValue == null)
                        throw new System.InvalidOperationException($"Journey validation failed: unbound {arrayPropertyName}[{index}].");
                }
            }

            var flow = new PrototypeFlowStateMachine();
            if (!flow.StartNewRun() || !flow.ConfirmPersonaSetup() || flow.NodeIndex != 0)
            {
                throw new System.InvalidOperationException("Journey validation failed: happy path state transitions are invalid.");
            }
            for (var node = 0; node < RunRoute.StageCount; node++)
            {
                // 入场阶段按节点类型分派：Boss 战先揭示、生成节点先铸牌、普通战直接开战
                var kind = RunRoute.GetNode(node).kind;
                var entryStage = kind == RunNodeKind.BossBattle ? PrototypeFlowStage.BossReveal
                    : kind == RunNodeKind.PersonaGen ? PrototypeFlowStage.PersonaGen
                    : PrototypeFlowStage.Battle;
                if (flow.Stage != entryStage)
                    throw new System.InvalidOperationException($"Journey validation failed: node {node} ({kind}) entry stage is invalid.");
                if (kind == RunNodeKind.PersonaGen)
                {
                    // 生成节点无战斗：确认获得后直接推进到下一节点
                    if (!flow.CompletePersonaGen() || flow.NodeIndex != node + 1)
                        throw new System.InvalidOperationException($"Journey validation failed: node {node} persona gen transition is invalid.");
                    continue;
                }
                if (kind == RunNodeKind.BossBattle && !flow.BeginBossBattle())
                    throw new System.InvalidOperationException($"Journey validation failed: node {node} boss reveal transition is invalid.");
                if (!flow.CompleteBattle(true))
                    throw new System.InvalidOperationException($"Journey validation failed: node {node} battle transition is invalid.");
                if (RunRoute.IsFinalNode(node))
                {
                    if (flow.Stage != PrototypeFlowStage.RunReport)
                        throw new System.InvalidOperationException("Journey validation failed: final node did not reach run report.");
                    continue;
                }
                if (!flow.ContinueFromReward())
                    throw new System.InvalidOperationException($"Journey validation failed: node {node} reward transition is invalid.");
                if (RunRoute.GetNode(node).hasShopAfter)
                {
                    if (flow.Stage != PrototypeFlowStage.Shop || !flow.ContinueFromShop())
                        throw new System.InvalidOperationException($"Journey validation failed: node {node} shop transition is invalid.");
                }
                if (flow.NodeIndex != node + 1)
                    throw new System.InvalidOperationException($"Journey validation failed: node {node} did not advance to node {node + 1}.");
            }
            if (!flow.ContinueToForge() || flow.Stage != PrototypeFlowStage.PersonaForge)
            {
                throw new System.InvalidOperationException("Journey validation failed: forge transition is invalid.");
            }

            Debug.Log($"Run route journey validation passed: scene bindings and {RunRoute.StageCount}-stage route are valid.");
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name) return transform.gameObject;
                }
            }
            return null;
        }

        private static FlowReferences BuildPrototypeFlow(Transform canvas, GameObject battleScreen)
        {
            var main = CreateScreenRoot(canvas, "01 Main Menu Screen");
            CreateBackground(main.transform);
            var mainCard = CreatePanel(main.transform, "Main Menu Card", new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.88f), new Color32(17, 18, 20, 242));
            CreateText(mainCard.transform, "Game Title", "人格牌", 74, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.96f), PaleGold, FontStyle.Bold);
            CreateText(mainCard.transform, "Subtitle", "用牌局，认识人格", 26, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.75f), Color.gray);
            var start = CreateButton(mainCard.transform, "Start Game Button", "开始游戏", new Vector2(0.17f, 0.50f), new Vector2(0.83f, 0.61f), Gold);
            var continueButton = CreateButton(mainCard.transform, "Continue Game Button", "继续游戏", new Vector2(0.17f, 0.37f), new Vector2(0.83f, 0.48f), new Color32(80, 68, 45, 255));
            continueButton.interactable = false;
            var collectionButton = CreateButton(mainCard.transform, "Collection Button", "人格收藏 / 装备", new Vector2(0.17f, 0.24f), new Vector2(0.83f, 0.35f), new Color32(55, 55, 57, 255));
            // P0-1H 设置入口：一行一个全宽按钮（用户 2026-08-21），占原「战斗教学」行
            var settings = CreateButton(mainCard.transform, "Settings Button", "设置", new Vector2(0.17f, 0.135f), new Vector2(0.83f, 0.225f), new Color32(55, 55, 57, 255));
            // P0-1G 教程重播入口：点击标记重播请求，下一次进入战斗自动播放（P0-1H 设置界面有同款入口，文案同步）
            var tutorialReplay = CreateButton(mainCard.transform, "Tutorial Replay Button", "战斗教学", new Vector2(0.17f, 0.03f), new Vector2(0.83f, 0.12f), new Color32(55, 55, 57, 255));
            var tutorialReplayLabel = tutorialReplay.transform.Find("Label").GetComponent<Text>();
            // P0-1H：卡片内已放满 5 个按钮，版本文本移出卡片挂主菜单屏底部
            CreateText(main.transform, "Version", "MVP 可玩闭环", 18, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.06f), Color.gray);

            var collection = CreateScreenRoot(canvas, "10 Persona Collection Screen");
            CreateBackground(collection.transform);
            var collectionCard = CreatePanel(collection.transform, "Persona Collection Card", new Vector2(0.055f, 0.065f), new Vector2(0.945f, 0.935f), new Color32(17, 18, 20, 248));
            CreateText(collectionCard.transform, "Title", "人格收藏 / 装备", 48, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), PaleGold, FontStyle.Bold);
            CreateText(collectionCard.transform, "Collection Hint", "选择左侧人格牌，再点击右侧槽位完成装备；同一人格不会重复装备。", 20, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.88f), Color.gray);

            var collectionListPanel = CreatePanel(collectionCard.transform, "Collection List Panel", new Vector2(0.035f, 0.18f), new Vector2(0.60f, 0.80f), new Color32(22, 23, 22, 235));
            CreateText(collectionListPanel.transform, "Collection List Title", "已收藏人格", 26, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f), Gold, FontStyle.Bold);
            var collectionCardButtons = new List<Button>();
            var collectionCardTexts = new List<Text>();
            for (var index = 0; index < 6; index++)
            {
                var column = index % 2;
                var row = index / 2;
                var minX = 0.04f + column * 0.48f;
                var maxX = minX + 0.44f;
                var maxY = 0.84f - row * 0.24f;
                var minY = maxY - 0.20f;
                var button = CreateButton(collectionListPanel.transform, $"Collection Card {index + 1}", "空", new Vector2(minX, minY), new Vector2(maxX, maxY), new Color32(48, 45, 38, 255));
                collectionCardButtons.Add(button);
                collectionCardTexts.Add(button.transform.Find("Label").GetComponent<Text>());
            }
            var collectionPrevious = CreateButton(collectionListPanel.transform, "Collection Previous Button", "◀ 上一页", new Vector2(0.05f, 0.03f), new Vector2(0.25f, 0.15f), new Color32(70, 70, 72, 255));
            var collectionPage = CreateText(collectionListPanel.transform, "Collection Page", "第 1 / 1 页", 18, TextAnchor.MiddleCenter, new Vector2(0.27f, 0.03f), new Vector2(0.73f, 0.15f), Color.gray);
            var collectionNext = CreateButton(collectionListPanel.transform, "Collection Next Button", "下一页 ▶", new Vector2(0.75f, 0.03f), new Vector2(0.95f, 0.15f), new Color32(70, 70, 72, 255));

            var equipmentPanel = CreatePanel(collectionCard.transform, "Collection Equipment Panel", new Vector2(0.625f, 0.18f), new Vector2(0.965f, 0.80f), new Color32(22, 23, 22, 235));
            CreateText(equipmentPanel.transform, "Equipment Title", "当前装备 · 上限 4 张", 25, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.98f), Gold, FontStyle.Bold);
            var equipmentButtons = new List<Button>();
            var equipmentTexts = new List<Text>();
            for (var index = 0; index < 4; index++)
            {
                var maxY = 0.84f - index * 0.17f;
                var minY = maxY - 0.14f;
                var button = CreateButton(equipmentPanel.transform, $"Collection Equipment Slot {index + 1}", $"0{index + 1}  空槽", new Vector2(0.07f, minY), new Vector2(0.93f, maxY), new Color32(52, 47, 38, 255));
                equipmentButtons.Add(button);
                equipmentTexts.Add(button.transform.Find("Label").GetComponent<Text>());
            }
            var collectionUnequip = CreateButton(equipmentPanel.transform, "Collection Unequip Button", "卸下选中槽位", new Vector2(0.22f, 0.04f), new Vector2(0.78f, 0.14f), new Color32(85, 55, 49, 255));

            var collectionDetailPanel = CreatePanel(collectionCard.transform, "Collection Detail Panel", new Vector2(0.06f, 0.055f), new Vector2(0.74f, 0.16f), new Color32(24, 24, 23, 230));
            var collectionDetail = CreateText(collectionDetailPanel.transform, "Collection Detail", "请选择一张已收藏的人格牌。", 19, TextAnchor.MiddleLeft, new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.92f), Color.white);
            var collectionBack = CreateButton(collectionCard.transform, "Collection Back Button", "保存并返回", new Vector2(0.77f, 0.055f), new Vector2(0.94f, 0.16f), Gold);

            var persona = CreateScreenRoot(canvas, "02 Persona Setup Screen");
            CreateBackground(persona.transform);
            var personaCard = CreatePanel(persona.transform, "Persona Setup Card", new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.90f), new Color32(17, 18, 20, 246));
            CreateText(personaCard.transform, "Title", "人格准备", 48, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.98f), PaleGold, FontStyle.Bold);
            CreateText(personaCard.transform, "Hint", "本局装备按 01 → 02 → 03 → 04 的顺序生效", 22, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.87f), Color.gray);
            var personaNames = new[] { "01  积累者", "02  执行者", "03  野心者", "04  空槽" };
            var personaRules = new[] { "+15 筹码", "+2 倍率", "对子或更高 ×1.10", "等待新的人格牌" };
            var personaSlotButtons = new List<Button>();
            var personaNameTexts = new List<Text>();
            var personaRuleTexts = new List<Text>();
            for (var index = 0; index < 4; index++)
            {
                var minX = 0.055f + index * 0.235f;
                var slot = CreatePanel(personaCard.transform, $"Loadout Slot {index + 1}", new Vector2(minX, 0.33f), new Vector2(minX + 0.205f, 0.75f),
                    index == 3 ? new Color32(37, 37, 39, 245) : new Color32(58, 49, 35, 250));
                var slotButton = slot.AddComponent<Button>();
                slotButton.targetGraphic = slot.GetComponent<Image>();
                personaSlotButtons.Add(slotButton);
                if (index < PersonaPortraitPaths.Length)
                    CreateArtwork(slot.transform, "Portrait Artwork", PersonaPortraitPaths[index], new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.92f));
                else
                    CreateText(slot.transform, "Empty Portrait", "+", 68, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.92f), Color.gray);
                personaNameTexts.Add(CreateText(slot.transform, "Name", personaNames[index], 24, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.43f), PaleGold, FontStyle.Bold));
                personaRuleTexts.Add(CreateText(slot.transform, "Rule", personaRules[index], 18, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.23f), Color.white));
            }
            CreateText(personaCard.transform, "Interaction Hint", "点击任意槽位：与右侧槽位交换", 18, TextAnchor.MiddleCenter, new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.31f), Gold);
            var personaBack = CreateButton(personaCard.transform, "Persona Back Button", "返回", new Vector2(0.23f, 0.10f), new Vector2(0.45f, 0.21f), new Color32(70, 70, 72, 255));
            var confirmPersona = CreateButton(personaCard.transform, "Confirm Persona Button", "确认装备", new Vector2(0.55f, 0.10f), new Vector2(0.77f, 0.21f), Gold);

            var boss = CreateScreenRoot(canvas, "03 Boss Reveal Screen");
            CreateBackground(boss.transform);
            var bossCard = CreatePanel(boss.transform, "Boss Reveal Card", new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.84f), new Color32(17, 18, 20, 250));
            CreateText(bossCard.transform, "Title", "Boss 开战", 52, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f), PaleGold, FontStyle.Bold);
            CreateArtwork(bossCard.transform, "Boss Portrait Artwork", BossPortraitPath, new Vector2(0.09f, 0.43f), new Vector2(0.40f, 0.68f));
            CreateText(bossCard.transform, "Boss Name", "镜厅守门人", 34, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.68f), new Vector2(0.42f, 0.80f), Gold, FontStyle.Bold);
            CreateText(bossCard.transform, "Boss Line", "“让我看看你的选择。”", 20, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.34f), new Vector2(0.42f, 0.45f), Color.gray, FontStyle.Italic);
            var rulePanel = CreatePanel(bossCard.transform, "Rule Panel", new Vector2(0.45f, 0.38f), new Vector2(0.92f, 0.78f), new Color32(48, 43, 35, 245));
            CreateText(rulePanel.transform, "Rule Title", "本场规则", 25, TextAnchor.UpperLeft, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), PaleGold, FontStyle.Bold);
            var bossRevealRule = CreateText(rulePanel.transform, "Rule Text", "主规则 · 重复审判\n本手牌型与上一手相同，最终得分 ×0.60。\n\n介入事件 · 先手鼓励\n第一手结算时获得 +30 筹码。", 18, TextAnchor.UpperLeft, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.78f), Color.white);
            CreateText(bossCard.transform, "Notice", "规则确认后进入战斗；战斗失败即结束本局。", 20, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.35f), new Color(0.84f, 0.80f, 0.70f));
            var bossBack = CreateButton(bossCard.transform, "Boss Back Button", "返回准备", new Vector2(0.20f, 0.08f), new Vector2(0.45f, 0.20f), new Color32(70, 70, 72, 255));
            var beginBattle = CreateButton(bossCard.transform, "Begin Battle Button", "开始战斗", new Vector2(0.55f, 0.08f), new Vector2(0.80f, 0.20f), Gold);

            var result = CreateScreenRoot(canvas, "05 Run Result Screen");
            CreateBackground(result.transform);
            var resultCard = CreatePanel(result.transform, "Run Result Card", new Vector2(0.31f, 0.22f), new Vector2(0.69f, 0.78f), new Color32(17, 18, 20, 250));
            CreateEmblem(resultCard.transform, "Failure Emblem", "×", new Vector2(0.40f, 0.73f), new Vector2(0.60f, 0.91f), new Color32(132, 72, 60, 255));
            var resultTitle = CreateText(resultCard.transform, "Result Title", "战斗结果", 50, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.75f), PaleGold, FontStyle.Bold);
            var resultSummaryPanel = CreatePanel(resultCard.transform, "Result Summary Panel", new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.60f), new Color32(23, 24, 23, 225));
            var resultSummary = CreateText(resultSummaryPanel.transform, "Result Summary", "等待结算", 26, TextAnchor.MiddleCenter, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.92f), Color.white);
            var resultReturn = CreateButton(resultCard.transform, "Result Return Button", "返回主界面", new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.23f), Gold);

            var reward = CreateScreenRoot(canvas, "05 Battle Reward Screen");
            CreateBackground(reward.transform);
            var rewardCard = CreatePanel(reward.transform, "Reward Card", new Vector2(0.25f, 0.18f), new Vector2(0.75f, 0.82f), new Color32(17, 18, 20, 250));
            CreateEmblem(rewardCard.transform, "Victory Emblem", "✦", new Vector2(0.43f, 0.83f), new Vector2(0.57f, 0.95f), Gold);
            CreateText(rewardCard.transform, "Title", "战斗胜利 · 选择奖励", 43, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.84f), PaleGold, FontStyle.Bold);
            CreateText(rewardCard.transform, "Reward Name", "锋利刻痕", 34, TextAnchor.MiddleCenter, new Vector2(0.15f, 0.62f), new Vector2(0.85f, 0.75f), Gold, FontStyle.Bold);
            var rewardCardText = CreateText(rewardCard.transform, "Selected Reward Card", "选择目标牌", 25, TextAnchor.MiddleCenter, new Vector2(0.24f, 0.36f), new Vector2(0.76f, 0.61f), Color.white, FontStyle.Bold);
            var rewardPrevious = CreateButton(rewardCard.transform, "Reward Previous Button", "◀", new Vector2(0.10f, 0.40f), new Vector2(0.22f, 0.55f), new Color32(70, 70, 72, 255));
            var rewardNext = CreateButton(rewardCard.transform, "Reward Next Button", "▶", new Vector2(0.78f, 0.40f), new Vector2(0.90f, 0.55f), new Color32(70, 70, 72, 255));
            CreateText(rewardCard.transform, "Reward Rule", "领取后，所选牌获得 +20 筹码强化，并带入后续战斗。", 19, TextAnchor.MiddleCenter, new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.35f), Color.gray);
            var rewardContinue = CreateButton(rewardCard.transform, "Reward Continue Button", "领取并继续", new Vector2(0.27f, 0.10f), new Vector2(0.73f, 0.24f), Gold);

            var shop = CreateScreenRoot(canvas, "06 Shop Screen");
            CreateBackground(shop.transform);
            var shopCard = CreatePanel(shop.transform, "Shop Card", new Vector2(0.16f, 0.13f), new Vector2(0.84f, 0.87f), new Color32(17, 18, 20, 250));
            CreateText(shopCard.transform, "Title", "旅途商店", 46, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), PaleGold, FontStyle.Bold);
            var shopCoins = CreateText(shopCard.transform, "Coins", "当前金币：3", 23, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.84f), Gold);
            CreateEmblem(shopCard.transform, "Coin Icon", "●", new Vector2(0.38f, 0.765f), new Vector2(0.415f, 0.835f), Gold, false);
            var shopCardText = CreateText(shopCard.transform, "Selected Shop Card", "选择目标牌", 25, TextAnchor.MiddleCenter, new Vector2(0.30f, 0.55f), new Vector2(0.70f, 0.73f), Color.white, FontStyle.Bold);
            var shopPrevious = CreateButton(shopCard.transform, "Shop Previous Button", "◀", new Vector2(0.16f, 0.57f), new Vector2(0.28f, 0.70f), new Color32(70, 70, 72, 255));
            var shopNext = CreateButton(shopCard.transform, "Shop Next Button", "▶", new Vector2(0.72f, 0.57f), new Vector2(0.84f, 0.70f), new Color32(70, 70, 72, 255));
            var shopDelete = CreateButton(shopCard.transform, "Shop Delete Button", "删除 · 费用 2", new Vector2(0.08f, 0.35f), new Vector2(0.31f, 0.49f), new Color32(85, 55, 49, 255));
            var shopReforge = CreateButton(shopCard.transform, "Shop Reforge Button", "重刻 · 费用 2", new Vector2(0.385f, 0.35f), new Vector2(0.615f, 0.49f), new Color32(62, 58, 50, 255));
            var shopEnhance = CreateButton(shopCard.transform, "Shop Enhance Button", "强化 · 费用 2", new Vector2(0.69f, 0.35f), new Vector2(0.92f, 0.49f), Gold);
            var shopStatus = CreateText(shopCard.transform, "Shop Status", "请选择一张牌和一项服务", 18, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.31f), Color.gray);
            var shopContinue = CreateButton(shopCard.transform, "Shop Continue Button", "离开商店", new Vector2(0.30f, 0.07f), new Vector2(0.70f, 0.19f), Gold); // 运行时按下一节点类型改写文案

            var report = CreateScreenRoot(canvas, "08 Run Report Screen");
            CreateBackground(report.transform);
            var reportCard = CreatePanel(report.transform, "Run Report Card", new Vector2(0.24f, 0.12f), new Vector2(0.76f, 0.88f), new Color32(17, 18, 20, 250));
            CreateText(reportCard.transform, "Title", "局终人格报告", 47, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), PaleGold, FontStyle.Bold);
            var reportSummary = CreateText(reportCard.transform, "Report Summary", "等待旅程数据", 27, TextAnchor.MiddleCenter, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.82f), Color.white);
            CreateText(reportCard.transform, "Evidence", "牌型集中度  82%     资源保留率  76%     终局贡献率  88%", 20, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.34f), Gold);
            var reportReturn = CreateButton(reportCard.transform, "Report Return Button", "开始人格铸造", new Vector2(0.24f, 0.07f), new Vector2(0.76f, 0.19f), Gold);

            var forge = CreateScreenRoot(canvas, "09 Persona Forge Screen");
            CreateBackground(forge.transform);
            var forgeCard = CreatePanel(forge.transform, "Persona Forge Card", new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f), new Color32(17, 18, 20, 248));
            CreateText(forgeCard.transform, "Title", "人格铸造 · 三选一", 46, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), PaleGold, FontStyle.Bold);
            var forgeRolls = CreateText(forgeCard.transform, "Forge Rolls", "映照 D20：—     偏转 D20：—     裂变 D20：—", 24, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.85f), Gold, FontStyle.Bold);
            var forgeCandidateTexts = new List<Text>();
            var forgeCandidateButtons = new List<Button>();
            for (var index = 0; index < 3; index++)
            {
                var minX = 0.055f + index * 0.315f;
                var candidate = CreatePanel(forgeCard.transform, $"Forge Candidate {index + 1}", new Vector2(minX, 0.31f), new Vector2(minX + 0.26f, 0.72f), new Color32(55, 47, 35, 250));
                var candidateButton = candidate.AddComponent<Button>();
                candidateButton.targetGraphic = candidate.GetComponent<Image>();
                forgeCandidateButtons.Add(candidateButton);
                CreateText(candidate.transform, "Portrait", "◇", 68, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.92f), Gold);
                forgeCandidateTexts.Add(CreateText(candidate.transform, "Description", "等待铸造", 22, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.54f), Color.white, FontStyle.Bold));
            }
            var forgeStatus = CreateText(forgeCard.transform, "Forge Status", "请选择一张人格牌", 19, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.21f), new Vector2(0.92f, 0.29f), Color.gray);
            var forgeConfirm = CreateButton(forgeCard.transform, "Forge Confirm Button", "确认获得", new Vector2(0.32f, 0.07f), new Vector2(0.68f, 0.19f), Gold);
            forgeConfirm.interactable = false;

            var battleProgress = CreateText(battleScreen.transform, "Journey Progress", $"旅程 1 / {RunRoute.BattleCount}", 20, TextAnchor.MiddleCenter, new Vector2(0.43f, 0.955f), new Vector2(0.57f, 0.995f), Gold, FontStyle.Bold); // 分母=战斗场数（生成节点不计入）

            // P0-1G 教程遮罩：最后创建 = 战斗屏最上层 sibling；全屏 Image raycastTarget 拦截下层战斗按钮点击（策划 11.3.1 遮罩外不响应战斗操作）
            // 面板内字号暂时自定（标题 30 / 正文·步骤 22 / 按钮全局 28）；TODO(美术)：待美术字体与字号规范导入后统一调整（用户 2026-08-21 提醒）
            var tutorialOverlay = CreatePanel(battleScreen.transform, "Tutorial Overlay", new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0.78f));
            var tutorialPanel = CreatePanel(tutorialOverlay.transform, "Tutorial Panel", new Vector2(0.18f, 0.25f), new Vector2(0.82f, 0.75f), new Color32(24, 25, 27, 252));
            var tutorialStep = CreateText(tutorialPanel.transform, "Tutorial Step", "教学 1 / 5", 22, TextAnchor.MiddleCenter, new Vector2(0.60f, 0.84f), new Vector2(0.94f, 0.94f), Color.gray);
            var tutorialTitle = CreateText(tutorialPanel.transform, "Tutorial Title", "得分与目标", 30, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.82f), PaleGold, FontStyle.Bold);
            var tutorialBody = CreateText(tutorialPanel.transform, "Tutorial Body", "等待教学", 22, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.62f), Color.white);
            var tutorialNext = CreateButton(tutorialPanel.transform, "Tutorial Next Button", "下一步", new Vector2(0.34f, 0.08f), new Vector2(0.62f, 0.22f), Gold);
            var tutorialSkip = CreateButton(tutorialPanel.transform, "Tutorial Skip Button", "跳过教学", new Vector2(0.65f, 0.08f), new Vector2(0.92f, 0.22f), new Color32(70, 70, 72, 255));
            var tutorialNextLabel = tutorialNext.transform.Find("Label").GetComponent<Text>();
            tutorialOverlay.SetActive(false); // 初始隐藏：仅教程激活时由 FlowController 打开

            // P0-1H 设置界面：Canvas 根独立屏（非 MainMenu 子对象），仅主菜单阶段可打开，遮罩拦截主菜单点击；
            // dim 层在 BuildSettingsScreen 内部最后创建（Canvas 根最上层 sibling）
            var settingsReferences = BuildSettingsScreen(canvas, settings);

            battleScreen.SetActive(false);
            collection.gameObject.SetActive(false);
            persona.gameObject.SetActive(false);
            boss.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            reward.gameObject.SetActive(false);
            shop.gameObject.SetActive(false);
            report.gameObject.SetActive(false);
            forge.gameObject.SetActive(false);
            main.gameObject.SetActive(true);

            return new FlowReferences(main.gameObject, collection.gameObject, persona.gameObject, boss.gameObject, reward.gameObject,
                shop.gameObject, report.gameObject, forge.gameObject, result.gameObject, start, continueButton,
                collectionButton, collectionBack, collectionPrevious, collectionNext, collectionUnequip,
                collectionCardButtons.ToArray(), collectionCardTexts.ToArray(), equipmentButtons.ToArray(), equipmentTexts.ToArray(),
                collectionDetail, collectionPage,
                confirmPersona, beginBattle, resultReturn, rewardContinue, shopContinue, reportReturn, personaBack, bossBack,
                resultTitle, resultSummary, battleProgress, bossRevealRule, reportSummary,
                rewardCardText, shopCardText, shopCoins, shopStatus, rewardPrevious, rewardNext,
                shopPrevious, shopNext, shopDelete, shopReforge, shopEnhance,
                personaSlotButtons.ToArray(), personaNameTexts.ToArray(), personaRuleTexts.ToArray(),
                forgeRolls, forgeStatus, forgeCandidateTexts.ToArray(), forgeCandidateButtons.ToArray(), forgeConfirm,
                tutorialOverlay, tutorialStep, tutorialTitle, tutorialBody, tutorialNext, tutorialSkip, tutorialNextLabel,
                tutorialReplay, tutorialReplayLabel,
                settingsReferences);
        }

        /// <summary>P0-1H 设置界面：Canvas 根独立屏（非 MainMenu 子对象，初始隐藏）。全屏遮罩拦截主菜单点击；卡片内 画面/声音/操作 三区 + 底部四按钮。</summary>
        private static SettingsReferences BuildSettingsScreen(Transform canvas, Button mainMenuEntry)
        {
            var settingsScreen = CreateScreenRoot(canvas, "05 Settings Screen");
            // 全屏遮罩：暗化主菜单并拦截下层点击（raycastTarget 保持默认 true）
            CreatePanel(settingsScreen.transform, "Settings Mask", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.55f));
            CreateText(settingsScreen.transform, "Settings Title", "设置", 40, TextAnchor.MiddleCenter, new Vector2(0.17f, 0.87f), new Vector2(0.97f, 0.97f), PaleGold, FontStyle.Bold);
            var back = CreateButton(settingsScreen.transform, "Settings Back Button", "← 返回", new Vector2(0.03f, 0.885f), new Vector2(0.15f, 0.955f), new Color32(70, 70, 72, 255));

            var card = CreatePanel(settingsScreen.transform, "Settings Card", new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.92f), new Color32(17, 18, 20, 248));

            // —— 画面设置区 ——
            CreateText(card.transform, "Settings Section 1", "画面设置", 24, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.935f), PaleGold, FontStyle.Bold);
            CreateText(card.transform, "Brightness Label", "亮度", 20, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.765f), new Vector2(0.17f, 0.845f), Color.white);
            var brightness = CreateSlider(card.transform, "Brightness Slider", new Vector2(0.19f, 0.785f), new Vector2(0.80f, 0.825f));
            var brightnessValue = CreateText(card.transform, "Brightness Value", "80%", 20, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.765f), new Vector2(0.96f, 0.845f), Gold);
            var animation = CreateToggle(card.transform, "UI Animation Toggle", "界面动效", new Vector2(0.05f, 0.655f), new Vector2(0.50f, 0.745f));
            var shake = CreateToggle(card.transform, "Screen Shake Toggle", "屏幕震动", new Vector2(0.52f, 0.655f), new Vector2(0.97f, 0.745f));

            // —— 声音设置区 ——
            CreateText(card.transform, "Settings Section 2", "声音设置", 24, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.565f), new Vector2(0.95f, 0.645f), PaleGold, FontStyle.Bold);
            CreateText(card.transform, "Volume Label", "主音量", 20, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.475f), new Vector2(0.17f, 0.555f), Color.white);
            var volume = CreateSlider(card.transform, "Master Volume Slider", new Vector2(0.19f, 0.495f), new Vector2(0.80f, 0.535f));
            var volumeValue = CreateText(card.transform, "Master Volume Value", "80%", 20, TextAnchor.MiddleCenter, new Vector2(0.82f, 0.475f), new Vector2(0.96f, 0.555f), Gold);
            CreateText(card.transform, "Volume Note", "按钮、选牌、出牌、弃牌等音效受主音量控制（当前版本暂无音频文件）", 16, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.395f), new Vector2(0.95f, 0.465f), Color.gray);

            // —— 操作设置区 ——
            CreateText(card.transform, "Settings Section 3", "操作设置", 24, TextAnchor.MiddleLeft, new Vector2(0.05f, 0.305f), new Vector2(0.95f, 0.385f), PaleGold, FontStyle.Bold);
            var playKey = CreateButton(card.transform, "Play Key Button", "出牌：空格", new Vector2(0.05f, 0.21f), new Vector2(0.34f, 0.29f), new Color32(55, 55, 57, 255));
            var discardKey = CreateButton(card.transform, "Discard Key Button", "弃牌：D", new Vector2(0.36f, 0.21f), new Vector2(0.65f, 0.29f), new Color32(55, 55, 57, 255));
            var settingsKey = CreateButton(card.transform, "Settings Key Button", "设置与返回：ESC", new Vector2(0.66f, 0.21f), new Vector2(0.95f, 0.29f), new Color32(55, 55, 57, 255));
            // 与主菜单「战斗教学」同款的 replay 标记 toggle（P0-1H）：文案由 FlowController.RefreshTutorialReplayLabels 同步
            var tutorialReplay = CreateButton(card.transform, "Settings Tutorial Replay Button", "战斗教学", new Vector2(0.05f, 0.105f), new Vector2(0.95f, 0.195f), new Color32(55, 55, 57, 255));

            // —— 底部按钮行 ——
            var returnMain = CreateButton(card.transform, "Settings Return Button", "返回主界面", new Vector2(0.03f, 0.015f), new Vector2(0.24f, 0.095f), new Color32(70, 70, 72, 255));
            var restoreDefaults = CreateButton(card.transform, "Settings Restore Defaults Button", "恢复默认", new Vector2(0.26f, 0.015f), new Vector2(0.47f, 0.095f), new Color32(70, 70, 72, 255));
            var cancel = CreateButton(card.transform, "Settings Cancel Button", "取消", new Vector2(0.49f, 0.015f), new Vector2(0.70f, 0.095f), new Color32(70, 70, 72, 255));
            var save = CreateButton(card.transform, "Settings Save Button", "保存", new Vector2(0.72f, 0.015f), new Vector2(0.95f, 0.095f), Gold);

            settingsScreen.gameObject.SetActive(false); // 初始隐藏：仅主菜单阶段打开设置时由 FlowController 激活

            // P0-1H 亮度 dim 层：Canvas 根最后创建 = 所有 UI 之上（含设置界面），raycastTarget=false 永不拦截点击；
            // alpha = 1 - 亮度（默认亮度 0.8 → 0.2），运行时由 FlowController.RefreshAppliedSettings 按门面值重设
            var dim = CreatePanel(canvas, "Screen Dim Layer", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 1f - GameSettings.Brightness));
            dim.GetComponent<Image>().raycastTarget = false;

            return new SettingsReferences(mainMenuEntry, settingsScreen.gameObject,
                brightness, brightnessValue, volume, volumeValue,
                animation, shake,
                playKey, playKey.transform.Find("Label").GetComponent<Text>(),
                discardKey, discardKey.transform.Find("Label").GetComponent<Text>(),
                settingsKey, settingsKey.transform.Find("Label").GetComponent<Text>(),
                tutorialReplay, tutorialReplay.transform.Find("Label").GetComponent<Text>(),
                back, returnMain, restoreDefaults, cancel, save,
                dim.GetComponent<Image>());
        }

        /// <summary>uGUI 滑条 helper（P0-1H）：水平 0~1、初始值 0.8（与默认亮度/音量一致）。结构照抄 uGUI 默认 Slider（Background + Fill Area + Handle Slide Area）。</summary>
        private static Slider CreateSlider(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), min, max);
            var background = CreatePanel(root.transform, "Background", new Vector2(0f, 0.30f), new Vector2(1f, 0.70f), new Color32(50, 50, 52, 255));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0.02f, 0.30f), new Vector2(0.98f, 0.70f));
            var fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one, Gold);
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(0.01f, 0f), new Vector2(0.99f, 1f));
            // 点锚点 (0.5,0.5)：Slider 按值驱动 handleRect 的 x 锚点，sizeDelta 定把手大小
            var handle = CreatePanel(handleArea.transform, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), PaleGold);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 26f);
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = background.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        private static void CreateBackground(Transform parent)
        {
            var background = new GameObject("00 Background Image", typeof(RectTransform), typeof(RawImage));
            background.transform.SetParent(parent, false);
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var image = background.GetComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BattleBackgroundPath);
            image.raycastTarget = false;
            var veil = CreatePanel(parent, "01 Dark Veil", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.28f));
            veil.GetComponent<Image>().raycastTarget = false;
        }

        private static PersonaPanelReferences BuildPersonaPanel(Transform parent)
        {
            CreateText(parent, "Header - 人格牌", "人格牌", 34, TextAnchor.MiddleCenter, new Vector2(0f, 0.91f), Vector2.one, Gold, FontStyle.Bold);
            CreateText(parent, "Hint", "按槽位从左到右结算", 18, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.91f), Color.gray);
            var names = new[] { "01  积累者", "02  执行者", "03  野心者", "04  空槽" };
            var rules = new[] { "+15 筹码", "+2 倍率", "对子或更高 ×1.10", "等待新人格牌" };
            var nameTexts = new List<Text>();
            var ruleTexts = new List<Text>();
            for (var index = 0; index < 4; index++)
            {
                var top = 0.82f - index * 0.195f;
                var slot = CreatePanel(parent, $"Persona Slot {index + 1}", new Vector2(0.07f, top - 0.16f), new Vector2(0.93f, top),
                    index == 3 ? new Color32(35, 35, 36, 210) : new Color32(54, 47, 36, 245));
                if (index < PersonaPortraitPaths.Length)
                    CreateArtwork(slot.transform, "Persona Portrait", PersonaPortraitPaths[index], new Vector2(0.035f, 0.08f), new Vector2(0.35f, 0.92f));
                else
                    CreateText(slot.transform, "Empty Portrait", "+", 42, TextAnchor.MiddleCenter, new Vector2(0.035f, 0.08f), new Vector2(0.35f, 0.92f), Color.gray);
                nameTexts.Add(CreateText(slot.transform, "Name", names[index], 24, TextAnchor.UpperLeft, new Vector2(0.39f, 0.51f), new Vector2(0.95f, 0.94f), index == 3 ? Color.gray : PaleGold, FontStyle.Bold));
                ruleTexts.Add(CreateText(slot.transform, "Rule", rules[index], 19, TextAnchor.LowerLeft, new Vector2(0.39f, 0.10f), new Vector2(0.95f, 0.52f), index == 3 ? Color.gray : Color.white));
            }
            return new PersonaPanelReferences(nameTexts.ToArray(), ruleTexts.ToArray());
        }

        private static CenterReferences BuildCenterPanel(Transform parent)
        {
            CreateText(parent, "Title", "人格牌 · 战斗原型", 38, TextAnchor.MiddleCenter, new Vector2(0f, 0.90f), new Vector2(1f, 0.99f), PaleGold, FontStyle.Bold);
            var played = CreatePanel(parent, "Played Card Slots", new Vector2(0.17f, 0.66f), new Vector2(0.83f, 0.88f), new Color(0f, 0f, 0f, 0.18f));
            for (var index = 0; index < 5; index++)
            {
                var slot = CreatePanel(played.transform, $"Played Slot {index + 1}", new Vector2(0.02f + index * 0.198f, 0.08f), new Vector2(0.18f + index * 0.198f, 0.92f), new Color(0.12f, 0.12f, 0.13f, 0.75f));
                slot.GetComponent<Image>().raycastTarget = false;
                CreateArtwork(slot.transform, "Occult Card Back", CardBackPath, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            }
            var preview = CreateText(parent, "Scoring Preview", "当前牌型：—\n预计得分：—", 25, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.65f), Color.white, FontStyle.Bold);
            var scoringLog = CreateText(parent, "Scoring Event Log", "等待出牌", 21, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.55f), Gold, FontStyle.Bold);
            var message = CreateText(parent, "Action Message", "选择 1—5 张牌，然后出牌或弃牌", 19, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.48f), new Color(0.84f, 0.80f, 0.70f));
            var hand = CreatePanel(parent, "Hand Area - Runtime Card Prefabs", new Vector2(0.02f, 0.17f), new Vector2(0.98f, 0.41f), new Color(0f, 0f, 0f, 0.20f));
            var play = CreateButton(parent, "Play Button", "出牌", new Vector2(0.23f, 0.03f), new Vector2(0.48f, 0.13f), Gold);
            var discard = CreateButton(parent, "Discard Button", "弃牌", new Vector2(0.52f, 0.03f), new Vector2(0.77f, 0.13f), new Color32(85, 63, 51, 255));
            AddButtonIcon(play, "♠", PaleGold);
            AddButtonIcon(discard, "×", new Color32(224, 175, 156, 255));
            // P0-1H：战斗屏「减少动效」Toggle 已归口到设置系统的「界面动效」开关（GameSettings.AnimationsEnabled），不再创建
            return new CenterReferences(hand.GetComponent<RectTransform>(), preview, message, play, discard,
                played.GetComponent<RectTransform>(), scoringLog);
        }

        private static RightReferences BuildRightPanel(Transform parent)
        {
            CreateArtwork(parent, "Boss Portrait", BossPortraitPath, new Vector2(0.08f, 0.75f), new Vector2(0.43f, 0.92f));
            CreateText(parent, "Observer Name", "镜厅守门人", 30, TextAnchor.MiddleLeft, new Vector2(0.47f, 0.84f), new Vector2(0.96f, 0.96f), Gold, FontStyle.Bold);
            CreateText(parent, "Observer Line", "“让我看看你的选择。”", 18, TextAnchor.MiddleLeft, new Vector2(0.47f, 0.72f), new Vector2(0.94f, 0.85f), Color.gray, FontStyle.Italic);
            var bossRule = CreateText(parent, "Boss Active Rule", "观察者未施加特殊规则", 16, TextAnchor.UpperLeft, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.72f), new Color32(212, 192, 145, 255), FontStyle.Bold);
            var score = CreateText(parent, "Score and Target", "当前得分\n0\n\n目标分数\n350", 25, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.58f), Color.white, FontStyle.Bold);
            var resources = CreateText(parent, "Battle Resources", "剩余出牌：4 / 4\n剩余弃牌：3 / 3\n牌堆：44", 20, TextAnchor.UpperLeft, new Vector2(0.25f, 0.25f), new Vector2(0.90f, 0.39f), PaleGold);
            CreateResourceIcon(parent, "Play Resource Icon", "♠", 0.350f);
            CreateResourceIcon(parent, "Discard Resource Icon", "×", 0.305f, new Color32(190, 120, 103, 255));
            CreateResourceIcon(parent, "Deck Resource Icon", "▣", 0.260f);
            var deckViewer = CreateButton(parent, "Deck Viewer Button", "牌库查看", new Vector2(0.08f, 0.13f), new Vector2(0.47f, 0.22f), new Color32(55, 55, 57, 255));
            var handReference = CreateButton(parent, "Hand Reference Button", "牌型规则", new Vector2(0.53f, 0.13f), new Vector2(0.92f, 0.22f), new Color32(55, 55, 57, 255));
            CreateText(parent, "Rules", "规则 · 选择 1—5 张 · 达到目标分立即胜利", 16, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f), Color.gray);
            return new RightReferences(score, resources, bossRule, deckViewer, handReference);
        }

        private static BattleModalReferences BuildBattleModals(Transform parent)
        {
            var deckViewer = CreatePanel(parent, "Deck Viewer Overlay", new Vector2(0.13f, 0.10f), new Vector2(0.87f, 0.90f), new Color32(13, 14, 14, 252));
            CreateText(deckViewer.transform, "Title", "牌库查看", 44, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.98f), PaleGold, FontStyle.Bold);
            var deckSummary = CreateText(deckViewer.transform, "Deck Viewer Summary", "总数 52", 18, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.88f), Color.gray);
            var zoneButtons = new List<Button>();
            var zoneNames = new[] { "全部牌", "抽牌堆", "已出牌", "已弃牌" };
            for (var index = 0; index < zoneNames.Length; index++)
            {
                var minX = 0.06f + index * 0.22f;
                zoneButtons.Add(CreateButton(deckViewer.transform, $"Deck Zone {zoneNames[index]}", zoneNames[index], new Vector2(minX, 0.73f), new Vector2(minX + 0.20f, 0.81f), new Color32(55, 55, 57, 255)));
            }
            var cardTexts = new List<Text>();
            for (var index = 0; index < 20; index++)
            {
                var column = index % 10;
                var row = index / 10;
                var minX = 0.055f + column * 0.089f;
                var minY = row == 0 ? 0.45f : 0.22f;
                var card = CreatePanel(deckViewer.transform, $"Deck Viewer Card {index + 1}", new Vector2(minX, minY), new Vector2(minX + 0.074f, minY + 0.20f), new Color32(44, 41, 35, 250));
                cardTexts.Add(CreateText(card.transform, "Card Text", "A\n♠", 24, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), PaleGold, FontStyle.Bold));
            }
            var deckPrevious = CreateButton(deckViewer.transform, "Deck Viewer Previous Button", "◀ 上一页", new Vector2(0.06f, 0.07f), new Vector2(0.22f, 0.16f), new Color32(70, 70, 72, 255));
            var deckPage = CreateText(deckViewer.transform, "Deck Viewer Page", "全部牌 · 第 1 / 3 页", 19, TextAnchor.MiddleCenter, new Vector2(0.24f, 0.07f), new Vector2(0.60f, 0.16f), Color.gray);
            var deckNext = CreateButton(deckViewer.transform, "Deck Viewer Next Button", "下一页 ▶", new Vector2(0.62f, 0.07f), new Vector2(0.78f, 0.16f), new Color32(70, 70, 72, 255));
            var deckClose = CreateButton(deckViewer.transform, "Deck Viewer Close Button", "关闭", new Vector2(0.81f, 0.07f), new Vector2(0.94f, 0.16f), Gold);

            var handReference = CreatePanel(parent, "Hand Reference Overlay", new Vector2(0.20f, 0.07f), new Vector2(0.80f, 0.93f), new Color32(13, 14, 14, 252));
            CreateText(handReference.transform, "Title", "基础计分图鉴", 44, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.89f), new Vector2(0.94f, 0.98f), PaleGold, FontStyle.Bold);
            CreateText(handReference.transform, "Formula", "最终得分 =（牌型筹码 + 计分牌 + 人格加成）× 当前倍率 × 最终修正", 18, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.83f), new Vector2(0.94f, 0.89f), Color.gray);
            var handRows = new List<Text>();
            for (var index = 0; index < 12; index++)
            {
                var maxY = 0.80f - index * 0.058f;
                var row = CreatePanel(handReference.transform, $"Hand Reference Row {index + 1}", new Vector2(0.07f, maxY - 0.049f), new Vector2(0.93f, maxY), index % 2 == 0 ? new Color32(42, 39, 33, 230) : new Color32(28, 29, 28, 230));
                handRows.Add(CreateText(row.transform, "Row Text", "01   高牌   未组成其他牌型   筹码 5   ×1", 18, TextAnchor.MiddleLeft, new Vector2(0.025f, 0.04f), new Vector2(0.975f, 0.96f), Color.white));
            }
            var handClose = CreateButton(handReference.transform, "Hand Reference Close Button", "了解规则", new Vector2(0.36f, 0.035f), new Vector2(0.64f, 0.105f), Gold);
            deckViewer.SetActive(false);
            handReference.SetActive(false);
            return new BattleModalReferences(deckViewer, deckClose, deckPrevious, deckNext, zoneButtons.ToArray(),
                cardTexts.ToArray(), deckSummary, deckPage, handReference, handClose, handRows.ToArray());
        }

        private static ResultReferences BuildResultOverlay(Transform parent)
        {
            var panel = CreatePanel(parent, "Result Overlay", new Vector2(0.31f, 0.27f), new Vector2(0.69f, 0.73f), new Color32(17, 18, 20, 250));
            CreateEmblem(panel.transform, "Battle Result Emblem", "✦", new Vector2(0.41f, 0.72f), new Vector2(0.59f, 0.92f), Gold);
            var text = CreateText(panel.transform, "Result Text", "战斗结果", 38, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.74f), PaleGold, FontStyle.Bold);
            var button = CreateButton(panel.transform, "New Battle Button", "开始新局", new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.24f), Gold);
            panel.SetActive(false);
            return new ResultReferences(panel, text, button);
        }

        private static BattleCardView CreateCardPrefab()
        {
            var root = new GameObject("BattleCardView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button), typeof(BattleCardView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(112f, 168f);
            var image = root.GetComponent<Image>();
            image.color = new Color32(103, 84, 51, 255);
            var button = root.GetComponent<Button>();
            button.targetGraphic = image;
            var artwork = CreateArtwork(root.transform, "Parchment Face Artwork", CardFacePath, new Vector2(0.025f, 0.017f), new Vector2(0.975f, 0.983f));
            var label = CreateText(root.transform, "Rank and Suit", "A\n♠", 27, TextAnchor.UpperLeft, new Vector2(0.10f, 0.64f), new Vector2(0.47f, 0.92f), Color.black, FontStyle.Bold);
            var centerSuit = CreateText(root.transform, "Center Suit", "♠", 46, TextAnchor.MiddleCenter, new Vector2(0.20f, 0.28f), new Vector2(0.80f, 0.70f), new Color(0.09f, 0.09f, 0.09f, 0.72f), FontStyle.Bold);
            var mirroredLabel = CreateText(root.transform, "Mirrored Rank and Suit", "A\n♠", 27, TextAnchor.UpperLeft, new Vector2(0.53f, 0.08f), new Vector2(0.90f, 0.36f), Color.black, FontStyle.Bold);
            mirroredLabel.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            var enhancement = CreateText(root.transform, "Enhancement Badge", string.Empty, 17, TextAnchor.MiddleCenter, new Vector2(0.29f, 0.08f), new Vector2(0.71f, 0.20f), new Color32(116, 77, 24, 255), FontStyle.Bold);
            root.GetComponent<BattleCardView>().ConfigurePrefab(image, artwork, button, label, centerSuit, mirroredLabel, enhancement, root.GetComponent<CanvasGroup>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath).GetComponent<BattleCardView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Stretch(gameObject.GetComponent<RectTransform>(), min, max);
            gameObject.GetComponent<Image>().color = color;
            if (IsMajorFrame(name))
                DecorateMajorFrame(gameObject);
            else if (IsInsetFrame(name))
                AddOutline(gameObject, SubtleGold, new Vector2(1f, -1f));
            return gameObject;
        }

        private static bool IsMajorFrame(string name)
        {
            return name is "Left - Persona Slots" or "Center - Table" or "Right - Battle Info" or
                "Main Menu Card" or "Persona Setup Card" or "Boss Reveal Card" or "Run Result Card" or
                "Reward Card" or "Shop Card" or "Run Report Card" or "Persona Forge Card" or "Result Overlay" or
                "Settings Card";
        }

        private static bool IsInsetFrame(string name)
        {
            return name.Contains("Slot") || name.Contains("Rule Panel") || name.Contains("Hand Area") ||
                name.Contains("Candidate") || name.Contains("Played Card Slots") || name.Contains("Summary Panel");
        }

        private static void DecorateMajorFrame(GameObject panel)
        {
            AddOutline(panel, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));
            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(5f, -5f);

            CreateFrameLine(panel.transform, "Top Gold Hairline", new Vector2(0.04f, 0.985f), new Vector2(0.96f, 0.988f));
            CreateFrameLine(panel.transform, "Bottom Gold Hairline", new Vector2(0.04f, 0.012f), new Vector2(0.96f, 0.015f));
            CreateCornerStud(panel.transform, "Top Left Stud", new Vector2(0.018f, 0.972f));
            CreateCornerStud(panel.transform, "Top Right Stud", new Vector2(0.982f, 0.972f));
            CreateCornerStud(panel.transform, "Bottom Left Stud", new Vector2(0.018f, 0.028f));
            CreateCornerStud(panel.transform, "Bottom Right Stud", new Vector2(0.982f, 0.028f));
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void CreateFrameLine(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            Stretch(line.GetComponent<RectTransform>(), min, max);
            var image = line.GetComponent<Image>();
            image.color = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
            image.raycastTarget = false;
        }

        private static void CreateCornerStud(Transform parent, string name, Vector2 anchor)
        {
            var stud = new GameObject(name, typeof(RectTransform), typeof(Image));
            stud.transform.SetParent(parent, false);
            var rect = stud.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(7f, 7f);
            rect.anchoredPosition = Vector2.zero;
            rect.localEulerAngles = new Vector3(0f, 0f, 45f);
            var image = stud.GetComponent<Image>();
            image.color = Gold;
            image.raycastTarget = false;
        }

        private static void CreateEmblem(Transform parent, string name, string symbol, Vector2 min, Vector2 max, Color color, bool framed = true)
        {
            Transform iconParent = parent;
            if (framed)
            {
                var badge = new GameObject(name + " Frame", typeof(RectTransform), typeof(Image));
                badge.transform.SetParent(parent, false);
                Stretch(badge.GetComponent<RectTransform>(), min, max);
                badge.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, 45f);
                var image = badge.GetComponent<Image>();
                image.color = new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 0.92f);
                image.raycastTarget = false;
                AddOutline(badge, new Color(color.r, color.g, color.b, 0.82f), new Vector2(1.2f, -1.2f));
                iconParent = badge.transform;
                var text = CreateText(iconParent, name, symbol, 46, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), color, FontStyle.Bold);
                text.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
                return;
            }

            CreateText(iconParent, name, symbol, 28, TextAnchor.MiddleCenter, min, max, color, FontStyle.Bold);
        }

        private static void AddButtonIcon(Button button, string symbol, Color color)
        {
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.24f, 0.04f);
                label.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
            CreateText(button.transform, "Action Icon", symbol, 27, TextAnchor.MiddleCenter, new Vector2(0.07f, 0.12f), new Vector2(0.26f, 0.88f), color, FontStyle.Bold);
        }

        private static void CreateResourceIcon(Transform parent, string name, string symbol, float centerY, Color? color = null)
        {
            var badge = new GameObject(name + " Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(parent, false);
            Stretch(badge.GetComponent<RectTransform>(), new Vector2(0.10f, centerY - 0.032f), new Vector2(0.20f, centerY + 0.032f));
            var image = badge.GetComponent<Image>();
            image.color = new Color32(35, 31, 24, 235);
            image.raycastTarget = false;
            AddOutline(badge, SubtleGold, new Vector2(1f, -1f));
            CreateText(badge.transform, name, symbol, 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, color ?? Gold, FontStyle.Bold);
        }

        private static RawImage CreateArtwork(Transform parent, string name, string assetPath, Vector2 min, Vector2 max)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            gameObject.transform.SetParent(parent, false);
            Stretch(gameObject.GetComponent<RectTransform>(), min, max);
            var artwork = gameObject.GetComponent<RawImage>();
            artwork.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            artwork.color = Color.white;
            artwork.raycastTarget = false;
            if (artwork.texture != null)
            {
                var fitter = gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)artwork.texture.width / artwork.texture.height;
            }
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
            outline.effectDistance = new Vector2(1f, -1f);
            return artwork;
        }

        private static RectTransform CreateScreenRoot(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            Stretch(rect, Vector2.zero, Vector2.one);
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Vector2 min, Vector2 max, Color color, FontStyle style = FontStyle.Normal)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Stretch(gameObject.GetComponent<RectTransform>(), min, max);
            var text = gameObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color color)
        {
            var primary = Approximately(color, Gold);
            var danger = !primary && color.r > color.g * 1.25f && color.r > color.b * 1.15f;
            var baseColor = primary ? PrimaryButton : danger ? new Color32(55, 27, 25, 248) : SecondaryButton;
            var gameObject = CreatePanel(parent, name, min, max, baseColor);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ConfigureButtonTransitions(button, primary, danger);
            AddOutline(gameObject, primary ? Gold : danger ? new Color32(126, 70, 57, 210) : SubtleGold, new Vector2(1.2f, -1.2f));
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(3f, -3f);
            var labelText = CreateText(gameObject.transform, "Label", label, 28, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), primary ? PaleGold : new Color32(213, 205, 185, 255), FontStyle.Bold);
            var textShadow = labelText.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            textShadow.effectDistance = new Vector2(1f, -1f);
            return button;
        }

        private static void ConfigureButtonTransitions(Button button, bool primary, bool danger)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = primary ? new Color32(255, 235, 180, 255) : danger ? new Color32(232, 176, 160, 255) : new Color32(224, 210, 178, 255),
                pressedColor = primary ? new Color32(154, 124, 72, 255) : danger ? new Color32(139, 92, 82, 255) : new Color32(150, 145, 132, 255),
                selectedColor = primary ? new Color32(242, 212, 145, 255) : danger ? new Color32(213, 151, 136, 255) : new Color32(206, 191, 157, 255),
                disabledColor = new Color32(92, 92, 88, 130),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        private static bool Approximately(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) < 0.01f && Mathf.Abs(left.g - right.g) < 0.01f &&
                Mathf.Abs(left.b - right.b) < 0.01f && Mathf.Abs(left.a - right.a) < 0.01f;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>(), min, max);
            var background = CreatePanel(root.transform, "Checkbox", new Vector2(0f, 0.18f), new Vector2(0.24f, 0.82f), new Color32(50, 50, 52, 255));
            var checkmark = CreatePanel(background.transform, "Checkmark", new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f), Gold);
            var text = CreateText(root.transform, "Label", label, 17, TextAnchor.MiddleLeft, new Vector2(0.30f, 0f), Vector2.one, PaleGold);
            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = false;
            return toggle;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/PersonaCards/UI", "Prefabs");
            }
        }

        private static void SetFirstBuildScene()
        {
            var paths = new List<string> { ScenePath };
            paths.AddRange(EditorBuildSettings.scenes.Select(scene => scene.path).Where(path => path != ScenePath));
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private readonly struct CenterReferences
        {
            public CenterReferences(RectTransform handRoot, Text preview, Text message, Button play, Button discard,
                RectTransform playedSlots, Text scoringLog)
            { HandRoot = handRoot; Preview = preview; Message = message; Play = play; Discard = discard;
                PlayedSlots = playedSlots; ScoringLog = scoringLog; }
            public RectTransform HandRoot { get; }
            public Text Preview { get; }
            public Text Message { get; }
            public Button Play { get; }
            public Button Discard { get; }
            public RectTransform PlayedSlots { get; }
            public Text ScoringLog { get; }
        }

        private readonly struct RightReferences
        {
            public RightReferences(Text score, Text resources, Text bossRule, Button deckViewer, Button handReference)
            { Score = score; Resources = resources; BossRule = bossRule; DeckViewer = deckViewer; HandReference = handReference; }
            public Text Score { get; }
            public Text Resources { get; }
            public Text BossRule { get; }
            public Button DeckViewer { get; }
            public Button HandReference { get; }
        }

        private readonly struct BattleModalReferences
        {
            public BattleModalReferences(GameObject deckViewer, Button deckViewerClose, Button deckViewerPrevious,
                Button deckViewerNext, Button[] deckViewerZones, Text[] deckViewerCards, Text deckViewerSummary,
                Text deckViewerPage, GameObject handReference, Button handReferenceClose, Text[] handReferenceRows)
            {
                DeckViewer = deckViewer;
                DeckViewerClose = deckViewerClose;
                DeckViewerPrevious = deckViewerPrevious;
                DeckViewerNext = deckViewerNext;
                DeckViewerZones = deckViewerZones;
                DeckViewerCards = deckViewerCards;
                DeckViewerSummary = deckViewerSummary;
                DeckViewerPage = deckViewerPage;
                HandReference = handReference;
                HandReferenceClose = handReferenceClose;
                HandReferenceRows = handReferenceRows;
            }
            public GameObject DeckViewer { get; }
            public Button DeckViewerClose { get; }
            public Button DeckViewerPrevious { get; }
            public Button DeckViewerNext { get; }
            public Button[] DeckViewerZones { get; }
            public Text[] DeckViewerCards { get; }
            public Text DeckViewerSummary { get; }
            public Text DeckViewerPage { get; }
            public GameObject HandReference { get; }
            public Button HandReferenceClose { get; }
            public Text[] HandReferenceRows { get; }
        }

        private readonly struct PersonaPanelReferences
        {
            public PersonaPanelReferences(Text[] names, Text[] rules) { Names = names; Rules = rules; }
            public Text[] Names { get; }
            public Text[] Rules { get; }
        }

        private readonly struct ResultReferences
        {
            public ResultReferences(GameObject panel, Text text, Button button) { Panel = panel; Text = text; Button = button; }
            public GameObject Panel { get; }
            public Text Text { get; }
            public Button Button { get; }
        }

        private readonly struct FlowReferences
        {
            public FlowReferences(GameObject mainMenu, GameObject collection, GameObject personaSetup, GameObject bossReveal,
                GameObject reward, GameObject shop, GameObject runReport, GameObject personaForge, GameObject failureResult,
                Button start, Button continueGame, Button collectionButton, Button collectionBack,
                Button collectionPrevious, Button collectionNext, Button collectionUnequip,
                Button[] collectionCards, Text[] collectionCardTexts,
                Button[] collectionEquipment, Text[] collectionEquipmentTexts,
                Text collectionDetail, Text collectionPage,
                Button confirmPersona, Button beginBattle, Button resultReturn,
                Button rewardContinue, Button shopContinue, Button reportReturn,
                Button personaBack, Button bossBack, Text resultTitle, Text resultSummary,
                Text battleProgress, Text bossRevealRule, Text reportSummary, Text rewardCard, Text shopCard, Text shopCoins, Text shopStatus,
                Button rewardPrevious, Button rewardNext, Button shopPrevious, Button shopNext,
                Button shopDelete, Button shopReforge, Button shopEnhance,
                Button[] personaSlots, Text[] personaNames, Text[] personaRules,
                Text forgeRolls, Text forgeStatus, Text[] forgeCandidates, Button[] forgeCandidateButtons,
                Button forgeConfirm,
                GameObject tutorialOverlay, Text tutorialStep, Text tutorialTitle, Text tutorialBody,
                Button tutorialNext, Button tutorialSkip, Text tutorialNextLabel, Button tutorialReplay, Text tutorialReplayLabel,
                SettingsReferences settings)
            {
                MainMenu = mainMenu;
                Collection = collection;
                PersonaSetup = personaSetup;
                BossReveal = bossReveal;
                Reward = reward;
                Shop = shop;
                RunReport = runReport;
                PersonaForge = personaForge;
                FailureResult = failureResult;
                Start = start;
                Continue = continueGame;
                CollectionButton = collectionButton;
                CollectionBack = collectionBack;
                CollectionPrevious = collectionPrevious;
                CollectionNext = collectionNext;
                CollectionUnequip = collectionUnequip;
                CollectionCards = collectionCards;
                CollectionCardTexts = collectionCardTexts;
                CollectionEquipment = collectionEquipment;
                CollectionEquipmentTexts = collectionEquipmentTexts;
                CollectionDetail = collectionDetail;
                CollectionPage = collectionPage;
                ConfirmPersona = confirmPersona;
                BeginBattle = beginBattle;
                ResultReturn = resultReturn;
                RewardContinue = rewardContinue;
                ShopContinue = shopContinue;
                ReportReturn = reportReturn;
                PersonaBack = personaBack;
                BossBack = bossBack;
                ResultTitle = resultTitle;
                ResultSummary = resultSummary;
                BattleProgress = battleProgress;
                BossRevealRule = bossRevealRule;
                ReportSummary = reportSummary;
                RewardCard = rewardCard;
                ShopCard = shopCard;
                ShopCoins = shopCoins;
                ShopStatus = shopStatus;
                RewardPrevious = rewardPrevious;
                RewardNext = rewardNext;
                ShopPrevious = shopPrevious;
                ShopNext = shopNext;
                ShopDelete = shopDelete;
                ShopReforge = shopReforge;
                ShopEnhance = shopEnhance;
                PersonaSlots = personaSlots;
                PersonaNames = personaNames;
                PersonaRules = personaRules;
                ForgeRolls = forgeRolls;
                ForgeStatus = forgeStatus;
                ForgeCandidates = forgeCandidates;
                ForgeCandidateButtons = forgeCandidateButtons;
                ForgeConfirm = forgeConfirm;
                TutorialOverlay = tutorialOverlay;
                TutorialStep = tutorialStep;
                TutorialTitle = tutorialTitle;
                TutorialBody = tutorialBody;
                TutorialNext = tutorialNext;
                TutorialSkip = tutorialSkip;
                TutorialNextLabel = tutorialNextLabel;
                TutorialReplay = tutorialReplay;
                TutorialReplayLabel = tutorialReplayLabel;
                Settings = settings;
            }

            public GameObject MainMenu { get; }
            public GameObject Collection { get; }
            public GameObject PersonaSetup { get; }
            public GameObject BossReveal { get; }
            public GameObject Reward { get; }
            public GameObject Shop { get; }
            public GameObject RunReport { get; }
            public GameObject PersonaForge { get; }
            public GameObject FailureResult { get; }
            public Button Start { get; }
            public Button Continue { get; }
            public Button CollectionButton { get; }
            public Button CollectionBack { get; }
            public Button CollectionPrevious { get; }
            public Button CollectionNext { get; }
            public Button CollectionUnequip { get; }
            public Button[] CollectionCards { get; }
            public Text[] CollectionCardTexts { get; }
            public Button[] CollectionEquipment { get; }
            public Text[] CollectionEquipmentTexts { get; }
            public Text CollectionDetail { get; }
            public Text CollectionPage { get; }
            public Button ConfirmPersona { get; }
            public Button BeginBattle { get; }
            public Button ResultReturn { get; }
            public Button RewardContinue { get; }
            public Button ShopContinue { get; }
            public Button ReportReturn { get; }
            public Button PersonaBack { get; }
            public Button BossBack { get; }
            public Text ResultTitle { get; }
            public Text ResultSummary { get; }
            public Text BattleProgress { get; }
            public Text BossRevealRule { get; }
            public Text ReportSummary { get; }
            public Text RewardCard { get; }
            public Text ShopCard { get; }
            public Text ShopCoins { get; }
            public Text ShopStatus { get; }
            public Button RewardPrevious { get; }
            public Button RewardNext { get; }
            public Button ShopPrevious { get; }
            public Button ShopNext { get; }
            public Button ShopDelete { get; }
            public Button ShopReforge { get; }
            public Button ShopEnhance { get; }
            public Button[] PersonaSlots { get; }
            public Text[] PersonaNames { get; }
            public Text[] PersonaRules { get; }
            public Text ForgeRolls { get; }
            public Text ForgeStatus { get; }
            public Text[] ForgeCandidates { get; }
            public Button[] ForgeCandidateButtons { get; }
            public Button ForgeConfirm { get; }
            public GameObject TutorialOverlay { get; }
            public Text TutorialStep { get; }
            public Text TutorialTitle { get; }
            public Text TutorialBody { get; }
            public Button TutorialNext { get; }
            public Button TutorialSkip { get; }
            public Text TutorialNextLabel { get; }
            public Button TutorialReplay { get; }
            public Text TutorialReplayLabel { get; }
            public SettingsReferences Settings { get; }
        }

        /// <summary>设置界面引用集合（P0-1H）：独立 struct 收编 22 个引用，避免 FlowReferences 构造器继续膨胀。</summary>
        private readonly struct SettingsReferences
        {
            public SettingsReferences(Button mainMenuEntry, GameObject overlay,
                Slider brightness, Text brightnessValue, Slider volume, Text volumeValue,
                Toggle animation, Toggle shake,
                Button playKey, Text playKeyText, Button discardKey, Text discardKeyText,
                Button settingsKey, Text settingsKeyText,
                Button tutorialReplay, Text tutorialReplayText,
                Button back, Button returnMain, Button restoreDefaults, Button cancel, Button save,
                Image dim)
            {
                MainMenuEntry = mainMenuEntry;
                Overlay = overlay;
                Brightness = brightness;
                BrightnessValue = brightnessValue;
                Volume = volume;
                VolumeValue = volumeValue;
                Animation = animation;
                Shake = shake;
                PlayKey = playKey;
                PlayKeyText = playKeyText;
                DiscardKey = discardKey;
                DiscardKeyText = discardKeyText;
                SettingsKey = settingsKey;
                SettingsKeyText = settingsKeyText;
                TutorialReplay = tutorialReplay;
                TutorialReplayText = tutorialReplayText;
                Back = back;
                ReturnMain = returnMain;
                RestoreDefaults = restoreDefaults;
                Cancel = cancel;
                Save = save;
                Dim = dim;
            }

            public Button MainMenuEntry { get; }
            public GameObject Overlay { get; }
            public Slider Brightness { get; }
            public Text BrightnessValue { get; }
            public Slider Volume { get; }
            public Text VolumeValue { get; }
            public Toggle Animation { get; }
            public Toggle Shake { get; }
            public Button PlayKey { get; }
            public Text PlayKeyText { get; }
            public Button DiscardKey { get; }
            public Text DiscardKeyText { get; }
            public Button SettingsKey { get; }
            public Text SettingsKeyText { get; }
            public Button TutorialReplay { get; }
            public Text TutorialReplayText { get; }
            public Button Back { get; }
            public Button ReturnMain { get; }
            public Button RestoreDefaults { get; }
            public Button Cancel { get; }
            public Button Save { get; }
            public Image Dim { get; }
        }
    }
}
