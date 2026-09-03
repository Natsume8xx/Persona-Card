using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PersonaCards.UI;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 列表型强化界面 prefab 一次性构建脚本（UI 重排第二批）：一模板两 prefab——
    /// Resources/Prefabs/HandEnhancePanel.prefab（牌型强化）与 Resources/Prefabs/PersonaMainAttrPanel.prefab（人格主词条强化）。
    /// 骨架由代码生成（照 BattlePrototypeSceneBuilder 惯例），候选行由 EnhanceListPanelView 运行时全量重建；
    /// 标题/说明/选中样式在 Configure 时由会话与样式参数写入，prefab 只留骨架。
    /// 候选列表带 ScrollRect 兜底（11 候选 6 行在 1080p 下可能溢出）。
    /// 菜单：Persona Cards/Rebuild Enhance List Panel Prefabs（重复执行幂等：覆盖同名 prefab）。
    /// </summary>
    public static class EnhanceListPanelPrefabBuilder
    {
        private const string HandPrefabPath = "Assets/PersonaCards/Resources/Prefabs/HandEnhancePanel.prefab";
        private const string PersonaPrefabPath = "Assets/PersonaCards/Resources/Prefabs/PersonaMainAttrPanel.prefab";

        private static readonly Color Panel = new Color32(14, 17, 17, 238);
        private static readonly Color Gold = new Color32(178, 139, 73, 255);
        private static readonly Color PaleGold = new Color32(232, 214, 173, 255);
        private static readonly Color PrimaryButton = new Color32(58, 47, 28, 248);
        private static readonly Color SecondaryButton = new Color32(31, 32, 31, 248);
        private static readonly Color SubtleGold = new Color32(112, 88, 49, 180);
        private static readonly Color DetailGold = new Color32(168, 142, 96, 255);

        [MenuItem("Persona Cards/Rebuild Enhance List Panel Prefabs")]
        public static void Build()
        {
            BuildOne("HandEnhancePanel", "Hand Enhance Card", HandPrefabPath);
            BuildOne("PersonaMainAttrPanel", "Persona Main Attr Card", PersonaPrefabPath);
        }

        private static void BuildOne(string rootName, string panelName, string prefabPath)
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 22);

            var root = new GameObject(rootName,
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(EnhanceListPanelView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            var overlay = root.GetComponent<Image>();
            overlay.color = new Color(0.04f, 0.03f, 0.02f, 0.78f); // 全屏深棕黑遮罩 + raycast 拦截

            var panel = CreatePanel(root.transform, panelName,
                new Vector2(0.19f, 0.16f), new Vector2(0.81f, 0.84f), Panel);
            AddOutline(panel, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));

            // Header：强化商品小标 + 大标题 + 说明 + 金色细分割线
            CreateText(panel.transform, "Tag", "强化商品", 16, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.945f), new Vector2(0.20f, 0.985f), SubtleGold, font, FontStyle.Normal);
            var title = CreateText(panel.transform, "Title", "牌型强化", 34, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.885f), new Vector2(0.97f, 0.955f), PaleGold, font, FontStyle.Bold);
            var description = CreateText(panel.transform, "Description", "", 18, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.830f), new Vector2(0.97f, 0.880f), DetailGold, font, FontStyle.Normal);
            CreateLine(panel.transform, "Divider", new Vector2(0.03f, 0.818f), new Vector2(0.97f, 0.822f));

            // 候选列表：ScrollRect + GridLayoutGroup 2 列（cell 540×88；行由视图运行时创建）
            var scroll = new GameObject("Entry Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(panel.transform, false);
            Stretch(scroll.GetComponent<RectTransform>(), new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.80f));
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scroll.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(540f, 88f);
            grid.spacing = new Vector2(10f, 6f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Footer：左下提示/价格 + 返回商店 + 确认购买（未选禁用由视图刷新）
            var footer = CreateText(panel.transform, "Footer", "本次价格：-- 金币", 20, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.03f), new Vector2(0.60f, 0.09f), PaleGold, font, FontStyle.Bold);
            var cancel = CreateButton(panel.transform, "Cancel Button", "返回商店", 20,
                new Vector2(0.62f, 0.035f), new Vector2(0.76f, 0.085f), new Color32(213, 205, 185, 255), font);
            var confirm = CreateButton(panel.transform, "Confirm Button", "确认购买", 20,
                new Vector2(0.77f, 0.035f), new Vector2(0.97f, 0.085f), Gold, font);

            root.GetComponent<EnhanceListPanelView>().ConfigurePrefab(title, description, contentRect, footer,
                cancel, confirm);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhanceListPanel] prefab 已重建：{AssetDatabase.GetAssetPath(prefab)}");
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Stretch(gameObject.GetComponent<RectTransform>(), min, max);
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment,
            Vector2 min, Vector2 max, Color color, Font font, FontStyle style = FontStyle.Normal)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Stretch(gameObject.GetComponent<RectTransform>(), min, max);
            var text = gameObject.GetComponent<Text>();
            text.font = font;
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

        private static Button CreateButton(Transform parent, string name, string label, int fontSize,
            Vector2 min, Vector2 max, Color color, Font font)
        {
            var primary = Approximately(color, Gold);
            var baseColor = primary ? PrimaryButton : SecondaryButton;
            var gameObject = CreatePanel(parent, name, min, max, baseColor);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ConfigureButtonTransitions(button, primary);
            AddOutline(gameObject, primary ? Gold : SubtleGold, new Vector2(1.2f, -1.2f));
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(3f, -3f);
            var labelText = CreateText(gameObject.transform, "Label", label, fontSize, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                primary ? PaleGold : new Color32(213, 205, 185, 255), font, FontStyle.Bold);
            var textShadow = labelText.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            textShadow.effectDistance = new Vector2(1f, -1f);
            return button;
        }

        private static void ConfigureButtonTransitions(Button button, bool primary)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = primary ? new Color32(255, 235, 180, 255) : new Color32(224, 210, 178, 255),
                pressedColor = primary ? new Color32(154, 124, 72, 255) : new Color32(150, 145, 132, 255),
                selectedColor = primary ? new Color32(242, 212, 145, 255) : new Color32(206, 191, 157, 255),
                disabledColor = new Color32(92, 92, 88, 130),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        private static void CreateLine(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            Stretch(line.GetComponent<RectTransform>(), min, max);
            var image = line.GetComponent<Image>();
            image.color = new Color(Gold.r, Gold.g, Gold.b, 0.42f);
            image.raycastTarget = false;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool Approximately(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) < 0.01f && Mathf.Abs(left.g - right.g) < 0.01f &&
                Mathf.Abs(left.b - right.b) < 0.01f && Mathf.Abs(left.a - right.a) < 0.01f;
        }
    }
}
