using System;
using System.Collections.Generic;
using PersonaCards.Cards;
using PersonaCards.Core;

namespace PersonaCards.Battle.Enhancements
{
    /// <summary>
    /// 三线强化等级状态（P0-11）：人格牌（按 TemplateId）/ 花色（Suit）/ 牌型（HandType）各一条升级线，
    /// 全部 0 级起步、满级 4。纯数据无引擎依赖：战斗（BattleStateMachine 效果计算）与 UI（商店购买/存档）
    /// 共用同一实例。升级与否与价格无关——价格由 EnhancementConfig 按表查，本类只记账等级。
    /// </summary>
    public sealed class EnhancementState
    {
        public const int PersonaMaxLevel = 4;
        public const int SuitMaxLevel = 4;
        public const int HandMaxLevel = 4;

        private readonly Dictionary<string, int> _personaLevels = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<Suit, int> _suitLevels = new Dictionary<Suit, int>();
        private readonly Dictionary<HandType, int> _handLevels = new Dictionary<HandType, int>();

        public EnhancementState()
        {
        }

        private EnhancementState(
            Dictionary<string, int> personaLevels,
            Dictionary<Suit, int> suitLevels,
            Dictionary<HandType, int> handLevels)
        {
            _personaLevels = personaLevels;
            _suitLevels = suitLevels;
            _handLevels = handLevels;
        }

        /// <summary>人格牌等级只读视图（键 = TemplateId；存档序列化遍历用，P0-11）。</summary>
        public IReadOnlyDictionary<string, int> PersonaLevels => _personaLevels;

        /// <summary>花色等级只读视图（存档序列化遍历用，P0-11）。</summary>
        public IReadOnlyDictionary<Suit, int> SuitLevels => _suitLevels;

        /// <summary>牌型等级只读视图（存档序列化遍历用，P0-11）。</summary>
        public IReadOnlyDictionary<HandType, int> HandLevels => _handLevels;

        /// <summary>人格牌等级（键 = PersonaCardDefinition.TemplateId）；未升级过 = 0。</summary>
        public int PersonaLevelOf(string templateId)
        {
            if (templateId == null) return 0;
            return _personaLevels.TryGetValue(templateId, out var level) ? level : 0;
        }

        public int SuitLevelOf(Suit suit)
        {
            return _suitLevels.TryGetValue(suit, out var level) ? level : 0;
        }

        public int HandLevelOf(HandType handType)
        {
            return _handLevels.TryGetValue(handType, out var level) ? level : 0;
        }

        /// <summary>升 1 级；满级或键非法返回 false（不产生副作用）。</summary>
        public bool TryUpgradePersona(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return false;
            var current = PersonaLevelOf(templateId);
            if (current >= PersonaMaxLevel) return false;
            _personaLevels[templateId] = current + 1;
            return true;
        }

        public bool TryUpgradeSuit(Suit suit)
        {
            var current = SuitLevelOf(suit);
            if (current >= SuitMaxLevel) return false;
            _suitLevels[suit] = current + 1;
            return true;
        }

        public bool TryUpgradeHand(HandType handType)
        {
            var current = HandLevelOf(handType);
            if (current >= HandMaxLevel) return false;
            _handLevels[handType] = current + 1;
            return true;
        }

        /// <summary>直接设等级（存档还原/测试用），钳制到 0..满级。</summary>
        public void SetPersonaLevel(string templateId, int level)
        {
            if (string.IsNullOrEmpty(templateId)) return;
            _personaLevels[templateId] = Clamp(level, PersonaMaxLevel);
        }

        public void SetSuitLevel(Suit suit, int level)
        {
            _suitLevels[suit] = Clamp(level, SuitMaxLevel);
        }

        public void SetHandLevel(HandType handType, int level)
        {
            _handLevels[handType] = Clamp(level, HandMaxLevel);
        }

        public EnhancementState Clone()
        {
            return new EnhancementState(
                new Dictionary<string, int>(_personaLevels, StringComparer.Ordinal),
                new Dictionary<Suit, int>(_suitLevels),
                new Dictionary<HandType, int>(_handLevels));
        }

        private static int Clamp(int level, int maxLevel)
        {
            return Math.Max(0, Math.Min(maxLevel, level));
        }
    }
}
