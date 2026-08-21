using System;
using System.Collections.Generic;
using System.Globalization;
using PersonaCards.Cards.Hands;
using PersonaCards.Core;
using PersonaCards.Data;

namespace PersonaCards.Battle.Personas
{
    /// <summary>
    /// 初始人格目录（P0-1E 门面化）：教学 3 张白盒（Accumulator/Executor/Ambitious）保持零改动静态锚点，
    /// 配表 16 张（PER_001~016）经 Configure 注入为强类型模板目录（Templates）。
    /// 白盒回落 = 空模板目录（Configure(null)/空资产/转换失败 → Templates 空），教学卡与 CreateDefaultLoadout
    /// 行为零差异；不存在「白盒 16 张拷贝」——从源头消灭配表漂移（P0-1B 教训）。
    /// </summary>
    public static class InitialPersonaCatalog
    {
        /// <summary>空模板目录（白盒回落值，避免每次回落都分配新数组）。</summary>
        private static readonly PersonaCardTemplate[] EmptyTemplates = new PersonaCardTemplate[0];

        /// <summary>当前配置模板目录（Configure 整体替换；默认空 = 白盒）。</summary>
        private static PersonaCardTemplate[] _templates = EmptyTemplates;

        /// <summary>教学卡「积累者」（静态属性幂等缓存：引用相等断言依赖，JourneyDeckStateTests）。</summary>
        public static PersonaCardDefinition Accumulator { get; } = new PersonaCardDefinition(
            "persona.initial.accumulator",
            "积累者",
            PersonaConditionKind.Always,
            HandType.HighCard,
            PersonaEffectKind.AddChips,
            15m);

        /// <summary>教学卡「执行者」。</summary>
        public static PersonaCardDefinition Executor { get; } = new PersonaCardDefinition(
            "persona.initial.executor",
            "执行者",
            PersonaConditionKind.Always,
            HandType.HighCard,
            PersonaEffectKind.AddMultiplier,
            2m);

        /// <summary>教学卡「野心者」。</summary>
        public static PersonaCardDefinition Ambitious { get; } = new PersonaCardDefinition(
            "persona.initial.ambitious",
            "野心者",
            PersonaConditionKind.MinimumHandPriority,
            HandType.Pair,
            PersonaEffectKind.MultiplyFinal,
            1.10m);

        /// <summary>教学 3 张 + 空槽 4（白盒锚点，零改动）。</summary>
        public static PersonaLoadout CreateDefaultLoadout()
        {
            return new PersonaLoadout(new[]
            {
                new PersonaSlot(0, Accumulator),
                new PersonaSlot(1, Executor),
                new PersonaSlot(2, Ambitious),
                new PersonaSlot(3, null)
            });
        }

        /// <summary>当前配置模板目录（PER_001~016 按人格牌_ID 升序；未 Configure/回落时为空）。</summary>
        public static IReadOnlyList<PersonaCardTemplate> Templates => _templates;

        /// <summary>最近一次 Configure 的摘要（null = 白盒回落）；由调用方打 [Persona] 日志（本程序集无引擎引用）。</summary>
        public static string LastConfiguredSummary { get; private set; }

        /// <summary>
        /// 注入配置条目列表（PrototypeFlowController 三态接线调用：资产校验通过后传 asset.entries）。
        /// null/空列表 → 空模板目录（白盒回落，summary 清空）；条目转换任一失败 → 整体回落空目录（防半状态）。
        /// 资产应先通过 PersonaConfigAsset.Validate（转换失败属防御性兜底）。
        /// 注意：参数为纯 C# 条目列表而非资产——Battle 是 noEngineReferences 程序集，资产类型（ScriptableObject）不能跨边界（P0-1D 教训）。
        /// </summary>
        public static void Configure(IReadOnlyList<PersonaConfigEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                _templates = EmptyTemplates;
                LastConfiguredSummary = null;
                return;
            }

            // 先全部转换成功再整体替换：中途失败不留下半成品目录
            var converted = new List<PersonaCardTemplate>(entries.Count);
            try
            {
                foreach (var entry in entries)
                {
                    converted.Add(ToTemplate(entry));
                }
            }
            catch (ArgumentException)
            {
                _templates = EmptyTemplates;
                LastConfiguredSummary = null;
                return;
            }

