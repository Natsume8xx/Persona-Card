using System;
using System.Collections.Generic;

namespace PersonaCards.UI
{
    /// <summary>
    /// ForgeUnlockState 存档出入编解码（UI 重排第二批）：schemaVersion 保持 3。
    /// 入档：只写解锁数 ≥1 的人格行；空状态写空列表。
    /// 出档：null-guard（旧档缺字段实测为空列表而非 null，仍双容忍）→ 跳过空占位/空 id 行 → 计数下限 1 钳制。
    /// </summary>
    public static class ForgeUnlockSaveCodec
    {
        /// <summary>入档：state null 抛 ArgumentNullException；返回新列表（调用方可安全持有）。</summary>
        public static List<SavedForgeUnlock> Save(ForgeUnlockState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var rows = new List<SavedForgeUnlock>();
            foreach (var pair in state.UnlockedEntries)
            {
                if (pair.Value < 1) continue;
                rows.Add(new SavedForgeUnlock
                {
                    isEmpty = false,
                    personaId = pair.Key,
                    unlockedCount = pair.Value
                });
            }
            return rows;
        }

        /// <summary>出档：saved 为 null（旧档）→ 全未解锁空状态；行级防御照 Save 注释。</summary>
        public static ForgeUnlockState Restore(List<SavedForgeUnlock> saved)
        {
            var state = new ForgeUnlockState();
            if (saved == null) return state;
            foreach (var row in saved)
            {
                if (row == null || row.isEmpty) continue;
                if (string.IsNullOrEmpty(row.personaId)) continue;
                state.SeedUnlocked(row.personaId, row.unlockedCount);
            }
            return state;
        }
    }
}
