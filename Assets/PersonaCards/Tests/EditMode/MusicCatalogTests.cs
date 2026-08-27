using NUnit.Framework;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 游戏音乐目录测试（音乐系统）：
    /// 验证阶段→BGM 映射、资源可加载、缓存幂等与未映射阶段的延续语义。
    /// </summary>
    public class MusicCatalogTests
    {
        [Test]
        public void PlannerSpecifiedStagesMapToExpectedBgm()
        {
            // 策划指定：主界面/铸牌→对战2；普通战斗/商店→对战1；Boss 揭示/战→对战3
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.MainMenu, false), Is.EqualTo(MusicCatalog.BgmBattle2));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.PersonaForge, false), Is.EqualTo(MusicCatalog.BgmBattle2));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.PersonaGen, false), Is.EqualTo(MusicCatalog.BgmBattle2));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.Battle, false), Is.EqualTo(MusicCatalog.BgmBattle1));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.Shop, false), Is.EqualTo(MusicCatalog.BgmBattle1));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.BossReveal, false), Is.EqualTo(MusicCatalog.BgmBattle3));
        }

        [Test]
        public void BattleStageDistinguishesBossFromNormal()
        {
            // Battle 阶段：普通节点→对战1，Boss 节点→对战3
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.Battle, true), Is.EqualTo(MusicCatalog.BgmBattle3));
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.Battle, false), Is.EqualTo(MusicCatalog.BgmBattle1));
        }

        [Test]
        public void UnspecifiedStagesReturnNullToKeepCurrentBgm()
        {
            // 准备/奖励/结算/失败结算：null = 延续当前曲不打断
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.PersonaSetup, false), Is.Null);
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.Reward, false), Is.Null);
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.RunReport, false), Is.Null);
            Assert.That(MusicCatalog.BgmKeyForStage(PrototypeFlowStage.FailureResult, false), Is.Null);
        }

        [Test]
        public void AllBgmClipsResolve()
        {
            // 3 首 BGM 均可加载
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle1), Is.Not.Null, "缺少对战1");
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle2), Is.Not.Null, "缺少对战2");
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle3), Is.Not.Null, "缺少对战3");
        }

        [Test]
        public void AllSfxClipsResolve()
        {
            // 7 个已入库音效均可加载（播放时机待策划指定）
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxClick), Is.Not.Null, "缺少点击");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxDraw), Is.Not.Null, "缺少抽牌");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxDiscard), Is.Not.Null, "缺少弃牌");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxCoin), Is.Not.Null, "缺少金币获取");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxScoreCount), Is.Not.Null, "缺少分数计算");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxVictory), Is.Not.Null, "缺少胜利");
            Assert.That(MusicCatalog.SfxClipFor(MusicCatalog.SfxDefeat), Is.Not.Null, "缺少失败");
        }

        [Test]
        public void SameKeyReturnsCachedSameInstance()
        {
            // 缓存幂等：同键两次加载返回同一 AudioClip 引用
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle1),
                Is.SameAs(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle1)));
        }

        [Test]
        public void DistinctBgmKeysMapToDistinctClips()
        {
            // 三首对战曲互不相同
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle1),
                Is.Not.SameAs(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle2)));
            Assert.That(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle2),
                Is.Not.SameAs(MusicCatalog.BgmClipFor(MusicCatalog.BgmBattle3)));
        }
    }
}
