using System;
using System.Collections.Generic;
using PersonaCards.Battle.Personas;
using PersonaCards.Core;

namespace PersonaCards.UI
{
    /// <summary>
    /// 获得新人格牌弹窗会话（UI 重排第一批 · 获得新人格牌弹窗）：纯 C# 无 UnityEngine 依赖，可单测。
    /// 构造时拷贝装备槽快照防别名；默认选中第一个空槽（无空槽则槽 01）；点槽切换目标；
    /// 文案（统计/类型/条件/效果/槽位/提示条/确认按钮）全部由会话生成；执行 = 委托 PersonaLoadoutState.EquipAt（替换语义）。
    /// 「暂不替换」无副作用（不调 ExecuteReplace 即拒绝路径）；「旧牌保留」天然成立——collection 无上限，槽位移除旧牌仍在收藏。
    /// 铸造候选名格式固定为「mode·name」（如「映照·洞察者」），TypeTag 按 '·' 切分取 mode。
    /// </summary>
    public sealed class PersonaEquipPromptSession
    {
        private readonly PersonaCardDefinition _candidate;
        private readonly int _collectionCount;
        private readonly PersonaCardDefinition[] _slots;
        private int _selectedSlotIndex;

        /// <summary>
        /// 构建弹窗会话：collectionCount = 新卡入收藏之后的收藏总数（「当前持有 N张」）；slots = 装备槽快照。
        /// 默认选中 = 第一个空槽；全满时选中槽 01（下标 0）。候选 null / slots null 抛 ArgumentNullException。
        /// </summary>
        public PersonaEquipPromptSession(PersonaCardDefinition candidate, int collectionCount,
            IReadOnlyList<PersonaCardDefinition> slots)
        {
            _candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            _slots = new PersonaCardDefinition[slots.Count];
            for (var i = 0; i < slots.Count; i++) _slots[i] = slots[i];
            _collectionCount = collectionCount;
            _selectedSlotIndex = Array.IndexOf(_slots, null);
            if (_selectedSlotIndex < 0) _selectedSlotIndex = 0;
        }

        /// <summary>本次获得的新人格牌（铸造确认候选）。</summary>
        public PersonaCardDefinition Candidate => _candidate;

        /// <summary>新卡入收藏后的收藏总数。</summary>
        public int CollectionCount => _collectionCount;

        /// <summary>装备槽非空数量（0..4）。</summary>
        public int EquippedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _slots.Length; i++)
                    if (_slots[i] != null) count++;
                return count;
            }
        }

        /// <summary>当前选中槽位下标（构造后即有效）。</summary>
        public int SelectedSlotIndex => _selectedSlotIndex;

        /// <summary>选中槽是否为空槽。</summary>
        public bool IsTargetEmpty => _slots[_selectedSlotIndex] == null;

        /// <summary>选中槽的旧卡；空槽为 null。</summary>
        public PersonaCardDefinition Replaced => _slots[_selectedSlotIndex];

        /// <summary>切换选中槽位；越界抛 ArgumentOutOfRangeException。</summary>
        public void SelectSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            _selectedSlotIndex = slotIndex;
        }

        /// <summary>顶部三列统计：「本次获得 1张 / 当前持有 5张 / 当前装备 3/4」。</summary>
        public string StatsText => $"本次获得 1张 / 当前持有 {_collectionCount}张 / 当前装备 {EquippedCount}/{_slots.Length}";

        /// <summary>类型标签：「类型·映照」——按铸造候选名「mode·name」切分取 mode。</summary>
        public string TypeTagText => $"类型·{_candidate.DisplayName.Split('·')[0]}";

        /// <summary>触发条件文案：MinimumHandPriority →「对子或更高」；Always →「始终生效」。</summary>
        public string ConditionText => _candidate.ConditionKind == PersonaConditionKind.Always
            ? "始终生效"
            : $"{HandNameOf(_candidate.MinimumHandType)}或更高";

        /// <summary>生效效果文案：「+10 筹码」/「+1.5 倍率」/「最终 ×1.07」。</summary>
        public string EffectText => _candidate.EffectKind switch
        {
            PersonaEffectKind.AddChips => $"+{_candidate.EffectValue:0} 筹码",
            PersonaEffectKind.AddMultiplier => $"+{_candidate.EffectValue:0.0} 倍率",
            _ => $"最终 ×{_candidate.EffectValue:0.00}"
        };

        /// <summary>槽位名：「01  积累者」/「04  空槽」（两位补零，项目惯例）；越界抛异常。</summary>
        public string SlotNameText(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            var name = _slots[slotIndex] == null ? "空槽" : _slots[slotIndex].DisplayName;
            return $"{slotIndex + 1:00}  {name}";
        }

        /// <summary>槽位状态文案：选中 →「将替换」（选中空槽 →「装备至此」），其余 →「选择」；越界抛异常。</summary>
        public string SlotStatusText(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            if (slotIndex != _selectedSlotIndex) return "选择";
            return IsTargetEmpty ? "装备至此" : "将替换";
        }

        /// <summary>底部提示条：空槽「{新卡名} → 槽位04，装备至空槽」；替换「{新卡名} → 槽位02，替换 {旧卡名}（旧牌保留）」。</summary>
        public string BarText => IsTargetEmpty
            ? $"{_candidate.DisplayName} → 槽位{_selectedSlotIndex + 1:00}，装备至空槽"
            : $"{_candidate.DisplayName} → 槽位{_selectedSlotIndex + 1:00}，替换 {Replaced.DisplayName}（旧牌保留）";

        /// <summary>确认按钮文案：空槽「装备至 槽位04 并继续」；替换「替换 {旧卡名} 并继续」（网页版 X = 旧卡名）。</summary>
        public string ConfirmButtonText => IsTargetEmpty
            ? $"装备至 槽位{_selectedSlotIndex + 1:00} 并继续"
            : $"替换 {Replaced.DisplayName} 并继续";

        /// <summary>执行替换：委托 EquipAt（同 TemplateId 已在其他槽则两槽互换）；返回目标槽位；loadout null 抛异常。</summary>
        public int ExecuteReplace(PersonaLoadoutState loadout)
        {
            if (loadout == null) throw new ArgumentNullException(nameof(loadout));
            loadout.EquipAt(_candidate, _selectedSlotIndex);
            return _selectedSlotIndex;
        }

        /// <summary>规则文案单源（FlowController.ForgeRule 委托于此，输出逐字一致）：「{牌型}或更高：+10 筹码」等。</summary>
        public static string RuleTextOf(PersonaCardDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return definition.EffectKind switch
            {
                PersonaEffectKind.AddChips => $"{HandNameOf(definition.MinimumHandType)}或更高：+{definition.EffectValue:0} 筹码",
                PersonaEffectKind.AddMultiplier => $"{HandNameOf(definition.MinimumHandType)}或更高：+{definition.EffectValue:0.0} 倍率",
                _ => $"{HandNameOf(definition.MinimumHandType)}或更高：最终 ×{definition.EffectValue:0.00}"
            };
        }

        /// <summary>牌型中文名单源（FlowController.HandName 委托于此，输出逐字一致）。</summary>
        public static string HandNameOf(HandType handType) => handType switch
        {
            HandType.Pair => "对子",
            HandType.TwoPair => "两对",
            HandType.ThreeOfAKind => "三条",
            HandType.Straight => "顺子",
            HandType.Flush => "同花",
            HandType.FullHouse => "葫芦",
            HandType.FourOfAKind => "四条",
            HandType.StraightFlush => "同花顺",
            _ => "高牌"
        };

        private void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
        }
    }
}
