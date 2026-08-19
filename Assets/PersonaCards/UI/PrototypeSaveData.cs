using System;
using System.Collections.Generic;

namespace PersonaCards.UI
{
    /// <summary>存档根对象：schemaVersion 3（P0-8 升级 v4 并迁移字段名）。</summary>
    [Serializable]
    public sealed class PrototypeSaveData
    {
        public int schemaVersion = 3;
        public bool hasActiveRun;
        public int stage;
        /// <summary>当前战斗节点索引（JSON 字段名沿用 battleNumber 以兼容旧档；P0-8 升 schema v4 时重命名为 nodeIndex）。</summary>
        public int battleNumber;
        /// <summary>装备阶段是否由 Boss 揭示"返回检查装备"进入：true 时确认装备回到揭示界面（保留节点）。旧档缺该字段时为 false，行为与新局一致。</summary>
        public bool personaSetupReturnsToBossReveal;
        /// <summary>本局种子：场次种子由它派生，保证同局存档恢复后手牌顺序一致。</summary>
        public uint runSeed;
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
        /// <summary>本场出牌上限（快照自记；旧档缺字段为 0，恢复时回落默认值）。</summary>
        public int playsLimit;
        /// <summary>本场弃牌上限（快照自记；旧档缺字段为 0，恢复时回落默认值）。</summary>
        public int discardsLimit;
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
