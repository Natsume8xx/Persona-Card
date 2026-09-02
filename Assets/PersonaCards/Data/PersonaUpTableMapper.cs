using System;
using System.Collections.Generic;

namespace PersonaCards.Data
{
    /// <summary>
    /// 人格牌强化配表契约（P0-1J）：与策划表格「商品_人格牌强化」sheet 的表头与枚举值约定。
    /// 修改表格结构或枚举值必须同步此处（契约变更需双方确认，并在 Docs/KF/P0-1J.md 记录）。
    /// 「主属性类型」三值定义在 PersonaUpRuleTableContract（强化规则表）共享。
    /// </summary>
    public static class PersonaUpTableContract
    {
        /// <summary>工作表名（人格牌强化数据）。</summary>
        public const string SheetName = "商品_人格牌强化";

        /// <summary>列名：人格_ID（PER_xxx；行标识，与人格牌配置表人格牌_ID 对应）。</summary>
        public const string ColPersonaId = "人格_ID";

        /// <summary>列名：人格名称（显示名）。</summary>
        public const string ColPersonaName = "人格名称";

        /// <summary>列名：主属性类型（筹码型/倍率型/独立倍率型，集合见 PersonaUpRuleTableContract）。</summary>
        public const string ColMainAttrType = "主属性类型";

        /// <summary>列名：Lv0（基础值；原文存储，整数与小数混写，语义解析留给后续阶段）。</summary>
        public const string ColLv0 = "Lv0";

        /// <summary>列名：Lv1（原文存储）。</summary>
        public const string ColLv1 = "Lv1";

        /// <summary>列名：Lv2（原文存储）。</summary>
        public const string ColLv2 = "Lv2";

        /// <summary>列名：Lv3（原文存储）。</summary>
        public const string ColLv3 = "Lv3";

        /// <summary>列名：Lv4（原文存储）。</summary>
        public const string ColLv4 = "Lv4";

        /// <summary>Lv 列名数组（Lv0~Lv4，遍历校验用）。</summary>
        public static readonly string[] LvColumns = { ColLv0, ColLv1, ColLv2, ColLv3, ColLv4 };
    }

    /// <summary>人格牌强化映射结果：Succeeded 为 true 时 Entries 可用；Errors 非空即失败（导入命令不得写入资产）。</summary>
    public sealed class PersonaUpMappingResult
    {
        public PersonaUpMappingResult(bool succeeded, List<PersonaUpEntry> entries, List<string> errors, List<string> warnings)
        {
            Succeeded = succeeded;
            Entries = entries;
            Errors = errors;
            Warnings = warnings;
        }

        /// <summary>是否全部行映射成功（false 时 Entries 为 null，资产零改动）。</summary>
        public bool Succeeded { get; }

        /// <summary>映射出的人格牌强化条目（按人格_ID 升序；仅 Succeeded 时非 null）。</summary>
        public IReadOnlyList<PersonaUpEntry> Entries { get; }

        /// <summary>全部错误（带行号定位，不 fail-fast，策划一次看到所有问题）。</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>全部警告（当前无触发场景，保留字段与同阶段结果结构一致）。</summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// 人格牌强化配表映射器：把 XlsxTableReader 输出的行字典列表转成 PersonaUpEntry 列表。
    /// 规则：人格_ID 必填唯一；人格名称必填；主属性类型只认三值；Lv0~Lv4 必填原文存储（混写不解析）。
    /// </summary>
    public static class PersonaUpTableMapper
    {
        /// <summary>映射行字典列表（XlsxTableReader.ReadTable 的输出）。</summary>
        public static PersonaUpMappingResult Map(List<Dictionary<string, string>> rows)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("商品_人格牌强化没有任何数据行。");
                return new PersonaUpMappingResult(false, null, errors, warnings);
            }

            var entries = new List<PersonaUpEntry>();
            var seenPersonaIds = new HashSet<string>();

            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                var personaId = Get(row, PersonaUpTableContract.ColPersonaId);
                var label = $"第 {rowIndex + 2} 行「{personaId}」"; // +2 = 表头行占 1 行，行号从数据行 1 起

                // 人格_ID：必填、唯一
                if (string.IsNullOrEmpty(personaId))
                {
                    errors.Add($"{label}：「人格_ID」为空（必填）。");
                    continue;
                }
                if (!seenPersonaIds.Add(personaId))
                {
                    errors.Add($"{label}：「人格_ID」重复，必须唯一。");
                    continue;
                }

                // 人格名称：必填（显示名）
                var personaName = Get(row, PersonaUpTableContract.ColPersonaName);
                if (string.IsNullOrWhiteSpace(personaName))
                {
                    errors.Add($"{label}：「人格名称」为空（必填）。");
                    continue;
                }

                // 主属性类型：只认三值（集合见强化规则表契约）
                var mainAttrType = Get(row, PersonaUpTableContract.ColMainAttrType);
                if (Array.IndexOf(PersonaUpRuleTableContract.MainAttrTypes, mainAttrType) < 0)
                {
                    errors.Add($"{label}：「主属性类型」值「{mainAttrType}」无效，应为 {string.Join("/", PersonaUpRuleTableContract.MainAttrTypes)}。");
                    continue;
                }

                // Lv0~Lv4：必填原文存储（15/1.3/0.05 混写，语义解析留给后续阶段）
                var levels = new string[PersonaUpTableContract.LvColumns.Length];
                var hasMissing = false;
                for (var columnIndex = 0; columnIndex < PersonaUpTableContract.LvColumns.Length; columnIndex++)
                {
                    levels[columnIndex] = Get(row, PersonaUpTableContract.LvColumns[columnIndex]);
                    if (string.IsNullOrEmpty(levels[columnIndex]))
                    {
                        errors.Add($"{label}：「{PersonaUpTableContract.LvColumns[columnIndex]}」为空（必填）。");
                        hasMissing = true;
                    }
                }
                if (hasMissing) continue;

                entries.Add(new PersonaUpEntry
                {
                    personaId = personaId,
                    personaName = personaName,
                    mainAttrType = mainAttrType,
                    lv0 = levels[0],
                    lv1 = levels[1],
                    lv2 = levels[2],
                    lv3 = levels[3],
                    lv4 = levels[4]
                });
            }

            if (errors.Count > 0)
            {
                return new PersonaUpMappingResult(false, null, errors, warnings);
            }

            // 按人格_ID 升序排列条目（资产 Inspector 与日志的可读性）
            entries.Sort((left, right) => string.CompareOrdinal(left.personaId, right.personaId));

            return new PersonaUpMappingResult(true, entries, errors, warnings);
        }

        /// <summary>取单元格文本；缺列与空单元格都按空串处理（调用方按需用 ContainsKey 区分）。</summary>
        private static string Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) ? value : "";
    }
}
