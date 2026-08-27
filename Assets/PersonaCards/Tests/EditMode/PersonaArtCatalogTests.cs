using NUnit.Framework;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 人格牌立绘目录测试（美术接入）：
    /// 验证 TemplateId → 立绘映射的收录范围、资源可加载、缓存幂等与未知键回落。
    /// </summary>
    public class PersonaArtCatalogTests
    {
        [Test]
        public void TutorialAnchorsMapToFirstThreeArts()
        {
            // 教学 3 锚点按槽位顺序显示人格牌 01~03 的美术
            Assert.That(PersonaArtCatalog.PortraitFor("persona.initial.accumulator"), Is.Not.Null);
            Assert.That(PersonaArtCatalog.PortraitFor("persona.initial.executor"), Is.Not.Null);
            Assert.That(PersonaArtCatalog.PortraitFor("persona.initial.ambitious"), Is.Not.Null);
        }

        [Test]
        public void FirstEightConfigIdsResolveToSprite()
        {
            // 配表前 8 张（PER_001~008）均有美术
            for (var index = 1; index <= 8; index++)
            {
                var id = $"PER_{index:D3}";
                Assert.That(PersonaArtCatalog.PortraitFor(id), Is.Not.Null, $"缺少立绘：{id}");
            }
        }

        [Test]
        public void UnmappedIdsReturnNull()
        {
            // PER_009~016 美术未到货、forge 候选与未知键一律回落 null
            Assert.That(PersonaArtCatalog.PortraitFor("PER_009"), Is.Null);
            Assert.That(PersonaArtCatalog.PortraitFor("PER_016"), Is.Null);
            Assert.That(PersonaArtCatalog.PortraitFor("persona.forge.映照.洞察者"), Is.Null);
            Assert.That(PersonaArtCatalog.PortraitFor("不存在的牌"), Is.Null);
        }

        [Test]
        public void NullOrEmptyKeyReturnsNull()
        {
            Assert.That(PersonaArtCatalog.PortraitFor(null), Is.Null);
            Assert.That(PersonaArtCatalog.PortraitFor(""), Is.Null);
        }

        [Test]
        public void SameKeyReturnsCachedSameInstance()
        {
            // 缓存幂等：同键两次加载返回同一 Sprite 引用
            var first = PersonaArtCatalog.PortraitFor("PER_001");
            var second = PersonaArtCatalog.PortraitFor("PER_001");
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void DistinctIdsMapToDistinctSprites()
        {
            // 不同键对应不同美术文件
            Assert.That(PersonaArtCatalog.PortraitFor("PER_001"),
                Is.Not.SameAs(PersonaArtCatalog.PortraitFor("PER_002")));
        }
    }
}
