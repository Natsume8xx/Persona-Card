using System;
using System.Collections.Generic;
using System.Globalization;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌次级属性配表契约（P0-1J 三表之一）：与策划表格「人格牌_次级属性」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 「所属人格」列当前填人格牌名称（非 PER_ID）；属性参数2 混写整数与小数（8/20/0.3/0.03/0.5/1/5），一律原文存储。
    /// </summary>
    public static class PersonaSubAttrTableContract
    {
        /// <summary>工作表名（次级属性数据）。</summary>
        public const string SheetName = "人格牌_次级属性";

        /// <summary>列名：次级属性_ID（SUB_xxx；行标识）。</summary>
        public const string ColSubAttrId = "次级属性_ID";

        /// <summary>列名：所属人格（当前填人格牌名称，原文存储）。</summary>
        public const string ColOwnerPersona = "所属人格";

        /// <summary>列名：权重（非负整数，池内抽取权重）。</summary>
        public const string ColWeight = "权重";

        /// <summary>列名：属性类型（基础筹码/基础倍率/独立倍率/出牌次数/弃牌次数/金币等，原文存储）。</summary>
        public const string ColAttrType = "属性类型";

        /// <summary>列名：属性参数1（当前全「增加」，原文存储）。</summary>
        public const string ColParam1 = "属性参数1";

        /// <summary>列名：属性参数2（数值混写，原文存储）。</summary>
        public const string ColParam2 = "属性参数2";

        /// <summary>列名：开放节点（AI1/AI2/AI3，原文存储）。</summary>
        public const string ColUnlockNode = "开放节点";
    }

    /// <summary>次级属性配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class PersonaSubAttrMappingResult
    {
        public PersonaSubAttrMappingResult(bool succeeded, List<PersonaSubAttrEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（资产形态，按次级属性_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaSubAttrEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 次级属性配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaSubAttrEntry 列表。
    /// 规则：次级属性_ID 必填唯一；所属人格/属性类型/属性参数1/开放节点必填；权重非负整数；属性参数2 原文存储（允许空）。
    /// </summary>
    public static class PersonaSubAttrTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static PersonaSubAttrMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("次级属性表没有任何数据行。");
                return new PersonaSubAttrMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaSubAttrEntry>();
            var seenSubAttrIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var subAttrId = Get(row, PersonaSubAttrTableContract.ColSubAttrId);
                var label = $"第 {rowIndex + 2} 行「{subAttrId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 次级属性_ID：必填、唯一
                if (string.IsNullOrEmpty(subAttrId))
                {
                    errors.Add($"{label}：「次级属性_ID」为空（必填）。");
                    continue;
                }
                if (!seenSubAttrIds.Add(subAttrId))
                {
                    errors.Add($"{label}：「次级属性_ID」重复，必须唯一。");
                    continue;
                }

                // 所属人格：必填，原文存储（当前是人格牌名称；接线时再定名称↔ID 映射）
                var ownerPersona = Get(row, PersonaSubAttrTableContract.ColOwnerPersona);
                if (string.IsNullOrEmpty(ownerPersona))
                {
                    errors.Add($"{label}：「所属人格」为空（必填）。");
                    continue;
                }

                // 权重：非负整数（池内抽取权重）
                var weightText = Get(row, PersonaSubAttrTableContract.ColWeight);
                if (!int.TryParse(weightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) || weight < 0)
                {
                    errors.Add($"{label}：「权重」值「{weightText}」不是非负整数。");
                    continue;
                }

                // 属性类型：必填，原文存储（B7 接线时解析）
                var attrType = Get(row, PersonaSubAttrTableContract.ColAttrType);
                if (string.IsNullOrEmpty(attrType))
                {
                    errors.Add($"{label}：「属性类型」为空（必填）。");
                    continue;
                }

                // 属性参数1：必填，原文存储（当前全「增加」）
                var param1 = Get(row, PersonaSubAttrTableContract.ColParam1);
                if (string.IsNullOrEmpty(param1))
                {
                    errors.Add($"{label}：「属性参数1」为空（必填）。");
                    continue;
                }

                // 属性参数2：原文存储（整数与小数混写；允许空）
                var param2 = Get(row, PersonaSubAttrTableContract.ColParam2);

                // 开放节点：必填，原文存储
                var unlockNode = Get(row, PersonaSubAttrTableContract.ColUnlockNode);
                if (string.IsNullOrEmpty(unlockNode))
                {
                    errors.Add($"{label}：「开放节点」为空（必填）。");
                    continue;
                }

                entries.Add(new PersonaSubAttrEntry
                {
                    subAttrId = subAttrId,
                    ownerPersona = ownerPersona,
                    weight = weight,
                    attrType = attrType,
                    param1 = param1,
                    param2 = param2,
                    unlockNode = unlockNode
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaSubAttrMappingResult(false, null, errors, warnings);
            }

            // 按次级属性_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.subAttrId, right.subAttrId));

            return new PersonaSubAttrMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
