using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PersonaCards.UI;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 选牌弹窗 prefab 一次性构建脚本（UI 重排第二批）：产出 Resources/Prefabs/CardPickPopup.prefab。
    /// 骨架由代码生成（照 BattlePrototypeSceneBuilder 惯例），卡格由 CardPickPopupView 运行时全量重建。
    /// 菜单：Persona Cards/Rebuild Card Pick Popup Prefab（重复执行幂等：覆盖同名 prefab）。
    /// </summary>
    public static class CardPickPopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/PersonaCards/Resources/Prefabs/CardPickPopup.prefab";

        private static readonly Color Panel = new Color32(14, 17, 17, 238);
        private static readonly Color Gold = new Color32(178, 139, 73, 255);
        private static readonly Color PaleGold = new Color32(232, 214, 173, 255);
        private static readonly Color PrimaryButton = new Color32(58, 47, 28, 248);
        private static readonly Color SecondaryButton = new Color32(31, 32, 31, 248);
        private static readonly Color SubtleGold = new Color32(112, 88, 49, 180);
        private static readonly Color DimText = new Color32(150, 145, 135, 140);
        private static readonly Color DetailGold = new Color32(168, 142, 96, 255);
        private static readonly Color InfoPanel = new Color32(26, 28, 26, 245);

        [MenuItem("Persona Cards/Rebuild Card Pick Popup Prefab")]
        public static void Build()
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 24);

            var root = new GameObject("CardPickPopup",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(CardPickPopupView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            var overlay = root.GetComponent<Image>();
            overlay.color = new Color(0.04f, 0.03f, 0.02f, 0.78f); // 全屏深棕黑遮罩 + raycast 拦截

            var panel = CreatePanel(root.transform, "Card Pick Modal",
                new Vector2(0.17f, 0.10f), new Vector2(0.83f, 0.90f), Panel);
            AddOutline(panel, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));

            var title = CreateText(panel.transform, "Title", "选择商品作用的卡牌", 32, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.925f), new Vector2(0.55f, 0.985f), PaleGold, font, FontStyle.Bold);
            var close = CreateButton(panel.transform, "Close", "×", 30,
                new Vector2(0.93f, 0.925f), new Vector2(0.975f, 0.985f), Gold, font);

            // 标签栏：当前牌库亮金 + 金下划线；已用牌/已弃牌置灰（项目无此数据源，记代策划确认）
            CreateText(panel.transform, "Tab Current", "当前牌库", 20, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.868f), new Vector2(0.16f, 0.922f), PaleGold, font, FontStyle.Bold);
            CreateLine(panel.transform, "Tab Current Underline", new Vector2(0.03f, 0.860f), new Vector2(0.16f, 0.865f));
            CreateText(panel.transform, "Tab Used", "已用牌", 20, TextAnchor.MiddleLeft,
                new Vector2(0.18f, 0.868f), new Vector2(0.26f, 0.922f), DimText, font, FontStyle.Normal);
            CreateText(panel.transform, "Tab Discarded", "已弃牌", 20, TextAnchor.MiddleLeft,
                new Vector2(0.27f, 0.868f), new Vector2(0.35f, 0.922f), DimText, font, FontStyle.Normal);

            // 统计 / 提示 / 排序按钮（选中态由视图刷底与描边）
            var stats = CreateText(panel.transform, "Stats", "总数 0 / 可选 0", 20, TextAnchor.MiddleLeft,
                new Vector2(0.40f, 0.870f), new Vector2(0.70f, 0.922f), PaleGold, font, FontStyle.Normal);
            var hint = CreateText(panel.transform, "Hint", "请选择 1 张目标牌", 16, TextAnchor.MiddleLeft,
                new Vector2(0.40f, 0.822f), new Vector2(0.70f, 0.870f), DetailGold, font, FontStyle.Normal);
            var sortRank = CreateButton(panel.transform, "Sort By Rank", "大小", 18,
                new Vector2(0.73f, 0.868f), new Vector2(0.80f, 0.922f), new Color32(213, 205, 185, 255), font);
            var sortSuit = CreateButton(panel.transform, "Sort By Suit", "花色", 18,
                new Vector2(0.81f, 0.868f), new Vector2(0.88f, 0.922f), new Color32(213, 205, 185, 255), font);

            // 卡牌网格 10×6（cell 62×62；格由视图运行时创建，此处只留 GridLayoutGroup 容器）
            var grid = new GameObject("Card Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(panel.transform, false);
            Stretch(grid.GetComponent<RectTransform>(), new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.80f));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(62f, 62f);
            layout.spacing = new Vector2(5f, 5f);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // 底部商品信息栏（标题/描述随选中动态刷新）
            var info = CreatePanel(panel.transform, "Product Info",
                new Vector2(0.04f, 0.05f), new Vector2(0.70f, 0.27f), InfoPanel);
            AddOutline(info, SubtleGold, new Vector2(1f, -1f));
            var thumb = CreatePanel(info.transform, "Service Thumb",
                new Vector2(0.02f, 0.10f), new Vector2(0.10f, 0.90f), new Color32(103, 84, 51, 255));
            var serviceName = CreateText(info.transform, "Service Name", "强化", 24, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.55f), new Vector2(0.98f, 0.95f), PaleGold, font, FontStyle.Bold);
            var serviceDetail = CreateText(info.transform, "Service Detail", "", 16, TextAnchor.MiddleLeft,
                new Vector2(0.12f, 0.10f), new Vector2(0.98f, 0.52f), DetailGold, font, FontStyle.Normal);

            var cancel = CreateButton(panel.transform, "Cancel Button", "取消购买", 24,
                new Vector2(0.72f, 0.17f), new Vector2(0.94f, 0.26f), new Color32(213, 205, 185, 255), font);
            var confirm = CreateButton(panel.transform, "Confirm Button", "确认购买并应用", 24,
                new Vector2(0.72f, 0.05f), new Vector2(0.94f, 0.14f), Gold, font);

            root.GetComponent<CardPickPopupView>().ConfigurePrefab(title, close, stats, hint, sortRank, sortSuit,
                grid.GetComponent<RectTransform>(), thumb.GetComponent<Image>(), serviceName, serviceDetail,
                cancel, confirm);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CardPickPopup] prefab 已重建：{AssetDatabase.GetAssetPath(prefab)}");
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
