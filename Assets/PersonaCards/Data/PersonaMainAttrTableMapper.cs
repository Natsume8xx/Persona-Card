using System;
using System.Collections.Generic;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌主属性配表契约（P0-1J 三表之一）：与策划表格「人格牌_主属性」sheet 的表头约定。
    /// 修改表格结构必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 属性参数2 混写整数与小数（15/40/30/1/0.05），一律原文存储——语义解析留给 B7，不在导入层引入格式判定。
    /// </summary>
    public static class PersonaMainAttrTableContract
    {
        /// <summary>工作表名（主属性数据）。</summary>
        public const string SheetName = "人格牌_主属性";

        /// <summary>列名：主属性_ID（MAIN_xxx；行标识，人格牌配置「主属性_ID」列引用）。</summary>
        public const string ColAttrId = "主属性_ID";

        /// <summary>列名：属性类型（基础筹码/基础倍率/独立倍率等，原文存储）。</summary>
        public const string ColAttrType = "属性类型";

        /// <summary>列名：属性参数1（当前全「增加」，原文存储）。</summary>
        public const string ColParam1 = "属性参数1";

        /// <summary>列名：属性参数2（数值混写，原文存储）。</summary>
        public const string ColParam2 = "属性参数2";

        /// <summary>列名：开放节点（默认/AI1…，原文存储）。</summary>
        public const string ColUnlockNode = "开放节点";
    }

    /// <summary>主属性配表映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）；Warnings 无论成败都可能非空。</summary>
    public sealed class PersonaMainAttrMappingResult
    {
        public PersonaMainAttrMappingResult(bool succeeded, List<PersonaMainAttrEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的条目（资产形态，按主属性_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaMainAttrEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 主属性配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaMainAttrEntry 列表。
    /// 规则：主属性_ID 必填唯一；属性类型/属性参数1/开放节点必填；属性参数2 原文存储（允许空）。
    /// </summary>
    public static class PersonaMainAttrTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static PersonaMainAttrMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("主属性表没有任何数据行。");
                return new PersonaMainAttrMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaMainAttrEntry>();
            var seenAttrIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var attrId = Get(row, PersonaMainAttrTableContract.ColAttrId);
                var label = $"第 {rowIndex + 2} 行「{attrId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 主属性_ID：必填、唯一
                if (string.IsNullOrEmpty(attrId))
                {
                    errors.Add($"{label}：「主属性_ID」为空（必填）。");
                    continue;
                }
                if (!seenAttrIds.Add(attrId))
                {
                    errors.Add($"{label}：「主属性_ID」重复，必须唯一。");
                    continue;
                }

                // 属性类型：必填，原文存储（B7 接线时解析）
                var attrType = Get(row, PersonaMainAttrTableContract.ColAttrType);
                if (string.IsNullOrEmpty(attrType))
                {
                    errors.Add($"{label}：「属性类型」为空（必填）。");
                    continue;
                }

                // 属性参数1：必填，原文存储（当前全「增加」）
                var param1 = Get(row, PersonaMainAttrTableContract.ColParam1);
                if (string.IsNullOrEmpty(param1))
                {
                    errors.Add($"{label}：「属性参数1」为空（必填）。");
                    continue;
                }

                // 属性参数2：原文存储（整数与小数混写；允许空）
                var param2 = Get(row, PersonaMainAttrTableContract.ColParam2);

                // 开放节点：必填，原文存储
                var unlockNode = Get(row, PersonaMainAttrTableContract.ColUnlockNode);
                if (string.IsNullOrEmpty(unlockNode))
                {
                    errors.Add($"{label}：「开放节点」为空（必填）。");
                    continue;
                }

                entries.Add(new PersonaMainAttrEntry
                {
                    attrId = attrId,
                    attrType = attrType,
                    param1 = param1,
                    param2 = param2,
                    unlockNode = unlockNode
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaMainAttrMappingResult(false, null, errors, warnings);
            }

            // 按主属性_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.attrId, right.attrId));

            return new PersonaMainAttrMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
