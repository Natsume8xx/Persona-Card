using System;
using System.Globalization;
using PersonaCards.Cards;

namespace PersonaCards.UI
{
    /// <summary>
    /// 商店主界面文案拼装静态门面（UI 重排第二批 · 人格铸造）：词条/主属性/副属性效果描述、
    /// 花色符号与卡牌名、解锁节点中文名与到达判定。纯 C# 无引擎依赖，可单测。
    /// 属性参数2 是配表原文字符串（混写整数与小数），格式化规则按属性类型分派：
    /// 独立倍率 ×100 转百分数（0.05→5%）、其余原文数字原样（配表已去尾零）；解析失败原样返回不抛。
    /// </summary>
    public static class PersonaShopText
    {
        /// <summary>
        /// 效果描述：「基础筹码 +15」「基础倍率 +0.3」「独立倍率 +5%」「出牌次数 +1」「金币 +5」。
        /// param1 非「增加」时按原文拼接（配表当前全为「增加」）；param2 缺失只返回类型名。
        /// </summary>
        public static string EffectTextOf(string attrType, string param1, string param2)
        {
            if (string.IsNullOrWhiteSpace(attrType))
                throw new ArgumentException("attrType 不能为空。", nameof(attrType));
            var value = ValueTextOf(attrType, param2);
            if (string.IsNullOrEmpty(value)) return attrType;
            var sign = string.IsNullOrWhiteSpace(param1) ||
                string.Equals(param1, "增加", StringComparison.Ordinal) ? "+" : param1;
            return $"{attrType} {sign}{value}";
        }

        /// <summary>参数2 数值格式化：独立倍率 → 百分数（0.05→5%）；其余类型原文数字原样（去首尾空白）。</summary>
        public static string ValueTextOf(string attrType, string param2)
        {
            if (string.IsNullOrWhiteSpace(param2)) return "";
            if (string.Equals(attrType, "独立倍率", StringComparison.Ordinal))
            {
                if (!float.TryParse(param2, NumberStyles.Float, CultureInfo.InvariantCulture, out var fraction))
                    return param2.Trim(); // 解析失败原样返回（数据异常在资产校验层暴露）
                return (fraction * 100f).ToString("0.###", CultureInfo.InvariantCulture) + "%";
            }
            return param2.Trim();
        }

        /// <summary>卡牌名：「黑桃A」「红桃10」（花色中文 + 点数，与 ShopState.TryParseCardName 口径互逆）。</summary>
        public static string CardTextOf(Suit suit, Rank rank)
        {
            return SuitNameOf(suit) + RankTextOf(rank);
        }

        /// <summary>花色符号：♠ ♥ ♣ ♦；未知花色回退「?」。</summary>
        public static string CardSymbolOf(Suit suit)
        {
            return suit switch
            {
                Suit.Spades => "♠",
                Suit.Hearts => "♥",
                Suit.Clubs => "♣",
                Suit.Diamonds => "♦",
                _ => "?"
            };
        }

        /// <summary>解锁节点中文名：AI1→第一章 / AI2→第二章 / AI3→第三章 / 默认→默认；未知原样返回。</summary>
        public static string UnlockRankOf(string unlockNode)
        {
            if (string.IsNullOrWhiteSpace(unlockNode)) return "默认";
            return unlockNode switch
            {
                "AI1" => "第一章",
                "AI2" => "第二章",
                "AI3" => "第三章",
                _ => unlockNode
            };
        }

        /// <summary>
        /// 节点到达判定（与 ShopState.GroupNameOf 口径一致）：已过生成节点数 0→AI1、1→AI2、≥2→AI3；
        /// 默认/未知节点不设限（返回 true）。
        /// </summary>
        public static bool IsNodeReached(string unlockNode, int generationNodeCount)
        {
            var required = RequiredNodesOf(unlockNode);
            if (required < 0) return true;
            return generationNodeCount >= required;
        }

        /// <summary>花色中文名（CardTextOf 内部用）：黑桃/红桃/梅花/方片；未知回退「未知」。</summary>
        public static string SuitNameOf(Suit suit)
        {
            return suit switch
            {
                Suit.Spades => "黑桃",
                Suit.Hearts => "红桃",
                Suit.Clubs => "梅花",
                Suit.Diamonds => "方片",
                _ => "未知"
            };
        }

        private static string RankTextOf(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "A",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                _ => ((int)rank).ToString(CultureInfo.InvariantCulture)
            };
        }

        private static int RequiredNodesOf(string unlockNode)
        {
            if (string.IsNullOrWhiteSpace(unlockNode)) return -1;
            return unlockNode switch
            {
                "AI1" => 0,
                "AI2" => 1,
                "AI3" => 2,
                _ => -1
            };
        }
    }
}
