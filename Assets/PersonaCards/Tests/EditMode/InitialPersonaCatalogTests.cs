using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// InitialPersonaCatalog 门面测试（P0-1E）：
    /// 白盒回落 = 空模板目录 + 教学 3 张零差异；Configure 注入 16 条目后 TryFind 全命中；
    /// 门面映射表与 Data 契约常量交叉校验防漂移；[TearDown] Configure(null) 防静态泄漏。
    /// </summary>
    public class InitialPersonaCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
            // 防静态状态泄漏到其他测试（与 PlayingCardRulesTests 同模式）
            InitialPersonaCatalog.Configure(null);
        }

        /// <summary>构建合法 16 条目资产（循环默认值 + 关键行覆盖真实配表值）。</summary>
        private static PersonaConfigAsset BuildValidAsset()
        {
            var asset = ScriptableObject.CreateInstance<PersonaConfigAsset>();
            for (var index = 1; index <= 16; index++)
            {
                asset.entries.Add(new PersonaConfigEntry
                {
                    personaId = $"PER_{index:D3}",
                    displayName = $"11111{index}（暂定",
                    quality = "基础",
                    qualityParam = "白色",
                    behaviorTagId = $"T{index:D2}",
                    trigger = "与上一手牌型相同",
                    comparator = "等于",
                    threshold = "1",
                    extraTrigger = "",
                    extraComparator = "",
                    extraThreshold = "",
                    extraConditionRaw = "",
                    effect = "增加筹码",
                    effectParam1 = "1",
                    effectParam2 = "0",
                    effectRaw = "",
                    effectCap = "",
                    independentSettlement = false
                });
            }

            // 关键行覆盖为配表真实值（PER_001/007/013/015/016 见配表快照）
            var per001 = asset.entries[0];
            per001.effectParam1 = "9";

            var per007 = asset.entries[6];
            per007.trigger = "本局移除牌数量";
            per007.comparator = "大于等于";
            per007.effect = "每单位增加倍率";
            per007.effectParam1 = "0.1";
            per007.effectCap = "0.7";

            var per013 = asset.entries[12];
            per013.quality = "异质";
            per013.qualityParam = "金色";
            per013.trigger = "牌库数量";
            per013.comparator = "小于等于";
            per013.threshold = "30";
            per013.extraConditionRaw = "另有计分牌数量条件*";
            per013.effect = "最终倍率乘算";
            per013.effectParam1 = "2.4500000000000002";
            per013.independentSettlement = true;

            var per015 = asset.entries[14];
            per015.quality = "异质";
            per015.trigger = "剩余出牌次数";
            per015.threshold = "1";
            per015.extraTrigger = "剩余弃牌次数";
            per015.extraComparator = "等于";
            per015.extraThreshold = "0";
            per015.extraConditionRaw = "剩余弃牌次数=0";
            per015.effect = "最终倍率乘算";
            per015.effectParam1 = "2.1";
            per015.independentSettlement = true;

            var per016 = asset.entries[15];
            per016.quality = "异质";
            per016.trigger = "人格触发次数";
            per016.comparator = "大于等于";
            per016.threshold = "3";
            per016.effect = "最终倍率乘算";
            per016.effectParam1 = "2.2000000000000002";
            per016.independentSettlement = true;

            return asset;
        }

        [Test]
        public void WhiteBoxTutorialsAreUnchangedAndIdempotent()
        {
            // 教学 3 张静态锚点：属性幂等缓存（引用相等），数值零差异
            Assert.That(InitialPersonaCatalog.Accumulator, Is.SameAs(InitialPersonaCatalog.Accumulator));
            Assert.That(InitialPersonaCatalog.Executor, Is.SameAs(InitialPersonaCatalog.Executor));
            Assert.That(InitialPersonaCatalog.Ambitious, Is.SameAs(InitialPersonaCatalog.Ambitious));

            Assert.That(InitialPersonaCatalog.Accumulator.TemplateId, Is.EqualTo("persona.initial.accumulator"));
            Assert.That(InitialPersonaCatalog.Accumulator.EffectValue, Is.EqualTo(15m));
            Assert.That(InitialPersonaCatalog.Executor.EffectValue, Is.EqualTo(2m));
            Assert.That(InitialPersonaCatalog.Ambitious.EffectValue, Is.EqualTo(1.10m));

            // CreateDefaultLoadout：3 教学 + 空槽 4，槽内引用 = 静态属性实例
            var loadout = InitialPersonaCatalog.CreateDefaultLoadout();
            Assert.That(loadout.Slots.Count, Is.EqualTo(4));
            Assert.That(loadout.Slots[0].Definition, Is.SameAs(InitialPersonaCatalog.Accumulator));
            Assert.That(loadout.Slots[1].Definition, Is.SameAs(InitialPersonaCatalog.Executor));
            Assert.That(loadout.Slots[2].Definition, Is.SameAs(InitialPersonaCatalog.Ambitious));
            Assert.That(loadout.Slots[3].Definition, Is.Null);
        }

        [Test]
        public void ConfigureNullFallsBackToEmptyTemplates()
        {
            InitialPersonaCatalog.Configure(null);

            Assert.That(InitialPersonaCatalog.Templates, Is.Empty);
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.Null);
            Assert.That(InitialPersonaCatalog.TryFind("PER_001", out _), Is.False);
            // 白盒教学卡不受影响
            Assert.That(InitialPersonaCatalog.Accumulator, Is.Not.Null);
        }

        [Test]
        public void ConfigureEmptyAssetFallsBackToEmptyTemplates()
        {
            var empty = ScriptableObject.CreateInstance<PersonaConfigAsset>();

            InitialPersonaCatalog.Configure(empty.entries);

            Assert.That(InitialPersonaCatalog.Templates, Is.Empty);
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void Configure16EntriesMakesAllPersonasFindableAndSummarySet()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);

            Assert.That(InitialPersonaCatalog.Templates.Count, Is.EqualTo(16));
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.EqualTo("16 张人格牌模板已加载。"));

            for (var index = 1; index <= 16; index++)
            {
                Assert.That(InitialPersonaCatalog.TryFind($"PER_{index:D3}", out _), Is.True);
            }
            Assert.That(InitialPersonaCatalog.TryFind("PER_017", out _), Is.False);
        }

        [Test]
        public void ConfiguredTemplatesMatchTableFieldByField()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);

            // PER_001：与上一手牌型相同 / 等于 / 阈值 1 / 增加筹码 9（默认循环值，锁死防回归）
            Assert.That(InitialPersonaCatalog.TryFind("PER_001", out var per001), Is.True);
            Assert.That(per001.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.SameHandTypeAsPrevious));
            Assert.That(per001.Comparator, Is.EqualTo(PersonaComparator.Equal));
            Assert.That(per001.ConditionThreshold, Is.EqualTo(1));
            Assert.That(per001.EffectType, Is.EqualTo(PersonaEffectType.AddChips));
            Assert.That(per001.EffectParam1, Is.EqualTo(9m));
            Assert.That(per001.EffectCap, Is.Null);

            // PER_007：每单位增加倍率 0.1，上限 0.7
            Assert.That(InitialPersonaCatalog.TryFind("PER_007", out var per007), Is.True);
            Assert.That(per007.EffectType, Is.EqualTo(PersonaEffectType.PerUnitMultiplier));
            Assert.That(per007.EffectParam1, Is.EqualTo(0.1m));
            Assert.That(per007.EffectCap, Is.EqualTo(0.7m));

            // PER_013：异质 / 牌库数量 <= 30 / 附加条件存原文（带星号未定稿）/ 浮点原文精确保存
            Assert.That(InitialPersonaCatalog.TryFind("PER_013", out var per013), Is.True);
            Assert.That(per013.Quality, Is.EqualTo(PersonaQuality.Mutant));
            Assert.That(per013.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.DeckSize));
            Assert.That(per013.Comparator, Is.EqualTo(PersonaComparator.LessOrEqual));
            Assert.That(per013.ConditionThreshold, Is.EqualTo(30));
            Assert.That(per013.ExtraCondition, Is.Null);
            Assert.That(per013.ExtraConditionRaw, Is.EqualTo("另有计分牌数量条件*"));
            Assert.That(per013.EffectParam1, Is.EqualTo(2.4500000000000002m));

            // PER_015：附加条件结构化（剩余弃牌次数 = 0）
            Assert.That(InitialPersonaCatalog.TryFind("PER_015", out var per015), Is.True);
            Assert.That(per015.ExtraCondition, Is.Not.Null);
            Assert.That(per015.ExtraCondition.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.DiscardsRemaining));
            Assert.That(per015.ExtraCondition.Comparator, Is.EqualTo(PersonaComparator.Equal));
            Assert.That(per015.ExtraCondition.Threshold, Is.EqualTo(0));
            Assert.That(per015.ExtraConditionRaw, Is.EqualTo("剩余弃牌次数=0"));

            // PER_016：人格触发次数 >= 3 / 独立结算 = 是
            Assert.That(InitialPersonaCatalog.TryFind("PER_016", out var per016), Is.True);
            Assert.That(per016.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.PersonaTriggerCount));
            Assert.That(per016.ConditionThreshold, Is.EqualTo(3));
            Assert.That(per016.IndependentSettlement, Is.True);
        }

        [Test]
        public void BadEntryFallsBackWithoutPartialState()
        {
            var asset = BuildValidAsset();
            asset.entries[4].effect = "翻倍筹码"; // 第 5 条未知效果（资产 Validate 会拦，门面防御性兜底）

            InitialPersonaCatalog.Configure(asset.entries);

            // 整体回落空目录，不是 4 条半状态
            Assert.That(InitialPersonaCatalog.Templates, Is.Empty);
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void ConfigureNullClearsPreviousTemplates()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);
            Assert.That(InitialPersonaCatalog.Templates.Count, Is.EqualTo(16));

            InitialPersonaCatalog.Configure(null);

            Assert.That(InitialPersonaCatalog.Templates, Is.Empty);
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.Null);
        }

        [Test]
        public void MappingTablesMatchContractConstants()
        {
            // 交叉校验防漂移：门面转换表（Battle）与契约常量（Data）逐文本逐序一致，
            // 且枚举 int 值从 1 起按序（防序列化 0 值误读）
            for (var index = 0; index < PersonaTableContract.QualityValues.Length; index++)
            {
                Assert.That(PersonaCardTemplate.TryMapQuality(PersonaTableContract.QualityValues[index], out var quality), Is.True);
                Assert.That((int)quality, Is.EqualTo(index + 1));
            }
            for (var index = 0; index < PersonaTableContract.TriggerValues.Length; index++)
            {
                Assert.That(PersonaCardTemplate.TryMapTrigger(PersonaTableContract.TriggerValues[index], out var trigger), Is.True);
                Assert.That((int)trigger, Is.EqualTo(index + 1));
            }
            for (var index = 0; index < PersonaTableContract.ComparatorValues.Length; index++)
            {
                Assert.That(PersonaCardTemplate.TryMapComparator(PersonaTableContract.ComparatorValues[index], out var comparator), Is.True);
                Assert.That((int)comparator, Is.EqualTo(index + 1));
            }
            for (var index = 0; index < PersonaTableContract.EffectValues.Length; index++)
            {
                Assert.That(PersonaCardTemplate.TryMapEffectType(PersonaTableContract.EffectValues[index], out var effect), Is.True);
                Assert.That((int)effect, Is.EqualTo(index + 1));
            }

            // 反向：契约集合外的文本必须映射失败（「特殊」兼容映射只在 Data 层 Mapper）
            Assert.That(PersonaCardTemplate.TryMapQuality("特殊", out _), Is.False);
            Assert.That(PersonaCardTemplate.TryMapTrigger("心情好", out _), Is.False);
            Assert.That(PersonaCardTemplate.TryMapComparator("大约", out _), Is.False);
            Assert.That(PersonaCardTemplate.TryMapEffectType("翻倍筹码", out _), Is.False);
        }

        [Test]
        public void ValidateRejectsDuplicatePersonaIdAndAllowsEmptyAsset()
        {
            // 空条目资产 = 白盒合法（空模板目录语义）
            var empty = ScriptableObject.CreateInstance<PersonaConfigAsset>();
            Assert.That(empty.Validate(out _), Is.True);

            // 重复 personaId → 校验失败（接线层据此回落白盒，门面不重复校验）
            var asset = BuildValidAsset();
            asset.entries.Add(asset.entries[0]);
            Assert.That(asset.Validate(out var error), Is.False);
            Assert.That(error, Does.Contain("重复"));
        }

        [Test]
        public void TryFindUnknownIdReturnsFalseWhenConfigured()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);

            Assert.That(InitialPersonaCatalog.TryFind("不存在的牌", out var template), Is.False);
            Assert.That(template, Is.Null);
        }
    }
}
