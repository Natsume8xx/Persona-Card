using System;
using PersonaCards.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace PersonaCards.UI
{
    public sealed class BattleCardView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private RawImage faceArtwork;
        [SerializeField] private Button button;
        [SerializeField] private Text label;
        [SerializeField] private Text centerSuit;
        [SerializeField] private Text mirroredLabel;
        [SerializeField] private Text enhancementLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        /// <summary>prefab 序列化引用的原始卡面底纹（羊皮纸），无美术贴图时的回退纹理。</summary>
        private Texture _fallbackFace;

        public RectTransform RectTransform => (RectTransform)transform;
        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            // 记录 prefab 原始底纹：美术贴图缺失的卡面回退到它
            if (faceArtwork != null) _fallbackFace = faceArtwork.texture;
        }

        public void ConfigurePrefab(Image image, RawImage artwork, Button clickButton, Text cardLabel,
            Text largeSuit, Text mirroredCardLabel, Text enhancement, CanvasGroup group)
        {
            background = image;
            faceArtwork = artwork;
            button = clickButton;
            label = cardLabel;
            centerSuit = largeSuit;
            mirroredLabel = mirroredCardLabel;
            enhancementLabel = enhancement;
            canvasGroup = group;
        }

        public void Configure(
            PlayingCardInstance card,
            bool selected,
            int index,
            int cardCount,
            Font font,
            Action<string> onClicked)
        {
            name = $"Card {card.Id}";
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.46f);
            rect.sizeDelta = new Vector2(112f, 168f);
            rect.anchoredPosition = new Vector2((index - (cardCount - 1) * 0.5f) * 121f, selected ? 28f : 0f);
            background.color = selected ? new Color32(211, 166, 76, 255) : new Color32(103, 84, 51, 255);
            // 美术牌面接入：52 张整卡贴图存在 → 替换底纹并隐藏手绘点数文本（美术自带，避免重叠）；
            // 贴图缺失 → 回退 prefab 原始羊皮纸 + 文本，选中染色保持旧行为；增强词条两种情况下都照常显示
            var face = CardFaceCatalog.FaceFor(card.Suit, card.Rank);
            var hasFace = face != null;
            faceArtwork.texture = hasFace ? face : _fallbackFace;
            faceArtwork.color = hasFace ? Color.white : selected ? new Color32(255, 244, 205, 255) : Color.white;
            label.gameObject.SetActive(!hasFace);
            centerSuit.gameObject.SetActive(!hasFace);
            mirroredLabel.gameObject.SetActive(!hasFace);
            label.font = font;
            centerSuit.font = font;
            mirroredLabel.font = font;
            enhancementLabel.font = font;
            var rankAndSuit = $"{RankLabel(card.Rank)}\n{SuitLabel(card.Suit)}";
            label.text = rankAndSuit;
            mirroredLabel.text = rankAndSuit;
            centerSuit.text = SuitLabel(card.Suit);
            enhancementLabel.text = EnhancementLabel(card.Enhancement);
            var inkColor = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds
                ? new Color32(151, 36, 42, 255)
                : new Color32(24, 24, 24, 255);
            label.color = inkColor;
            mirroredLabel.color = inkColor;
            centerSuit.color = new Color(inkColor.r, inkColor.g, inkColor.b, 0.72f);
            enhancementLabel.color = new Color32(116, 77, 24, 255);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(MusicManager.Instance.PlayClick); // 音效：点牌（RemoveAllListeners 会清掉，故在此处补挂）
            button.onClick.AddListener(() => onClicked(card.Id));
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
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

        private static string EnhancementLabel(CardEnhancement enhancement)
        {
            return enhancement switch
            {
                CardEnhancement.ChipBoost => "+20",
                CardEnhancement.MultBoost => "×3",
                _ => string.Empty
            };
        }
    }
}
