using System;
using PersonaCards.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    /// <summary>
    /// 选牌弹窗视图（UI 重排第二批 · 选牌弹窗）：6 种单卡类服务共用（筹码/金币/倍率/独立乘区/花色/移除）。
    /// prefab 由一次性编辑器脚本构建（本类不参与场景，仅被 Resources/Prefabs/CardPickPopup 挂载）。
    /// Awake 里给全部子节点赋系统中文字体（PersonaEquipPopupView 惯例，美术替换字体时只换资源）。
    /// 卡牌格运行时按会话 Cards 列表全量重建（销毁旧格），不复用 BattleCardView（其尺寸写死 112×168，弹窗格 62×62）。
    /// Configure(session, onCancel, onConfirm) 写静态文案 + 绑定交互；RefreshFromSession() 重建卡格 + 刷统计/排序钮/信息栏/确认钮。
    /// </summary>
    public sealed class CardPickPopupView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text statsText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button sortByRankButton;
        [SerializeField] private Button sortBySuitButton;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private Image serviceThumb;
        [SerializeField] private Text serviceNameText;
        [SerializeField] private Text serviceDetailText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;

        private static readonly Color32 CellFaceColor = new Color32(103, 84, 51, 255);        // 羊皮纸色卡面
        private static readonly Color32 CellTextColor = new Color32(24, 20, 14, 255);
        private static readonly Color32 SelectedBorderColor = new Color32(178, 139, 73, 255); // 选中金色描边
        private static readonly Color32 SortSelectedBase = new Color32(58, 47, 28, 248);
        private static readonly Color32 SortNormalBase = new Color32(31, 32, 31, 248);
        private static readonly Color32 SortSelectedOutline = new Color32(178, 139, 73, 255);
        private static readonly Color32 SortNormalOutline = new Color32(112, 88, 49, 180);

        private Font _runtimeFont;
        private CardPickSession _session;
        private Action _onCancel;
        private Action _onConfirm;

        private void Awake()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 24);
            if (_runtimeFont == null) return;
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = _runtimeFont;
        }

        /// <summary>prefab 构建脚本专用：一次性注入全部引用（构建期调用一次，存进 prefab）。</summary>
        public void ConfigurePrefab(Text title, Button close, Text stats, Text hint, Button sortByRank,
            Button sortBySuit, RectTransform grid, Image thumb, Text serviceName, Text serviceDetail,
            Button cancel, Button confirm)
        {
            titleText = title;
            closeButton = close;
            statsText = stats;
            hintText = hint;
            sortByRankButton = sortByRank;
            sortBySuitButton = sortBySuit;
            gridRoot = grid;
            serviceThumb = thumb;
            serviceNameText = serviceName;
            serviceDetailText = serviceDetail;
            cancelButton = cancel;
            confirmButton = confirm;
        }

        /// <summary>运行时配置：写静态文案 + 全部交互绑定，最后整体刷动态文案与卡格。</summary>
        public void Configure(CardPickSession session, Action onCancel, Action onConfirm)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _onCancel = onCancel;
            _onConfirm = onConfirm;

            titleText.text = CardPickSession.TitleText;
            hintText.text = "请选择 1 张目标牌";

            sortByRankButton.onClick.RemoveAllListeners();
            sortByRankButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            sortByRankButton.onClick.AddListener(() => ApplySort(CardPickSortMode.ByRank));
            sortBySuitButton.onClick.RemoveAllListeners();
            sortBySuitButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            sortBySuitButton.onClick.AddListener(() => ApplySort(CardPickSortMode.BySuit));

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            closeButton.onClick.AddListener(() => _onCancel?.Invoke());
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            cancelButton.onClick.AddListener(() => _onCancel?.Invoke());
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            confirmButton.onClick.AddListener(() => _onConfirm?.Invoke());

            RefreshFromSession();
        }

        /// <summary>全量刷新：统计/信息栏/排序钮选中态/确认钮可用性 + 卡格重建。</summary>
        private void RefreshFromSession()
        {
            if (_session == null) return;
            statsText.text = _session.StatsText;
            serviceNameText.text = _session.ServiceName;
            serviceDetailText.text = _session.ServiceDetailText;
            confirmButton.interactable = _session.CanConfirm;
            SyncSortButtons();
            RebuildGrid();
        }

        /// <summary>排序切换：已在该模式则不动，否则翻一次（会话只提供 ToggleSort）。</summary>
        private void ApplySort(CardPickSortMode mode)
        {
            if (_session.SortMode != mode) _session.ToggleSort();
            RefreshFromSession();
        }

        private void SyncSortButtons()
        {
            StyleSortButton(sortByRankButton, _session.SortMode == CardPickSortMode.ByRank);
            StyleSortButton(sortBySuitButton, _session.SortMode == CardPickSortMode.BySuit);
        }

        private static void StyleSortButton(Button button, bool selected)
        {
            var image = button.GetComponent<Image>();
            if (image == null) return;
            image.color = selected ? SortSelectedBase : SortNormalBase;
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? SortSelectedOutline : SortNormalOutline;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;
        }

        /// <summary>卡格全量重建：销毁旧格，按会话当前排序逐张新建（选中金边、不可选置灰半透明）。</summary>
        private void RebuildGrid()
        {
            for (var i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);
            if (_session == null) return;
            foreach (var card in _session.Cards)
                CreateCell(card);
        }

        private void CreateCell(PlayingCardInstance card)
        {
            var cell = new GameObject("Card " + card.Id, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            cell.transform.SetParent(gridRoot, false);
            var image = cell.GetComponent<Image>();
            image.color = CellFaceColor;
            var button = cell.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var cellId = card.Id;
            button.onClick.AddListener(MusicManager.Instance.PlayClick);
            button.onClick.AddListener(() =>
            {
                _session.Select(cellId);
                RefreshFromSession();
            });

            CreateCellText(cell.transform, "Rank", RankLabel(card.Rank), 16, TextAnchor.UpperLeft,
                new Vector2(0.06f, 0.62f), new Vector2(0.52f, 0.94f), FontStyle.Bold);
            CreateCellText(cell.transform, "Suit", SuitLabel(card.Suit), 24, TextAnchor.MiddleCenter,
                new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.60f), FontStyle.Bold);

            if (string.Equals(card.Id, _session.SelectedCardId, StringComparison.Ordinal))
            {
                var outline = cell.AddComponent<Outline>();
                outline.effectColor = SelectedBorderColor;
                outline.effectDistance = new Vector2(2.2f, -2.2f);
                outline.useGraphicAlpha = true;
            }

            if (!_session.IsEligible(card.Id))
            {
                cell.GetComponent<CanvasGroup>().alpha = 0.35f;
                button.interactable = false;
            }
        }

        private Text CreateCellText(Transform parent, string name, string value, int size, TextAnchor alignment,
            Vector2 min, Vector2 max, FontStyle style)
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
            text.color = CellTextColor;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static string RankLabel(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J", _ => ((int)rank).ToString()
            };
        }

        private static string SuitLabel(Suit suit)
        {
            return suit switch { Suit.Spades => "♠", Suit.Hearts => "♥", Suit.Diamonds => "♦", _ => "♣" };
        }
    }
}
