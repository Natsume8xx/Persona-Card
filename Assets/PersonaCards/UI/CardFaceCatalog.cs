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

        /// <summary>
        /// 按 (花色, 点数) 取整卡牌面；资源缺失返回 null（调用方回退羊皮纸 + 文本）。
        /// </summary>
        public static Texture2D FaceFor(Suit suit, Rank rank)
        {
            var key = SuitKey(suit) + "-" + RankKey(rank);
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var texture = Resources.Load<Texture2D>(ResourcePrefix + key);
            Cache[key] = texture;
            return texture;
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
