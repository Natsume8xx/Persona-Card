using System;
using System.Collections.Generic;

namespace PersonaCards.UI
{
    /// <summary>
    /// 人格铸造副属性解锁运行时状态（UI 重排第二批）：纯 C# 可单测，存档出入由 ForgeUnlockSaveCodec 负责。
    /// 语义（已拍板口径）：每人格 maxSubAttrs=2 个副属性槽，5 金→8 金顺序解锁（价格来自 ShopForge.asset FORGE_001/002）。
    /// 顺序钳制（第 k 个副属性需先解锁第 k-1 个）由调用方 ShopUiSession.TryUnlockSubAttr 保证；
    /// 本类只负责计数上限与真实扣款：计数 ≥ 上限或金币不足均不生效（无副作用）。
    /// </summary>
    public sealed class ForgeUnlockState
    {
        private readonly Dictionary<string, int> _unlocked = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>某人格已解锁副属性数（未知人格 = 0）。personaId 为空抛 ArgumentException。</summary>
        public int UnlockedCountOf(string personaId)
        {
            if (string.IsNullOrEmpty(personaId))
                throw new ArgumentException("personaId 不能为空。", nameof(personaId));
            return _unlocked.TryGetValue(personaId, out var count) ? count : 0;
        }

        /// <summary>
        /// 尝试解锁一个副属性：上限钳制（当前计数 ≥ maxSlots 拒绝，不扣款）→ 真实扣款（金币不足返回 false 不生效）。
        /// cost 负数 / maxSlots < 1 抛 ArgumentOutOfRangeException；deck null 抛 ArgumentNullException。
        /// </summary>
        public bool TryUnlock(string personaId, int maxSlots, int cost, JourneyDeckState deck)
        {
            if (string.IsNullOrEmpty(personaId))
                throw new ArgumentException("personaId 不能为空。", nameof(personaId));
            if (maxSlots < 1) throw new ArgumentOutOfRangeException(nameof(maxSlots));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            var current = UnlockedCountOf(personaId);
            if (current >= maxSlots) return false;
            if (!deck.TrySpend(cost)) return false;
            _unlocked[personaId] = current + 1;
            return true;
        }

        /// <summary>已解锁人格条目快照（仅计数 ≥1；顺序不保证，存档遍历用）。</summary>
        public IReadOnlyList<KeyValuePair<string, int>> UnlockedEntries
        {
            get
            {
                var entries = new List<KeyValuePair<string, int>>(_unlocked.Count);
                foreach (var pair in _unlocked) entries.Add(pair);
                return entries;
            }
        }

        /// <summary>存档恢复写入（ForgeUnlockSaveCodec.Restore 专用）：count 下限 1 钳制；重复调用取较大值（合并语义）。</summary>
        public void SeedUnlocked(string personaId, int count)
        {
            if (string.IsNullOrEmpty(personaId))
                throw new ArgumentException("personaId 不能为空。", nameof(personaId));
            var clamped = count < 1 ? 1 : count;
            _unlocked.TryGetValue(personaId, out var current);
            _unlocked[personaId] = Math.Max(current, clamped);
        }
    }
}
