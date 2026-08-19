using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace PersonaCards.Core.Xlsx
{
    /// <summary>xlsx 读取异常：带上下文的读取/解析错误（sheet 缺失、坏文件、重复表头等）。</summary>
    public sealed class XlsxTableReaderException : Exception
    {
        public XlsxTableReaderException(string message) : base(message) { }

        public XlsxTableReaderException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// xlsx 表格读取器（纯 BCL，编辑器导入专用）：把指定工作表读成"表头 → 行字典"列表。
    /// 规则：表头行 = 首个非空行；"—"、空单元格 → 空串；全空行跳过；数值单元格按 invariant 字符串返回。
    /// </summary>
    public static class XlsxTableReader
    {
        /// <summary>工作表主命名空间（workbook/sheet/sharedStrings 共用）。</summary>
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        /// <summary>包关系命名空间（_rels 文件里的 Relationship 元素）。</summary>
        private static readonly XNamespace PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        /// <summary>读取指定 sheet 全部数据行；xlsxStream 由调用方持有与定位（本方法不关闭流）。</summary>
        public static List<Dictionary<string, string>> ReadTable(Stream xlsxStream, string sheetName)
        {
            if (xlsxStream == null) throw new ArgumentNullException(nameof(xlsxStream));
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("工作表名称不能为空。", nameof(sheetName));

            ZipArchive archive;
            try
            {
                archive = new ZipArchive(xlsxStream, ZipArchiveMode.Read, leaveOpen: true);
            }
            catch (Exception exception)
            {
                throw new XlsxTableReaderException("无法作为 xlsx（zip）打开：文件已损坏或格式错误。", exception);
            }

            using (archive)
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheetDocument = LoadEntry(archive, ResolveSheetPath(archive, sheetName));
                return ParseRows(sheetDocument, sharedStrings, sheetName);
            }
        }

        /// <summary>按 sheet 名定位工作表文件路径：workbook.xml 找 r:id → 关系表映射 Target（sheetId 不是文件名，必须走关系表）。</summary>
        private static string ResolveSheetPath(ZipArchive archive, string sheetName)
        {
            var workbook = LoadEntry(archive, "xl/workbook.xml");
            var sheetElement = workbook.Descendants(SpreadsheetNs + "sheet")
                .FirstOrDefault(element => LocalAttribute(element, "name") == sheetName);
            if (sheetElement == null)
                throw new XlsxTableReaderException($"找不到名为「{sheetName}」的工作表。");

            var relationshipId = LocalAttribute(sheetElement, "id");
            if (string.IsNullOrEmpty(relationshipId))
                throw new XlsxTableReaderException($"工作表「{sheetName}」缺少关系 id。");

            var target = LoadEntry(archive, "xl/_rels/workbook.xml.rels")
                .Descendants(PackageRelNs + "Relationship")
                .FirstOrDefault(element => LocalAttribute(element, "Id") == relationshipId)
                ?.Attribute("Target")?.Value;
            if (string.IsNullOrEmpty(target))
                throw new XlsxTableReaderException($"工作表「{sheetName}」的关系映射缺失（r:id {relationshipId}）。");

            // Target 可能是 "/xl/worksheets/sheet1.xml"（绝对）或 "worksheets/sheet1.xml"（相对 xl/ 目录）
            return target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
        }

        /// <summary>读取共享字符串表；文件缺失时返回空表（该工作簿可能没有字符串单元格）。</summary>
        private static string[] ReadSharedStrings(ZipArchive archive)
        {
            if (archive.GetEntry("xl/sharedStrings.xml") == null) return Array.Empty<string>();
            return LoadEntry(archive, "xl/sharedStrings.xml").Descendants(SpreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                .ToArray();
        }

        /// <summary>解析 sheetData：表头行 = 首个非空行；空表头列忽略；重复表头报错；"—" 与空单元格归一为 ""。</summary>
        private static List<Dictionary<string, string>> ParseRows(XDocument sheetDocument, string[] sharedStrings, string sheetName)
        {
            var rows = sheetDocument.Descendants(SpreadsheetNs + "row").ToList();
            if (rows.Count == 0)
                throw new XlsxTableReaderException($"工作表「{sheetName}」没有任何行。");

            var result = new List<Dictionary<string, string>>();
            var header = (List<string>)null;
            foreach (var rowElement in rows)
            {
                var cells = ReadRowCells(rowElement, sharedStrings);
                if (cells.TrueForAll(string.IsNullOrEmpty)) continue; // 全空行跳过（含表头前的空行）

                if (header == null)
                {
                    header = new List<string>();
                    foreach (var cell in cells)
                    {
                        var name = cell.Trim();
                        if (name.Length == 0)
                        {
                            header.Add(null); // 空表头列：该列数据忽略
                            continue;
                        }
                        if (header.Contains(name))
                            throw new XlsxTableReaderException($"工作表「{sheetName}」存在重复表头「{name}」。");
                        header.Add(name);
                    }
                    continue;
                }

                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < header.Count; index++)
                {
                    if (header[index] == null) continue;
                    row[header[index]] = index < cells.Count ? cells[index] : "";
                }
                result.Add(row);
            }

            if (header == null)
                throw new XlsxTableReaderException($"工作表「{sheetName}」没有表头行。");
            return result;
        }

        /// <summary>把一行读成按列序展开的字符串列表（稀疏单元格补空串）；列号优先取 r 属性，缺失时按出现顺序递增。</summary>
        private static List<string> ReadRowCells(XElement rowElement, string[] sharedStrings)
        {
            var byColumn = new Dictionary<int, string>();
            var maximumColumn = -1;
            var fallbackColumn = 0;
            foreach (var cell in rowElement.Elements(SpreadsheetNs + "c"))
            {
                var reference = LocalAttribute(cell, "r");
                var column = string.IsNullOrEmpty(reference) ? fallbackColumn : ColumnIndexOf(reference);
                fallbackColumn = column + 1;
                if (column > maximumColumn) maximumColumn = column;
                byColumn[column] = ReadCellValue(cell, sharedStrings);
            }

            var result = new List<string>(maximumColumn + 1);
            for (var index = 0; index <= maximumColumn; index++)
                result.Add(byColumn.TryGetValue(index, out var value) ? value : "");
            return result;
        }

        /// <summary>把 "AB12" 形式的单元格引用换算成 0 起列序号（只取字母段，A→0，AA→26）。</summary>
        private static int ColumnIndexOf(string reference)
        {
            var index = 0;
            foreach (var character in reference)
            {
                if (character >= 'A' && character <= 'Z') index = index * 26 + (character - 'A' + 1);
                else if (character >= 'a' && character <= 'z') index = index * 26 + (character - 'a' + 1);
                else if (index > 0) break; // 字母段结束，进入行号数字
            }
            return Math.Max(0, index - 1);
        }

        /// <summary>读取单元格文本：共享字符串按索引查表；inlineStr 取内嵌文本；布尔转 0/1；错误值按空处理；数值原样返回（xlsx 数值本就是 invariant 格式）。</summary>
        private static string ReadCellValue(XElement cell, string[] sharedStrings)
        {
            var type = LocalAttribute(cell, "t");
            if (type == "inlineStr")
                return Normalize(string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value)));

            var raw = cell.Element(SpreadsheetNs + "v")?.Value ?? "";
            if (type == "s")
            {
                if (!int.TryParse(raw, out var index) || index < 0 || index >= sharedStrings.Length) return "";
                return Normalize(sharedStrings[index]);
            }
            if (type == "b") return raw == "1" ? "1" : "0";
            if (type == "e") return ""; // 错误单元格（#DIV/0! 等）按空处理
            return Normalize(raw); // 无类型（数值）与 "str"（公式结果字符串）都直接返回原文
        }

        /// <summary>单元格值归一："—" 或空 → 空串；其余保留原文（含内部空格）。</summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Trim() == "—" ? "" : value;
        }

        /// <summary>按本地名取属性值（忽略命名空间差异，兼容不同 Excel 生成器）。</summary>
        private static string LocalAttribute(XElement element, string localName) =>
            element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

        /// <summary>读取并解析压缩包内 XML 条目；缺失或损坏时统一抛 XlsxTableReaderException。</summary>
        private static XDocument LoadEntry(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path)
                ?? throw new XlsxTableReaderException($"压缩包缺少 {path}。");
            using var stream = entry.Open();
            try
            {
                return XDocument.Load(stream);
            }
            catch (Exception exception)
            {
                throw new XlsxTableReaderException($"无法解析 {path}。", exception);
            }
        }
    }
}
