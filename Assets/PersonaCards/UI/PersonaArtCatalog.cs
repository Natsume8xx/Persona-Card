using System.Collections.Generic;
using UnityEngine;

namespace PersonaCards.UI
{
    /// <summary>
    /// 人格牌立绘运行时目录（美术接入）：
    /// 按 TemplateId 返回立绘 Sprite，Resources.Load 按名加载 + 进程内缓存（每键只加载一次）。
    /// 美术文件位于 Assets/PersonaCards/Resources/PersonaArt/persona-art-01~08.png（977×1610 整卡原画），
    /// 序号与策划资源 MS/人格牌美术资源 的排列顺序一致：
    ///   01 隐境寻路者 / 02 断舍离者 / 03 花色漫游者 / 04 同色共鸣者
    ///   05 满手承诺者 / 06 终局观察者 / 07 克制的赌徒 / 08 结构收藏家
    /// 对应配表 图片配置 表 IMAGE_064~071（人格牌01~08 原画）＝ PER_001~PER_008；
    /// 教学 3 锚点（persona.initial.*）作为白盒占位，按槽位顺序显示 01~03 号美术，
    /// 待配表装备流程（PER_xxx 键）接入后自然接管，此映射可随时删除。
    /// 未收录的键返回 null —— 调用方保持序列化占位图不动。
    /// </summary>
    public static class PersonaArtCatalog
    {
        /// <summary>Resources 相对路径前缀（不带扩展名，编号补零两位）。</summary>
        private const string ResourcePrefix = "PersonaArt/persona-art-";

        /// <summary>TemplateId → 美术编号（两位字符串）。</summary>
        private static readonly Dictionary<string, string> KeyToArtNumber = new Dictionary<string, string>
        {
            // 教学 3 锚点（白盒占位卡）：槽位 01~03 按序显示人格牌 01~03 的立绘
            { "persona.initial.accumulator", "01" },
            { "persona.initial.executor", "02" },
            { "persona.initial.ambitious", "03" },
            // 配表 16 张中已有美术的前 8 张（PER_009~016 待美术到货后追加）
            { "PER_001", "01" },
            { "PER_002", "02" },
            { "PER_003", "03" },
            { "PER_004", "04" },
            { "PER_005", "05" },
            { "PER_006", "06" },
            { "PER_007", "07" },
            { "PER_008", "08" },
            // 铸造三选一候选（临时映射，待策划/美术确认）：
            // 映照→映照组 1 号（终局观察者）、偏转→偏转组 1 号（花色漫游者）、裂变→暂无裂变美术，暂用 01 隐境寻路者
            { "persona.forge.映照.洞察者", "06" },
            { "persona.forge.偏转.调律者", "03" },
            { "persona.forge.裂变.破局者", "01" },
        };

        /// <summary>加载缓存：TemplateId → Sprite（含加载失败的 null 结果，避免重复 Resources.Load）。</summary>
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 按 TemplateId 取立绘；未知键或资源缺失返回 null（调用方保持原图）。
        /// </summary>
        public static Sprite PortraitFor(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return null;
            if (Cache.TryGetValue(templateId, out var cached)) return cached;
            if (!KeyToArtNumber.TryGetValue(templateId, out var number))
            {
                Cache[templateId] = null; // 未知键也缓存 null，避免每次刷新都查字典后落空
                return null;
            }
            var sprite = Resources.Load<Sprite>(ResourcePrefix + number);
            Cache[templateId] = sprite;
            return sprite;
        }
    }
}
