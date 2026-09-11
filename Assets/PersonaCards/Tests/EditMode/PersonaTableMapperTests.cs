using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// PersonaTableMapper 测试。夹具 = Docs/人格牌.xlsx 新版 8 列引用式结构的 3 个 sheet 手写快照
    /// （「人格牌配置」8 行 + 「人格牌_词条」8 行 + 「人格牌_主属性」8 行），与配表当前内容逐字段一致。
    /// 配表改版时：先同步夹具快照，再按契约预期调整断言（契约变更需策划确认）。
    /// </summary>
    public class PersonaTableMapperTests
    {
        /// <summary>配置表 8 行快照：(id, 名称, 词条_ID, 主属性_ID, 次级属性_ID, 最大属性数量, 最大次级属性数量, 次级属性池数量)。</summary>
        private static readonly (string, string, string, string, string, string, string, string)[] FixtureConfigRows =
        {
            ("PER_001", "终局观察者", "ENTRY_001", "MAIN_001", "SUB_001", "3", "2", "5"),
            ("PER_002", "克制的赌徒", "ENTRY_002", "MAIN_002", "SUB_006", "3", "2", "5"),
            ("PER_003", "结构收藏家", "ENTRY_003", "MAIN_003", "SUB_011", "3", "2", "5"),
            ("PER_004", "隐境寻路者", "ENTRY_004", "MAIN_004", "SUB_016", "3", "2", "5"),
            ("PER_005", "断舍离者",   "ENTRY_005", "MAIN_005", "SUB_021", "3", "2", "5"),
            ("PER_006", "善变漫游者", "ENTRY_006", "MAIN_006", "SUB_026", "3", "2", "5"),
            ("PER_007", "留手谋划者", "ENTRY_007", "MAIN_007", "SUB_031", "3", "2", "5"),
            ("PER_008", "满手承诺者", "ENTRY_008", "MAIN_008", "SUB_036", "3", "2", "5"),
        };

        /// <summary>词条表 8 行快照：(词条_ID, 条件类型, 比较符, 条件参数)。</summary>
        private static readonly (string, string, string, string)[] FixtureEntryRows =
        {
            ("ENTRY_001", "连续牌型", "EQ", "2"),
            ("ENTRY_002", "计分牌数量", "GTE", "4"),
            ("ENTRY_003", "牌型品质", "EQ", "NORMAL"),
            ("ENTRY_004", "弃牌次数", "EQ", "0"),
            ("ENTRY_005", "弃牌后出牌", "EQ", "1"),
            ("ENTRY_006", "连续牌型", "NEQ", "2"),
            ("ENTRY_007", "出牌数量", "LTE", "4"),
            ("ENTRY_008", "牌型品质", "EQ", "RARE"),
        };

        /// <summary>主属性表 8 行快照：(主属性_ID, 属性类型, 属性参数1, 属性参数2)。</summary>
        private static readonly (string, string, string, string)[] FixtureMainAttrRows =
        {
            ("MAIN_001", "基础筹码", "增加", "15"),
            ("MAIN_002", "基础倍率", "增加", "1"),
            ("MAIN_003", "基础筹码", "增加", "40"),
            ("MAIN_004", "基础筹码", "增加", "30"),
            ("MAIN_005", "基础倍率", "增加", "1"),
            ("MAIN_006", "基础筹码", "增加", "20"),
            ("MAIN_007", "基础筹码", "增加", "20"),
            ("MAIN_008", "独立倍率", "增加", "0.05"),
        };

        /// <summary>PER_001~008 图片配置绑定 ID 集合（8 张基础人格牌图均已在图片配置中）。</summary>
        private static List<string> AllImageBindingIds() =>
            Enumerable.Range(1, 8).Select(index => $"PER_{index:D3}").ToList();

        /// <summary>配置快照 → 行字典列表（表头名取契约常量）。</summary>
        private static List<Dictionary<string, string>> ConfigRows()
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var (id, name, entryId, mainAttrId, subAttrId, maxAttr, maxSubAttr, poolCount) in FixtureConfigRows)
            {
                rows.Add(new Dictionary<string, string>
                {
                    [PersonaTableContract.ColPersonaId] = id,
                    [PersonaTableContract.ColName] = name,
                    [PersonaTableContract.ColEntryId] = entryId,
                    [PersonaTableContract.ColMainAttrId] = mainAttrId,
                    [PersonaTableContract.ColSubAttrId] = subAttrId,
                    [PersonaTableContract.ColMaxAttrCount] = maxAttr,
                    [PersonaTableContract.ColMaxSubAttrCount] = maxSubAttr,
                    [PersonaTableContract.ColSubAttrPoolCount] = poolCount,
                });
            }
            return rows;
        }

        /// <summary>词条快照 → 行字典列表。</summary>
        private static List<Dictionary<string, string>> EntryRows()
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var (id, conditionType, comparator, param) in FixtureEntryRows)
            {
                rows.Add(new Dictionary<string, string>
                {
                    [PersonaTableContract.ColEntryIdOfEntries] = id,
                    [PersonaTableContract.ColEntryConditionType] = conditionType,
                    [PersonaTableContract.ColEntryComparator] = comparator,
                    [PersonaTableContract.ColEntryParam] = param,
                });
            }
            return rows;
        }

        /// <summary>主属性快照 → 行字典列表。</summary>
        private static List<Dictionary<string, string>> MainAttrRows()
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var (id, type, param1, param2) in FixtureMainAttrRows)
            {
                rows.Add(new Dictionary<string, string>
                {
                    [PersonaTableContract.ColMainAttrIdOfMainAttrs] = id,
                    [PersonaTableContract.ColMainAttrType] = type,
                    [PersonaTableContract.ColMainAttrParam1] = param1,
                    [PersonaTableContract.ColMainAttrParam2] = param2,
                });
            }
            return rows;
        }

        /// <summary>按人格牌_ID 找配置行（测试内改动单个单元格用）。</summary>
        private static Dictionary<string, string> FindRow(List<Dictionary<string, string>> rows, string personaId) =>
            rows.First(row => row[PersonaTableContract.ColPersonaId] == personaId);

        /// <summary>按词条_ID 找词条行。</summary>
        private static Dictionary<string, string> FindEntryRow(List<Dictionary<string, string>> rows, string entryId) =>
            rows.First(row => row[PersonaTableContract.ColEntryIdOfEntries] == entryId);

        /// <summary>按主属性_ID 找主属性行。</summary>
        private static Dictionary<string, string> FindMainAttrRow(List<Dictionary<string, string>> rows, string mainAttrId) =>
            rows.First(row => row[PersonaTableContract.ColMainAttrIdOfMainAttrs] == mainAttrId);

        [Test]
        public void MapsAll8RowsTranslatesReferencesAndSortsById()
        {
            // 输入故意乱序（快照序反过来），验证输出按 ID 升序
            var configRows = ConfigRows();
            configRows.Reverse();

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Errors, Is.Empty);
            // 警告仅 8 条：PER_009~016 待策划补充
            Assert.That(result.Warnings.Count, Is.EqualTo(8));
            Assert.That(result.Warnings, Has.All.Matches<string>(w => w.Contains("待策划补充")));

            Assert.That(result.Entries.Count, Is.EqualTo(8));
            Assert.That(result.Entries[0].personaId, Is.EqualTo("PER_001"));
            Assert.That(result.Entries[7].personaId, Is.EqualTo("PER_008"));

            // PER_001：连续牌型 EQ 2 → 连续使用相同牌型次数/等于/2；基础筹码 15 → 增加筹码 15
            var per001 = result.Entries[0];
            Assert.That(per001.displayName, Is.EqualTo("终局观察者"));
            Assert.That(per001.trigger, Is.EqualTo(PersonaTableContract.TriggerSameHandTypeStreak));
            Assert.That(per001.comparator, Is.EqualTo(PersonaTableContract.ComparatorEqual));
            Assert.That(per001.threshold, Is.EqualTo("2"));
            Assert.That(per001.conditionParam, Is.EqualTo(""));
            Assert.That(per001.effect, Is.EqualTo(PersonaTableContract.EffectAddChips));
            Assert.That(per001.effectParam1, Is.EqualTo("15"));
            // 新版无列默认值：基础品质/无标签/无附加/参数2=0/无上限/非独立结算
            Assert.That(per001.quality, Is.EqualTo(PersonaTableContract.QualityBasic));
            Assert.That(per001.behaviorTagId, Is.EqualTo(""));
            Assert.That(per001.extraConditionRaw, Is.EqualTo(""));
            Assert.That(per001.effectParam2, Is.EqualTo("0"));
            Assert.That(per001.effectCap, Is.EqualTo(""));
            Assert.That(per001.independentSettlement, Is.False);
            // 新版 8 列结构次级属性相关列照抄
            Assert.That(per001.subAttributeId, Is.EqualTo("SUB_001"));
            Assert.That(per001.maxAttributeCount, Is.EqualTo("3"));
            Assert.That(per001.maxSubAttributeCount, Is.EqualTo("2"));
            Assert.That(per001.subAttributePoolCount, Is.EqualTo("5"));

            // PER_003：牌型品质 EQ NORMAL → 牌型品质/等于/无阈值/条件参数 NORMAL
            var per003 = result.Entries[2];
            Assert.That(per003.trigger, Is.EqualTo(PersonaTableContract.TriggerHandTypeQuality));
            Assert.That(per003.threshold, Is.EqualTo(""));
            Assert.That(per003.conditionParam, Is.EqualTo(PersonaTableContract.QualityParamNormal));
            Assert.That(per003.effectParam1, Is.EqualTo("40"));

            // PER_005：弃牌后出牌 EQ 1 → 弃牌后出牌/等于/1
            var per005 = result.Entries[4];
            Assert.That(per005.trigger, Is.EqualTo(PersonaTableContract.TriggerAfterDiscardPlay));
            Assert.That(per005.comparator, Is.EqualTo(PersonaTableContract.ComparatorEqual));
            Assert.That(per005.threshold, Is.EqualTo("1"));

            // PER_006：连续牌型 NEQ 2 → 连续使用不同牌型次数/不等于/2
            var per006 = result.Entries[5];
            Assert.That(per006.trigger, Is.EqualTo(PersonaTableContract.TriggerDifferentHandTypeStreak));
            Assert.That(per006.comparator, Is.EqualTo(PersonaTableContract.ComparatorNotEqual));
            Assert.That(per006.threshold, Is.EqualTo("2"));

            // PER_007：出牌数量 LTE 4 → 出牌数量/小于等于/4
            var per007 = result.Entries[6];
            Assert.That(per007.trigger, Is.EqualTo(PersonaTableContract.TriggerSubmittedCardCount));
            Assert.That(per007.comparator, Is.EqualTo(PersonaTableContract.ComparatorLessOrEqual));
            Assert.That(per007.threshold, Is.EqualTo("4"));

            // PER_008：牌型品质 EQ RARE + 独立倍率 0.05 → 增加独立倍率（decimal 原文精确保存）
            var per008 = result.Entries[7];
            Assert.That(per008.trigger, Is.EqualTo(PersonaTableContract.TriggerHandTypeQuality));
            Assert.That(per008.conditionParam, Is.EqualTo(PersonaTableContract.QualityParamRare));
            Assert.That(per008.effect, Is.EqualTo(PersonaTableContract.EffectAddIndependentMultiplier));
            Assert.That(per008.effectParam1, Is.EqualTo("0.05"));
        }

        [Test]
        public void NoWarningsWhenPer009To016Present()
        {
            var configRows = ConfigRows();
            // 补 PER_009~016（复用 PER_001 的引用即可，仅验证齐全检查的警告消失）；图片绑定集合同步扩到 16
            var per001 = FindRow(configRows, "PER_001");
            var imageIds = Enumerable.Range(1, 16).Select(index => $"PER_{index:D3}").ToList();
            for (var index = 9; index <= 16; index++)
            {
                configRows.Add(new Dictionary<string, string>(per001) { [PersonaTableContract.ColPersonaId] = $"PER_{index:D3}" });
            }

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), imageIds);

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Entries.Count, Is.EqualTo(16));
        }

        [Test]
        public void RejectsUnknownConditionTypeMapping()
        {
            var entryRows = EntryRows();
            FindEntryRow(entryRows, "ENTRY_001")[PersonaTableContract.ColEntryConditionType] = "心情好";

            var result = PersonaTableMapper.Map(ConfigRows(), entryRows, MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("ENTRY_001") && e.Contains("组合未收录")));
        }

        [Test]
        public void RejectsUnsupportedComparatorSymbol()
        {
            // GT 不在词条表支持符号内（EQ/NEQ/GTE/LTE）→ 组合未收录
            var entryRows = EntryRows();
            FindEntryRow(entryRows, "ENTRY_002")[PersonaTableContract.ColEntryComparator] = "GT";

            var result = PersonaTableMapper.Map(ConfigRows(), entryRows, MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("ENTRY_002") && e.Contains("组合未收录")));
        }

        [Test]
        public void RejectsEntryIdNotInEntrySheet()
        {
            var configRows = ConfigRows();
            FindRow(configRows, "PER_001")[PersonaTableContract.ColEntryId] = "ENTRY_999";

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("ENTRY_999") && e.Contains("不存在")));
        }

        [Test]
        public void RejectsMainAttrIdNotInMainAttrSheet()
        {
            var configRows = ConfigRows();
            FindRow(configRows, "PER_002")[PersonaTableContract.ColMainAttrId] = "MAIN_999";

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_002") && e.Contains("MAIN_999") && e.Contains("不存在")));
        }

        [Test]
        public void RejectsBadQualityParam()
        {
            var entryRows = EntryRows();
            FindEntryRow(entryRows, "ENTRY_003")[PersonaTableContract.ColEntryParam] = "LEGENDARY";

            var result = PersonaTableMapper.Map(ConfigRows(), entryRows, MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("ENTRY_003") && e.Contains("品质等级")));
        }

        [Test]
        public void RejectsNonIntegerConditionParam()
        {
            var entryRows = EntryRows();
            FindEntryRow(entryRows, "ENTRY_001")[PersonaTableContract.ColEntryParam] = "abc";

            var result = PersonaTableMapper.Map(ConfigRows(), entryRows, MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("ENTRY_001") && e.Contains("非负整数")));
        }

        [Test]
        public void RejectsNegativeEffectParam()
        {
            var mainAttrRows = MainAttrRows();
            FindMainAttrRow(mainAttrRows, "MAIN_001")[PersonaTableContract.ColMainAttrParam2] = "-1";

            var result = PersonaTableMapper.Map(ConfigRows(), EntryRows(), mainAttrRows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("MAIN_001") && e.Contains("非负数字")));
        }

        [Test]
        public void RejectsUnknownMainAttrMapping()
        {
            var mainAttrRows = MainAttrRows();
            FindMainAttrRow(mainAttrRows, "MAIN_002")[PersonaTableContract.ColMainAttrType] = "暴击率";

            var result = PersonaTableMapper.Map(ConfigRows(), EntryRows(), mainAttrRows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("MAIN_002") && e.Contains("组合未收录")));
        }

        [Test]
        public void CollectsAllRowErrorsWithoutFailFast()
        {
            var configRows = ConfigRows();
            FindRow(configRows, "PER_001")[PersonaTableContract.ColName] = "";
            FindRow(configRows, "PER_002")[PersonaTableContract.ColEntryId] = "";

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("名称")));
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_002") && e.Contains("词条_ID") && e.Contains("为空")));
        }

        [Test]
        public void RejectsDuplicatePersonaId()
        {
            var configRows = ConfigRows();
            configRows.Add(configRows[0]); // 复制 PER_001 行（行对象共享引用，Map 只读不改行）

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("重复")));
        }

        [Test]
        public void RejectsEmptyPersonaId()
        {
            var configRows = ConfigRows();
            FindRow(configRows, "PER_001")[PersonaTableContract.ColPersonaId] = "";

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("人格牌_ID」为空")));
        }

        [Test]
        public void RejectsDuplicateEntryId()
        {
            var entryRows = EntryRows();
            entryRows.Add(entryRows[0]); // 复制 ENTRY_001 行

            var result = PersonaTableMapper.Map(ConfigRows(), entryRows, MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("ENTRY_001") && e.Contains("重复")));
        }

        [Test]
        public void RejectsDuplicateMainAttrId()
        {
            var mainAttrRows = MainAttrRows();
            mainAttrRows.Add(mainAttrRows[0]); // 复制 MAIN_001 行

            var result = PersonaTableMapper.Map(ConfigRows(), EntryRows(), mainAttrRows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("MAIN_001") && e.Contains("重复")));
        }

        [Test]
        public void ReportsMissingPerIdButAllowsExtraIds()
        {
            var configRows = ConfigRows();
            configRows.RemoveAll(row => row[PersonaTableContract.ColPersonaId] == "PER_005");
            // 多出 PER_017（复制 PER_001 行改 ID）——卡池可扩展，允许
            var extra = FindRow(configRows, "PER_001");
            configRows.Add(new Dictionary<string, string>(extra) { [PersonaTableContract.ColPersonaId] = "PER_017" });

            var result = PersonaTableMapper.Map(configRows, EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("缺少 PER_005")));
            Assert.That(result.Errors, Has.None.Matches<string>(e => e.Contains("PER_017")));
        }

        [Test]
        public void WarnsButDoesNotBlockWhenIdMissingFromImageBindings()
        {
            var imageIds = AllImageBindingIds();
            imageIds.Remove("PER_008");

            var result = PersonaTableMapper.Map(ConfigRows(), EntryRows(), MainAttrRows(), imageIds);

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Warnings, Has.Some.Matches<string>(w => w.Contains("PER_008") && w.Contains("绑定ID")));
        }

        [Test]
        public void RejectsEmptyConfigTable()
        {
            var result = PersonaTableMapper.Map(
                new List<Dictionary<string, string>>(), EntryRows(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("人格牌配置表没有任何数据行")));
        }

        [Test]
        public void RejectsEmptyEntryTable()
        {
            var result = PersonaTableMapper.Map(
                ConfigRows(), new List<Dictionary<string, string>>(), MainAttrRows(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("词条表没有任何数据行")));
        }

        [Test]
        public void RejectsEmptyMainAttrTable()
        {
            var result = PersonaTableMapper.Map(
                ConfigRows(), EntryRows(), new List<Dictionary<string, string>>(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("主属性表没有任何数据行")));
        }
    }
}
