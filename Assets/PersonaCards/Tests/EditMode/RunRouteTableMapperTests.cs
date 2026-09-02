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
        public void RealTableFixtureMapsToSeventeenNodes()
        {
            // 与 Docs/人格牌.xlsx「关卡流程」sheet 当前 17 行一致的夹具：12 普通战斗 + 4 生成（顺序 4/8/12/16）+ 最终 Boss（顺序 17）
            var rows = new List<Dictionary<string, string>>();
            var battleIndex = 0;
            for (var order = 1; order <= 17; order++)
            {
                if (order == 4 || order == 8 || order == 12)
                {
                    rows.Add(Row(order, RunRouteTableContract.KindGen, stageId: $"STAGE_{order:00}",
                        scoreType: RunRouteTableContract.ScoreTypeNone, score: "0", plays: "0", discards: "0", aiNode: "1",
                        rewardType1: RunRouteTableContract.RewardNone, rewardParam1: "0",
                        rewardType2: RunRouteTableContract.RewardPersona, rewardParam2: "1", name: $"AI{order / 4}"));
                }
                else if (order == 16)
                {
                    rows.Add(Row(order, RunRouteTableContract.KindGen, stageId: "STAGE_16",
                        scoreType: RunRouteTableContract.ScoreTypeNone, score: "0", plays: "0", discards: "0", aiNode: "0",
                        rewardType1: RunRouteTableContract.RewardNone, rewardParam1: "0",
                        rewardType2: RunRouteTableContract.RewardNone, rewardParam2: "0", name: "商店"));
                }
                else
                {
                    battleIndex++;
                    var isBoss = order == 17;
                    rows.Add(Row(order, isBoss ? RunRouteTableContract.KindBoss : RunRouteTableContract.KindNormal,
                        stageId: $"STAGE_{order:00}", scoreType: RunRouteTableContract.ScoreTypePass,
                        score: ScoreOf(order).ToString(CultureInfo.InvariantCulture), plays: "4", discards: "3", aiNode: "0",
                        rewardType1: isBoss ? RunRouteTableContract.RewardNone : RunRouteTableContract.RewardGold,
                        rewardParam1: isBoss ? "0" : CoinOf(order).ToString(CultureInfo.InvariantCulture),
                        rewardType2: RunRouteTableContract.RewardNone,
                        rewardParam2: order == 13 ? "" : "0",
                        name: $"关卡{battleIndex}"));
                }
            }

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Nodes, Has.Count.EqualTo(17));
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 仅 STAGE_16「商店」生成节点未指定 AI 节点
            Assert.That(result.Warnings[0], Does.Contain("未指定 AI 节点").And.Contain("商店"));

            var expectedKinds = new[]
            {
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.BossBattle
            };
            for (var index = 0; index < result.Nodes.Count; index++)
            {
                var node = result.Nodes[index];
                Assert.That(node.kind, Is.EqualTo(expectedKinds[index]), $"节点 {index} 类型不符");
                Assert.That(node.stageId, Is.EqualTo($"STAGE_{index + 1:00}"), $"节点 {index} 阶段_ID 不符");
                if (node.kind == RunNodeKind.PersonaGen)
                {
                    Assert.That(node.targetScore, Is.EqualTo(0), $"节点 {index} 生成节点不应有分数");
                    Assert.That(node.hasShopAfter, Is.False, $"节点 {index} 生成节点不能接商店");
                    Assert.That(node.genCount, Is.EqualTo(1), $"节点 {index} 生成数量不符");
                    Assert.That(node.rewardType1, Is.EqualTo(RunRouteTableContract.RewardNone));
                    Assert.That(node.rewardParam1, Is.EqualTo("0"));
                    if (index == 15)
                    {
                        Assert.That(node.rewardType2, Is.EqualTo(RunRouteTableContract.RewardNone)); // STAGE_16 商店：无奖励
                        Assert.That(node.rewardParam2, Is.EqualTo("0"));
                    }
                    else
                    {
                        Assert.That(node.rewardType2, Is.EqualTo(RunRouteTableContract.RewardPersona)); // 生成奖励 = 人格牌 1
                        Assert.That(node.rewardParam2, Is.EqualTo("1"));
                    }
                }
                else
                {
                    var isBoss = index == 16;
                    Assert.That(node.targetScore, Is.EqualTo(ScoreOf(index + 1)), $"节点 {index} 目标分不符");
                    Assert.That(node.playsLimit, Is.EqualTo(4), $"节点 {index} 手牌限制不符");
                    Assert.That(node.discardsLimit, Is.EqualTo(3), $"节点 {index} 弃牌限制不符");
                    Assert.That(node.hasShopAfter, Is.EqualTo(!isBoss), $"节点 {index} 商店标记不符");
                    Assert.That(node.rewardType1, Is.EqualTo(isBoss ? RunRouteTableContract.RewardNone : RunRouteTableContract.RewardGold));
                    Assert.That(node.rewardType2, Is.EqualTo(RunRouteTableContract.RewardNone));
                    Assert.That(node.rewardParam2, Is.EqualTo(index == 12 ? "" : "0")); // STAGE_13 奖励参数2 空单元格 → 空串
                    if (isBoss)
                    {
                        Assert.That(node.bossPoolId, Is.EqualTo(BossPoolId.Primary), "首个 Boss 应分配初级池");
                        Assert.That(node.rewardParam1, Is.EqualTo("0"));
                    }
                    else
                    {
                        Assert.That(node.bossPoolId, Is.EqualTo(BossPoolId.None), $"节点 {index} 普通战不应有 Boss 池");
                        Assert.That(node.rewardParam1, Is.EqualTo(CoinOf(index + 1).ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }
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
                Row(4, RunRouteTableContract.KindGen, aiNode: "1", name: "AI生成"),
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
        public void MissingShopColumnDefaultsToYesSilently()
        {
            // 不提供"是否商店"键（= 表格缺列，列已从配表永久删除）：非最终战斗默认"是"，最终关例外，均不再发提示
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
            Assert.That(result.Warnings, Is.Empty); // 缺列不再发全局提示
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
                Row(1, RunRouteTableContract.KindGen, score: "999", plays: "7", discards: "5", aiNode: "1", name: "AI生成"),
                Row(2, RunRouteTableContract.KindNormal, score: "100", shop: "否", name: "战斗二")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            var genNode = result.Nodes[0];
            Assert.That(genNode.kind, Is.EqualTo(RunNodeKind.PersonaGen));
            Assert.That(genNode.targetScore, Is.EqualTo(0)); // 分数忽略
            Assert.That(genNode.playsLimit, Is.EqualTo(0)); // 手牌限制忽略
            Assert.That(genNode.discardsLimit, Is.EqualTo(0)); // 弃牌限制忽略
            Assert.That(genNode.genCount, Is.EqualTo(1)); // 生成数量固定 1（配表列已删除）
            Assert.That(genNode.hasShopAfter, Is.False);
            Assert.That(result.Warnings, Has.Count.EqualTo(1)); // 忽略战斗字段
            Assert.That(result.Warnings[0], Does.Contain("忽略"));
        }

        [Test]
        public void AiNodeZeroIsTreatedAsEmpty()
        {
            // 当前配表战斗行 AI节点 统一填 0（占位）：必须按空处理，否则 12 条战斗行全是噪音警告
            var zero = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "否", aiNode: "0", name: "战斗一")
            };
            var zeroResult = RunRouteTableMapper.Map(zero);
            Assert.That(zeroResult.Succeeded, Is.True, string.Join("\n", zeroResult.Errors));
            Assert.That(zeroResult.Warnings, Is.Empty);

            // 非 0 值仍是提示（战斗节点不应指定 AI 节点）
            var real = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "否", aiNode: "1", name: "战斗一")
            };
            var realResult = RunRouteTableMapper.Map(real);
            Assert.That(realResult.Warnings, Has.Count.EqualTo(1));
            Assert.That(realResult.Warnings[0], Does.Contain("AI 节点"));
        }

        [Test]
        public void ScoreTypeMismatchWarnsButSucceeds()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "是", scoreType: RunRouteTableContract.ScoreTypeNone, name: "战斗一"),
                Row(2, RunRouteTableContract.KindGen, aiNode: "1", scoreType: RunRouteTableContract.ScoreTypePass, name: "AI生成"),
                Row(3, RunRouteTableContract.KindNormal, score: "300", shop: "否", scoreType: RunRouteTableContract.ScoreTypePass, name: "战斗三")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Nodes, Has.Count.EqualTo(3));
            Assert.That(result.Warnings, Has.Count.EqualTo(2)); // 战斗配「无」+ 生成配「通关分数」各一条
            Assert.That(result.Warnings[0], Does.Contain("分数类型").And.Contain("应为「通关分数」"));
            Assert.That(result.Warnings[1], Does.Contain("分数类型").And.Contain("应为「无」"));
        }

        [Test]
        public void RewardColumnsStoredRawAndValidated()
        {
            var rows = new List<Dictionary<string, string>>
            {
                Row(1, RunRouteTableContract.KindNormal, score: "100", shop: "否", name: "战斗一",
                    rewardType1: RunRouteTableContract.RewardGold, rewardParam1: "3",
                    rewardType2: RunRouteTableContract.RewardNone, rewardParam2: ""),
                Row(2, RunRouteTableContract.KindNormal, score: "200", shop: "否", name: "战斗二",
                    rewardType1: RunRouteTableContract.RewardGold, rewardParam1: "5",
                    rewardType2: "经验", rewardParam2: "8")
            };

            var result = RunRouteTableMapper.Map(rows);

            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            var first = result.Nodes[0];
            Assert.That(first.rewardType1, Is.EqualTo(RunRouteTableContract.RewardGold));
            Assert.That(first.rewardParam1, Is.EqualTo("3")); // 参数原文存储，不解析数值
            Assert.That(first.rewardType2, Is.EqualTo(RunRouteTableContract.RewardNone));
            Assert.That(first.rewardParam2, Is.Empty); // 空单元格 → 空串（配表 STAGE_13 同款）

            var second = result.Nodes[1];
            Assert.That(second.rewardType2, Is.EqualTo("经验")); // 未知类型原文照存
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("奖励类型").And.Contain("经验"));
        }

        /// <summary>表顺序号 → 目标分（与 Docs/人格牌.xlsx 当前白盒一致）。</summary>
        private static long ScoreOf(int order)
        {
            switch (order)
            {
                case 1: return 950;
                case 2: return 1100;
                case 3: return 1250;
                case 5: return 1350;
                case 6: return 1500;
                case 7: return 1650;
                case 9: return 1750;
                case 10: return 1950;
                case 11: return 2150;
                case 13: return 2300;
                case 14: return 2500;
                case 15: return 2750;
                case 17: return 3200;
                default: return 0;
            }
        }

        /// <summary>表顺序号 → 金币奖励数（与 Docs/人格牌.xlsx 当前白盒一致；生成/Boss 行无金币）。</summary>
        private static int CoinOf(int order)
        {
            switch (order)
            {
                case 1: case 2: case 5: case 6: case 9: case 10: case 14:
                    return 3;
                case 3: case 7: case 11: case 15:
                    return 4;
                case 13:
                    return 2;
                default:
                    return 0;
            }
        }

        /// <summary>构造一行夹具；null 参数 = 该列不存在（缺列），空串 = 空单元格（与 XlsxTableReader 输出形状一致）。</summary>
        private static Dictionary<string, string> Row(int order, string kind, string score = null, string plays = null,
            string discards = null, string shop = null, string aiNode = null, string name = null,
            string stageId = null, string scoreType = null, string rewardType1 = null, string rewardParam1 = null,
            string rewardType2 = null, string rewardParam2 = null)
        {
            var row = new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                [RunRouteTableContract.ColStageName] = name ?? "",
                [RunRouteTableContract.ColOrder] = order.ToString(CultureInfo.InvariantCulture),
                [RunRouteTableContract.ColKind] = kind
            };
            Add(row, RunRouteTableContract.ColStageId, stageId);
            Add(row, RunRouteTableContract.ColScoreType, scoreType);
            Add(row, RunRouteTableContract.ColScore, score);
            Add(row, RunRouteTableContract.ColPlays, plays);
            Add(row, RunRouteTableContract.ColDiscards, discards);
            Add(row, RunRouteTableContract.ColAiNode, aiNode);
            Add(row, RunRouteTableContract.ColRewardType1, rewardType1);
            Add(row, RunRouteTableContract.ColRewardParam1, rewardParam1);
            Add(row, RunRouteTableContract.ColRewardType2, rewardType2);
            Add(row, RunRouteTableContract.ColRewardParam2, rewardParam2);
            Add(row, RunRouteTableContract.ColShopAfter, shop);
            return row;
        }

        /// <summary>value 为 null 时跳过（缺列），其余写入（含空串 = 空单元格）。</summary>
        private static void Add(Dictionary<string, string> row, string column, string value)
        {
            if (value != null) row[column] = value;
        }
    }
}
