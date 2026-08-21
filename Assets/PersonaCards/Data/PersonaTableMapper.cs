using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        /// <summary>列名：人格牌_ID（PER_xxx；权威查询键，PER_001~016 必须齐全）。</summary>
        public const string ColPersonaId = "人格牌_ID";

        /// <summary>列名：人格牌名称（显示名；当前配表为「1111xx（暂定」占位，仅存值不参与逻辑）。</summary>
        public const string ColName = "人格牌名称";

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

        /// <summary>触发条件合法值集合（与 Battle 的 PersonaTriggerCondition 枚举按表出现序对应）。</summary>
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
            TriggerPersonaTriggerCount
        };

        /// <summary>枚举值：比较符「等于」。</summary>
        public const string ComparatorEqual = "等于";

        /// <summary>枚举值：比较符「大于等于」。</summary>
        public const string ComparatorGreaterOrEqual = "大于等于";

        /// <summary>枚举值：比较符「小于」。</summary>
        public const string ComparatorLess = "小于";

        /// <summary>枚举值：比较符「小于等于」。</summary>
        public const string ComparatorLessOrEqual = "小于等于";

        /// <summary>比较符合法值集合（与 Battle 的 PersonaComparator 枚举对应）。</summary>
        public static readonly string[] ComparatorValues =
            { ComparatorEqual, ComparatorGreaterOrEqual, ComparatorLess, ComparatorLessOrEqual };

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

        /// <summary>效果类型合法值集合（与 Battle 的 PersonaEffectType 枚举对应）。</summary>
        public static readonly string[] EffectValues =
        {
            EffectAddChips,
            EffectAddMultiplier,
            EffectAddChipsAndMultiplier,
            EffectPerUnitMultiplier,
            EffectPerUnitChips,
            EffectMultiplyFinal
        };

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

        /// <summary>全部警告（品质「特殊」规范化、附加条件存原文、ID 不在图片配置绑定 ID 集合等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 人格牌配表映射器：把 XlsxTableReader 输出的行字典列表直接转成 PersonaConfigEntry 列表（Data 不能引用 Battle，
    /// 枚举文本在此层校验与规范化，Battle 门面 Configure 时再转配置枚举）。
    /// 规则：PER_001~016 必须齐全（防策划误删）；品质「特殊」→「异质」规范化 + 警告（A1）；附加条件可解析的解析、其余存原文
    /// + 警告（A8）；效果参数 decimal 原文精确保存（xlsx 浮点垃圾如 2.4500000000000002 与表一致）。
    /// </summary>
    public static class PersonaTableMapper
    {
        /// <summary>附加条件里比较符符号 → 契约比较符文本（先长后短匹配；「&gt;」不在契约中，不支持）。</summary>
        private static readonly (string Symbol, string Comparator)[] ExtraComparatorSymbols =
        {
            (">=", PersonaTableContract.ComparatorGreaterOrEqual),
            ("<=", PersonaTableContract.ComparatorLessOrEqual),
            ("=", PersonaTableContract.ComparatorEqual),
            ("<", PersonaTableContract.ComparatorLess)
        };

        /// <summary>
        /// 映射行字典列表（XlsxTableReader.ReadTable 的输出）。
        /// imageBindingIds = 图片配置 sheet 的绑定 ID 集合（null 表示跳过对照，测试用）。
        /// </summary>
        public static PersonaMappingResult Map(List<Dictionary<string, string>> rows, ICollection<string> imageBindingIds)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("人格牌配置表没有任何数据行。");
                return new PersonaMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaConfigEntry>();
            var seenPersonaIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
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

                // 人格牌名称：暂定占位但不得为空（A9：定稿后直接同步配表即可）
                var displayName = Get(row, PersonaTableContract.ColName);
                if (string.IsNullOrEmpty(displayName))
                {
                    errors.Add($"{label}：「人格牌名称」为空（必填）。");
                    continue;
                }

                // 品质类型：A1 拍板统一「异质」；旧写法「特殊」规范化 + 警告（策划改表后警告消失）
                var quality = Get(row, PersonaTableContract.ColQuality);
                if (quality == PersonaTableContract.LegacyQualityMutant)
                {
                    warnings.Add($"{label}：品质类型「特殊」已按 A1 规范化为「异质」（请策划将配表同步改为「异质」）。");
                    quality = PersonaTableContract.QualityMutant;
                }
                if (Array.IndexOf(PersonaTableContract.QualityValues, quality) < 0)
                {
                    errors.Add($"{label}：「品质类型」值「{quality}」无效，应为 {string.Join("/", PersonaTableContract.QualityValues)}。");
                    continue;
                }

                // 触发条件：12 种统计类条件固定映射
                var trigger = Get(row, PersonaTableContract.ColTrigger);
                if (Array.IndexOf(PersonaTableContract.TriggerValues, trigger) < 0)
                {
                    errors.Add($"{label}：「触发条件」值「{trigger}」无效，应为 {string.Join("/", PersonaTableContract.TriggerValues)}。");
                    continue;
                }

                // 比较符：等于/大于等于/小于/小于等于
                var comparator = Get(row, PersonaTableContract.ColComparator);
                if (Array.IndexOf(PersonaTableContract.ComparatorValues, comparator) < 0)
                {
                    errors.Add($"{label}：「比较符」值「{comparator}」无效，应为 {string.Join("/", PersonaTableContract.ComparatorValues)}。");
                    continue;
                }

                // 条件阈值：允许空；非空必须是非负整数
                var threshold = Get(row, PersonaTableContract.ColThreshold);
                if (threshold.Length > 0
                    && (!int.TryParse(threshold, NumberStyles.Integer, CultureInfo.InvariantCulture, out var thresholdValue)
                        || thresholdValue < 0))
                {
                    errors.Add($"{label}：「条件阈值」值「{threshold}」不是非负整数。");
                    continue;
                }

                // 附加条件：可解析的「条件+符号+阈值」结构化三字段；其余存原文 + 警告（A8：PER_013 带星号未写全）
                var extraTrigger = "";
                var extraComparator = "";
                var extraThreshold = "";
                var extraRaw = Get(row, PersonaTableContract.ColExtra);
                if (extraRaw.Length > 0 && !TryParseExtra(extraRaw, out extraTrigger, out extraComparator, out extraThreshold))
                {
                    warnings.Add($"{label}：附加条件「{extraRaw}」格式未识别（如带星号未定稿），已存原文容错；请策划补全（见代策划确认 A8）。");
                }

                // 效果类型1：6 种效果固定映射
                var effect = Get(row, PersonaTableContract.ColEffect);
                if (Array.IndexOf(PersonaTableContract.EffectValues, effect) < 0)
                {
                    errors.Add($"{label}：「效果类型1」值「{effect}」无效，应为 {string.Join("/", PersonaTableContract.EffectValues)}。");
                    continue;
                }

                // 效果参数1：必填非负 decimal（原文精确保存，含 xlsx 浮点垃圾如 2.4500000000000002）
                var effectParam1 = Get(row, PersonaTableContract.ColEffectParam1);
                if (effectParam1.Length == 0
                    || !decimal.TryParse(effectParam1, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectParam1Value)
                    || effectParam1Value < 0m)
                {
                    errors.Add($"{label}：「效果参数1」值「{effectParam1}」不是非负数字。");
                    continue;
                }

                // 效果参数2：空 = 0；非空必须非负 decimal
                var effectParam2 = Get(row, PersonaTableContract.ColEffectParam2);
                if (effectParam2.Length > 0
                    && (!decimal.TryParse(effectParam2, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectParam2Value)
                        || effectParam2Value < 0m))
                {
                    errors.Add($"{label}：「效果参数2」值「{effectParam2}」不是非负数字。");
                    continue;
                }
                if (effectParam2.Length == 0) effectParam2 = "0";

                // 效果上限：允许空（= 无上限）；非空必须非负 decimal
                var effectCap = Get(row, PersonaTableContract.ColEffectCap);
                if (effectCap.Length > 0
                    && (!decimal.TryParse(effectCap, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectCapValue)
                        || effectCapValue < 0m))
                {
                    errors.Add($"{label}：「效果上限」值「{effectCap}」不是非负数字。");
                    continue;
                }

                // 独立结算：是/否
                var independent = Get(row, PersonaTableContract.ColIndependent);
                if (independent != PersonaTableContract.IndependentYes && independent != PersonaTableContract.IndependentNo)
                {
                    errors.Add($"{label}：「独立结算」值「{independent}」无效，应为是/否。");
                    continue;
                }

                // 行为标签_ID：允许空；非空必须是 Txx 格式（范围不校验：A5 预留 T09~T16）
                var behaviorTag = Get(row, PersonaTableContract.ColBehaviorTag);
                if (behaviorTag.Length > 0 && !Regex.IsMatch(behaviorTag, PersonaTableContract.BehaviorTagPattern))
                {
                    errors.Add($"{label}：「行为标签_ID」值「{behaviorTag}」格式无效，应为 T01~T99。");
                    continue;
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
                    quality = quality,
                    qualityParam = Get(row, PersonaTableContract.ColQualityParam),
                    behaviorTagId = behaviorTag,
                    trigger = trigger,
                    comparator = comparator,
                    threshold = threshold,
                    extraTrigger = extraTrigger,
                    extraComparator = extraComparator,
                    extraThreshold = extraThreshold,
                    extraConditionRaw = extraRaw,
                    effect = effect,
                    effectParam1 = effectParam1,
                    effectParam2 = effectParam2,
                    effectRaw = Get(row, PersonaTableContract.ColEffectRaw),
                    effectCap = effectCap,
                    independentSettlement = independent == PersonaTableContract.IndependentYes
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaMappingResult(false, null, errors, warnings);
            }

            // PER_001~016 齐全检查（防策划误删行）：缺任一 = 错误；多出的 ID 允许（卡池可扩展）
            for (var index = 1; index <= 16; index++)
            {
                var expected = $"PER_{index:D3}";
                if (!seenPersonaIds.Contains(expected))
                {
                    errors.Add($"人格牌配置表缺少 {expected} 的行（PER_001~016 应齐全）：请确认该行未被误删。");
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

        /// <summary>
        /// 解析附加条件文本（A8 容错策略）：形如「剩余弃牌次数=0」→ 触发条件 + 比较符 + 阈值。
        /// 条件文本按长度降序匹配开头（防「其他人格触发次数」被「人格触发次数」截胡），随后是比较符符号（&gt;= &lt;= = &lt;），
        /// 最后必须是非负整数。任一步失败即整体失败（调用方存原文 + 警告）。
        /// </summary>
        private static bool TryParseExtra(string text, out string trigger, out string comparator, out string threshold)
        {
            // 失败路径保持空串（条目三字段恒非 null，资产 Validate 与门面转换可直接用 Length）
            trigger = string.Empty;
            comparator = string.Empty;
            threshold = string.Empty;

            // 条件文本最长优先匹配开头
            foreach (var candidate in PersonaTableContract.TriggerValues.OrderByDescending(value => value.Length))
            {
                if (!text.StartsWith(candidate, StringComparison.Ordinal)) continue;
                trigger = candidate;
                text = text.Substring(candidate.Length);
                break;
            }
            if (trigger == null) return false;

            // 比较符符号（先长后短）→ 契约比较符文本
            foreach (var (symbol, comparatorText) in ExtraComparatorSymbols)
            {
                if (!text.StartsWith(symbol, StringComparison.Ordinal)) continue;
                comparator = comparatorText;
                text = text.Substring(symbol.Length);
                break;
            }
            if (comparator == null) return false;

            // 阈值 = 非负整数
            if (text.Length == 0
                || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var thresholdValue)
                || thresholdValue < 0)
            {
                return false;
            }
            threshold = text;
            return true;
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
