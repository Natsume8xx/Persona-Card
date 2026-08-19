using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using NUnit.Framework;
using PersonaCards.Core.Xlsx;

namespace PersonaCards.Tests.EditMode
{
    /// <summary>xlsx 读取器测试：全部使用内存 zip 夹具（手写 Excel 文件结构），不依赖真实 xlsx 文件。</summary>
    public sealed class XlsxTableReaderTests
    {
        private const string WorksheetOpen =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>";

        private const string WorksheetClose = "</sheetData></worksheet>";

        [Test]
        public void ReadTableReturnsHeaderDrivenRows()
        {
            // 表头行（共享字符串）+ 数据行（共享字符串与数值混合）
            var worksheet = WorksheetOpen +
                Row(1, SharedCell("A1", 0) + SharedCell("B1", 1) + SharedCell("C1", 2)) +
                Row(2, SharedCell("A2", 3) + NumberCell("B2", "550") + SharedCell("C2", 4)) +
                WorksheetClose;
            var xlsx = BuildXlsx("关卡流程", worksheet, new[] { "阶段_id", "分数参数", "是否商店", "战斗", "是" });

            var rows = XlsxTableReader.ReadTable(new MemoryStream(xlsx), "关卡流程");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0]["阶段_id"], Is.EqualTo("战斗"));
            Assert.That(rows[0]["分数参数"], Is.EqualTo("550"));
            Assert.That(rows[0]["是否商店"], Is.EqualTo("是"));
        }

        [Test]
        public void NumbersStayInInvariantFormatWithoutSharedStrings()
        {
            // 关系表 Target 用绝对路径形式（真实 Excel 常见输出）；本夹具无 sharedStrings.xml
            var worksheet = WorksheetOpen +
                Row(1, SharedCell("A1", 0) + SharedCell("B1", 1)) +
                Row(2, NumberCell("A2", "1900") + NumberCell("B2", "3.5")) +
                WorksheetClose;
            var xlsx = BuildXlsx("数值表", worksheet, new[] { "整数", "小数" }, relTarget: "/xl/worksheets/sheet1.xml");

            var rows = XlsxTableReader.ReadTable(new MemoryStream(xlsx), "数值表");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0]["整数"], Is.EqualTo("1900"));
            Assert.That(rows[0]["小数"], Is.EqualTo("3.5"));
        }

        [Test]
        public void DashAndEmptyCellsNormalizeToEmptyString()
        {
            var worksheet = WorksheetOpen +
                Row(1, SharedCell("A1", 0) + SharedCell("B1", 1) + SharedCell("C1", 2)) +
                Row(2, InlineCell("A2", "—") + SharedCell("B2", 3) + SharedCell("C2", 5)) +
                Row(3, SharedCell("A3", 5) + NumberCell("B3", "100")) + // C3 缺失
                WorksheetClose;
            var xlsx = BuildXlsx("归一表", worksheet, new[] { "列A", "列B", "列C", "", "—", "值" });

            var rows = XlsxTableReader.ReadTable(new MemoryStream(xlsx), "归一表");

            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0]["列A"], Is.EqualTo("")); // "—" → 空串
            Assert.That(rows[0]["列B"], Is.EqualTo("")); // 共享字符串空值 → 空串
            Assert.That(rows[0]["列C"], Is.EqualTo("值"));
            Assert.That(rows[1]["列A"], Is.EqualTo("值"));
            Assert.That(rows[1]["列B"], Is.EqualTo("100"));
            Assert.That(rows[1]["列C"], Is.EqualTo("")); // 缺失单元格 → 空串
        }

        [Test]
        public void FullyEmptyRowsAreSkipped()
        {
            // 表头前空行、表头与数据之间空行、尾部空行：全部跳过
            var worksheet = WorksheetOpen +
                "<row r=\"1\"/>" +
                Row(2, SharedCell("A2", 0) + SharedCell("B2", 1)) +
                "<row r=\"3\"/>" +
                Row(4, SharedCell("A4", 2) + NumberCell("B4", "7")) +
                "<row r=\"5\"/>" +
                WorksheetClose;
            var xlsx = BuildXlsx("空行表", worksheet, new[] { "列A", "列B", "数据" });

            var rows = XlsxTableReader.ReadTable(new MemoryStream(xlsx), "空行表");

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0]["列A"], Is.EqualTo("数据"));
            Assert.That(rows[0]["列B"], Is.EqualTo("7"));
        }

        [Test]
        public void MissingSheetThrowsWithSheetName()
        {
            var worksheet = WorksheetOpen + Row(1, SharedCell("A1", 0)) + WorksheetClose;
            var xlsx = BuildXlsx("关卡流程", worksheet, new[] { "列A" });

            var exception = Assert.Throws<XlsxTableReaderException>(() =>
                XlsxTableReader.ReadTable(new MemoryStream(xlsx), "不存在的表"));

            Assert.That(exception.Message, Does.Contain("不存在的表"));
        }

        [Test]
        public void CorruptZipThrowsReaderException()
        {
            var exception = Assert.Throws<XlsxTableReaderException>(() =>
                XlsxTableReader.ReadTable(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }), "任意"));

            Assert.That(exception.Message, Does.Contain("xlsx"));
        }

        /// <summary>构造内存 xlsx：单 sheet，workbook/rels/sharedStrings 全部手写（模仿 Excel 输出结构）。</summary>
        private static byte[] BuildXlsx(string sheetName, string worksheetXml, string[] sharedStrings,
            string relTarget = "worksheets/sheet1.xml")
        {
            var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    $"<sheets><sheet name=\"{sheetName}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                AddEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    $"<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"{relTarget}\"/></Relationships>");
                if (sharedStrings != null)
                {
                    var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>")
                        .Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"")
                        .Append(sharedStrings.Length).Append("\" uniqueCount=\"").Append(sharedStrings.Length).Append("\">");
                    foreach (var value in sharedStrings)
                        builder.Append("<si><t>").Append(SecurityElement.Escape(value)).Append("</t></si>");
                    builder.Append("</sst>");
                    AddEntry(archive, "xl/sharedStrings.xml", builder.ToString());
                }
                AddEntry(archive, "xl/worksheets/sheet1.xml", worksheetXml);
            }
            return stream.ToArray();
        }

        private static void AddEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        private static string Row(int number, string cells) => $"<row r=\"{number}\">{cells}</row>";

        /// <summary>共享字符串单元格（t="s"，值 = 共享字符串索引）。</summary>
        private static string SharedCell(string reference, int sharedIndex) =>
            $"<c r=\"{reference}\" t=\"s\"><v>{sharedIndex}</v></c>";

        /// <summary>数值单元格（无 t 属性，v 即 invariant 文本）。</summary>
        private static string NumberCell(string reference, string invariantValue) =>
            $"<c r=\"{reference}\"><v>{invariantValue}</v></c>";

        /// <summary>内联字符串单元格（t="inlineStr"）。</summary>
        private static string InlineCell(string reference, string value) =>
            $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(value)}</t></is></c>";
    }
}
