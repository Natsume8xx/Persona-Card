using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PersonaCards.UI;

namespace PersonaCards.UI.Editor
{
    /// <summary>
    /// 商店主界面 prefab 一次性构建脚本（UI 重排第二批）：Resources/Prefabs/ShopUi.prefab。
    /// 根名「Shop Ui」刻意避开 "Card"（IsMajorFrame 名单含旧场景节点 "Shop Card"）。
    /// 三列布局（1920×1080 参考）：顶部大标题 + 玫瑰窗占位；左列玩家信息侧边栏 (0.02,0.04)-(0.20,0.955)、
    /// 中列标签页区 (0.22,0.04)-(0.68,0.955)（商品/人格铸造两标签 + 状态行）、右列详情区 (0.70,0.04)-(0.98,0.955)
    /// （商品详情/铸造详情两区块 + 共用离开按钮）。商品行/服务行为固定骨架；铸造行与副属性行由 ShopUiView 运行时重建。
    /// 菜单：Persona Cards/Rebuild Shop Ui Prefab（重复执行幂等：覆盖同名 prefab）。
    /// </summary>
    public static class ShopUiPrefabBuilder
    {
        private const string PrefabPath = "Assets/PersonaCards/Resources/Prefabs/ShopUi.prefab";

        private static readonly Color Panel = new Color32(14, 17, 17, 238);
        private static readonly Color InfoPanel = new Color32(26, 28, 26, 245);
        private static readonly Color Gold = new Color32(178, 139, 73, 255);
        private static readonly Color PaleGold = new Color32(232, 214, 173, 255);
        private static readonly Color PrimaryButton = new Color32(58, 47, 28, 248);
        private static readonly Color SecondaryButton = new Color32(31, 32, 31, 248);
        private static readonly Color SubtleGold = new Color32(112, 88, 49, 180);
        private static readonly Color DetailGold = new Color32(168, 142, 96, 255);
        private static readonly Color LeaveButtonBase = new Color32(20, 28, 44, 248);
        private static readonly Color32 LeaveButtonLabel = new Color32(213, 205, 185, 255);

        [MenuItem("Persona Cards/Rebuild Shop Ui Prefab")]
        public static void Build()
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 22);

            var root = new GameObject("Shop Ui", typeof(RectTransform), typeof(Image), typeof(ShopUiView));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            root.GetComponent<Image>().color = new Color32(12, 14, 14, 255);

