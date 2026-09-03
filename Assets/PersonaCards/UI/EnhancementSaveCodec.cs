using System;
using System.Collections.Generic;
using PersonaCards.Battle.Enhancements;
using PersonaCards.Cards;
using PersonaCards.Core;

namespace PersonaCards.UI
{
    /// <summary>
    /// 三线强化等级与存档列表的互转（P0-11）。纯静态无引擎依赖，供 FlowController 读写与 EditMode 测试共用。
    /// 还原路径全 null-guard：旧档缺字段时 JsonUtility 不跑字段初始化器 → 列表为 null → 全 0 级。
    /// isEmpty 占位行与 null 条目跳过；枚举值直接强转（与 SavedPlayingCard.suit 还原惯例一致），等级经 Set*Level 钳制 0..满级。
    /// </summary>
    public static class EnhancementSaveCodec
    {
        public static EnhancementState Restore(PrototypeSaveData data)
        {
            var state = new EnhancementState();
            if (data.personaLevels != null)
            {
                foreach (var entry in data.personaLevels)
                {
                    if (entry != null && !entry.isEmpty && !string.IsNullOrEmpty(entry.templateId))
                        state.SetPersonaLevel(entry.templateId, entry.level);
                }
            }
            if (data.suitLevels != null)
            {
                foreach (var entry in data.suitLevels)
                {
                    if (entry != null && !entry.isEmpty)
                        state.SetSuitLevel((Suit)entry.suit, entry.level);
                }
            }
            if (data.handLevels != null)
            {
                foreach (var entry in data.handLevels)
                {
                    if (entry != null && !entry.isEmpty)
                        state.SetHandLevel((HandType)entry.handType, entry.level);
                }
            }
            return state;
        }
    }
}
