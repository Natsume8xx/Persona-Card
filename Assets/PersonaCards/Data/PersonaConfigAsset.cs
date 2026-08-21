using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌配置条目：配表 15 列全量落地，枚举文本与数值均存规范化 string 原文（空 = 无/0）。
    /// 顶层纯 C# 类（非 ScriptableObject 嵌套）：Battle 是 noEngineReferences 程序集，资产类型不能跨边界
    /// （P0-1D 教训），门面 Configure 接收本条目列表而非资产；资产（PersonaConfigAsset）只在 UI/Data 引擎程序集流转。
    /// </summary>
    [Serializable]
    public sealed class PersonaConfigEntry
    {
        [Tooltip("人格牌_ID（PER_xxx，权威查询键与美术绑定 ID）。")]
        public string personaId;

        [Tooltip("人格牌名称（当前配表为暂定占位，仅存值；A9）。")]
        public string displayName;

        [Tooltip("品质类型：基础/进阶/稀有/异质（A1 规范值）。")]
        public string quality;

        [Tooltip("品质参数原文（当前为颜色名，语义待策划说明；A9）。")]
        public string qualityParam;

        [Tooltip("行为标签_ID（T01~T16，格式校验；范围不校验——A5 预留）。")]
        public string behaviorTagId;

        [Tooltip("触发条件（12 种统计类条件之一）。")]
        public string trigger;

        [Tooltip("比较符：等于/大于等于/小于/小于等于。")]
        public string comparator;

        [Tooltip("条件阈值原文（非负整数；空 = 无条件阈值）。")]
        public string threshold;

        [Tooltip("附加条件·触发条件（可解析时；空 = 无结构化附加条件）。")]
        public string extraTrigger;

        [Tooltip("附加条件·比较符（可解析时）。")]
        public string extraComparator;

        [Tooltip("附加条件·阈值原文（可解析时，非负整数）。")]
        public string extraThreshold;

        [Tooltip("附加条件原文（无论可解析与否都保留，供日志与策划核对；A8）。")]
        public string extraConditionRaw;

        [Tooltip("效果类型1（6 种效果之一）。")]
        public string effect;

        [Tooltip("效果参数1 原文（必填，非负 decimal；如 2.4500000000000002 精确保存）。")]
        public string effectParam1;

        [Tooltip("效果参数2 原文（非负 decimal；空 = 0）。")]
        public string effectParam2;

        [Tooltip("效果列原文（当前全空 = 预留，仅存原文；A9）。")]
        public string effectRaw;

        [Tooltip("效果上限原文（非负 decimal；空 = 无上限）。")]
        public string effectCap;

        [Tooltip("独立结算：是/否。")]
        public bool independentSettlement;
    }

    /// <summary>
    /// 人格牌配置资产：16 张人格牌（PER_001~016）的配表落地，由菜单「导入人格牌配置数据」写入。
    /// P0-1E 白盒语义：空条目资产合法（Battle 门面回落 = 空模板目录，教学 3 张独立静态锚点），
    /// 因此 PER_001~016 齐全校验不在此层（在 PersonaTableMapper 导入层，防误删）。
    /// 程序集边界：Data 不能引用 Battle → 枚举文本（品质/触发条件/比较符/效果类型）以 string 存规范值，
    /// 由 Battle 门面 Configure(条目列表) 时转换；数值列存 string 原文（如 PER_013 的 2.4500000000000002 精确保存），空串 = 无。
    /// </summary>
    [CreateAssetMenu(menuName = "PersonaCards/PersonaConfig", fileName = "PersonaConfig")]
    public sealed class PersonaConfigAsset : ScriptableObject
    {
        /// <summary>资产固定路径（导入命令与场景构建器共用）。</summary>
        public const string AssetPath = "Assets/PersonaCards/Data/PersonaConfig.asset";

        [Tooltip("人格牌配置条目（PER_001~016 齐全，导入后按人格牌_ID 升序；空列表 = 白盒合法）。")]
        public List<PersonaConfigEntry> entries = new List<PersonaConfigEntry>();

        /// <summary>
        /// 单错误模式校验（同 CardConfigAsset 惯例）：返回 false 时 error 带原因，调用方警告并回落白盒。
        /// 空条目列表 = 合法（白盒）。校验规则与 PersonaTableContract 常量对照（契约不变式）。
        /// </summary>
        public bool Validate(out string error)
        {
            var seenPersonaIds = new HashSet<string>();
            foreach (var entry in entries)
            {
                // 条目与权威键
                if (entry == null)
                {
                    error = "存在空条目：请删除或重新导入。";
                    return false;
                }
                if (string.IsNullOrEmpty(entry.personaId))
                {
                    error = "存在「人格牌_ID」为空的条目：请删除或重新导入。";
                    return false;
                }
                if (!seenPersonaIds.Add(entry.personaId))
                {
                    error = $"「人格牌_ID」重复：{entry.personaId}（必须唯一）。";
                    return false;
                }

                // 名称（暂定占位但不得为空）
                if (string.IsNullOrEmpty(entry.displayName))
                {
                    error = $"「{entry.personaId}」的「人格牌名称」为空。";
                    return false;
                }

                // 四文本枚举只认规范值（品质「特殊」的兼容映射只在 Mapper 层，防双处兼容漂移）
                if (Array.IndexOf(PersonaTableContract.QualityValues, entry.quality) < 0)
                {
                    error = $"「{entry.personaId}」的「品质类型」值「{entry.quality}」无效，应为 {string.Join("/", PersonaTableContract.QualityValues)}。";
                    return false;
                }
                if (Array.IndexOf(PersonaTableContract.TriggerValues, entry.trigger) < 0)
                {
                    error = $"「{entry.personaId}」的「触发条件」值「{entry.trigger}」无效。";
                    return false;
                }
                if (Array.IndexOf(PersonaTableContract.ComparatorValues, entry.comparator) < 0)
                {
                    error = $"「{entry.personaId}」的「比较符」值「{entry.comparator}」无效。";
                    return false;
                }
                if (Array.IndexOf(PersonaTableContract.EffectValues, entry.effect) < 0)
                {
                    error = $"「{entry.personaId}」的「效果类型1」值「{entry.effect}」无效。";
                    return false;
                }

                // 行为标签_ID：空或 T\d{2} 格式
                if (entry.behaviorTagId.Length > 0
                    && !Regex.IsMatch(entry.behaviorTagId, PersonaTableContract.BehaviorTagPattern))
                {
                    error = $"「{entry.personaId}」的「行为标签_ID」值「{entry.behaviorTagId}」格式无效，应为 T01~T99。";
                    return false;
                }

                // 条件阈值原文：空允许；非空必须非负整数
                if (entry.threshold.Length > 0
                    && (!int.TryParse(entry.threshold, NumberStyles.Integer, CultureInfo.InvariantCulture, out var thresholdValue)
                        || thresholdValue < 0))
                {
                    error = $"「{entry.personaId}」的「条件阈值」值「{entry.threshold}」不是非负整数。";
                    return false;
                }

                // 效果参数1：必填非负 decimal；效果参数2：空允许、非空非负 decimal；效果上限：空允许、非空非负 decimal
                if (entry.effectParam1.Length == 0
                    || !decimal.TryParse(entry.effectParam1, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectParam1Value)
                    || effectParam1Value < 0m)
                {
                    error = $"「{entry.personaId}」的「效果参数1」值「{entry.effectParam1}」不是非负数字。";
                    return false;
                }
                if (entry.effectParam2.Length > 0
                    && (!decimal.TryParse(entry.effectParam2, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectParam2Value)
                        || effectParam2Value < 0m))
                {
                    error = $"「{entry.personaId}」的「效果参数2」值「{entry.effectParam2}」不是非负数字。";
                    return false;
                }
                if (entry.effectCap.Length > 0
                    && (!decimal.TryParse(entry.effectCap, NumberStyles.Number, CultureInfo.InvariantCulture, out var effectCapValue)
                        || effectCapValue < 0m))
                {
                    error = $"「{entry.personaId}」的「效果上限」值「{entry.effectCap}」不是非负数字。";
                    return false;
                }

                // 附加条件一致性：extraTrigger 非空时 extraComparator/extraThreshold 必须有效；反之不能只有一半
                if (entry.extraTrigger.Length > 0)
                {
                    if (Array.IndexOf(PersonaTableContract.ComparatorValues, entry.extraComparator) < 0
                        || entry.extraThreshold.Length == 0
                        || !int.TryParse(entry.extraThreshold, NumberStyles.Integer, CultureInfo.InvariantCulture, out var extraThresholdValue)
                        || extraThresholdValue < 0)
                    {
                        error = $"「{entry.personaId}」的附加条件不完整（触发条件「{entry.extraTrigger}」缺有效比较符/阈值）。";
                        return false;
                    }
                }
                else if (entry.extraComparator.Length > 0 || entry.extraThreshold.Length > 0)
                {
                    error = $"「{entry.personaId}」的附加条件只有比较符/阈值而无触发条件。";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
