using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using PersonaCards.Data;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>配表映射器测试：用行字典夹具（= XlsxTableReader 的输出形状）验证契约解析，不依赖真实 xlsx 文件。</summary>
    public sealed class RunRouteTableMapperTests
    {
        [Test]
        public void RealTableFixtureMapsToThirteenNodes()
        {
            // 与 Docs/人格牌.xlsx「关卡流程」sheet 当前 13 行一致的夹具：10 战斗 + 3 生成（顺序 4/8/12）
            var rows = new List<Dictionary<string, string>>();
            for (var order = 1; order <= 13; order++)
            {
                if (order == 4 || order == 8 || order == 12)
                    rows.Add(Row(order, RunRouteTableContract.KindGen, genCount: "1", aiNode: "1", name: $"AI生成{order / 4}"));
                else
                    rows.Add(Row(order, RunRouteTableContract.KindNormal, score: ScoreOf(order).ToString(CultureInfo.InvariantCulture),
                        plays: "4", discards: "3", shop: order == 13 ? "否" : "是", name: $"战斗{order}"));
            }

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Nodes, Has.Count.EqualTo(13));
            Assert.That(result.Warnings, Is.Empty); // 契约值全部是新写法，不应有任何警告

            var expectedKinds = new[]
            {
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle
            };
            var battleCount = 0;
            for (var index = 0; index < result.Nodes.Count; index++)
            {
                var node = result.Nodes[index];
                Assert.That(node.kind, Is.EqualTo(expectedKinds[index]), $"节点 {index} 类型不符");
                if (node.kind == RunNodeKind.PersonaGen)
                {
                    Assert.That(node.targetScore, Is.EqualTo(0), $"节点 {index} 生成节点不应有分数");
                    Assert.That(node.hasShopAfter, Is.False, $"节点 {index} 生成节点不能接商店");
                    Assert.That(node.genCount, Is.EqualTo(1), $"节点 {index} 生成数量不符");
                }
                else
                {
                    battleCount++;
                    Assert.That(node.targetScore, Is.EqualTo(ScoreOf(nodeIndexOrderOf(index))), $"节点 {index} 目标分不符");
                    Assert.That(node.playsLimit, Is.EqualTo(4), $"节点 {index} 手牌限制不符");
                    Assert.That(node.discardsLimit, Is.EqualTo(3), $"节点 {index} 弃牌限制不符");
                    Assert.That(node.bossPoolId, Is.EqualTo(BossPoolId.None), $"节点 {index} 普通战不应有 Boss 池");
                    Assert.That(node.hasShopAfter, Is.EqualTo(index != 12), $"节点 {index} 商店标记不符（最终关除外）");
                }
            }
            Assert.That(battleCount, Is.EqualTo(10));
        }

        [Test]
        public void LegacyNormalKindValueIsAcceptedWithWarning()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, "战斗", score: "100", shop: "否", name: "旧写法关")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Nodes[0].kind, Is.EqualTo(RunNodeKind.NormalBattle));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("普通战斗"));
        }

        [Test]
        public void BossPoolsAreAssignedByAppearanceOrder()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindBoss, score: "100", shop: "是", name: "Boss一"),
                Row(2, RunRouteTableContract.KindNormal, score: "150", shop: "是", name: "普通一"),
                Row(3, RunRouteTableContract.KindBoss, score: "200", shop: "是", name: "Boss二"),
                Row(4, RunRouteTableContract.KindGen, genCount: "1", aiNode: "1", name: "AI生成"),
                Row(5, RunRouteTableContract.KindBoss, score: "300", shop: "是", name: "Boss三"),
                Row(6, RunRouteTableContract.KindBoss, score: "400", shop: "否", name: "Boss四")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Nodes[0].bossPoolId, Is.EqualTo(BossPoolId.Primary)); // 第 1 个 Boss → 初级
            Assert.That(result.Nodes[1].bossPoolId, Is.EqualTo(BossPoolId.None)); // 普通战不受影响
            Assert.That(result.Nodes[2].bossPoolId, Is.EqualTo(BossPoolId.Intermediate)); // 第 2 个 → 中级
            Assert.That(result.Nodes[4].bossPoolId, Is.EqualTo(BossPoolId.Advanced)); // 第 3 个 → 高级（无警告）
            Assert.That(result.Nodes[5].bossPoolId, Is.EqualTo(BossPoolId.Advanced)); // 第 4 个 → 高级
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 只有第 4 个 Boss 警告
            Assert.That(result.Warnings[0], Does.Contain("高级"));
        }

        [Test]
        public void MissingShopColumnDefaultsToYesWithWarning()
        {
            // 不提供"是否商店"键（= 表格缺列）：非最终战斗默认"是"，最终关例外
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", name: "战斗一"),
                Row(2, RunRouteTableContract.KindNormal, score: "200", name: "战斗二"),
                Row(3, RunRouteTableContract.KindNormal, score: "300", name: "战斗三")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Nodes[0].hasShopAfter, Is.True);
            Assert.That(result.Nodes[1].hasShopAfter, Is.True);
            Assert.That(result.Nodes[2].hasShopAfter, Is.False); // 最终关例外
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 缺列只提示一次（全局）
            Assert.That(result.Warnings[0], Does.Contain("是否商店").And.Contain("默认"));
        }

        [Test]
        public void ShopValuesParseAndInvalidRejected()
        {
            // 场景一：显式"是"/空单元格/"否" 解析正确（空单元格行级警告）
            var explicitRows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "是", name: "战斗一"),
                Row(2, RunRouteTableContract.KindNormal, score: "200", shop: "", name: "战斗二"),
                Row(3, RunRouteTableContract.KindNormal, score: "300", shop: "否", name: "战斗三")
            };
            var parsed = RunRouteTableMapper.Map(explicitRows);
            Assert.That(parsed.Succeeded, Is.True, string.Join("\n", parsed.Errors));
            Assert.That(parsed.Nodes[0].hasShopAfter, Is.True);
            Assert.That(parsed.Nodes[1].hasShopAfter, Is.True); // 空 → 默认"是"
            Assert.That(parsed.Nodes[2].hasShopAfter, Is.False);
            Assert.That(parsed.Warnings, Has.Count.EqualTo(1));
            Assert.That(parsed.Warnings[0], Does.Contain("「是否商店」为空"));

            // 场景二：非法值报错
            var invalidRows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "随便", name: "战斗一"),
                Row(2, RunRouteTableContract.KindNormal, score: "200", shop: "否", name: "战斗二")
            };
            var rejected = RunRouteTableMapper.Map(invalidRows);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.Nodes, Is.Null);
            Assert.That(rejected.Errors, Has.Count.EqualTo(1));
            Assert.That(rejected.Errors[0], Does.Contain("是否商店").And.Contain("随便"));
        }

        [Test]
        public void AllRowErrorsAreCollectedNotFailFast()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "abc", shop: "是", name: "坏分数"),
                Row(2, "隐藏关", score: "100", shop: "是", name: "坏类型"),
                Row(3, RunRouteTableContract.KindNormal, score: "300", shop: "否", name: "好行")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Nodes, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2)); // 两行错误全部收集
            Assert.That(result.Errors[0], Does.Contain("坏分数").And.Contain("分数参数"));
            Assert.That(result.Errors[1], Does.Contain("隐藏关").And.Contain("关卡类型"));
        }

        [Test]
        public void DuplicateOrMissingOrderFails()
        {
            // 场景一：顺序重复
            var duplicate = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "是", name: "战斗一"),
                Row(1, RunRouteTableContract.KindNormal, score: "200", shop: "否", name: "战斗二")
            };
            var duplicateResult = RunRouteTableMapper.Map(duplicate);
            Assert.That(duplicateResult.Succeeded, Is.False);
            Assert.That(duplicateResult.Errors, Has.Count.EqualTo(1));
            Assert.That(duplicateResult.Errors[0], Does.Contain("重复"));

            // 场景二：顺序列不是正整数
            var nonNumeric = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "否", name: "战斗一")
            };
            nonNumeric[0][RunRouteTableContract.ColOrder] = "abc";
            var nonNumericResult = RunRouteTableMapper.Map(nonNumeric);
            Assert.That(nonNumericResult.Succeeded, Is.False);
            Assert.That(nonNumericResult.Errors[0], Does.Contain("顺序"));

            // 场景三：顺序列缺失
            var missing = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "否", name: "战斗一")
            };
            missing[0].Remove(RunRouteTableContract.ColOrder);
            var missingResult = RunRouteTableMapper.Map(missing);
            Assert.That(missingResult.Succeeded, Is.False);
            Assert.That(missingResult.Errors[0], Does.Contain("顺序"));
        }

        [Test]
        public void BattleScoreMustBeNumeric()
        {
            // 场景一：分数不是数字
            var nonNumeric = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "abc", shop: "否", name: "战斗一")
            };
            var nonNumericResult = RunRouteTableMapper.Map(nonNumeric);
            Assert.That(nonNumericResult.Succeeded, Is.False);
            Assert.That(nonNumericResult.Errors[0], Does.Contain("分数参数").And.Contain("abc"));

            // 场景二：分数为空
            var empty = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindBoss, shop: "否", name: "Boss一")
            };
            var emptyResult = RunRouteTableMapper.Map(empty);
            Assert.That(emptyResult.Succeeded, Is.False);
            Assert.That(emptyResult.Errors[0], Does.Contain("分数参数"));
        }

        [Test]
        public void PersonaGenIgnoresScoreAndLimits()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindGen, score: "999", plays: "7", discards: "5", genCount: "3", aiNode: "1", name: "AI生成"),
                Row(2, RunRouteTableContract.KindNormal, score: "100", shop: "否", name: "战斗二")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            var genNode = result.Nodes[0];
            Assert.That(genNode.kind, Is.EqualTo(RunNodeKind.PersonaGen));
            Assert.That(genNode.targetScore, Is.EqualTo(0)); // 分数忽略
            Assert.That(genNode.playsLimit, Is.EqualTo(0)); // 手牌限制忽略
            Assert.That(genNode.discardsLimit, Is.EqualTo(0)); // 弃牌限制忽略
            Assert.That(genNode.genCount, Is.EqualTo(1)); // >1 强制为 1
            Assert.That(genNode.hasShopAfter, Is.False);
            Assert.That(result.Warnings, Has.Count.EqualTo(2)); // 忽略战斗字段 + 生成数量超标
            Assert.That(result.Warnings[0], Does.Contain("忽略"));
            Assert.That(result.Warnings[1], Does.Contain("生成数量"));
        }

        /// <summary>表顺序号 → 目标分（与 Docs/人格牌.xlsx 当前白盒一致）。</summary>
        private static long ScoreOf(int order)
        {
            switch (order)
            {
                case 1: return 550;
                case 2: return 625;
                case 3: return 675;
                case 5: return 775;
                case 6: return 875;
                case 7: return 975;
                case 9: return 1050;
                case 10: return 1275;
                case 11: return 1475;
                case 13: return 1900;
                default: return 0;
            }
        }

        /// <summary>节点列表下标（0 起）→ 表顺序号：测试 1 夹具按 1..13 连续顺序，两值互转仅此适用。</summary>
        private static int nodeIndexOrderOf(int nodeIndex) => nodeIndex + 1;

        /// <summary>构造一行夹具；null 参数 = 该列不存在（缺列），空串 = 空单元格（与 XlsxTableReader 输出形状一致）。</summary>
        private static Dictionary<string, string> Row(int order, string kind, string score = null, string plays = null,
            string discards = null, string genCount = null, string shop = null, string aiNode = null, string name = null)
        {
            var row = new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                [RunRouteTableContract.ColStageName] = name ?? "",
                [RunRouteTableContract.ColOrder] = order.ToString(CultureInfo.InvariantCulture),
                [RunRouteTableContract.ColKind] = kind
            };
            Add(row, RunRouteTableContract.ColScore, score);
            Add(row, RunRouteTableContract.ColPlays, plays);
            Add(row, RunRouteTableContract.ColDiscards, discards);
            Add(row, RunRouteTableContract.ColGenCount, genCount);
            Add(row, RunRouteTableContract.ColShopAfter, shop);
            Add(row, RunRouteTableContract.ColAiNode, aiNode);
            return row;
        }

        /// <summary>value 为 null 时跳过（缺列），其余写入（含空串 = 空单元格）。</summary>
        private static void Add(Dictionary<string, string> row, string column, string value)
        {
            if (value != null) row[column] = value;
        }
    }
}