            converted.Sort((left, right) => string.CompareOrdinal(left.PersonaId, right.PersonaId));
            _templates = converted.ToArray();
            LastConfiguredSummary = $"{_templates.Length} 张人格牌模板已加载。";
        }

        /// <summary>按人格牌_ID 查模板（16 条规模线性查找足够；白盒回落时恒 false）。</summary>
        public static bool TryFind(string personaId, out PersonaCardTemplate template)
        {
            foreach (var candidate in _templates)
            {
                if (candidate.PersonaId == personaId)
                {
                    template = candidate;
                    return true;
                }
            }
            template = null;
            return false;
        }

        /// <summary>
        /// 资产 Entry（string 原文）→ 强类型模板。枚举文本转换失败抛 ArgumentException（Configure 层 catch 回落）；
        /// 数值 parse 均 invariant（decimal.Parse 精确保存 xlsx 浮点原文，如 PER_013 的 2.4500000000000002）。
        /// </summary>
        private static PersonaCardTemplate ToTemplate(PersonaConfigEntry entry)
        {
            if (!PersonaCardTemplate.TryMapQuality(entry.quality, out var quality))
            {
                throw new ArgumentException($"未知品质类型「{entry.quality}」：{entry.personaId}");
            }
            if (!PersonaCardTemplate.TryMapTrigger(entry.trigger, out var trigger))
            {
                throw new ArgumentException($"未知触发条件「{entry.trigger}」：{entry.personaId}");
            }
            if (!PersonaCardTemplate.TryMapComparator(entry.comparator, out var comparator))
            {
                throw new ArgumentException($"未知比较符「{entry.comparator}」：{entry.personaId}");
            }
            if (!PersonaCardTemplate.TryMapEffectType(entry.effect, out var effect))
            {
                throw new ArgumentException($"未知效果类型「{entry.effect}」：{entry.personaId}");
            }

            // 条件阈值：空 = 无
            var threshold = entry.threshold.Length == 0
                ? (int?)null
                : int.Parse(entry.threshold, NumberStyles.Integer, CultureInfo.InvariantCulture);

            // 附加条件：可解析 → 结构化；否则 null（原文保留）
            ExtraConditionSpec extra = null;
            if (entry.extraTrigger.Length > 0)
            {
                if (!PersonaCardTemplate.TryMapTrigger(entry.extraTrigger, out var extraTrigger))
                {
                    throw new ArgumentException($"未知附加条件触发条件「{entry.extraTrigger}」：{entry.personaId}");
                }
                if (!PersonaCardTemplate.TryMapComparator(entry.extraComparator, out var extraComparator))
                {
                    throw new ArgumentException($"未知附加条件比较符「{entry.extraComparator}」：{entry.personaId}");
                }
                var extraThreshold = int.Parse(entry.extraThreshold, NumberStyles.Integer, CultureInfo.InvariantCulture);
                extra = new ExtraConditionSpec(extraTrigger, extraComparator, extraThreshold);
            }

            // 效果参数：1 必填、2 空 = 0、上限空 = 无
            var effectParam1 = decimal.Parse(entry.effectParam1, NumberStyles.Number, CultureInfo.InvariantCulture);
            var effectParam2 = entry.effectParam2.Length == 0
                ? 0m
                : decimal.Parse(entry.effectParam2, NumberStyles.Number, CultureInfo.InvariantCulture);
            var effectCap = entry.effectCap.Length == 0
                ? (decimal?)null
                : decimal.Parse(entry.effectCap, NumberStyles.Number, CultureInfo.InvariantCulture);

            return new PersonaCardTemplate(
                entry.personaId,
                entry.displayName,
                quality,
                entry.qualityParam,
                entry.behaviorTagId,
                trigger,
                comparator,
                threshold,
                extra,
                entry.extraConditionRaw,
                effect,
                effectParam1,
                effectParam2,
                entry.effectRaw,
                effectCap,
                entry.independentSettlement);
        }
    }
}
