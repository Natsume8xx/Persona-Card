using System;
using NUnit.Framework;
using PersonaCards.Cards;
using PersonaCards.UI;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>
    /// 扑克牌面目录测试（美术接入）：
    /// 验证 (花色,点数) → 牌面贴图的 52 张全覆盖、资源可加载、缓存幂等与不同键不同图。
    /// </summary>
    public class CardFaceCatalogTests
    {
        [Test]
        public void AllFiftyTwoCardsResolveToTexture()
        {
            // 4 花色 × 13 点数（2~A）全部有整卡贴图
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var rank = (Rank)rankValue;
                    var texture = CardFaceCatalog.FaceFor(suit, rank);
                    Assert.That(texture, Is.Not.Null, $"缺少牌面：{suit} {rank}");
                    Assert.That(texture, Is.InstanceOf<UnityEngine.Texture2D>());
                }
            }
        }

        [Test]
        public void SameKeyReturnsCachedSameInstance()
        {
            // 缓存幂等：同键两次加载返回同一 Texture2D 引用
            var first = CardFaceCatalog.FaceFor(Suit.Spades, Rank.Ace);
            var second = CardFaceCatalog.FaceFor(Suit.Spades, Rank.Ace);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void DistinctCardsMapToDistinctTextures()
        {
            // 同花色不同点数、同点数不同花色都应是不同美术文件
            Assert.That(CardFaceCatalog.FaceFor(Suit.Spades, Rank.Ace),
                Is.Not.SameAs(CardFaceCatalog.FaceFor(Suit.Spades, Rank.Two)));
            Assert.That(CardFaceCatalog.FaceFor(Suit.Spades, Rank.Ace),
                Is.Not.SameAs(CardFaceCatalog.FaceFor(Suit.Hearts, Rank.Ace)));
        }

        [Test]
        public void AllDistinctKeysMapToDistinctTextures()
        {
            // 52 张牌面两两不同（美术资源互不重复）
            var seen = new System.Collections.Generic.HashSet<UnityEngine.Texture2D>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var texture = CardFaceCatalog.FaceFor(suit, (Rank)rankValue);
                    Assert.That(seen.Add(texture), Is.True, $"重复贴图：{suit} {(Rank)rankValue}");
                }
            }
            Assert.That(seen.Count, Is.EqualTo(StandardDeckFactory.StandardCardCount));
        }

        // —— Sprite 转换（商店/弹窗图片展示用）——

        [Test]
        public void SpriteForReturnsFullRectSprite()
        {
            // 52 张全部可转 Sprite（Image 组件展示路径）
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var rankValue = (int)Rank.Two; rankValue <= (int)Rank.Ace; rankValue++)
                {
                    var sprite = CardFaceCatalog.SpriteFor(suit, (Rank)rankValue);
                    Assert.That(sprite, Is.Not.Null, $"缺少牌面 Sprite：{suit} {(Rank)rankValue}");
                    Assert.That(sprite, Is.InstanceOf<UnityEngine.Sprite>());
                    Assert.That(sprite.rect.width, Is.GreaterThan(0f));
                }
            }
        }

        [Test]
        public void SpriteForReturnsCachedSameInstance()
        {
            // Sprite 缓存幂等：同键两次调用返回同一 Sprite 引用
            var first = CardFaceCatalog.SpriteFor(Suit.Hearts, Rank.King);
            var second = CardFaceCatalog.SpriteFor(Suit.Hearts, Rank.King);
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first));
            // 不同键不同 Sprite
            Assert.That(CardFaceCatalog.SpriteFor(Suit.Hearts, Rank.Queen), Is.Not.SameAs(first));
        }

        [Test]
        public void SpriteForRebuildsAfterDestroyedSpriteCached()
        {
            // 回归防护：DisableDomainReload 下退出 Play Mode 后静态缓存残留已销毁 Sprite（假 null），
            // 命中时必须移除重建而非永久返回 null（商店卡面/选牌弹窗整卡图曾因此整体消失）。
            var field = typeof(CardFaceCatalog).GetField("SpriteCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var cache = (System.Collections.Generic.Dictionary<string, UnityEngine.Sprite>)field.GetValue(null);
            var face = CardFaceCatalog.FaceFor(Suit.Diamonds, Rank.Jack);
            var dead = UnityEngine.Sprite.Create(face, new UnityEngine.Rect(0f, 0f, face.width, face.height),
                new UnityEngine.Vector2(0.5f, 0.5f));
            UnityEngine.Object.DestroyImmediate(dead);
            cache["diamonds-jack"] = dead; // 注入已销毁对象（Unity == null 为 true、ReferenceEquals 为 false）
            var rebuilt = CardFaceCatalog.SpriteFor(Suit.Diamonds, Rank.Jack);
            Assert.That(rebuilt, Is.Not.Null);
            Assert.That(rebuilt.rect.width, Is.GreaterThan(0f));
        }
    }
}
