using System;
using PersonaCards.Battle.Personas;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    /// <summary>
    /// 获得新人格牌弹窗视图（UI 重排第一批 · 获得新人格牌弹窗）：仿 BattleCardView 的 Configure 模式。
    /// prefab 由一次性编辑器脚本构建（本类不参与场景，仅被 Resources/Prefabs/PersonaEquipPopup 挂载）。
    /// Awake 里给全部子节点赋系统中文字体（BattlePrototypeController 惯例，美术替换字体时只换节点贴图/字体资源）。
    /// Configure(session, onDecline, onConfirm) 写静态文案 + 立绘换图（无立绘反置「◇」占位）+ 槽位按钮与双按钮挂回调。
    /// RefreshFromSession() 全量刷槽名/状态/选中高亮/提示条/确认按钮文案。
    /// </summary>
    public sealed class PersonaEquipPopupView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text statsText;
        [SerializeField] private RawImage portraitImage;
        [SerializeField] private Text portraitFallback;
        [SerializeField] private Text cardNameText;
        [SerializeField] private Text typeTagText;
        [SerializeField] private Text conditionText;
        [SerializeField] private Text effectText;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Text[] slotNameTexts;
        [SerializeField] private Text[] slotStatusTexts;
        [SerializeField] private Image[] slotFrames;
        [SerializeField] private Text barText;
        [SerializeField] private Button declineButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmLabel;

        private static readonly Color32 SelectedSlotColor = new Color32(91, 70, 34, 255); // 铸造候选选中色惯例
        private static readonly Color32 NormalSlotColor = new Color32(26, 28, 26, 245);   // 槽位深色底

        private Font _runtimeFont;
        private PersonaEquipPromptSession _session;
        private Action _onDecline;
        private Action _onConfirm;

        private void Awake()
        {
            _runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 28);
            if (_runtimeFont == null) return;
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = _runtimeFont;
        }

        /// <summary>prefab 构建脚本专用：一次性注入全部引用（构建期调用一次，存进 prefab）。</summary>
        public void ConfigurePrefab(Text title, Text stats, RawImage portrait, Text fallback, Text cardName,
            Text typeTag, Text condition, Text effect, Button[] slots, Text[] slotNames, Text[] slotStatuses,
            Image[] slotFrames, Text bar, Button decline, Button confirm, Text confirmLabel)
        {
            titleText = title;
            statsText = stats;
            portraitImage = portrait;
            portraitFallback = fallback;
            cardNameText = cardName;
            typeTagText = typeTag;
            conditionText = condition;
            effectText = effect;
            slotButtons = slots;
            slotNameTexts = slotNames;
            slotStatusTexts = slotStatuses;
            this.slotFrames = slotFrames;
            barText = bar;
            declineButton = decline;
            confirmButton = confirm;
            this.confirmLabel = confirmLabel;
        }

        /// <summary>运行时配置：写静态文案 + 立绘换图 + 全部交互绑定，最后整体刷动态文案。</summary>
        public void Configure(PersonaEquipPromptSession session, Action onDecline, Action onConfirm)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _onDecline = onDecline;
            _onConfirm = onConfirm;

            titleText.text = "获得新的人格牌";
            cardNameText.text = session.Candidate.DisplayName;
            typeTagText.text = session.TypeTagText;
            conditionText.text = session.ConditionText;
            effectText.text = session.EffectText;
            SyncPortrait();

            for (var i = 0; i < slotButtons.Length; i++)
            {
                var index = i;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(MusicManager.Instance.PlayClick);
                slotButtons[i].onClick.AddListener(() =>
                {
                    _session.SelectSlot(index);
                    RefreshFromSession();
                });
            }

            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            declineButton.onClick.AddListener(() => _onDecline?.Invoke());
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(MusicManager.Instance.PlayClick);
            confirmButton.onClick.AddListener(() => _onConfirm?.Invoke());

            RefreshFromSession();
        }

        /// <summary>全量刷新动态文案：统计/槽名/槽状态/选中高亮/提示条/确认按钮。</summary>
        private void RefreshFromSession()
        {
            if (_session == null) return;
            statsText.text = _session.StatsText;
            barText.text = _session.BarText;
            confirmLabel.text = _session.ConfirmButtonText;
            var count = Mathf.Min(slotButtons.Length, slotNameTexts.Length, slotStatusTexts.Length);
            for (var i = 0; i < count; i++)
            {
                slotNameTexts[i].text = _session.SlotNameText(i);
                slotStatusTexts[i].text = _session.SlotStatusText(i);
                if (i < slotFrames.Length && slotFrames[i] != null)
                    slotFrames[i].color = i == _session.SelectedSlotIndex ? SelectedSlotColor : NormalSlotColor;
            }
        }

        /// <summary>立绘切换：PersonaArtCatalog 有映射则显示贴图，无则反置「◇」字符占位。</summary>
        private void SyncPortrait()
        {
            var sprite = PersonaArtCatalog.PortraitFor(_session.Candidate.TemplateId);
            if (sprite == null)
            {
                portraitImage.color = new Color(0f, 0f, 0f, 0f);
                portraitFallback.gameObject.SetActive(true);
                return;
            }
            portraitImage.texture = sprite.texture;
            portraitImage.color = Color.white;
            portraitFallback.gameObject.SetActive(false);
        }
    }
}
