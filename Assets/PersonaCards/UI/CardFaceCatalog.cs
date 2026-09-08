using System.Collections.Generic;
using PersonaCards.Cards;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 扑克牌面运行时目录（美术接入）：
    /// 按 (花色, 点数) 返回整卡牌面 Texture2D，Resources.Load 按名加载 + 进程内缓存（每键只加载一次）。
    /// 美术文件位于 Assets/PersonaCards/Resources/CardFace/card-face-{suit}-{rank}.png（共 52 张），
    /// 与策划资源 MS/扑克牌面资源 的四个花色文件夹一一对应（黑桃/红桃/草花/方块，各 13 张）；
    /// 语义顺序与配表 图片配置 表 IMAGE_001~052 一致（黑桃 A~K / 红桃 A~K / 梅花 A~K / 方块 A~K），
    /// 代码按枚举直接映射，不依赖表中空白的「卡牌链接」列。
    /// 导入设置：Texture2D（非 Sprite）、maxSize 1024、无 mipmap、DXT 压缩（52 张控制内存）。
    /// 加载失败返回 null —— 调用方回退羊皮纸底纹 + 文本点数。
    /// </summary>
    public static class CardFaceCatalog
    {
        /// <summary>Resources 相对路径前缀（不带扩展名）。</summary>
        private const string ResourcePrefix = "CardFace/card-face-";

        /// <summary>加载缓存："{suit}-{rank}" → Texture2D（含加载失败的 null 结果，避免重复 Resources.Load）。</summary>
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        /// <summary>Sprite 缓存："{suit}-{rank}" → Sprite（含加载失败的 null 结果）。</summary>
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 按 (花色, 点数) 取整卡牌面；资源缺失返回 null（调用方回退羊皮纸 + 文本）。
        /// </summary>
        public static Texture2D FaceFor(Suit suit, Rank rank)
        {
            var key = SuitKey(suit) + "-" + RankKey(rank);
            if (Cache.TryGetValue(key, out var cached))
            {
                if (cached != null) return cached;
                // 已销毁对象（DisableDomainReload 下跨 Play Mode 残留的假 null）→ 移除重建；真 null（缺失记录）保留
                if (!ReferenceEquals(cached, null)) Cache.Remove(key);
                else return null;
            }
            var texture = Resources.Load<Texture2D>(ResourcePrefix + key);
            Cache[key] = texture;
            return texture;
        }

        /// <summary>
        /// 按 (花色, 点数) 取整卡牌面 Sprite（供 Image 组件展示，UI 重排后统一用 Image 显示卡面）：
        /// FaceFor 结果创建全矩形中心 pivot 的 Sprite；资源缺失返回 null（调用方回退文本格/无图布局）。
        /// Sprite.Create 产物是运行时对象：DisableDomainReload 下退出 Play Mode 会被 Unity 销毁而缓存残留，
        /// 故命中假 null（已销毁对象）时移除并重新创建。
        /// </summary>
        public static Sprite SpriteFor(Suit suit, Rank rank)
        {
            var key = SuitKey(suit) + "-" + RankKey(rank);
            if (SpriteCache.TryGetValue(key, out var cached))
            {
                if (cached != null) return cached;
                if (!ReferenceEquals(cached, null)) SpriteCache.Remove(key);
                else return null;
            }
            var texture = FaceFor(suit, rank);
            Sprite sprite = null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = key; // 与贴图同名（牌库查看 SpriteForFace 惯例），调试/日志可读
            }
            SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>花色 → 文件名段（spades/hearts/clubs/diamonds）。</summary>
        private static string SuitKey(Suit suit)
        {
            return suit switch
            {
                Suit.Spades => "spades",
                Suit.Hearts => "hearts",
                Suit.Clubs => "clubs",
                Suit.Diamonds => "diamonds",
                _ => "unknown"
            };
        }

        /// <summary>点数 → 文件名段（2~10/ace/jack/queen/king）。</summary>
        private static string RankKey(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "ace",
                Rank.King => "king",
                Rank.Queen => "queen",
                Rank.Jack => "jack",
                _ => ((int)rank).ToString()
            };
        }
    }
}
