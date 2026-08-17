using System;
using System.Collections.Generic;

namespace PersonaCards.UI
{
    [Serializable]
    public sealed class PrototypeSaveData
    {
        public int schemaVersion = 3;
        public bool hasActiveRun;
        public int stage;
        public int battleNumber;
        public int coins;
        public int selectedJourneyCardIndex;
        public bool rewardClaimed;
        public List<SavedPlayingCard> deck = new List<SavedPlayingCard>();
        public List<SavedPersona> collection = new List<SavedPersona>();
        public List<SavedPersona> equipped = new List<SavedPersona>();
        public SavedBehavior behavior = new SavedBehavior();
        public SavedBattle battle = new SavedBattle();
    }

    [Serializable]
    public sealed class SavedPlayingCard
    {
        public string id;
        public int suit;
        public int rank;
        public int enhancement;
    }

    [Serializable]
    public sealed class SavedPersona
    {
        public bool isEmpty;
        public string templateId;
        public string displayName;
        public int conditionKind;
        public int minimumHandType;
        public int effectKind;
        public string effectValue;
    }

    [Serializable]
    public sealed class SavedBehavior
    {
        public int plays;
        public int discards;
        public int cardsPlayed;
        public int cardsDiscarded;
        public long score;
        public List<int> handTypes = new List<int>();
        public List<int> handCounts = new List<int>();
    }

    [Serializable]
    public sealed class SavedBattle
    {
        public bool hasSnapshot;
        public long targetScore;
        public long totalScore;
        public int playsRemaining;
        public int discardsRemaining;
        public int status;
        public string bossEncounterId;
        public int bossHandsPlayed;
        public bool bossHasPreviousHand;
        public int bossPreviousHandType;
        public List<SavedPlayingCard> drawPile = new List<SavedPlayingCard>();
        public List<SavedPlayingCard> hand = new List<SavedPlayingCard>();
        public List<SavedPlayingCard> played = new List<SavedPlayingCard>();
        public List<SavedPlayingCard> discarded = new List<SavedPlayingCard>();
        public List<string> selectedCardIds = new List<string>();
    }
}
