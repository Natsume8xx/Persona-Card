using System.Collections.Generic;
using NUnit.Framework;
using PersonaCards.Data;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    public sealed class RunRouteTests
    {
        /// <summary>测试间隔离：RunRoute 是静态门面，自定义资产不能泄漏到其他测试。</summary>
        [TearDown]
        public void ResetRouteToDefault()
        {
            RunRoute.Configure(null);
        }

        [Test]
        public void DefaultRouteHasExpectedStructure()
        {
            RunRoute.Configure(null);

            Assert.That(RunRoute.StageCount, Is.EqualTo(13));
            Assert.That(RunRoute.BattleCount, Is.EqualTo(10));
            Assert.That(RunRoute.ShopCount, Is.EqualTo(9)); // 9 个非最终战斗节点全部带商店

            var expectedKinds = new[]
            {
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.NormalBattle, RunNodeKind.PersonaGen,
                RunNodeKind.NormalBattle
            };
            var expectedScores = new long?[] { 550, 625, 675, null, 775, 875, 975, null, 1050, 1275, 1475, null, 1900 };
            for (var index = 0; index < RunRoute.StageCount; index++)
            {
                var node = RunRoute.GetNode(index);
                Assert.That(node.Index, Is.EqualTo(index));
                Assert.That(node.kind, Is.EqualTo(expectedKinds[index]), $"节点 {index} 类型与配表白盒不符");
                if (node.kind == RunNodeKind.PersonaGen)
                {
                    Assert.That(node.genCount, Is.EqualTo(1), $"节点 {index} 生成数量与配表白盒不符");
                    Assert.That(node.hasShopAfter, Is.False, $"节点 {index} 生成节点不得带商店");
                }
                else
                {
                    Assert.That(node.targetScore, Is.EqualTo(expectedScores[index].Value), $"节点 {index} 目标分与配表白盒不符");
                    Assert.That(node.hasShopAfter, Is.EqualTo(index != 12), $"节点 {index} 商店标记不符（非最终战斗都带商店）");
                }
            }

            Assert.That(RunRoute.IsFinalNode(12), Is.True);
            Assert.That(RunRoute.IsFinalNode(11), Is.False);
        }

        [Test]
        public void ConfiguredAssetOverridesDefaultRoute()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Advanced, false)
            };

            RunRoute.Configure(asset);

            Assert.That(RunRoute.StageCount, Is.EqualTo(2));
            Assert.That(RunRoute.BattleCount, Is.EqualTo(2));
            Assert.That(RunRoute.ShopCount, Is.EqualTo(1));
            Assert.That(RunRoute.GetNode(0).targetScore, Is.EqualTo(100));
            Assert.That(RunRoute.GetNode(1).targetScore, Is.EqualTo(200));
            Assert.That(RunRoute.GetNode(1).bossPoolId, Is.EqualTo(BossPoolId.Advanced));
            Assert.That(RunRoute.IsFinalNode(1), Is.True);
            Assert.That(RunRoute.NextNodeKindOf(0), Is.EqualTo(RunNodeKind.BossBattle)); // 下一节点是 Boss 战

            RunRoute.Configure(null);
            Assert.That(RunRoute.BattleCount, Is.EqualTo(10)); // 回落内置默认路线
        }

        [Test]
        public void IndexIsAssignedByListPosition()
        {
            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.NormalBattle, 200, BossPoolId.None, false)
            };
            asset.battleNodes[0].Index = 999; // 手填脏值应被门面按列表位置覆盖
            asset.battleNodes[1].Index = 999;

            RunRoute.Configure(asset);

            Assert.That(RunRoute.GetNode(0).Index, Is.EqualTo(0));
            Assert.That(RunRoute.GetNode(1).Index, Is.EqualTo(1));
        }

        [Test]
        public void ValidateRejectsPerKindViolations()
        {
            var empty = ScriptableObject.CreateInstance<RunRouteAsset>();
            empty.battleNodes = new List<RunBattleNode>();
            Assert.That(empty.Validate(out var emptyError), Is.False);
            Assert.That(emptyError, Is.Not.Null.And.Not.EqualTo(string.Empty));

            // Boss 战必须指定难度池
            var bossWithoutPool = ScriptableObject.CreateInstance<RunRouteAsset>();
            bossWithoutPool.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.BossBattle, 100, BossPoolId.None, false)
            };
            Assert.That(bossWithoutPool.Validate(out var poolError), Is.False);
            Assert.That(poolError, Does.Contain("难度池"));

            // 战斗类目标分必须为正
            var zeroScore = ScriptableObject.CreateInstance<RunRouteAsset>();
            zeroScore.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 0, BossPoolId.None, false)
            };
            Assert.That(zeroScore.Validate(out var scoreError), Is.False);
            Assert.That(scoreError, Does.Contain("目标分"));

            // 生成节点生成数量至少为 1
            var genWithoutCount = ScriptableObject.CreateInstance<RunRouteAsset>();
            genWithoutCount.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 0),
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false)
            };
            Assert.That(genWithoutCount.Validate(out var countError), Is.False);
            Assert.That(countError, Does.Contain("生成数量"));

            // 生成节点不能带商店
            var genWithShop = ScriptableObject.CreateInstance<RunRouteAsset>();
            genWithShop.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, true, genCount: 1),
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false)
            };
            Assert.That(genWithShop.Validate(out var genShopError), Is.False);
            Assert.That(genShopError, Does.Contain("商店"));

            // 最终节点必须是战斗类型
            var finalIsGen = ScriptableObject.CreateInstance<RunRouteAsset>();
            finalIsGen.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1)
            };
            Assert.That(finalIsGen.Validate(out var finalKindError), Is.False);
            Assert.That(finalKindError, Does.Contain("最终节点"));

            // 最终节点不能带商店
            var finalWithShop = ScriptableObject.CreateInstance<RunRouteAsset>();
            finalWithShop.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.NormalBattle, 200, BossPoolId.None, true)
            };
            Assert.That(finalWithShop.Validate(out var finalShopError), Is.False);
            Assert.That(finalShopError, Does.Contain("最终节点"));

            // 出牌/弃牌限制不能为负
            var negativePlays = ScriptableObject.CreateInstance<RunRouteAsset>();
            negativePlays.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false, playsLimit: -1)
            };
            Assert.That(negativePlays.Validate(out var playsError), Is.False);
            Assert.That(playsError, Does.Contain("限制"));

            // 合法路线：中段不带商店现在合法（配表可自由指定），最终节点战斗且不带商店
            var valid = ScriptableObject.CreateInstance<RunRouteAsset>();
            valid.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false)
            };
            Assert.That(valid.Validate(out var validError), Is.True, validError);
        }

        [Test]
        public void GetNodeRejectsOutOfRangeIndex()
        {
            RunRoute.Configure(null);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunRoute.GetNode(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunRoute.GetNode(RunRoute.StageCount));
        }

        [Test]
        public void NextNodeKindOfMatchesRouteTable()
        {
            RunRoute.Configure(null);

            Assert.That(RunRoute.NextNodeKindOf(0), Is.EqualTo(RunNodeKind.NormalBattle));
            Assert.That(RunRoute.NextNodeKindOf(1), Is.EqualTo(RunNodeKind.NormalBattle));
            Assert.That(RunRoute.NextNodeKindOf(2), Is.EqualTo(RunNodeKind.PersonaGen)); // 商店 3 后是生成节点
            Assert.That(RunRoute.NextNodeKindOf(3), Is.EqualTo(RunNodeKind.NormalBattle));
            Assert.That(RunRoute.NextNodeKindOf(6), Is.EqualTo(RunNodeKind.PersonaGen));
            Assert.That(RunRoute.NextNodeKindOf(10), Is.EqualTo(RunNodeKind.PersonaGen));

            // 最终节点没有下一节点
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunRoute.NextNodeKindOf(12));
        }

        [Test]
        public void BattleOrdinalOfCountsBattlesOnly()
        {
            RunRoute.Configure(null);

            Assert.That(RunRoute.BattleOrdinalOf(0), Is.EqualTo(1));
            Assert.That(() => RunRoute.BattleOrdinalOf(3), Throws.TypeOf<System.InvalidOperationException>()); // 生成节点没有战斗序号
            Assert.That(RunRoute.BattleOrdinalOf(4), Is.EqualTo(4)); // 生成节点不计入
            Assert.That(RunRoute.BattleOrdinalOf(12), Is.EqualTo(10));
        }

        [Test]
        public void PlaysLimitOfResolvesDefaultAndCustom()
        {
            RunRoute.Configure(null);

            // 白盒节点未配置限制（0）→ 回落默认 4/3
            Assert.That(RunRoute.PlaysLimitOf(0), Is.EqualTo(4));
            Assert.That(RunRoute.DiscardsLimitOf(0), Is.EqualTo(3));
            Assert.That(() => RunRoute.PlaysLimitOf(3), Throws.TypeOf<System.InvalidOperationException>()); // 生成节点无此概念

            var asset = ScriptableObject.CreateInstance<RunRouteAsset>();
            asset.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false, playsLimit: 5, discardsLimit: 2)
            };

            RunRoute.Configure(asset);
            Assert.That(RunRoute.PlaysLimitOf(0), Is.EqualTo(5));
            Assert.That(RunRoute.DiscardsLimitOf(0), Is.EqualTo(2));
        }
    }
}
