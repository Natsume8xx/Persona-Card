using NUnit.Framework;
using PersonaCards.Data;
using PersonaCards.UI;
using UnityEngine;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 人格铸造目录测试（商店图片展示扩展）：PersonaIdByName 按人格名反查人格牌_ID（商店右侧立绘键路径）。
    /// 目录为全局静态（ShopUiSessionTests 同款夹具，各测试 SetUp 自行 Configure，互不依赖）。
    /// </summary>
    public sealed class PersonaForgeCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            PersonaForgeCatalog.Configure(BuildCards(), null, null, null, null);
        }

        [Test]
        public void PersonaIdByName_命中返回人格牌Id()
        {
            Assert.That(PersonaForgeCatalog.PersonaIdByName("人格牌01"), Is.EqualTo("PER_001"));
            Assert.That(PersonaForgeCatalog.PersonaIdByName("人格牌02"), Is.EqualTo("PER_002"));
        }

        [Test]
        public void PersonaIdByName_未命中或空名_返回空()
        {
            Assert.That(PersonaForgeCatalog.PersonaIdByName("人格牌03"), Is.Null);
            Assert.That(PersonaForgeCatalog.PersonaIdByName(""), Is.Null);
            Assert.That(PersonaForgeCatalog.PersonaIdByName(null), Is.Null);
        }

        [Test]
        public void PersonaIdByName_目录未Configure_返回空()
        {
            PersonaForgeCatalog.Configure(null, null, null, null, null);
            Assert.That(PersonaForgeCatalog.PersonaIdByName("人格牌01"), Is.Null);
        }

        /// <summary>2 人格合成夹具（与 ShopUiSessionTests 一致：「人格牌01」↔ PER_001）。</summary>
        private static PersonaCardAsset BuildCards()
        {
            var asset = ScriptableObject.CreateInstance<PersonaCardAsset>();
            asset.entries.Add(new PersonaCardEntry { personaId = "PER_001", personaName = "人格牌01", entryId = "ENTRY_001", mainAttrId = "MAIN_001", subAttrId = "SUB_001", maxAttrs = 3, maxSubAttrs = 2, subPoolSize = 5 });
            asset.entries.Add(new PersonaCardEntry { personaId = "PER_002", personaName = "人格牌02", entryId = "ENTRY_002", mainAttrId = "MAIN_002", subAttrId = "SUB_004", maxAttrs = 3, maxSubAttrs = 2, subPoolSize = 5 });
            return asset;
        }
    }
}
