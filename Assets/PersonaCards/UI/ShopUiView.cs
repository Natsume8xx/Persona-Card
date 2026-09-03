using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店主界面视图（UI 重排第二批 · 商品页 + 人格铸造标签页）：替换旧商店屏「Shop Card」的运行时展示。
    /// prefab 由一次性编辑器脚本构建（本类不参与场景，仅被 Resources/Prefabs/ShopUi.prefab 挂载）。
    /// Awake 里给全部子节点赋系统中文字体；商品行/服务行/详情为 prefab 固定骨架（文案随会话刷新），
    /// 铸造行与副属性行运行时按会话全量重建（销毁旧行，签名无变化时跳过重建）。
    /// Configure(session, onBuy, onLeave, onServiceRow, onForgeChanged) 绑定全部交互；Refresh() 全量重绘。
    /// 购买委托 FlowController.PurchaseShopSlot（旧流程内部 Render → 主界面自动刷新）；
    /// 副属性解锁在视图内执行（会话 TryUnlockSubAttr 真实扣款），成功后回调 onForgeChanged（控制器存档 + 状态文案）。
    /// </summary>
    public sealed class ShopUiView : MonoBehaviour
    {
        [SerializeField] private Text sidebarStatsText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button productsTabButton;
        [SerializeField] private Button forgeTabButton;
        [SerializeField] private Text productsTabLabel;
        [SerializeField] private Text forgeTabLabel;
        [SerializeField] private RectTransform productsArea;
        [SerializeField] private RectTransform forgeArea;
        [SerializeField] private Button[] productRowButtons;
        [SerializeField] private Text[] productRowLabels;
        [SerializeField] private Button[] serviceRowButtons;
        [SerializeField] private Text[] serviceRowLabels;
        [SerializeField] private RectTransform productDetailRoot;
        [SerializeField] private Text productNameText;
        [SerializeField] private Text productTypeText;
        [SerializeField] private Text productDetailText;
        [SerializeField] private Text productPriceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buyButtonLabel;
        [SerializeField] private RectTransform forgeDetailRoot;
        [SerializeField] private Text forgeNameText;
        [SerializeField] private Text forgeEntryText;
        [SerializeField] private Text forgeMainTypeText;
        [SerializeField] private Text forgeMainAttrText;
        [SerializeField] private RectTransform subAttrRoot;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Text leaveButtonLabel;

        private static readonly Color32 TabSelectedBase = new Color32(58, 47, 28, 248);       // 选中标签深棕底
        private static readonly Color32 TabUnselectedBase = new Color32(31, 32, 31, 248);     // 未选标签黑底
        private static readonly Color32 TabSelectedLabel = new Color32(232, 214, 173, 255);   // 选中标签 PaleGold
        private static readonly Color32 TabUnselectedLabel = new Color32(168, 142, 96, 255);  // 未选标签 DetailGold
        private static readonly Color32 RowSelectedOutline = new Color32(178, 139, 73, 255);  // 选中行金描边
        private static readonly Color32 RowUnselectedOutline = new Color32(112, 88, 49, 180); // 未选行细描边
        private static readonly Color32 ForgeRowBase = new Color32(26, 28, 26, 245);          // 铸造行深色底
        private static readonly Color32 NameColor = new Color32(232, 214, 173, 255);
        private static readonly Color32 ProgressColor = new Color32(178, 139, 73, 255);
        private static readonly Color32 NodeColor = new Color32(168, 142, 96, 255);

        private Font _runtimeFont;
        private ShopUiSession _session;
        private Action _onBuy;
        private Action _onLeave;
        private Action<int> _onServiceRow;
        private Action _onForgeChanged;
        /// <summary>已重建的铸造页签名（选中 + 各槽解锁/可解锁态）：无变化时跳过行重建。</summary>
        private string _builtForgeSignature;

        private void Awake()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 22);
            if (_runtimeFont == null) return;
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = _runtimeFont;
        }

        /// <summary>prefab 构建脚本专用：一次性注入全部引用（构建期调用一次，存进 prefab）。</summary>
        public void ConfigurePrefab(
            Text sidebarStats, Text status,
            Button productsTab, Button forgeTab, Text productsTabLabel, Text forgeTabLabel,
            RectTransform productsArea, RectTransform forgeArea,
            Button[] productRows, Text[] productRowLabels,
            Button[] serviceRows, Text[] serviceRowLabels,
            RectTransform productDetailRoot, Text productName, Text productType, Text productDetail, Text productPrice,
            Button buy, Text buyLabel,
            RectTransform forgeDetailRoot, Text forgeName, Text forgeEntry, Text forgeMainType, Text forgeMainAttr,
            RectTransform subAttrRoot,
            Button leave, Text leaveLabel)
        {
            sidebarStatsText = sidebarStats;
            statusText = status;
            productsTabButton = productsTab;
            forgeTabButton = forgeTab;
            this.productsTabLabel = productsTabLabel;
            this.forgeTabLabel = forgeTabLabel;
            this.productsArea = productsArea;
            this.forgeArea = forgeArea;
            productRowButtons = productRows;
            this.productRowLabels = productRowLabels;
            serviceRowButtons = serviceRows;
            this.serviceRowLabels = serviceRowLabels;
            this.productDetailRoot = productDetailRoot;
            productNameText = productName;
            productTypeText = productType;
            productDetailText = productDetail;
            productPriceText = productPrice;
            buyButton = buy;
            buyButtonLabel = buyLabel;
            this.forgeDetailRoot = forgeDetailRoot;
            forgeNameText = forgeName;
            forgeEntryText = forgeEntry;
            forgeMainTypeText = forgeMainType;
            forgeMainAttrText = forgeMainAttr;
            this.subAttrRoot = subAttrRoot;
            leaveButton = leave;
            leaveButtonLabel = leaveLabel;
        }

        /// <summary>运行时配置：写入会话（单源）与全部交互回调，最后整体刷动态文案。</summary>
        public void Configure(ShopUiSession session, Action onBuy, Action onLeave, Action<int> onServiceRow,
            Action onForgeChanged)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _onBuy = onBuy;
            _onLeave = onLeave;
            _onServiceRow = onServiceRow;
            _onForgeChanged = onForgeChanged;
            _builtForgeSignature = null;

            productsTabButton.onClick.RemoveAllListeners();
            productsTabButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            productsTabButton.onClick.AddListener(() =>
            {
                _session.ShowProducts();
                Refresh();
            });
            forgeTabButton.onClick.RemoveAllListeners();
            forgeTabButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            forgeTabButton.onClick.AddListener(() =>
            {
                _session.ShowForge();
                Refresh();
            });

            for (var index = 0; index < productRowButtons.Length; index++)
            {
                var rowIndex = index;
                productRowButtons[index].onClick.RemoveAllListeners();
                productRowButtons[index].onClick.AddListener(MusicManager.Instance.PlayClick);
                productRowButtons[index].onClick.AddListener(() =>
                {
                    _session.SelectProduct(rowIndex);
                    Refresh();
                });
            }
            for (var index = 0; index < serviceRowButtons.Length; index++)
            {
                var rowIndex = index;
                serviceRowButtons[index].onClick.RemoveAllListeners();
                serviceRowButtons[index].onClick.AddListener(MusicManager.Instance.PlayClick);
                serviceRowButtons[index].onClick.AddListener(() => _onServiceRow?.Invoke(rowIndex));
            }
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            buyButton.onClick.AddListener(() => _onBuy?.Invoke());
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            leaveButton.onClick.AddListener(() => _onLeave?.Invoke());

            Refresh();
        }

        /// <summary>全量刷新：侧边栏/去向文案/标签态/区块显隐，按当前标签刷商品页或铸造页。</summary>
        public void Refresh()
        {
            if (_session == null || !_session.IsConfigured) return; // 会话未注入时跳过（视图 Configure 早于会话 Configure 的防御）
            sidebarStatsText.text = _session.SidebarStatsText;
            leaveButtonLabel.text = _session.LeaveLabel;
            UpdateTabVisuals();
            productsArea.gameObject.SetActive(!_session.IsForgeTab);
            forgeArea.gameObject.SetActive(_session.IsForgeTab);
            productDetailRoot.gameObject.SetActive(!_session.IsForgeTab);
            forgeDetailRoot.gameObject.SetActive(_session.IsForgeTab);
            if (_session.IsForgeTab) RefreshForge();
            else RefreshProducts();
        }

        /// <summary>状态行文案（FlowController 单一来源写入；Refresh 不覆盖）。</summary>
        public void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        // ---------- 标签与商品页 ----------

        private void UpdateTabVisuals()
        {
            var productsSelected = !_session.IsForgeTab;
            var productsImage = productsTabButton.targetGraphic as Image;
            if (productsImage != null) productsImage.color = productsSelected ? TabSelectedBase : TabUnselectedBase;
            var forgeImage = forgeTabButton.targetGraphic as Image;
            if (forgeImage != null) forgeImage.color = productsSelected ? TabUnselectedBase : TabSelectedBase;
            productsTabLabel.color = productsSelected ? TabSelectedLabel : TabUnselectedLabel;
            forgeTabLabel.color = productsSelected ? TabUnselectedLabel : TabSelectedLabel;
        }

        private void RefreshProducts()
        {
            for (var rowIndex = 0; rowIndex < productRowButtons.Length; rowIndex++)
            {
                var inRange = rowIndex < _session.ProductRowVisibleCount;
                productRowButtons[rowIndex].gameObject.SetActive(inRange);
                if (!inRange) continue;
                productRowLabels[rowIndex].text = _session.ProductRowText(rowIndex);
                productRowButtons[rowIndex].interactable = _session.HasProduct(rowIndex);
                var outline = productRowButtons[rowIndex].GetComponent<Outline>();
                if (outline == null) continue;
                var selected = rowIndex == _session.SelectedProductIndex && _session.HasProduct(rowIndex);
                outline.effectColor = selected ? RowSelectedOutline : RowUnselectedOutline;
                outline.effectDistance = selected ? new Vector2(1.8f, -1.8f) : new Vector2(1f, -1f);
            }
            for (var rowIndex = 0; rowIndex < serviceRowButtons.Length; rowIndex++)
            {
                var inRange = rowIndex < _session.ServiceRowCount;
                serviceRowButtons[rowIndex].gameObject.SetActive(inRange);
                if (!inRange) continue;
                serviceRowLabels[rowIndex].text = _session.ServiceRowText(rowIndex);
                serviceRowButtons[rowIndex].interactable = _session.CanOpenService(rowIndex);
            }
            productNameText.text = _session.ProductNameText;
            productTypeText.text = _session.ProductTypeText;
            productDetailText.text = _session.ProductDetailText;
            productPriceText.text = _session.ProductPriceText;
            buyButtonLabel.text = _session.BuyButtonText;
            buyButton.interactable = _session.CanBuySelected;
        }

        // ---------- 铸造页 ----------

        private void RefreshForge()
        {
            var signature = BuildForgeSignature();
            if (!string.Equals(signature, _builtForgeSignature, StringComparison.Ordinal))
            {
                _builtForgeSignature = signature;
                RebuildForgeRows();
                RebuildSubAttrRows();
            }
            forgeNameText.text = _session.ForgeRowName(_session.SelectedForgeIndex);
            forgeEntryText.text = _session.ForgeEntryText(_session.SelectedForgeIndex);
            forgeMainTypeText.text = $"主属性 · {_session.ForgeMainAttrType(_session.SelectedForgeIndex)}";
            forgeMainAttrText.text = _session.ForgeMainAttrText(_session.SelectedForgeIndex);
        }

        /// <summary>铸造页重建签名：选中序号 + 各副属性槽（已解锁/可解锁）态；解锁后签名变化触发行重建。</summary>
        private string BuildForgeSignature()
        {
            var builder = new StringBuilder().Append(_session.SelectedForgeIndex);
            var count = _session.SubAttrSlotCount(_session.SelectedForgeIndex);
            builder.Append('|').Append(count);
            for (var slot = 0; slot < count; slot++)
                builder.Append('|').Append(_session.IsSubAttrUnlocked(_session.SelectedForgeIndex, slot))
                    .Append(',').Append(_session.CanUnlockSubAttr(_session.SelectedForgeIndex, slot));
            return builder.ToString();
        }

        /// <summary>铸造行全量重建：销毁旧行，按目录 8 人格逐行新建（选中行金色粗描边）。</summary>
        private void RebuildForgeRows()
        {
            for (var i = forgeArea.childCount - 1; i >= 0; i--)
                Destroy(forgeArea.GetChild(i).gameObject);
            if (_session == null) return;
            for (var index = 0; index < _session.ForgeCount; index++)
                CreateForgeRow(index);
        }

        private void CreateForgeRow(int index)
        {
            var row = new GameObject("Forge Row " + index, typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(forgeArea, false);
            var image = row.GetComponent<Image>();
            image.color = ForgeRowBase;
            var button = row.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var rowIndex = index;
            button.onClick.AddListener(MusicManager.Instance.PlayClick);
            button.onClick.AddListener(() =>
            {
                _session.SelectForge(rowIndex);
                Refresh();
            });
            var selected = index == _session.SelectedForgeIndex;
            var outline = row.AddComponent<Outline>();
            outline.useGraphicAlpha = true;
            outline.effectColor = selected ? RowSelectedOutline : RowUnselectedOutline;
            outline.effectDistance = selected ? new Vector2(1.8f, -1.8f) : new Vector2(1f, -1f);
            CreateRowText(row.transform, "Name", _session.ForgeRowName(index), 20, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0f), new Vector2(0.68f, 1f), NameColor, FontStyle.Bold);
            CreateRowText(row.transform, "Progress", _session.ForgeRowProgress(index), 17, TextAnchor.MiddleRight,
                new Vector2(0.70f, 0f), new Vector2(0.97f, 1f), ProgressColor, FontStyle.Bold);
        }

        /// <summary>副属性槽位行全量重建：每行 = 状态文案 + 解锁节点文案 + 解锁按钮（金色可点/禁用灰态）。</summary>
        private void RebuildSubAttrRows()
        {
            for (var i = subAttrRoot.childCount - 1; i >= 0; i--)
                Destroy(subAttrRoot.GetChild(i).gameObject);
            if (_session == null) return;
            var count = _session.SubAttrSlotCount(_session.SelectedForgeIndex);
            for (var slot = 0; slot < count; slot++)
                CreateSubAttrRow(slot, count);
        }

        private void CreateSubAttrRow(int slot, int count)
        {
            var row = new GameObject("Sub Attr Row " + slot, typeof(RectTransform));
            row.transform.SetParent(subAttrRoot, false);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f - (slot + 1) / (float)count);
            rect.anchorMax = new Vector2(1f, 1f - slot / (float)count);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var forgeIndex = _session.SelectedForgeIndex;
            CreateRowText(row.transform, "Status", _session.SubAttrStatusText(forgeIndex, slot), 16, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.50f), new Vector2(0.60f, 0.95f), NameColor, FontStyle.Bold);
            CreateRowText(row.transform, "Node", _session.SubAttrNodeText(forgeIndex, slot), 13, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.05f), new Vector2(0.60f, 0.50f), NodeColor, FontStyle.Normal);

            var buttonObject = new GameObject("Unlock Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(row.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.62f, 0.18f);
            buttonRect.anchorMax = new Vector2(1f, 0.82f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            var image = buttonObject.GetComponent<Image>();
            image.color = TabSelectedBase; // 金按钮底（PrimaryButton）
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color32(255, 235, 180, 255),
                pressedColor = new Color32(154, 124, 72, 255),
                selectedColor = new Color32(242, 212, 145, 255),
                disabledColor = new Color32(92, 92, 88, 130),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = RowSelectedOutline;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;
            button.interactable = _session.CanUnlockSubAttr(forgeIndex, slot);
            var slotIndex = slot;
            button.onClick.AddListener(MusicManager.Instance.PlayClick);
            button.onClick.AddListener(() =>
            {
                if (_session.TryUnlockSubAttr(_session.SelectedForgeIndex, slotIndex))
                {
                    _onForgeChanged?.Invoke();
                    Refresh();
                }
            });
            CreateRowText(buttonObject.transform, "Label", _session.UnlockButtonText(forgeIndex, slot), 14,
                TextAnchor.MiddleCenter, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), TabSelectedLabel,
                FontStyle.Bold);
        }

        private Text CreateRowText(Transform parent, string name, string value, int size, TextAnchor alignment,
            Vector2 min, Vector2 max, Color32 color, FontStyle style)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = gameObject.GetComponent<Text>();
            if (_runtimeFont != null) text.font = _runtimeFont;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            return text;
        }
    }
}
