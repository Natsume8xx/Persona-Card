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

        public RectTransform RectTransform => (RectTransform)transform;
        public CanvasGroup CanvasGroup => canvasGroup;

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
            faceArtwork.color = selected ? new Color32(255, 244, 205, 255) : Color.white;
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