            // 顶部：大标题 + 玫瑰窗占位（美术替换位）
            CreateText(root.transform, "Title", "商店", 42, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.962f), new Vector2(0.30f, 0.992f), PaleGold, font, FontStyle.Bold);
            var roseWindow = new GameObject("Rose Window Placeholder", typeof(RectTransform), typeof(Image));
            roseWindow.transform.SetParent(root.transform, false);
            Stretch(roseWindow.GetComponent<RectTransform>(), new Vector2(0.925f, 0.958f), new Vector2(0.985f, 0.995f));
            var roseImage = roseWindow.GetComponent<Image>();
            roseImage.sprite = CreateCircleSprite();
            roseImage.color = DetailGold;
            roseImage.raycastTarget = false;
            AddOutline(roseWindow, Gold, new Vector2(1.2f, -1.2f));

            // 左列：玩家信息侧边栏
            var sidebar = CreatePanel(root.transform, "Sidebar", new Vector2(0.02f, 0.04f), new Vector2(0.20f, 0.955f), InfoPanel);
            AddOutline(sidebar, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));
            CreateText(sidebar.transform, "Header", "玩家信息", 19, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.915f), new Vector2(0.95f, 0.945f), PaleGold, font, FontStyle.Bold);
            CreateLine(sidebar.transform, "Line Top", new Vector2(0.05f, 0.895f), new Vector2(0.95f, 0.90f));
            var sidebarStats = CreateText(sidebar.transform, "Stats", "金币 -- · 牌库 -- 张 · 人格 0/0", 17,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.875f), PaleGold, font,
                FontStyle.Normal);
            sidebarStats.horizontalOverflow = HorizontalWrapMode.Wrap;
            CreateLine(sidebar.transform, "Line Mid", new Vector2(0.05f, 0.775f), new Vector2(0.95f, 0.78f));
            CreateLine(sidebar.transform, "Line Bottom", new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.035f));

            // 中列：标签页区（商品 / 人格铸造）
            var middle = CreatePanel(root.transform, "Middle", new Vector2(0.22f, 0.04f), new Vector2(0.68f, 0.955f), Panel);
            AddOutline(middle, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));
            var productsTab = CreateTabButton(middle.transform, "Products Tab", "商品",
                new Vector2(0.02f, 0.895f), new Vector2(0.125f, 0.94f), font);
            var forgeTab = CreateTabButton(middle.transform, "Forge Tab", "人格铸造",
                new Vector2(0.135f, 0.895f), new Vector2(0.24f, 0.94f), font);
            var productsTabLabel = productsTab.transform.Find("Label").GetComponent<Text>();
            var forgeTabLabel = forgeTab.transform.Find("Label").GetComponent<Text>();

            // 商品区块：4 商品行 + 服务区块 3 行
            var productsArea = CreatePanel(middle.transform, "Products Area",
                new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.885f), new Color(0f, 0f, 0f, 0f));
            productsArea.GetComponent<Image>().raycastTarget = false;
            CreateText(productsArea.transform, "Products Header", "商品", 19, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.925f), new Vector2(0.25f, 0.985f), PaleGold, font, FontStyle.Bold);
            var productRows = new Button[4];
            var productRowLabels = new Text[4];
            for (var index = 0; index < 4; index++)
            {
                var y = 0.79f - index * 0.135f;
                var row = CreateButton(productsArea.transform, "Product Row " + index, "--", 17,
                    new Vector2(0f, y), new Vector2(1f, y + 0.115f), DetailGold, font);
                productRows[index] = row;
                productRowLabels[index] = row.transform.Find("Label").GetComponent<Text>();
            }
            CreateText(productsArea.transform, "Services Header", "服务", 16, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.30f), new Vector2(0.25f, 0.375f), SubtleGold, font, FontStyle.Normal);
            var serviceRows = new Button[3];
            var serviceRowLabels = new Text[3];
            for (var index = 0; index < 3; index++)
            {
                var y = 0.205f - index * 0.095f;
                var row = CreateButton(productsArea.transform, "Service Row " + index, "--", 17,
                    new Vector2(0f, y), new Vector2(1f, y + 0.085f), DetailGold, font);
                serviceRows[index] = row;
                serviceRowLabels[index] = row.transform.Find("Label").GetComponent<Text>();
            }

            // 铸造区块：ScrollRect + 单列 GridLayoutGroup（行由视图运行时创建）
            var forgeArea = CreateForgeScroll(middle.transform, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.885f));

            var status = CreateText(middle.transform, "Status", "欢迎光临商店。", 15, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.005f), new Vector2(0.98f, 0.035f), DetailGold, font, FontStyle.Normal);

            // 右列：详情区（商品详情 / 铸造详情两区块 + 共用离开按钮）
            var detail = CreatePanel(root.transform, "Detail", new Vector2(0.70f, 0.04f), new Vector2(0.98f, 0.955f), Panel);
            AddOutline(detail, new Color(Gold.r, Gold.g, Gold.b, 0.72f), new Vector2(1.4f, -1.4f));

            var productDetailRoot = CreatePanel(detail.transform, "Product Detail Root",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
            productDetailRoot.GetComponent<Image>().raycastTarget = false;
            CreateText(productDetailRoot.transform, "Header", "商品详情", 15, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.90f), new Vector2(0.40f, 0.94f), SubtleGold, font, FontStyle.Normal);
            var productName = CreateText(productDetailRoot.transform, "Name", "--", 26, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.895f), PaleGold, font, FontStyle.Bold);
            var productType = CreateText(productDetailRoot.transform, "Type", "类型·--", 16, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.755f), new Vector2(0.95f, 0.815f), DetailGold, font, FontStyle.Normal);
            CreateLine(productDetailRoot.transform, "Divider", new Vector2(0.05f, 0.73f), new Vector2(0.95f, 0.735f));
            var productDetail = CreateText(productDetailRoot.transform, "Effect", "该商品位无货。", 17,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.56f), new Vector2(0.95f, 0.72f), PaleGold, font,
                FontStyle.Normal);
            productDetail.horizontalOverflow = HorizontalWrapMode.Wrap;
            var productPrice = CreateText(productDetailRoot.transform, "Price", "--", 21, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.55f), Gold, font, FontStyle.Bold);
            var buy = CreateButton(productDetailRoot.transform, "Buy Button", "购买商品", 20,
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.45f), Gold, font);
            var buyLabel = buy.transform.Find("Label").GetComponent<Text>();

            var forgeDetailRoot = CreatePanel(detail.transform, "Forge Detail Root",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
            forgeDetailRoot.GetComponent<Image>().raycastTarget = false;
            CreateText(forgeDetailRoot.transform, "Header", "铸造详情", 15, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.90f), new Vector2(0.40f, 0.94f), SubtleGold, font, FontStyle.Normal);
            var forgeName = CreateText(forgeDetailRoot.transform, "Name", "--", 26, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.815f), new Vector2(0.95f, 0.895f), PaleGold, font, FontStyle.Bold);
            var forgeEntry = CreateText(forgeDetailRoot.transform, "Entry", "--", 17, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.735f), new Vector2(0.95f, 0.805f), DetailGold, font, FontStyle.Normal);
            forgeEntry.horizontalOverflow = HorizontalWrapMode.Wrap;
            var forgeMainType = CreateText(forgeDetailRoot.transform, "Main Type", "主属性 · --", 14,
                TextAnchor.MiddleLeft, new Vector2(0.05f, 0.665f), new Vector2(0.95f, 0.725f), SubtleGold, font,
                FontStyle.Normal);
            var forgeMainAttr = CreateText(forgeDetailRoot.transform, "Main Attr", "--", 21, TextAnchor.MiddleLeft,
                new Vector2(0.05f, 0.585f), new Vector2(0.95f, 0.655f), Gold, font, FontStyle.Bold);
            CreateLine(forgeDetailRoot.transform, "Divider", new Vector2(0.05f, 0.565f), new Vector2(0.95f, 0.57f));
            var subAttrRoot = CreatePanel(forgeDetailRoot.transform, "Sub Attr Root",
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.555f), new Color(0f, 0f, 0f, 0f));
            subAttrRoot.GetComponent<Image>().raycastTarget = false;

            var leave = CreateLeaveButton(detail.transform, "Leave Button", "离开商店", font);
            var leaveLabel = leave.transform.Find("Label").GetComponent<Text>();

            root.GetComponent<ShopUiView>().ConfigurePrefab(
                sidebarStats, status,
                productsTab, forgeTab, productsTabLabel, forgeTabLabel,
                productsArea.GetComponent<RectTransform>(), forgeArea,
                productRows, productRowLabels,
                serviceRows, serviceRowLabels,
                productDetailRoot.GetComponent<RectTransform>(), productName, productType, productDetail, productPrice,
                buy, buyLabel,
                forgeDetailRoot.GetComponent<RectTransform>(), forgeName, forgeEntry, forgeMainType, forgeMainAttr,
                subAttrRoot.GetComponent<RectTransform>(),
                leave, leaveLabel);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopUi] prefab 已重建：{AssetDatabase.GetAssetPath(prefab)}");
        }

        /// <summary>铸造列表滚动区：ScrollRect + 单列 GridLayoutGroup（cell 560×54），返回 Content 供视图挂行。</summary>
        private static RectTransform CreateForgeScroll(Transform parent, Vector2 min, Vector2 max)
        {
            var scroll = new GameObject("Forge Scroll", typeof(RectTransform), typeof(ScrollRect));
            scroll.transform.SetParent(parent, false);
            Stretch(scroll.GetComponent<RectTransform>(), min, max);
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
            grid.cellSize = new Vector2(560f, 54f);
            grid.spacing = new Vector2(0f, 6f);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            return contentRect;
        }

        /// <summary>标签按钮：未选态黑底 + 细金描边 + 暗金标签（选中态由视图按标签切换改色）。</summary>
        private static Button CreateTabButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Font font)
        {
            var gameObject = CreatePanel(parent, name, min, max, SecondaryButton);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ConfigureButtonTransitions(button, false);
            AddOutline(gameObject, SubtleGold, new Vector2(1.2f, -1.2f));
            CreateText(gameObject.transform, "Label", label, 18, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), DetailGold, font, FontStyle.Bold);
            return button;
        }

        /// <summary>离开按钮：深蓝底 + 金框 + 浅金标签（去向后缀文案由视图随会话刷新）。</summary>
        private static Button CreateLeaveButton(Transform parent, string name, string label, Font font)
        {
            var gameObject = CreatePanel(parent, name, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.12f), LeaveButtonBase);
            var button = gameObject.AddComponent<Button>();
            button.targetGraphic = gameObject.GetComponent<Image>();
            ConfigureButtonTransitions(button, true);
            AddOutline(gameObject, Gold, new Vector2(1.4f, -1.4f));
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(3f, -3f);
            CreateText(gameObject.transform, "Label", label, 20, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), LeaveButtonLabel, font, FontStyle.Bold);
            return button;
        }

        /// <summary>玫瑰窗占位圆环贴图（美术替换位：纯代码生成圆环 sprite）。</summary>
        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = distance <= radius && distance >= radius * 0.82f ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
