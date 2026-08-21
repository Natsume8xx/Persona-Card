using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// PersonaTableMapper 测试。夹具 = Docs/人格牌.xlsx「人格牌配置」sheet 16 行手写快照（2026-08-21 版），
    /// 与配表当前内容逐字段一致（名称占位、品质「特殊」旧写法、PER_013 附加条件带星号均按原样）。
    /// 配表改版时：先同步夹具快照，再按契约预期调整断言（契约变更需策划确认）。
    /// </summary>
    public class PersonaTableMapperTests
    {
        /// <summary>16 行快照：(id, 名称, 品质, 品质参数, 标签, 触发条件, 比较符, 阈值, 附加条件, 效果, 参数1, 参数2, 效果原文, 上限, 独立结算)。</summary>
        private static readonly (string, string, string, string, string, string, string, string, string, string, string, string, string, string, string)[] FixtureRows =
        {
            ("PER_001", "111111（暂定", "基础", "白色", "T01", "与上一手牌型相同", "等于", "1", "", "增加筹码", "9", "0", "", "", "否"),
            ("PER_002", "111112（暂定", "基础", "白色", "T02", "计分牌数量", "大于等于", "4", "", "增加倍率", "0.9", "0", "", "", "否"),
            ("PER_003", "111113（暂定", "基础", "白色", "T03", "已使用弃牌次数", "等于", "0", "", "增加筹码和倍率", "5", "0.2", "", "", "否"),
            ("PER_004", "111114（暂定", "基础", "白色", "T04", "命中AI偏好", "等于", "1", "", "增加筹码", "6", "0", "", "", "否"),
            ("PER_005", "111115（暂定", "进阶", "绿色", "T05", "与上一手牌型相同", "等于", "1", "", "增加倍率", "1.25", "0", "", "", "否"),
            ("PER_006", "111116（暂定", "进阶", "绿色", "T06", "剩余弃牌次数", "大于等于", "1", "", "每单位增加倍率", "0.25", "0", "", "", "否"),
            ("PER_007", "111117（暂定", "进阶", "绿色", "T07", "本局移除牌数量", "大于等于", "1", "", "每单位增加倍率", "0.1", "0", "", "0.7", "否"),
            ("PER_008", "111118（暂定", "进阶", "绿色", "T08", "本局新增牌数量", "大于等于", "1", "", "每单位增加筹码", "1", "0", "", "8", "否"),
            ("PER_009", "111119（暂定", "稀有", "蓝色", "T09", "连续使用相同牌型次数", "大于等于", "3", "", "最终倍率乘算", "1.42", "0", "", "", "是"),
            ("PER_010", "111120（暂定", "稀有", "蓝色", "T10", "牌库数量", "小于", "40", "", "增加倍率", "0.78", "0", "", "", "否"),
            ("PER_011", "111121（暂定", "稀有", "蓝色", "T11", "其他人格触发次数", "大于等于", "2", "", "最终倍率乘算", "1.36", "0", "", "", "是"),
            ("PER_012", "111122（暂定", "稀有", "蓝色", "T12", "计分牌数量", "等于", "5", "", "最终倍率乘算", "1.42", "0", "", "", "是"),
            ("PER_013", "111123（暂定", "特殊", "金色", "T13", "牌库数量", "小于等于", "30", "另有计分牌数量条件*", "最终倍率乘算", "2.4500000000000002", "0", "", "", "是"),
            ("PER_014", "111124（暂定", "特殊", "金色", "T14", "连续使用相同牌型次数", "大于等于", "4", "", "最终倍率乘算", "3", "0", "", "", "是"),
            ("PER_015", "111125（暂定", "特殊", "金色", "T15", "剩余出牌次数", "等于", "1", "剩余弃牌次数=0", "最终倍率乘算", "2.1", "0", "", "", "是"),
            ("PER_016", "111126（暂定", "特殊", "金色", "T16", "人格触发次数", "大于等于", "3", "", "最终倍率乘算", "2.2000000000000002", "0", "", "", "是"),
        };

        /// <summary>PER_001~016 图片配置绑定 ID 集合（16 张人格牌图均已在图片配置中）。</summary>
        private static List<string> AllImageBindingIds() =>
            Enumerable.Range(1, 16).Select(index => $"PER_{index:D3}").ToList();

        /// <summary>快照 → 行字典列表（表头名取契约常量）。</summary>
        private static List<Dictionary<string, string>> Rows()
        {
            var rows = new List<Dictionary<string, string>>();
            foreach (var (id, name, quality, qualityParam, tag, trigger, comparator, threshold, extra, effect, param1, param2, effectRaw, cap, independent) in FixtureRows)
            {
                rows.Add(new Dictionary<string, string>
                {
                    [PersonaTableContract.ColPersonaId] = id,
                    [PersonaTableContract.ColName] = name,
                    [PersonaTableContract.ColQuality] = quality,
                    [PersonaTableContract.ColQualityParam] = qualityParam,
                    [PersonaTableContract.ColBehaviorTag] = tag,
                    [PersonaTableContract.ColTrigger] = trigger,
                    [PersonaTableContract.ColComparator] = comparator,
                    [PersonaTableContract.ColThreshold] = threshold,
                    [PersonaTableContract.ColExtra] = extra,
                    [PersonaTableContract.ColEffect] = effect,
                    [PersonaTableContract.ColEffectParam1] = param1,
                    [PersonaTableContract.ColEffectParam2] = param2,
                    [PersonaTableContract.ColEffectRaw] = effectRaw,
                    [PersonaTableContract.ColEffectCap] = cap,
                    [PersonaTableContract.ColIndependent] = independent,
                });
            }
            return rows;
        }

        /// <summary>按人格牌_ID 找夹具行（测试内改动单个单元格用）。</summary>
        private static Dictionary<string, string> FindRow(List<Dictionary<string, string>> rows, string personaId) =>
            rows.First(row => row[PersonaTableContract.ColPersonaId] == personaId);

        /// <summary>把 PER_013 的附加条件置空（模拟策划补全后的干净状态）。</summary>
        private static List<Dictionary<string, string>> RowsWithCleanExtra()
        {
            var rows = Rows();
            FindRow(rows, "PER_013")[PersonaTableContract.ColExtra] = "";
            return rows;
        }

        [Test]
        public void MapsAll16RowsNormalizesSpecialAndSortsById()
        {
            // 输入故意乱序（快照序反过来），验证输出按 ID 升序
            var rows = Rows();
            rows.Reverse();

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Errors, Is.Empty);
            // 警告仅 5 条：4 条「特殊→异质」规范化 + 1 条 PER_013 附加条件带星号
            Assert.That(result.Warnings.Count, Is.EqualTo(5));
            Assert.That(result.Warnings, Has.Some.Matches<string>(w => w.Contains("PER_013") && w.Contains("附加条件")));
            Assert.That(result.Warnings.Count(w => w.Contains("规范化为「异质」")), Is.EqualTo(4));

            Assert.That(result.Entries.Count, Is.EqualTo(16));
            Assert.That(result.Entries[0].personaId, Is.EqualTo("PER_001"));
            Assert.That(result.Entries[15].personaId, Is.EqualTo("PER_016"));

            // 关键字段抽查：品质「特殊」→「异质」、数值 string 原文精确保存、参数2 空→0、上限空→""
            var per001 = result.Entries[0];
            Assert.That(per001.quality, Is.EqualTo(PersonaTableContract.QualityBasic));
            Assert.That(per001.effectParam2, Is.EqualTo("0"));
            Assert.That(per001.effectCap, Is.EqualTo(""));
            Assert.That(per001.independentSettlement, Is.False);

            var per007 = result.Entries[6];
            Assert.That(per007.effectCap, Is.EqualTo("0.7"));
            Assert.That(per007.effect, Is.EqualTo(PersonaTableContract.EffectPerUnitMultiplier));

            var per013 = result.Entries[12];
            Assert.That(per013.quality, Is.EqualTo(PersonaTableContract.QualityMutant));
            Assert.That(per013.effectParam1, Is.EqualTo("2.4500000000000002"));
            Assert.That(per013.comparator, Is.EqualTo(PersonaTableContract.ComparatorLessOrEqual));

            var per015 = result.Entries[14];
            Assert.That(per015.independentSettlement, Is.True);
            Assert.That(per015.trigger, Is.EqualTo(PersonaTableContract.TriggerPlaysRemaining));
        }

        [Test]
        public void NoWarningsWhenQualityNormalizedAndExtraConditionAbsent()
        {
            var rows = RowsWithCleanExtra();
            foreach (var row in rows) row[PersonaTableContract.ColQuality] = PersonaTableContract.QualityMutant;

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ParsesStructuredExtraConditionAndStoresRawFallback()
        {
            var result = PersonaTableMapper.Map(Rows(), AllImageBindingIds());
            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));

            // PER_015「剩余弃牌次数=0」→ 结构化三字段（触发条件 + 比较符 + 阈值），原文保留
            var per015 = result.Entries.Single(entry => entry.personaId == "PER_015");
            Assert.That(per015.extraTrigger, Is.EqualTo(PersonaTableContract.TriggerDiscardsRemaining));
            Assert.That(per015.extraComparator, Is.EqualTo(PersonaTableContract.ComparatorEqual));
            Assert.That(per015.extraThreshold, Is.EqualTo("0"));
            Assert.That(per015.extraConditionRaw, Is.EqualTo("剩余弃牌次数=0"));

            // PER_013「另有计分牌数量条件*」→ 不可解析，存原文，三字段空
            var per013 = result.Entries.Single(entry => entry.personaId == "PER_013");
            Assert.That(per013.extraConditionRaw, Is.EqualTo("另有计分牌数量条件*"));
            Assert.That(per013.extraTrigger, Is.EqualTo(""));
            Assert.That(per013.extraComparator, Is.EqualTo(""));
            Assert.That(per013.extraThreshold, Is.EqualTo(""));
        }

        [Test]
        public void RejectsUnknownQuality()
        {
            var rows = Rows();
            FindRow(rows, "PER_001")[PersonaTableContract.ColQuality] = "传说";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("品质类型")));
        }

        [Test]
        public void RejectsUnknownTrigger()
        {
            var rows = Rows();
            FindRow(rows, "PER_002")[PersonaTableContract.ColTrigger] = "心情好";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_002") && e.Contains("触发条件")));
        }

        [Test]
        public void RejectsUnknownComparator()
        {
            var rows = Rows();
            FindRow(rows, "PER_003")[PersonaTableContract.ColComparator] = "大约";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_003") && e.Contains("比较符")));
        }

        [Test]
        public void RejectsUnknownEffect()
        {
            var rows = Rows();
            FindRow(rows, "PER_004")[PersonaTableContract.ColEffect] = "翻倍筹码";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_004") && e.Contains("效果类型1")));
        }

        [Test]
        public void RejectsInvalidIndependentSettlement()
        {
            var rows = Rows();
            FindRow(rows, "PER_005")[PersonaTableContract.ColIndependent] = "也许";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_005") && e.Contains("独立结算")));
        }

        [Test]
        public void RejectsNonIntegerThreshold()
        {
            var rows = Rows();
            FindRow(rows, "PER_006")[PersonaTableContract.ColThreshold] = "abc";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_006") && e.Contains("条件阈值")));
        }

        [Test]
        public void RejectsNegativeEffectParam()
        {
            var rows = Rows();
            FindRow(rows, "PER_007")[PersonaTableContract.ColEffectParam1] = "-1";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_007") && e.Contains("效果参数1")));
        }

        [Test]
        public void CollectsAllRowErrorsWithoutFailFast()
        {
            var rows = Rows();
            FindRow(rows, "PER_001")[PersonaTableContract.ColName] = "";
            FindRow(rows, "PER_002")[PersonaTableContract.ColEffectParam2] = "x";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(2));
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("名称")));
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_002") && e.Contains("效果参数2")));
        }

        [Test]
        public void RejectsBadBehaviorTagFormat()
        {
            var rows = Rows();
            FindRow(rows, "PER_008")[PersonaTableContract.ColBehaviorTag] = "T1";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_008") && e.Contains("行为标签_ID")));
        }

        [Test]
        public void RejectsDuplicatePersonaId()
        {
            var rows = Rows();
            rows.Add(rows[0]); // 复制 PER_001 行（行对象共享引用，Map 只读不改行）

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("PER_001") && e.Contains("重复")));
        }

        [Test]
        public void RejectsEmptyPersonaId()
        {
            var rows = Rows();
            FindRow(rows, "PER_001")[PersonaTableContract.ColPersonaId] = "";

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("人格牌_ID」为空")));
        }

        [Test]
        public void ReportsMissingPerIdButAllowsExtraIds()
        {
            var rows = Rows();
            rows.RemoveAll(row => row[PersonaTableContract.ColPersonaId] == "PER_005");
            // 多出 PER_017（复制 PER_016 行改 ID）——卡池可扩展，允许
            var extra = FindRow(rows, "PER_016");
            rows.Add(new Dictionary<string, string>(extra) { [PersonaTableContract.ColPersonaId] = "PER_017" });

            var result = PersonaTableMapper.Map(rows, AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("缺少 PER_005")));
            Assert.That(result.Errors, Has.None.Matches<string>(e => e.Contains("PER_017")));
        }

        [Test]
        public void WarnsButDoesNotBlockWhenIdMissingFromImageBindings()
        {
            var rows = Rows();
            var imageIds = AllImageBindingIds();
            imageIds.Remove("PER_016");

            var result = PersonaTableMapper.Map(rows, imageIds);

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.Warnings, Has.Some.Matches<string>(w => w.Contains("PER_016") && w.Contains("绑定ID")));
        }

        [Test]
        public void RejectsEmptyTable()
        {
            var result = PersonaTableMapper.Map(new List<Dictionary<string, string>>(), AllImageBindingIds());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<string>(e => e.Contains("没有任何数据行")));
        }
    }
}
