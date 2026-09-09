using NUnit.Framework;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 运行时字体工厂测试（WebGL 兼容改造）：编辑器环境走 OS 字体分支，
    /// 断言返回可用字体且覆盖中文；WebGL 分支用的随包字体资源在编辑器下可直接加载验证
    /// （LegacyRuntime 兜底分支靠构建验证）。
    /// </summary>
    public class RuntimeFontFactoryTests
    {
        [Test]
        public void GetRuntimeFont_返回非空字体()
        {
            var font = RuntimeFontFactory.GetRuntimeFont(22);
            Assert.That(font, Is.Not.Null, "编辑器环境下运行时字体不应为空");
        }

        [Test]
        public void GetRuntimeFont_覆盖中文()
        {
            var font = RuntimeFontFactory.GetRuntimeFont(28);
            Assert.That(font, Is.Not.Null);
            Assert.That(font.HasCharacter('人'), Is.True, "运行时字体应覆盖中文常用字");
            Assert.That(font.HasCharacter('牌'), Is.True, "运行时字体应覆盖中文常用字");
        }

        [Test]
        public void GetRuntimeFont_重复调用返回同一实例()
        {
            var first = RuntimeFontFactory.GetRuntimeFont(22);
            var second = RuntimeFontFactory.GetRuntimeFont(40);
            Assert.That(second, Is.SameAs(first), "工厂应缓存同一字体实例");
        }

        [Test]
        public void 随包字体资源_可加载且覆盖中文()
        {
            var font = Resources.Load<Font>(RuntimeFontFactory.BundledFontPath);
            Assert.That(font, Is.Not.Null, "随包字体资源应存在（WebGL 分支主用，缺失则 WebGL 中文全灭）");
            Assert.That(font.HasCharacter('人'), Is.True, "随包字体应覆盖中文常用字");
            Assert.That(font.HasCharacter('牌'), Is.True, "随包字体应覆盖中文常用字");
            Assert.That(font.HasCharacter('A'), Is.True, "随包字体应覆盖拉丁字母");
        }
    }
}
