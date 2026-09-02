using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 关卡流程配表契约：与策划表格「关卡流程」sheet 的表头与枚举值约定。
    /// 修改表格结构或枚举值必须同步此处（契约变更需双方确认，并在 Docs/KF 开发文档记录）。
    /// </summary>
    public static class RunRouteTableContract
    {
        /// <summary>工作表名。</summary>
        public const string SheetName = "关卡流程";

        /// <summary>列名：关卡名称（仅用于错误/警告消息定位，不影响导入数据）。</summary>
        public const string ColStageName = "关卡名称";

        /// <summary>列名：阶段_ID（原文存储，仅定位与展示用，不参与校验）。</summary>
        public const string ColStageId = "阶段_ID";

        /// <summary>列名：顺序（必填，正整数，决定节点次序）。</summary>
        public const string ColOrder = "顺序";

        /// <summary>列名：关卡类型（必填）。</summary>
        public const string ColKind = "关卡类型";

        /// <summary>列名：分数类型（战斗节点=通关分数、生成节点=无；缺列或空静默跳过，违反仅警告）。</summary>
        public const string ColScoreType = "分数类型";

        /// <summary>列名：分数参数（战斗类节点必填，目标分）。</summary>
        public const string ColScore = "分数参数";

        /// <summary>列名：手牌限制（空或 0 = 默认值 4，仅战斗类节点）。</summary>
        public const string ColPlays = "手牌限制";

        /// <summary>列名：弃牌限制（空或 0 = 默认值 3，仅战斗类节点）。</summary>
        public const string ColDiscards = "弃牌限制";

        /// <summary>列名：AI节点（仅人格牌生成节点有效；「0」按空处理——配表统一用 0 占位）。</summary>
        public const string ColAiNode = "AI节点";

        /// <summary>列名：奖励类型1 / 奖励参数1 / 奖励类型2 / 奖励参数2（全部原文存储，参数列不校验格式；语义接线留给后续阶段）。</summary>
        public const string ColRewardType1 = "奖励类型1";
        public const string ColRewardParam1 = "奖励参数1";
        public const string ColRewardType2 = "奖励类型2";
        public const string ColRewardParam2 = "奖励参数2";

        /// <summary>列名：是否商店（战斗类节点有效；列已从配表永久删除，缺列或空默认"是"，最终关除外，不再发缺列提示）。</summary>
        public const string ColShopAfter = "是否商店";

        /// <summary>关卡类型枚举值：普通战斗。</summary>
        public const string KindNormal = "普通战斗";

        /// <summary>关卡类型旧写法：旧表用"战斗"表示普通战斗，导入时兼容并提示策划改名。</summary>
        public const string KindLegacyNormal = "战斗";

        /// <summary>关卡类型枚举值：Boss 战斗。</summary>
        public const string KindBoss = "Boss战斗";

        /// <summary>关卡类型枚举值：人格牌生成。</summary>
        public const string KindGen = "人格牌生成";

        /// <summary>分数类型枚举值：通关分数（战斗节点）。</summary>
        public const string ScoreTypePass = "通关分数";

        /// <summary>分数类型枚举值：无（生成节点）。</summary>
        public const string ScoreTypeNone = "无";

        /// <summary>奖励类型枚举值：金币。</summary>
        public const string RewardGold = "金币";

        /// <summary>奖励类型枚举值：无。</summary>
        public const string RewardNone = "无";

        /// <summary>奖励类型枚举值：人格牌。</summary>
        public const string RewardPersona = "人格牌";

        /// <summary>是否商店枚举值：是。</summary>
        public const string ShopYes = "是";

        /// <summary>是否商店枚举值：否。</summary>
        public const string ShopNo = "否";
    }

    /// <summary>配表映射结果：Succeeded 为 true 时 Nodes 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class RunRouteMappingResult
    {
        public RunRouteMappingResult(bool succeeded, List<RunBattleNode> nodes, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Nodes = nodes;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Nodes 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的节点列表（仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<RunBattleNode> Nodes { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（旧写法兼容、缺省回落、Boss 池溢出等提示）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 关卡流程配表映射器：把 XlsxTableReader 输出的行字典列表转成 RunRouteAsset 节点列表。
    /// 规则：按"顺序"列排序（重复/非正整数 = 错误，不连续 = 警告）；行级全量校验，任一行出错整体失败；
    /// Boss 难度池按出现序自动分配（第 1/2/3 个 → 初级/中级/高级，第 4+ 个 → 高级 + 警告）；
    /// 阶段_ID/奖励 4 列原文存储；AI 节点「0」按空处理。
    /// </summary>
    public static class RunRouteTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）；结果见 RunRouteMappingResult。</summary>
        public static RunRouteMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("关卡流程表没有任何数据行。");
                return new RunRouteMappingResult(false, null, errors, warnings);
            }

            // 第一步：解析顺序列（决定节点次序），重复与非正整数直接判错
            var ordered = new List<KeyValuePair<int, Dictionary<string, string>>>();
            var seen = new HashSet<int>();
            foreach (var row in rows)
            {
                var orderText = Get(row, RunRouteTableContract.ColOrder);
                var label = $"「{StageNameOf(row)}」";
                if (!int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order) || order < 1)
                {
                    errors.Add($"行 {label} 的顺序列「{orderText}」不是正整数。");
                    continue;
                }
                if (!seen.Add(order))
                {
                    errors.Add($"顺序 {order} 重复（{label}）：顺序列必须唯一。");
                    continue;
                }
                ordered.Add(new KeyValuePair<int, Dictionary<string, string>>(order, row));
            }
            if (errors.Count > 0)
                return new RunRouteMappingResult(false, null, errors, warnings);

            ordered.Sort((left, right) => left.Key.CompareTo(right.Key));

            // 顺序不连续：仅警告（策划可能预留空档），不阻断导入
            if (ordered[0].Key != 1)
                warnings.Add($"顺序列从 {ordered[0].Key} 开始而不是 1。");
            for (var index = 1; index < ordered.Count; index++)
            {
                if (ordered[index].Key != ordered[index - 1].Key + 1)
                    warnings.Add($"顺序列不连续：{ordered[index - 1].Key} 之后直接是 {ordered[index].Key}。");
            }

            // 第二步：行级全量校验 + 节点构造（错误全部收集，不 fail-fast）
            var nodes = new List<RunBattleNode>();
            var bossAppearance = 0; // Boss 出现序：第 1 个 → 初级池，第 2 个 → 中级池，第 3+ 个 → 高级池

            for (var index = 0; index < ordered.Count; index++)
            {
                var order = ordered[index].Key;
                var row = ordered[index].Value;
                var isFinal = index == ordered.Count - 1;
                var label = $"{order}「{StageNameOf(row)}」";

                // 关卡类型（必填）；"战斗"是旧写法，兼容为普通战斗并提示改名
                var kindText = Get(row, RunRouteTableContract.ColKind);
                RunNodeKind kind;
                switch (kindText)
                {
                    case RunRouteTableContract.KindNormal:
                        kind = RunNodeKind.NormalBattle;
                        break;
                    case RunRouteTableContract.KindBoss:
                        kind = RunNodeKind.BossBattle;
                        break;
                    case RunRouteTableContract.KindGen:
                        kind = RunNodeKind.PersonaGen;
                        break;
                    case RunRouteTableContract.KindLegacyNormal:
                        kind = RunNodeKind.NormalBattle;
                        warnings.Add($"行 {label}：关卡类型「战斗」是旧写法，请改为「普通战斗」（本次按普通战斗导入）。");
                        break;
                    default:
                        errors.Add($"行 {label}：关卡类型「{kindText}」无效，应为「普通战斗」「Boss战斗」或「人格牌生成」。");
                        continue;
                }

                // 最终节点必须是战斗类（生成节点之后必须有下一节点，否则流程无处可去）
                if (isFinal && !RunRouteAsset.IsBattleKind(kind))
                {
                    errors.Add($"行 {label}：最终节点必须是战斗类型（普通战斗或 Boss 战斗）。");
                    continue;
                }

                // 手牌/弃牌限制：空或 0 = 默认值（4/3）；负数或非整数 = 错误
                var playsText = Get(row, RunRouteTableContract.ColPlays);
                var playsLimit = ParseNonNegativeInt(playsText);
                if (playsLimit < 0)
                {
                    errors.Add($"行 {label}：手牌限制「{playsText}」不是非负整数。");
                    continue;
                }
                var discardsText = Get(row, RunRouteTableContract.ColDiscards);
                var discardsLimit = ParseNonNegativeInt(discardsText);
                if (discardsLimit < 0)
                {
                    errors.Add($"行 {label}：弃牌限制「{discardsText}」不是非负整数。");
                    continue;
                }

                // 阶段_ID / 奖励 4 列：一律原文存储（语义接线留给后续阶段），奖励类型仅做枚举合法性提示
                var stageId = Get(row, RunRouteTableContract.ColStageId);
                var rewardType1 = Get(row, RunRouteTableContract.ColRewardType1);
                var rewardParam1 = Get(row, RunRouteTableContract.ColRewardParam1);
                var rewardType2 = Get(row, RunRouteTableContract.ColRewardType2);
                var rewardParam2 = Get(row, RunRouteTableContract.ColRewardParam2);
                ValidateRewardType(label, rewardType1, warnings);
                ValidateRewardType(label, rewardType2, warnings);

                // 分数类型：战斗节点必须「通关分数」、生成节点必须「无」；缺列或空（旧表）静默跳过
                var scoreTypeText = Get(row, RunRouteTableContract.ColScoreType);
                if (!string.IsNullOrEmpty(scoreTypeText))
                {
                    var expectedScoreType = kind == RunNodeKind.PersonaGen
                        ? RunRouteTableContract.ScoreTypeNone
                        : RunRouteTableContract.ScoreTypePass;
                    if (scoreTypeText != expectedScoreType)
                        warnings.Add($"行 {label}：分数类型「{scoreTypeText}」与关卡类型不符，应为「{expectedScoreType}」。");
                }

                if (kind == RunNodeKind.PersonaGen)
                {
                    nodes.Add(MapPersonaGenNode(row, label, playsLimit, discardsLimit, stageId,
                        rewardType1, rewardParam1, rewardType2, rewardParam2, errors, warnings));
                    continue;
                }

                // —— 以下为战斗类节点（普通战斗 / Boss 战斗）——
                // 分数参数：必填正整数（目标分）
                var scoreText = Get(row, RunRouteTableContract.ColScore);
                if (!long.TryParse(scoreText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetScore) || targetScore <= 0)
                {
                    errors.Add($"行 {label}：分数参数「{scoreText}」不是正整数（战斗关卡必须指定目标分）。");
                    continue;
                }

                // 是否商店："是"/"否"直接解析；缺列或空默认"是"（最终关静默 false）——列已从配表永久删除，缺列不再发全局提示；非法值报错
                var shopText = Get(row, RunRouteTableContract.ColShopAfter);
                bool hasShopAfter;
                if (shopText == RunRouteTableContract.ShopYes)
                {
                    if (isFinal)
                    {
                        errors.Add($"行 {label}：最终节点不能配置「是否商店 = 是」（流程无后续阶段可接商店）。");
                        continue;
                    }
                    hasShopAfter = true;
                }
                else if (shopText == RunRouteTableContract.ShopNo)
                {
                    hasShopAfter = false;
                }
                else if (string.IsNullOrEmpty(shopText))
                {
                    hasShopAfter = !isFinal; // 缺省回落"是"；最终关空值合法（本来就没有商店），静默 false
                    if (!isFinal && row.ContainsKey(RunRouteTableContract.ColShopAfter))
                        warnings.Add($"行 {label}：「是否商店」为空，默认结束后进商店。");
                }
                else
                {
                    errors.Add($"行 {label}：「是否商店」值「{shopText}」无效，应为「是」或「否」。");
                    continue;
                }

                // AI节点：仅生成节点使用；「0」按空处理（当前配表战斗行统一填 0），其他非空值提示忽略
                if (!AiNodeIsEmpty(Get(row, RunRouteTableContract.ColAiNode)))
                    warnings.Add($"行 {label}：战斗节点指定了 AI 节点（仅人格牌生成节点使用），已忽略。");

                // Boss 难度池按出现序自动分配
                var pool = BossPoolId.None;
                if (kind == RunNodeKind.BossBattle)
                {
                    bossAppearance++;
                    pool = bossAppearance == 1 ? BossPoolId.Primary
                        : bossAppearance == 2 ? BossPoolId.Intermediate
                        : BossPoolId.Advanced;
                    if (bossAppearance > 3)
                        warnings.Add($"行 {label}：第 {bossAppearance} 场 Boss 超出 3 个难度池，已使用高级池（当前版本共 3 池）。");
                }

                nodes.Add(new RunBattleNode(kind, targetScore, pool, hasShopAfter, playsLimit, discardsLimit,
                    stageId: stageId, rewardType1: rewardType1, rewardParam1: rewardParam1,
                    rewardType2: rewardType2, rewardParam2: rewardParam2));
            }

            return new RunRouteMappingResult(errors.Count == 0, errors.Count == 0 ? nodes : null, errors, warnings);
        }

        /// <summary>构造人格牌生成节点：生成数量固定 1（配表「人格牌生成数量」列已删除）；战斗字段（分数/限制）填非零值提示忽略；AI 节点未指定提示；显式"是否商店 = 是"报错。</summary>
        private static RunBattleNode MapPersonaGenNode(Dictionary<string, string> row, string label,
            int playsLimit, int discardsLimit, string stageId,
            string rewardType1, string rewardParam1, string rewardType2, string rewardParam2,
            List<string> errors, List<string> warnings)
        {
            // 战斗字段在生成节点上无效：「0」= 未填（当前配表生成行统一填 0），填了非零值则提示忽略
            var scoreText = Get(row, RunRouteTableContract.ColScore);
            if ((!string.IsNullOrEmpty(scoreText) && scoreText.Trim() != "0") || playsLimit != 0 || discardsLimit != 0)
                warnings.Add($"行 {label}：人格牌生成节点忽略战斗字段（分数参数/手牌限制/弃牌限制）。");

            // AI节点：生成节点未指定则提示（「0」按空处理；配表 STAGE_16「商店」行 AI 节点为 0，属策划待确认项）
            if (AiNodeIsEmpty(Get(row, RunRouteTableContract.ColAiNode)))
                warnings.Add($"行 {label}：人格牌生成节点未指定 AI 节点。");

            // 是否商店：生成节点不能接商店（流程确认后直接推进）
            var shopText = Get(row, RunRouteTableContract.ColShopAfter);
            if (shopText == RunRouteTableContract.ShopYes)
                errors.Add($"行 {label}：人格牌生成节点不能配置「是否商店 = 是」（确认后直接推进到下一节点）。");
            else if (!string.IsNullOrEmpty(shopText) && shopText != RunRouteTableContract.ShopNo)
                errors.Add($"行 {label}：「是否商店」值「{shopText}」无效，应为「是」或「否」。");

            return new RunBattleNode(RunNodeKind.PersonaGen, 0, BossPoolId.None, false, genCount: 1,
                stageId: stageId, rewardType1: rewardType1, rewardParam1: rewardParam1,
                rewardType2: rewardType2, rewardParam2: rewardParam2);
        }

        /// <summary>AI 节点列是否视为未填：「0」= 空（当前配表统一用 0 占位）。</summary>
        private static bool AiNodeIsEmpty(string text) => string.IsNullOrEmpty(text) || text.Trim() == "0";

        /// <summary>奖励类型合法性提示：非空且不在已知枚举（金币/无/人格牌）内 → 警告（原文照存不阻塞）。</summary>
        private static void ValidateRewardType(string label, string rewardType, List<string> warnings)
        {
            if (string.IsNullOrEmpty(rewardType)) return;
            if (rewardType != RunRouteTableContract.RewardGold
                && rewardType != RunRouteTableContract.RewardNone
                && rewardType != RunRouteTableContract.RewardPersona)
                warnings.Add($"行 {label}：奖励类型「{rewardType}」不在已知枚举（金币/无/人格牌）内，已按原文存储。");
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";

        /// <summary>行标签里的人类可读名称：取"关卡名称"列，空时用占位符。</summary>
        private static string StageNameOf(Dictionary<string, string> row)
        {
            var name = Get(row, RunRouteTableContract.ColStageName);
            return string.IsNullOrEmpty(name) ? "?" : name;
        }

        /// <summary>解析非负整数：空串 → 0（= 使用默认值）；非整数或负数 → -1（调用方判错）。</summary>
        private static int ParseNonNegativeInt(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= 0 ? value : -1;
        }
    }
}
