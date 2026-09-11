using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌配置配表契约：与策划表格「人格牌配置」sheet 的表头与枚举值约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1E.md 记录）。
    /// 程序集边界（P0-1E）：Battle 引用 Data 不能反向，故本契约只含 string 常量（枚举在 Battle.Personas 的配置枚举，
    /// 两处的顺序与取值由交叉校验测试锁住，防漂移）。
    /// </summary>
    public static class PersonaTableContract
    {
        /// <summary>工作表名（人格牌数据）。</summary>
        public const string SheetName = "人格牌配置";

        /// <summary>工作表名（词条表：新版 8 列引用式结构的触发条件定义，词条_ID 被「人格牌配置」引用）。</summary>
        public const string EntrySheetName = "人格牌_词条";

        /// <summary>工作表名（主属性表：新版 8 列引用式结构的效果定义，主属性_ID 被「人格牌配置」引用）。</summary>
        public const string MainAttrSheetName = "人格牌_主属性";

        /// <summary>列名：人格牌_ID（PER_xxx；权威查询键，新版基础人格牌 PER_001~008 必须齐全）。</summary>
        public const string ColPersonaId = "人格牌_ID";

        /// <summary>列名：人格牌名称（显示名）。</summary>
        public const string ColName = "人格牌名称";

        /// <summary>列名：词条_ID（新版引用列，指向「人格牌_词条」sheet 的词条_ID）。</summary>
        public const string ColEntryId = "词条_ID";

        /// <summary>列名：主属性_ID（新版引用列，指向「人格牌_主属性」sheet 的主属性_ID）。</summary>
        public const string ColMainAttrId = "主属性_ID";

        /// <summary>列名：次级属性_ID（新版引用列，指向「人格牌_次级属性」sheet；仅存原文）。</summary>
        public const string ColSubAttrId = "次级属性_ID";

        /// <summary>列名：最大属性数量（新版；仅存原文）。</summary>
        public const string ColMaxAttrCount = "最大属性数量";

        /// <summary>列名：最大次级属性数量（新版；仅存原文）。</summary>
        public const string ColMaxSubAttrCount = "最大次级属性数量";

        /// <summary>列名：次级属性池数量（新版；仅存原文）。</summary>
        public const string ColSubAttrPoolCount = "次级属性池数量";

        /// <summary>列名：品质类型（基础/进阶/稀有/异质；「特殊」= 旧写法，兼容规范化 + 警告，A1）。</summary>
        public const string ColQuality = "品质类型";

        /// <summary>列名：品质参数（当前填颜色名，语义待策划说明 A9，仅存原文）。</summary>
        public const string ColQualityParam = "品质参数";

        /// <summary>列名：行为标签_ID（T01~T16，格式校验；范围不校验，A5 预留 T09~T16）。</summary>
        public const string ColBehaviorTag = "行为标签_ID";

        /// <summary>列名：触发条件（12 种统计类条件，固定映射）。</summary>
        public const string ColTrigger = "触发条件";

        /// <summary>列名：比较符（等于/大于等于/小于/小于等于，固定映射）。</summary>
        public const string ColComparator = "比较符";

        /// <summary>列名：条件阈值（非负整数；允许空）。</summary>
        public const string ColThreshold = "条件阈值";

        /// <summary>列名：附加条件（可解析的「条件+符号+阈值」结构化；其余存原文 + 警告，A8）。</summary>
        public const string ColExtra = "附加条件";

        /// <summary>列名：效果类型1（6 种效果，固定映射）。</summary>
        public const string ColEffect = "效果类型1";

        /// <summary>列名：效果参数1（必填，非负 decimal）。</summary>
        public const string ColEffectParam1 = "效果参数1";

        /// <summary>列名：效果参数2（非负 decimal；空 = 0）。</summary>
        public const string ColEffectParam2 = "效果参数2";

        /// <summary>列名：效果（原文列，当前全空=预留，仅存原文）。</summary>
        public const string ColEffectRaw = "效果";

        /// <summary>列名：效果上限（非负 decimal；允许空 = 无上限）。</summary>
        public const string ColEffectCap = "效果上限";

        /// <summary>列名：独立结算（是/否）。</summary>
        public const string ColIndependent = "独立结算";

        /// <summary>枚举值：品质「基础」。</summary>
        public const string QualityBasic = "基础";

        /// <summary>枚举值：品质「进阶」。</summary>
        public const string QualityAdvanced = "进阶";

        /// <summary>枚举值：品质「稀有」。</summary>
        public const string QualityRare = "稀有";

        /// <summary>枚举值：品质「异质」（A1 拍板统一名）。</summary>
        public const string QualityMutant = "异质";

        /// <summary>兼容值：品质旧写法「特殊」（A1 拍板前配表用词，规范化 + 警告）。</summary>
        public const string LegacyQualityMutant = "特殊";

        /// <summary>品质规范值集合（与 Battle 的 PersonaQuality 枚举对应，顺序一致）。</summary>
        public static readonly string[] QualityValues = { QualityBasic, QualityAdvanced, QualityRare, QualityMutant };

        /// <summary>枚举值：触发条件「与上一手牌型相同」。</summary>
        public const string TriggerSameHandTypeAsPrevious = "与上一手牌型相同";

        /// <summary>枚举值：触发条件「计分牌数量」。</summary>
        public const string TriggerScoringCardCount = "计分牌数量";

        /// <summary>枚举值：触发条件「已使用弃牌次数」。</summary>
        public const string TriggerDiscardsUsed = "已使用弃牌次数";

        /// <summary>枚举值：触发条件「命中AI偏好」。</summary>
        public const string TriggerHitAiPreference = "命中AI偏好";

        /// <summary>枚举值：触发条件「剩余弃牌次数」。</summary>
        public const string TriggerDiscardsRemaining = "剩余弃牌次数";

        /// <summary>枚举值：触发条件「本局移除牌数量」。</summary>
        public const string TriggerCardsRemovedThisRun = "本局移除牌数量";

        /// <summary>枚举值：触发条件「本局新增牌数量」。</summary>
        public const string TriggerCardsAddedThisRun = "本局新增牌数量";

        /// <summary>枚举值：触发条件「连续使用相同牌型次数」。</summary>
        public const string TriggerSameHandTypeStreak = "连续使用相同牌型次数";

        /// <summary>枚举值：触发条件「牌库数量」。</summary>
        public const string TriggerDeckSize = "牌库数量";

        /// <summary>枚举值：触发条件「其他人格触发次数」。</summary>
        public const string TriggerOtherPersonaTriggerCount = "其他人格触发次数";

        /// <summary>枚举值：触发条件「剩余出牌次数」。</summary>
        public const string TriggerPlaysRemaining = "剩余出牌次数";

        /// <summary>枚举值：触发条件「人格触发次数」。</summary>
        public const string TriggerPersonaTriggerCount = "人格触发次数";

        /// <summary>枚举值：触发条件「牌型品质」（新版词条表；条件参数为品质文本，非数值阈值）。</summary>
        public const string TriggerHandTypeQuality = "牌型品质";

        /// <summary>枚举值：触发条件「弃牌后出牌」（新版词条表）。</summary>
        public const string TriggerAfterDiscardPlay = "弃牌后出牌";

        /// <summary>枚举值：触发条件「连续使用不同牌型次数」（新版词条表）。</summary>
        public const string TriggerDifferentHandTypeStreak = "连续使用不同牌型次数";

        /// <summary>枚举值：触发条件「出牌数量」（新版词条表）。</summary>
        public const string TriggerSubmittedCardCount = "出牌数量";

        /// <summary>触发条件合法值集合（与 Battle 的 PersonaTriggerCondition 枚举按表出现序对应，追加值必须续在末尾）。</summary>
        public static readonly string[] TriggerValues =
        {
            TriggerSameHandTypeAsPrevious,
            TriggerScoringCardCount,
            TriggerDiscardsUsed,
            TriggerHitAiPreference,
            TriggerDiscardsRemaining,
            TriggerCardsRemovedThisRun,
            TriggerCardsAddedThisRun,
            TriggerSameHandTypeStreak,
            TriggerDeckSize,
            TriggerOtherPersonaTriggerCount,
            TriggerPlaysRemaining,
            TriggerPersonaTriggerCount,
            TriggerHandTypeQuality,
            TriggerAfterDiscardPlay,
            TriggerDifferentHandTypeStreak,
            TriggerSubmittedCardCount
        };

        /// <summary>枚举值：比较符「等于」。</summary>
        public const string ComparatorEqual = "等于";

        /// <summary>枚举值：比较符「大于等于」。</summary>
        public const string ComparatorGreaterOrEqual = "大于等于";

        /// <summary>枚举值：比较符「小于」。</summary>
        public const string ComparatorLess = "小于";

        /// <summary>枚举值：比较符「小于等于」。</summary>
        public const string ComparatorLessOrEqual = "小于等于";

        /// <summary>枚举值：比较符「不等于」。</summary>
        public const string ComparatorNotEqual = "不等于";

        /// <summary>比较符合法值集合（与 Battle 的 PersonaComparator 枚举对应，追加值必须续在末尾）。</summary>
        public static readonly string[] ComparatorValues =
            { ComparatorEqual, ComparatorGreaterOrEqual, ComparatorLess, ComparatorLessOrEqual, ComparatorNotEqual };

        /// <summary>枚举值：效果「增加筹码」。</summary>
        public const string EffectAddChips = "增加筹码";

        /// <summary>枚举值：效果「增加倍率」。</summary>
        public const string EffectAddMultiplier = "增加倍率";

        /// <summary>枚举值：效果「增加筹码和倍率」。</summary>
        public const string EffectAddChipsAndMultiplier = "增加筹码和倍率";

        /// <summary>枚举值：效果「每单位增加倍率」。</summary>
        public const string EffectPerUnitMultiplier = "每单位增加倍率";

        /// <summary>枚举值：效果「每单位增加筹码」。</summary>
        public const string EffectPerUnitChips = "每单位增加筹码";

        /// <summary>枚举值：效果「最终倍率乘算」。</summary>
        public const string EffectMultiplyFinal = "最终倍率乘算";

        /// <summary>枚举值：效果「增加独立倍率」（新版主属性表）。</summary>
        public const string EffectAddIndependentMultiplier = "增加独立倍率";

        /// <summary>效果类型合法值集合（与 Battle 的 PersonaEffectType 枚举对应，追加值必须续在末尾）。</summary>
        public static readonly string[] EffectValues =
        {
            EffectAddChips,
            EffectAddMultiplier,
            EffectAddChipsAndMultiplier,
            EffectPerUnitMultiplier,
            EffectPerUnitChips,
            EffectMultiplyFinal,
            EffectAddIndependentMultiplier
        };

        /// <summary>枚举值：品质等级「NORMAL」（牌型品质条件参数，来自「牌型品质定义表」sheet）。</summary>
        public const string QualityParamNormal = "NORMAL";

        /// <summary>枚举值：品质等级「RARE」（牌型品质条件参数，来自「牌型品质定义表」sheet）。</summary>
        public const string QualityParamRare = "RARE";

        /// <summary>牌型品质条件参数字段合法值集合（与「牌型品质定义表」sheet 的品质等级列一致）。</summary>
        public static readonly string[] QualityParamValues = { QualityParamNormal, QualityParamRare };

        /// <summary>词条表·列名：词条_ID（ENTRY_xxx）。</summary>
        public const string ColEntryIdOfEntries = "词条_ID";

        /// <summary>词条表·列名：条件类型（连续牌型/计分牌数量/牌型品质/弃牌次数/弃牌后出牌/出牌数量）。</summary>
        public const string ColEntryConditionType = "条件类型";

        /// <summary>词条表·列名：比较符（EQ/NEQ/GT/GTE/LT/LTE，符号值）。</summary>
        public const string ColEntryComparator = "比较符";

        /// <summary>词条表·列名：条件参数（数值阈值或品质文本 NORMAL/RARE）。</summary>
        public const string ColEntryParam = "条件参数";

        /// <summary>主属性表·列名：主属性_ID（MAIN_xxx）。</summary>
        public const string ColMainAttrIdOfMainAttrs = "主属性_ID";

        /// <summary>主属性表·列名：属性类型（基础筹码/基础倍率/独立倍率）。</summary>
        public const string ColMainAttrType = "属性类型";

        /// <summary>主属性表·列名：属性参数1（当前恒「增加」，效果方向）。</summary>
        public const string ColMainAttrParam1 = "属性参数1";

        /// <summary>主属性表·列名：属性参数2（效果数值）。</summary>
        public const string ColMainAttrParam2 = "属性参数2";

        /// <summary>枚举值：独立结算「是」。</summary>
        public const string IndependentYes = "是";

        /// <summary>枚举值：独立结算「否」。</summary>
        public const string IndependentNo = "否";

        /// <summary>行为标签格式（T01~T16）。</summary>
        public const string BehaviorTagPattern = @"^T\d{2}$";
    }

    /// <summary>人格牌配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class PersonaMappingResult
    {
        public PersonaMappingResult(bool succeeded, List<PersonaConfigEntry> entries,
            List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的资产条目（全规范化值，按人格牌_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaConfigEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（PER_009~016 待策划补充、ID 不在图片配置绑定 ID 集合等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 人格牌配表映射器：把 XlsxTableReader 输出的行字典列表直接转成 PersonaConfigEntry 列表（Data 不能引用 Battle，
    /// 枚举文本在此层校验与规范化，Battle 门面 Configure 时再转配置枚举）。
    /// 新版 8 列引用式结构：跨「人格牌配置」+「人格牌_词条」+「人格牌_主属性」3 个 sheet 解析——配置行按词条_ID/主属性_ID
    /// 引用两侧定义表，经组合映射翻译回 PersonaConfigEntry 的规范化字段（触发条件/比较符/阈值或条件参数/效果/效果参数1）。
    /// 旧 15 列扁平结构在旧表中存在的品质/行为标签/附加条件/效果参数2/效果上限/独立结算列，新版表未含 → 按基础品质/
    /// 无标签/无附加/参数2=0/无上限/非独立结算落地（策划补列后扩展词条或主属性表即可）。
    /// 规则：PER_001~008 必须齐全（防策划误删，新版基础人格牌口径）；PER_009~016 缺 = 警告不阻断（旧 16 张口径待策划补充）。
    /// </summary>
    public static class PersonaTableMapper
    {
        /// <summary>词条表（条件类型 + 比较符符号）→ 契约触发文本。条件类型语义：连续牌型 EQ=相同/NEX=不同（网页版 DIFFERENT_FROM_PREVIOUS_HAND）。</summary>
        private static readonly (string ConditionType, string ComparatorSymbol, string Trigger)[] EntryConditionMappings =
        {
            ("连续牌型", "EQ", PersonaTableContract.TriggerSameHandTypeStreak),
            ("连续牌型", "NEQ", PersonaTableContract.TriggerDifferentHandTypeStreak),
            ("计分牌数量", "GTE", PersonaTableContract.TriggerScoringCardCount),
            ("牌型品质", "EQ", PersonaTableContract.TriggerHandTypeQuality),
            ("弃牌次数", "EQ", PersonaTableContract.TriggerDiscardsUsed),
            ("弃牌后出牌", "EQ", PersonaTableContract.TriggerAfterDiscardPlay),
            ("出牌数量", "LTE", PersonaTableContract.TriggerSubmittedCardCount)
        };

        /// <summary>词条表比较符符号 → 契约比较符文本（词条表当前只用这 4 种；GT/LT/IN/NOT_IN 出现即报错）。</summary>
        private static readonly (string Symbol, string Comparator)[] EntryComparatorSymbols =
        {
            ("EQ", PersonaTableContract.ComparatorEqual),
            ("NEQ", PersonaTableContract.ComparatorNotEqual),
            ("GTE", PersonaTableContract.ComparatorGreaterOrEqual),
            ("LTE", PersonaTableContract.ComparatorLessOrEqual)
        };

        /// <summary>主属性表（属性类型 + 属性参数1）→ 契约效果文本（参数1 恒「增加」= 效果方向）。</summary>
        private static readonly (string Type, string Param1, string Effect)[] MainAttrEffectMappings =
        {
            ("基础筹码", "增加", PersonaTableContract.EffectAddChips),
            ("基础倍率", "增加", PersonaTableContract.EffectAddMultiplier),
            ("独立倍率", "增加", PersonaTableContract.EffectAddIndependentMultiplier)
        };

        /// <summary>
        /// 映射新版 8 列引用式结构（XlsxTableReader.ReadTable 的输出，3 个 sheet）。
        /// configRows = 「人格牌配置」；entryRows = 「人格牌_词条」；mainAttrRows = 「人格牌_主属性」。
        /// imageBindingIds = 图片配置 sheet 的绑定 ID 集合（null 表示跳过对照，测试用）。
        /// </summary>
        public static PersonaMappingResult Map(
            List<Dictionary<string, string>> configRows,
            List<Dictionary<string, string>> entryRows,
            List<Dictionary<string, string>> mainAttrRows,
            ICollection<string> imageBindingIds)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (configRows == null || configRows.Count == 0)
            {
                errors.Add("人格牌配置表没有任何数据行。");
                return new PersonaMappingResult(false, null, errors, warnings);
            }
            if (entryRows == null || entryRows.Count == 0)
            {
                errors.Add("词条表没有任何数据行。");
                return new PersonaMappingResult(false, null, errors, warnings);
            }
            if (mainAttrRows == null || mainAttrRows.Count == 0)
            {
                errors.Add("主属性表没有任何数据行。");
                return new PersonaMappingResult(false, null, errors, warnings);
            }

            // 词条索引：词条_ID → (条件类型, 比较符符号, 条件参数)；ID 重复/为空 = 错误（全局资源，不 fail-fast 全收集）
            var entryById = new Dictionary<string, (string ConditionType, string Comparator, string Param)>();
            for (var entryIndex = 0; entryIndex < entryRows.Count; entryIndex++)
            {
                var row = entryRows[entryIndex];
                var id = Get(row, PersonaTableContract.ColEntryIdOfEntries);
                var label = $"词条表第 {entryIndex + 2} 行「{id}」"; // +2 = 表头行占 1 行
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"{label}：「词条_ID」为空（必填）。");
                    continue;
                }
                if (entryById.ContainsKey(id))
                {
                    errors.Add($"{label}：「词条_ID」重复，必须唯一。");
                    continue;
                }
                entryById[id] = (
                    Get(row, PersonaTableContract.ColEntryConditionType),
                    Get(row, PersonaTableContract.ColEntryComparator),
                    Get(row, PersonaTableContract.ColEntryParam));
            }

            // 主属性索引：主属性_ID → (属性类型, 属性参数1, 属性参数2)
            var mainAttrById = new Dictionary<string, (string Type, string Param1, string Param2)>();
            for (var mainIndex = 0; mainIndex < mainAttrRows.Count; mainIndex++)
            {
                var row = mainAttrRows[mainIndex];
                var id = Get(row, PersonaTableContract.ColMainAttrIdOfMainAttrs);
                var label = $"主属性表第 {mainIndex + 2} 行「{id}」";
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"{label}：「主属性_ID」为空（必填）。");
                    continue;
                }
                if (mainAttrById.ContainsKey(id))
                {
                    errors.Add($"{label}：「主属性_ID」重复，必须唯一。");
                    continue;
                }
                mainAttrById[id] = (
                    Get(row, PersonaTableContract.ColMainAttrType),
                    Get(row, PersonaTableContract.ColMainAttrParam1),
                    Get(row, PersonaTableContract.ColMainAttrParam2));
            }

            var entries = new List<PersonaConfigEntry>();
            var seenPersonaIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < configRows.Count; rowIndex++)
            {
                var row = configRows[rowIndex];
                var personaId = Get(row, PersonaTableContract.ColPersonaId);
                var label = $"第 {rowIndex + 2} 行「{personaId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 人格牌_ID：非空、唯一（权威查询键与美术绑定 ID）
                if (string.IsNullOrEmpty(personaId))
                {
                    errors.Add($"{label}：「人格牌_ID」为空（必填）。");
                    continue;
                }
                if (!seenPersonaIds.Add(personaId))
                {
                    errors.Add($"{label}：「人格牌_ID」重复，必须唯一。");
                    continue;
                }

                // 人格牌名称：不得为空
                var displayName = Get(row, PersonaTableContract.ColName);
                if (string.IsNullOrEmpty(displayName))
                {
                    errors.Add($"{label}：「人格牌名称」为空（必填）。");
                    continue;
                }

                // 词条_ID → 词条表引用解析（缺引用 = 错误）
                var entryId = Get(row, PersonaTableContract.ColEntryId);
                if (string.IsNullOrEmpty(entryId))
                {
                    errors.Add($"{label}：「词条_ID」为空（必填）。");
                    continue;
                }
                if (!entryById.TryGetValue(entryId, out var entry))
                {
                    errors.Add($"{label}：「词条_ID」值「{entryId}」在词条表中不存在。");
                    continue;
                }

                // 词条（条件类型 + 比较符符号）→ 契约触发文本（组合映射；未收录组合 = 错误）
                var trigger = "";
                foreach (var (conditionType, comparatorSymbol, triggerText) in EntryConditionMappings)
                {
                    if (conditionType == entry.ConditionType && comparatorSymbol == entry.Comparator)
                    {
                        trigger = triggerText;
                        break;
                    }
                }
                if (trigger.Length == 0)
                {
                    errors.Add($"{label}：词条「{entryId}」的条件类型「{entry.ConditionType}」+ 比较符「{entry.Comparator}」组合未收录（当前只支持连续牌型/计分牌数量/牌型品质/弃牌次数/弃牌后出牌/出牌数量 与 EQ/NEQ/GTE/LTE）。");
                    continue;
                }

                // 词条比较符符号 → 契约比较符文本（触发映射已按符号收录，此处必命中，防御性保留失败路径）
                var comparator = "";
                foreach (var (symbol, comparatorText) in EntryComparatorSymbols)
                {
                    if (symbol == entry.Comparator)
                    {
                        comparator = comparatorText;
                        break;
                    }
                }
                if (comparator.Length == 0)
                {
                    errors.Add($"{label}：词条「{entryId}」的比较符「{entry.Comparator}」未收录。");
                    continue;
                }

                // 条件参数：品质类条件 = 品质文本（NORMAL/RARE）入 conditionParam；数值类条件 = 非负整数入 threshold
                var threshold = "";
                var conditionParam = "";
                if (trigger == PersonaTableContract.TriggerHandTypeQuality)
                {
                    if (Array.IndexOf(PersonaTableContract.QualityParamValues, entry.Param) < 0)
                    {
                        errors.Add($"{label}：词条「{entryId}」的条件参数「{entry.Param}」不是合法品质等级，应为 {string.Join("/", PersonaTableContract.QualityParamValues)}。");
                        continue;
                    }
                    conditionParam = entry.Param;
                }
                else
                {
                    if (entry.Param.Length == 0
                        || !int.TryParse(entry.Param, NumberStyles.Integer, CultureInfo.InvariantCulture, out var paramValue)
                        || paramValue < 0)
                    {
                        errors.Add($"{label}：词条「{entryId}」的条件参数「{entry.Param}」不是非负整数。");
                        continue;
                    }
                    threshold = entry.Param;
                }

                // 主属性_ID → 主属性表引用解析（缺引用 = 错误）
                var mainAttrId = Get(row, PersonaTableContract.ColMainAttrId);
                if (string.IsNullOrEmpty(mainAttrId))
                {
                    errors.Add($"{label}：「主属性_ID」为空（必填）。");
                    continue;
                }
                if (!mainAttrById.TryGetValue(mainAttrId, out var mainAttr))
                {
                    errors.Add($"{label}：「主属性_ID」值「{mainAttrId}」在主属性表中不存在。");
                    continue;
                }

                // 主属性（属性类型 + 参数1）→ 契约效果文本；参数2 = 效果参数1（非负 decimal）
                var effect = "";
                foreach (var (type, param1, effectText) in MainAttrEffectMappings)
                {
                    if (type == mainAttr.Type && param1 == mainAttr.Param1)
                    {
                        effect = effectText;
                        break;
                    }
                }
                if (effect.Length == 0)
                {
                    errors.Add($"{label}：主属性「{mainAttrId}」的属性类型「{mainAttr.Type}」+ 属性参数1「{mainAttr.Param1}」组合未收录（当前只支持基础筹码/基础倍率/独立倍率 与「增加」）。");
                    continue;
                }
                var effectParam1 = mainAttr.Param2;
                if (effectParam1.Length == 0
                    || !decimal.TryParse(effectParam1, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectParam1Value)
                    || effectParam1Value < 0m)
                {
                    errors.Add($"{label}：主属性「{mainAttrId}」的属性参数2「{effectParam1}」不是非负数字。");
                    continue;
                }

                // 新版 8 列结构的次级属性相关列：照抄原文 + 格式校验
                var subAttributeId = Get(row, PersonaTableContract.ColSubAttrId);
                if (subAttributeId.Length > 0 && !Regex.IsMatch(subAttributeId, @"^SUB_\d{3}$"))
                {
                    errors.Add($"{label}：「次级属性_ID」值「{subAttributeId}」格式无效，应为 SUB_xxx。");
                    continue;
                }
                var maxAttributeCount = Get(row, PersonaTableContract.ColMaxAttrCount);
                var maxSubAttributeCount = Get(row, PersonaTableContract.ColMaxSubAttrCount);
                var subAttributePoolCount = Get(row, PersonaTableContract.ColSubAttrPoolCount);
                foreach (var (fieldName, text) in new[]
                {
                    ("最大属性数量", maxAttributeCount),
                    ("最大次级属性数量", maxSubAttributeCount),
                    ("次级属性池数量", subAttributePoolCount)
                })
                {
                    if (text.Length > 0
                        && (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var countValue)
                            || countValue < 0))
                    {
                        errors.Add($"{label}：「{fieldName}」值「{text}」不是非负整数。");
                        continue; // 只跳过本字段，行照常（错误已记录，最终整体失败）
                    }
                }

                // 人格牌_ID 对照图片配置「绑定ID」列：不在集合 = 警告容错（策划改 ID 只需同步图片配置）
                if (imageBindingIds != null && !imageBindingIds.Contains(personaId))
                {
                    warnings.Add($"{label}：「人格牌_ID」值「{personaId}」不在图片配置「绑定ID」列中（图可能未同步，程序已存值容错）。");
                }

                entries.Add(new PersonaConfigEntry
                {
                    personaId = personaId,
                    displayName = displayName,
                    // 新版结构无品质/行为标签/附加条件/效果参数2/效果上限/独立结算列：按基础品质/无标签/无附加/0/无上限/否落地
                    quality = PersonaTableContract.QualityBasic,
                    qualityParam = "",
                    behaviorTagId = "",
                    trigger = trigger,
                    comparator = comparator,
                    threshold = threshold,
                    conditionParam = conditionParam,
                    subAttributeId = subAttributeId,
                    maxAttributeCount = maxAttributeCount,
                    maxSubAttributeCount = maxSubAttributeCount,
                    subAttributePoolCount = subAttributePoolCount,
                    extraTrigger = "",
                    extraComparator = "",
                    extraThreshold = "",
                    extraConditionRaw = "",
                    effect = effect,
                    effectParam1 = effectParam1,
                    effectParam2 = "0",
                    effectRaw = "",
                    effectCap = "",
                    independentSettlement = false
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaMappingResult(false, null, errors, warnings);
            }

            // PER_001~008 齐全检查（防策划误删行）：缺任一 = 错误；多出的 ID 允许（卡池可扩展）
            for (var index = 1; index <= 8; index++)
            {
                var expected = $"PER_{index:D3}";
                if (!seenPersonaIds.Contains(expected))
                {
                    errors.Add($"人格牌配置表缺少 {expected} 的行（PER_001~008 应齐全）：请确认该行未被误删。");
                }
            }
            // PER_009~016：新版表尚未提供，缺 = 警告（旧版 16 张口径已由新版 8 列结构替换，待策划补充）
            for (var index = 9; index <= 16; index++)
            {
                var expected = $"PER_{index:D3}";
                if (!seenPersonaIds.Contains(expected))
                {
                    warnings.Add($"人格牌配置表缺少 {expected} 的行（新版表仅提供 PER_001~008 基础人格牌，PER_009~016 待策划补充）。");
                }
            }
            if (errors.Count > 0)
            {
                return new PersonaMappingResult(false, null, errors, warnings);
            }

            // 按人格牌_ID 升序排列条目（资产 Inspector 与日志的可读性；门面 Configure 不依赖顺序）
            entries.Sort((left, right) => string.CompareOrdinal(left.personaId, right.personaId));

            return new PersonaMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
