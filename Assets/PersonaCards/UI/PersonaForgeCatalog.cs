using System;
using System.Collections.Generic;
using PersonaCards.Data;

namespace PersonaCards.UI
{
    /// <summary>
    /// 人格铸造目录门面（UI 重排第二批）：Configure 注入 4+1 个契约资产（P0-1J）
    /// —— PersonaCardCatalog（8 人格）/ PersonaEntryCatalog（词条）/ PersonaMainAttrCatalog（主属性）/
    /// PersonaSubAttrCatalog（副属性池 40 行）/ ShopForge（解锁价格 FORGE_001=5金/FORGE_002=8金）。
    /// 注入缺失/空表 → HasContent false → 铸造页显示空列表（功能缺席不崩溃，照 EnhancementTableBootstrap 惯例）。
    /// 副属性槽位内容按池内行序取第 slotIndex 行（契约阶段占位，B7 按权重真抽取后替换）。
    /// </summary>
    public static class PersonaForgeCatalog
    {
        private static List<PersonaCardEntry> _cards = new List<PersonaCardEntry>();
        private static readonly Dictionary<string, PersonaEntryEntry> Entries = new Dictionary<string, PersonaEntryEntry>(StringComparer.Ordinal);
        private static readonly Dictionary<string, PersonaMainAttrEntry> Mains = new Dictionary<string, PersonaMainAttrEntry>(StringComparer.Ordinal);
        private static List<PersonaSubAttrEntry> _subs = new List<PersonaSubAttrEntry>();
        private static List<ShopForgeEntry> _forge = new List<ShopForgeEntry>();

        /// <summary>注入 5 资产（任一项可为 null → 该部分按空处理）。</summary>
        public static void Configure(PersonaCardAsset cards, PersonaEntryAsset entries, PersonaMainAttrAsset mains,
            PersonaSubAttrAsset subs, ShopForgeAsset forge)
        {
            _cards = CopyOf(cards != null ? cards.entries : null);
            Entries.Clear();
            foreach (var entry in CopyOf(entries != null ? entries.entries : null))
                Entries[entry.entryId] = entry;
            Mains.Clear();
            foreach (var entry in CopyOf(mains != null ? mains.entries : null))
                Mains[entry.attrId] = entry;
            _subs = CopyOf(subs != null ? subs.entries : null);
            _forge = CopyOf(forge != null ? forge.entries : null);
        }

        /// <summary>铸造列表是否有内容（卡目录非空）。</summary>
        public static bool HasContent => _cards.Count > 0;

        /// <summary>铸造列表行数（配表 8）。</summary>
        public static int CardCount => _cards.Count;

        /// <summary>第 index 行人格卡条目；越界抛 ArgumentOutOfRangeException。</summary>
        public static PersonaCardEntry CardAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _cards[index];
        }

        /// <summary>词条触发条件描述（ENTRY_xxx →「连续两次使用相同牌型」）；未收录/描述为空返回空串。</summary>
        public static string EntryDescriptionOf(string entryId)
        {
            if (string.IsNullOrEmpty(entryId)) return "";
            return Entries.TryGetValue(entryId, out var entry) ? entry.description ?? "" : "";
        }

        /// <summary>主属性效果描述（如「基础筹码 +15」）；主属性_ID 未收录/参数2缺失返回空串。</summary>
        public static string MainAttrEffectTextOf(PersonaCardEntry card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (!Mains.TryGetValue(card.mainAttrId ?? "", out var main)) return "";
            return PersonaShopText.EffectTextOf(main.attrType, main.param1, main.param2);
        }

        /// <summary>主属性类型名（「基础筹码」等；未收录返回空串）。</summary>
        public static string MainAttrTypeOf(PersonaCardEntry card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            return Mains.TryGetValue(card.mainAttrId ?? "", out var main) ? main.attrType ?? "" : "";
        }

        /// <summary>某人格副属性池行列表（ownerPersona 精确匹配，目录序）；返回新列表可安全持有。</summary>
        public static IReadOnlyList<PersonaSubAttrEntry> SubAttrsOf(string personaName)
        {
            var result = new List<PersonaSubAttrEntry>();
            if (string.IsNullOrEmpty(personaName)) return result;
            foreach (var entry in _subs)
            {
                if (entry != null && string.Equals(entry.ownerPersona, personaName, StringComparison.Ordinal))
                    result.Add(entry);
            }
            return result;
        }

        /// <summary>第 slotIndex 个副属性槽位对应的池内行（契约占位：按目录序）；池不足/越界返回 null。</summary>
        public static PersonaSubAttrEntry SubAttrAt(PersonaCardEntry card, int slotIndex)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (slotIndex < 0) return null;
            var pool = SubAttrsOf(card.personaName);
            return slotIndex < pool.Count ? pool[slotIndex] : null;
        }

        /// <summary>第 slotIndex 个副属性槽位解锁价格（ShopForge 目录序：0→5金/1→8金）；无对应行返回 -1。</summary>
        public static int ForgePriceAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _forge.Count) return -1;
            var entry = _forge[slotIndex];
            return entry != null ? entry.price : -1;
        }

        private static List<T> CopyOf<T>(List<T> source)
        {
            var result = new List<T>();
            if (source == null) return result;
            foreach (var item in source)
                if (item != null) result.Add(item);
            return result;
        }
    }
}
