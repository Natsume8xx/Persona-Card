using System;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    /// <summary>列表型强化界面选中高亮样式（UI 重排第二批 · 网页版差异已拍板）。</summary>
    public enum EnhanceListHighlightStyle
    {
        /// <summary>牌型强化：选中项蓝色高亮粗边。</summary>
        BlueBorder,
        /// <summary>人格主词条强化：选中项浅金底色。</summary>
        PaleGoldFill
    }

    /// <summary>
    /// 列表型强化界面视图（UI 重排第二批 · 牌型强化 / 人格主词条强化两界面同构共用）：
    /// 一模板两 prefab（HandEnhancePanel / PersonaMainAttrPanel，仅骨架文案与选中样式不同）。
    /// prefab 由一次性编辑器脚本构建（本类不参与场景，仅被 Resources/Prefabs/ 下两个 prefab 挂载）。
    /// Awake 里给全部子节点赋系统中文字体；候选行运行时按会话 Count 全量重建（销毁旧行）。
    /// Configure(session, highlightStyle, onCancel, onConfirm) 写标题/说明 + 绑定交互；RefreshFromSession() 重建列表 + 刷底部文案/确认钮。
    /// </summary>
    public sealed class EnhanceListPanelView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Text footerText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;

        private static readonly Color32 EntryNormalColor = new Color32(26, 28, 26, 245);       // 候选行深色底
        private static readonly Color32 EntryPaleGoldColor = new Color32(64, 55, 36, 255);     // 选中浅金底（主词条）
        private static readonly Color32 BlueBorderColor = new Color32(60, 110, 180, 255);      // 选中蓝色粗边（牌型）
        private static readonly Color32 EntryOutlineColor = new Color32(112, 88, 49, 180);     // 未选细描边
        private static readonly Color32 NameColor = new Color32(232, 214, 173, 255);
        private static readonly Color32 DetailColor = new Color32(168, 142, 96, 255);
        private static readonly Color32 LevelColor = new Color32(178, 139, 73, 255);

        private Font _runtimeFont;
        private IEnhanceListSession _session;
        private EnhanceListHighlightStyle _highlightStyle;
        private Action _onCancel;
        private Action _onConfirm;

        private void Awake()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 22);
            if (_runtimeFont == null) return;
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = _runtimeFont;
        }

        /// <summary>prefab 构建脚本专用：一次性注入全部引用（构建期调用一次，存进 prefab）。</summary>
        public void ConfigurePrefab(Text title, Text description, RectTransform list, Text footer,
            Button cancel, Button confirm)
        {
            titleText = title;
            descriptionText = description;
            listRoot = list;
            footerText = footer;
            cancelButton = cancel;
            confirmButton = confirm;
        }

        /// <summary>运行时配置：写标题/说明（来自会话单源）+ 选中样式 + 全部交互绑定，最后整体刷动态文案。</summary>
        public void Configure(IEnhanceListSession session, EnhanceListHighlightStyle highlightStyle,
            Action onCancel, Action onConfirm)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _highlightStyle = highlightStyle;
            _onCancel = onCancel;
            _onConfirm = onConfirm;

            titleText.text = session.Title;
            descriptionText.text = session.Description;

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            cancelButton.onClick.AddListener(() => _onCancel?.Invoke());
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            confirmButton.onClick.AddListener(() => _onConfirm?.Invoke());

            RefreshFromSession();
        }

        /// <summary>全量刷新：候选行重建 + 底部文案（提示/价格）+ 确认钮可用性。</summary>
        private void RefreshFromSession()
        {
            if (_session == null) return;
            RebuildList();
            footerText.text = !string.IsNullOrEmpty(_session.Hint) && _session.SelectedIndex < 0
                ? _session.Hint
                : _session.PriceText(_session.SelectedIndex);
            confirmButton.interactable = _session.CanConfirm;
        }

        /// <summary>候选行全量重建：销毁旧行，按会话逐行新建（选中行按样式高亮）。</summary>
        private void RebuildList()
        {
            for (var i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);
            if (_session == null) return;
            for (var index = 0; index < _session.Count; index++)
                CreateEntry(index);
        }

        private void CreateEntry(int index)
        {
            var entry = new GameObject("Entry " + index, typeof(RectTransform), typeof(Image), typeof(Button));
            entry.transform.SetParent(listRoot, false);
            var image = entry.GetComponent<Image>();
            image.color = EntryNormalColor;
            var button = entry.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var entryIndex = index;
            button.onClick.AddListener(MusicManager.Instance.PlayClick);
            button.onClick.AddListener(() =>
            {
                _session.Select(entryIndex);
                RefreshFromSession();
            });

            var outline = entry.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            if (index == _session.SelectedIndex)
            {
                if (_highlightStyle == EnhanceListHighlightStyle.BlueBorder)
                {
                    image.color = EntryNormalColor;
                    outline.effectColor = BlueBorderColor;
                    outline.effectDistance = new Vector2(2.6f, -2.6f);
                }
                else
                {
                    image.color = EntryPaleGoldColor;
                    outline.effectColor = new Color32(178, 139, 73, 255);
                    outline.effectDistance = new Vector2(1.2f, -1.2f);
                }
            }
            else
            {
                outline.effectColor = EntryOutlineColor;
            }

            CreateEntryText(entry.transform, "Name", _session.NameText(index), 22, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.52f), new Vector2(0.97f, 0.97f), NameColor, FontStyle.Bold);
            CreateEntryText(entry.transform, "Detail", _session.DetailText(index), 15, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.10f), new Vector2(0.72f, 0.50f), DetailColor, FontStyle.Normal);
            CreateEntryText(entry.transform, "Level", _session.LevelText(index), 16, TextAnchor.MiddleRight,
                new Vector2(0.72f, 0.10f), new Vector2(0.97f, 0.50f), LevelColor, FontStyle.Bold);
        }

        private Text CreateEntryText(Transform parent, string name, string value, int size, TextAnchor alignment,
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
