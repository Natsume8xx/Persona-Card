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
        public void DefaultRouteMatchesGddWhiteboxValues()
        {
            RunRoute.Configure(null);

            Assert.That(RunRoute.BattleCount, Is.EqualTo(6));
            Assert.That(RunRoute.ShopCount, Is.EqualTo(5));

            var expectedScores = new long[] { 350, 550, 1000, 1500, 2800, 4200 };
            var expectedPools = new[] { BossPoolId.None, BossPoolId.Primary, BossPoolId.None, BossPoolId.Intermediate, BossPoolId.None, BossPoolId.Advanced };
            for (var index = 0; index < RunRoute.BattleCount; index++)
            {
                var node = RunRoute.GetNode(index);
                Assert.That(node.Index, Is.EqualTo(index));
                Assert.That(node.targetScore, Is.EqualTo(expectedScores[index]), $"节点 {index} 目标分与 GDD 白盒值不符");
                Assert.That(node.bossPoolId, Is.EqualTo(expectedPools[index]), $"节点 {index} 难度池与路线表不符");
                var expectBoss = index == 1 || index == 3 || index == 5;
                Assert.That(node.kind, Is.EqualTo(expectBoss ? RunNodeKind.BossBattle : RunNodeKind.NormalBattle));
                Assert.That(node.hasShopAfter, Is.EqualTo(index != 5), $"节点 {index} 商店标记不符");
            }

            Assert.That(RunRoute.IsFinalNode(5), Is.True);
            Assert.That(RunRoute.IsFinalNode(4), Is.False);
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

            Assert.That(RunRoute.BattleCount, Is.EqualTo(2));
            Assert.That(RunRoute.ShopCount, Is.EqualTo(1));
            Assert.That(RunRoute.GetNode(0).targetScore, Is.EqualTo(100));
            Assert.That(RunRoute.GetNode(1).targetScore, Is.EqualTo(200));
            Assert.That(RunRoute.GetNode(1).bossPoolId, Is.EqualTo(BossPoolId.Advanced));
            Assert.That(RunRoute.IsFinalNode(1), Is.True);
            Assert.That(RunRoute.NextNodeIsBoss(0), Is.True);

            RunRoute.Configure(null);
            Assert.That(RunRoute.BattleCount, Is.EqualTo(6)); // 回落内置默认路线
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
        public void ValidateRejectsBadRoute()
        {
            var empty = ScriptableObject.CreateInstance<RunRouteAsset>();
            empty.battleNodes = new List<RunBattleNode>();
            Assert.That(empty.Validate(out var emptyError), Is.False);
            Assert.That(emptyError, Is.Not.Null.And.Not.EqualTo(string.Empty));

            var bossWithoutPool = ScriptableObject.CreateInstance<RunRouteAsset>();
            bossWithoutPool.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.BossBattle, 100, BossPoolId.None, false)
            };
            Assert.That(bossWithoutPool.Validate(out var poolError), Is.False);
            Assert.That(poolError, Does.Contain("难度池"));

            var midNodeWithoutShop = ScriptableObject.CreateInstance<RunRouteAsset>();
            midNodeWithoutShop.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, false),
                new RunBattleNode(RunNodeKind.NormalBattle, 200, BossPoolId.None, false)
            };
            Assert.That(midNodeWithoutShop.Validate(out var shopError), Is.False);
            Assert.That(shopError, Does.Contain("商店"));

            var valid = ScriptableObject.CreateInstance<RunRouteAsset>();
            valid.battleNodes = new List<RunBattleNode>
            {
                new RunBattleNode(RunNodeKind.NormalBattle, 100, BossPoolId.None, true),
                new RunBattleNode(RunNodeKind.BossBattle, 200, BossPoolId.Primary, false)
            };
            Assert.That(valid.Validate(out var validError), Is.True, validError);
        }

        [Test]
        public void GetNodeRejectsOutOfRangeIndex()
        {
            RunRoute.Configure(null);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunRoute.GetNode(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunRoute.GetNode(RunRoute.BattleCount));
        }

        [Test]
        public void NextNodeIsBossMatchesRouteTable()
        {
            RunRoute.Configure(null);

            Assert.That(RunRoute.NextNodeIsBoss(0), Is.True);  // 商店 1 后是 Boss（节点 1）
            Assert.That(RunRoute.NextNodeIsBoss(1), Is.False); // 商店 2 后是普通战（节点 2）
            Assert.That(RunRoute.NextNodeIsBoss(2), Is.True);  // 商店 3 后是 Boss（节点 3）
            Assert.That(RunRoute.NextNodeIsBoss(3), Is.False); // 商店 4 后是普通战（节点 4）
            Assert.That(RunRoute.NextNodeIsBoss(4), Is.True);  // 商店 5 后是最终 Boss（节点 5）
        }
    }
}
