using NUnit.Framework;
using PersonaCards.Battle.Personas;
using PersonaCards.Data;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// InitialPersonaCatalog 门面测试（P0-1E）：
    /// 白盒回落 = 空模板目录 + 教学 3 张零差异；Configure 注入 8 条目后 TryFind 全命中；
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

        /// <summary>新版 8 列引用式结构 8 张基础人格牌：(id, 名称, 触发, 比较符, 阈值, 条件参数, 效果, 参数1)。</summary>
        private static readonly (string, string, string, string, string, string, string, string)[] FixtureEntries =
        {
            ("PER_001", "终局观察者", "连续使用相同牌型次数", "等于", "2", "", "增加筹码", "15"),
            ("PER_002", "克制的赌徒", "计分牌数量", "大于等于", "4", "", "增加倍率", "1"),
            ("PER_003", "结构收藏家", "牌型品质", "等于", "", "NORMAL", "增加筹码", "40"),
            ("PER_004", "隐境寻路者", "已使用弃牌次数", "等于", "0", "", "增加筹码", "30"),
            ("PER_005", "断舍离者",   "弃牌后出牌", "等于", "1", "", "增加倍率", "1"),
            ("PER_006", "善变漫游者", "连续使用不同牌型次数", "不等于", "2", "", "增加筹码", "20"),
            ("PER_007", "留手谋划者", "出牌数量", "小于等于", "4", "", "增加筹码", "20"),
            ("PER_008", "满手承诺者", "牌型品质", "等于", "", "RARE", "增加独立倍率", "0.05"),
        };

        /// <summary>构建合法 8 条目资产（与新版配表逐字段一致；新版无列字段按 Mapper 默认值）。</summary>
        private static PersonaConfigAsset BuildValidAsset()
        {
            var asset = ScriptableObject.CreateInstance<PersonaConfigAsset>();
            for (var index = 0; index < FixtureEntries.Length; index++)
            {
                var (id, name, trigger, comparator, threshold, conditionParam, effect, effectParam1) = FixtureEntries[index];
                asset.entries.Add(new PersonaConfigEntry
                {
                    personaId = id,
                    displayName = name,
                    quality = "基础",
                    qualityParam = "",
                    behaviorTagId = "",
                    trigger = trigger,
                    comparator = comparator,
                    threshold = threshold,
                    conditionParam = conditionParam,
                    subAttributeId = $"SUB_{index * 5 + 1:D3}",
                    maxAttributeCount = "3",
                    maxSubAttributeCount = "2",
                    subAttributePoolCount = "5",
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
        public void Configure8EntriesMakesAllPersonasFindableAndSummarySet()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);

            Assert.That(InitialPersonaCatalog.Templates.Count, Is.EqualTo(8));
            Assert.That(InitialPersonaCatalog.LastConfiguredSummary, Is.EqualTo("8 张人格牌模板已加载。"));

            for (var index = 1; index <= 8; index++)
            {
                Assert.That(InitialPersonaCatalog.TryFind($"PER_{index:D3}", out _), Is.True);
            }
            Assert.That(InitialPersonaCatalog.TryFind("PER_009", out _), Is.False);
        }

        [Test]
        public void ConfiguredTemplatesMatchTableFieldByField()
        {
            InitialPersonaCatalog.Configure(BuildValidAsset().entries);

            // PER_001：连续使用相同牌型次数 = 2 / 增加筹码 15
            Assert.That(InitialPersonaCatalog.TryFind("PER_001", out var per001), Is.True);
            Assert.That(per001.DisplayName, Is.EqualTo("终局观察者"));
            Assert.That(per001.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.SameHandTypeStreak));
            Assert.That(per001.Comparator, Is.EqualTo(PersonaComparator.Equal));
            Assert.That(per001.ConditionThreshold, Is.EqualTo(2));
            Assert.That(per001.ConditionParam, Is.EqualTo(""));
            Assert.That(per001.EffectType, Is.EqualTo(PersonaEffectType.AddChips));
            Assert.That(per001.EffectParam1, Is.EqualTo(15m));

            // PER_003：牌型品质 = NORMAL（无阈值，条件参数承载品质文本）
            Assert.That(InitialPersonaCatalog.TryFind("PER_003", out var per003), Is.True);
            Assert.That(per003.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.HandTypeQuality));
            Assert.That(per003.ConditionThreshold, Is.Null);
            Assert.That(per003.ConditionParam, Is.EqualTo("NORMAL"));

            // PER_005：弃牌后出牌 = 1
            Assert.That(InitialPersonaCatalog.TryFind("PER_005", out var per005), Is.True);
            Assert.That(per005.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.AfterDiscardPlay));
            Assert.That(per005.ConditionThreshold, Is.EqualTo(1));

            // PER_006：连续使用不同牌型次数 != 2（新比较符「不等于」）
            Assert.That(InitialPersonaCatalog.TryFind("PER_006", out var per006), Is.True);
            Assert.That(per006.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.DifferentHandTypeStreak));
            Assert.That(per006.Comparator, Is.EqualTo(PersonaComparator.NotEqual));
            Assert.That(per006.ConditionThreshold, Is.EqualTo(2));

            // PER_007：出牌数量 <= 4
            Assert.That(InitialPersonaCatalog.TryFind("PER_007", out var per007), Is.True);
            Assert.That(per007.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.SubmittedCardCount));
            Assert.That(per007.Comparator, Is.EqualTo(PersonaComparator.LessOrEqual));
            Assert.That(per007.ConditionThreshold, Is.EqualTo(4));

            // PER_008：牌型品质 = RARE + 增加独立倍率 0.05（新效果，decimal 原文精确保存）
            Assert.That(InitialPersonaCatalog.TryFind("PER_008", out var per008), Is.True);
            Assert.That(per008.TriggerCondition, Is.EqualTo(PersonaTriggerCondition.HandTypeQuality));
            Assert.That(per008.ConditionParam, Is.EqualTo("RARE"));
            Assert.That(per008.EffectType, Is.EqualTo(PersonaEffectType.AddIndependentMultiplier));
            Assert.That(per008.EffectParam1, Is.EqualTo(0.05m));
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
            Assert.That(InitialPersonaCatalog.Templates.Count, Is.EqualTo(8));

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
        public void ValidateRejectsConditionParamMismatch()
        {
            // 品质类触发缺条件参数 → 校验失败
            var missingParam = BuildValidAsset();
            missingParam.entries[2].conditionParam = "";
            Assert.That(missingParam.Validate(out var error), Is.False);
            Assert.That(error, Does.Contain("条件参数"));

            // 非品质类触发带条件参数 → 校验失败
            var strayParam = BuildValidAsset();
            strayParam.entries[0].conditionParam = "NORMAL";
            Assert.That(strayParam.Validate(out var error2), Is.False);
            Assert.That(error2, Does.Contain("必须为空"));

            // 条件参数值不在品质等级值域 → 校验失败
            var badValue = BuildValidAsset();
            badValue.entries[2].conditionParam = "LEGENDARY";
            Assert.That(badValue.Validate(out var error3), Is.False);
            Assert.That(error3, Does.Contain("LEGENDARY"));
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
