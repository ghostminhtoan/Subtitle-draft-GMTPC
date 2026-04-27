using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class TutorialExcelMarkdownExporter
    {
        private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static string ExportMarkdown(string filePath, string title)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return ExportMarkdown(stream, title, new Uri(Path.GetFullPath(filePath)).AbsoluteUri);
            }
        }

        public static string ExportMarkdown(Stream stream, string title, string sourceUrl)
        {
            var workbook = LoadWorkbook(stream, title, sourceUrl);
            var markdown = new StringBuilder();
            markdown.AppendLine("# " + (string.IsNullOrWhiteSpace(title) ? "Excel" : title));

            foreach (var sheet in workbook.Sheets)
            {
                markdown.AppendLine();
                markdown.Append(sheet.Markdown);
            }

            return markdown.ToString();
        }

        public static TutorialExcelWorkbookDocument LoadWorkbook(Stream stream, string title, string sourceUrl)
        {
            var workbookDocument = new TutorialExcelWorkbookDocument(title, sourceUrl);

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheets = ReadSheets(archive);

                foreach (var sheet in sheets)
                {
                    var rows = ReadRows(archive, sheet.TargetPath, sharedStrings);
                    if (rows.Count == 0)
                    {
                        continue;
                    }

                    workbookDocument.Sheets.Add(
                        new TutorialExcelSheetDocument(sheet.Name, BuildSheetMarkdown(sheet.Name, rows)));
                }
            }

            return workbookDocument;
        }

        public static TutorialExcelWorkbookDocument LoadWorkbook(string filePath, string title)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return LoadWorkbook(stream, title, new Uri(Path.GetFullPath(filePath)).AbsoluteUri);
            }
        }

        private static string BuildSheetMarkdown(string sheetName, List<RowData> rows)
        {
            var markdown = new StringBuilder();
            markdown.AppendLine("## " + CleanText(sheetName));
            markdown.AppendLine();

            var headerRowIndex = FindHeaderRow(rows);
            if (headerRowIndex < 0)
            {
                headerRowIndex = 0;
            }

            for (var i = 0; i < headerRowIndex; i++)
            {
                var rowText = BuildPlainRowText(rows[i]);
                if (string.IsNullOrWhiteSpace(rowText))
                {
                    continue;
                }

                if (i == 0 && rows[i].NonEmptyCount == 1)
                {
                    markdown.AppendLine("# " + rowText);
                    markdown.AppendLine();
                    continue;
                }

                markdown.AppendLine("> " + rowText);
                markdown.AppendLine();
            }

            var headerRow = rows[headerRowIndex];
            var columnCount = Math.Max(GetMaxColumnIndex(rows), headerRow.MaxColumnIndex);
            if (columnCount <= 0)
            {
                return markdown.ToString().TrimEnd();
            }

            markdown.Append("| ");
            markdown.Append(string.Join(" | ", BuildRowValues(headerRow, columnCount)));
            markdown.AppendLine(" |");

            markdown.Append("| ");
            markdown.Append(string.Join(" | ", BuildSeparatorValues(columnCount)));
            markdown.AppendLine(" |");

            for (var i = headerRowIndex + 1; i < rows.Count; i++)
            {
                var values = BuildRowValues(rows[i], columnCount);
                if (IsRowEmpty(values))
                {
                    continue;
                }

                markdown.Append("| ");
                markdown.Append(string.Join(" | ", values));
                markdown.AppendLine(" |");
            }

            return markdown.ToString().TrimEnd();
        }

        private static string[] BuildRowValues(RowData row, int columnCount)
        {
            var values = new string[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                values[i] = string.Empty;
            }

            foreach (var pair in row.Cells)
            {
                var index = pair.Key - 1;
                if (index >= 0 && index < values.Length)
                {
                    values[index] = CleanText(pair.Value);
                }
            }

            return values;
        }

        private static string[] BuildSeparatorValues(int columnCount)
        {
            var values = new string[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                values[i] = "---";
            }

            return values;
        }

        private static bool IsRowEmpty(string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildPlainRowText(RowData row)
        {
            var builder = new StringBuilder();
            foreach (var pair in row.Cells)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(CleanText(pair.Value));
            }

            return builder.ToString().Trim();
        }

        private static int FindHeaderRow(List<RowData> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.NonEmptyCount < 2)
                {
                    continue;
                }

                if (row.ContainsText("Tiếng Việt") ||
                    row.ContainsText("Phím tắt") ||
                    row.ContainsText("Tiếng Anh") ||
                    row.ContainsText("Shortcut") ||
                    row.ContainsText("Group") ||
                    row.ContainsText("Nhóm"))
                {
                    return i;
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].NonEmptyCount >= 2)
                {
                    return i;
                }
            }

            return 0;
        }

        private static int GetMaxColumnIndex(List<RowData> rows)
        {
            var max = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].MaxColumnIndex > max)
                {
                    max = rows[i].MaxColumnIndex;
                }
            }

            return max;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var sharedStrings = new List<string>();
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return sharedStrings;
            }

            var document = ReadXml(entry);
            var namespaceManager = CreateNamespaceManager(document);
            var nodes = document.SelectNodes("/m:sst/m:si", namespaceManager);
            if (nodes == null)
            {
                return sharedStrings;
            }

            foreach (XmlNode node in nodes)
            {
                sharedStrings.Add(CleanText(node.InnerText));
            }

            return sharedStrings;
        }

        private static List<SheetInfo> ReadSheets(ZipArchive archive)
        {
            var sheets = new List<SheetInfo>();
            var workbook = ReadXml(archive.GetEntry("xl/workbook.xml"));
            var rels = ReadXml(archive.GetEntry("xl/_rels/workbook.xml.rels"));

            var workbookNs = CreateNamespaceManager(workbook);
            var relNs = new XmlNamespaceManager(rels.NameTable);
            relNs.AddNamespace("rel", PackageRelationshipNamespace);

            var relationMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var relNodes = rels.SelectNodes("/rel:Relationships/rel:Relationship", relNs);
            if (relNodes != null)
            {
                foreach (XmlNode node in relNodes)
                {
                    var id = GetAttribute(node, "Id");
                    var target = GetAttribute(node, "Target");
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(target))
                    {
                        relationMap[id] = target;
                    }
                }
            }

            var sheetNodes = workbook.SelectNodes("/m:workbook/m:sheets/m:sheet", workbookNs);
            if (sheetNodes == null)
            {
                return sheets;
            }

            foreach (XmlNode node in sheetNodes)
            {
                var name = GetAttribute(node, "name");
                var relId = GetAttribute(node, "id", RelationshipNamespace);
                if (string.IsNullOrWhiteSpace(relId) || !relationMap.ContainsKey(relId))
                {
                    continue;
                }

                sheets.Add(new SheetInfo(name, NormalizeSheetTarget(relationMap[relId])));
            }

            return sheets;
        }

        private static List<RowData> ReadRows(ZipArchive archive, string sheetTargetPath, List<string> sharedStrings)
        {
            var rows = new List<RowData>();
            var entry = archive.GetEntry("xl/" + sheetTargetPath);
            if (entry == null)
            {
                return rows;
            }

            var document = ReadXml(entry);
            var ns = CreateNamespaceManager(document);
            var rowNodes = document.SelectNodes("/m:worksheet/m:sheetData/m:row", ns);
            if (rowNodes == null)
            {
                return rows;
            }

            foreach (XmlNode rowNode in rowNodes)
            {
                var rowData = new RowData();
                var cellNodes = rowNode.SelectNodes("m:c", ns);
                if (cellNodes != null)
                {
                    foreach (XmlNode cellNode in cellNodes)
                    {
                        var reference = GetAttribute(cellNode, "r");
                        var columnIndex = GetColumnIndexFromCellReference(reference);
                        if (columnIndex <= 0)
                        {
                            continue;
                        }

                        rowData.Cells[columnIndex] = ReadCellValue(cellNode, sharedStrings);
                    }
                }

                rowData.MaxColumnIndex = GetRowMaxColumnIndex(rowData.Cells);
                rowData.NonEmptyCount = GetRowNonEmptyCount(rowData.Cells);
                rows.Add(rowData);
            }

            return rows;
        }

        private static string ReadCellValue(XmlNode cellNode, List<string> sharedStrings)
        {
            var cellType = GetAttribute(cellNode, "t");
            var valueNode = cellNode.SelectSingleNode("m:v", CreateNamespaceManager(cellNode.OwnerDocument));
            var value = valueNode != null ? valueNode.InnerText : string.Empty;

            if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase))
            {
                int index;
                if (int.TryParse(value, out index) && index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }

                return string.Empty;
            }

            if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                var inline = cellNode.SelectSingleNode("m:is", CreateNamespaceManager(cellNode.OwnerDocument));
                return inline != null ? inline.InnerText : string.Empty;
            }

            if (string.Equals(cellType, "b", StringComparison.OrdinalIgnoreCase))
            {
                return value == "1" ? "TRUE" : "FALSE";
            }

            return value;
        }

        private static int GetRowMaxColumnIndex(Dictionary<int, string> cells)
        {
            var max = 0;
            foreach (var key in cells.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            return max;
        }

        private static int GetRowNonEmptyCount(Dictionary<int, string> cells)
        {
            var count = 0;
            foreach (var value in cells.Values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    count++;
                }
            }

            return count;
        }

        private static XmlDocument ReadXml(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                var document = new XmlDocument();
                document.Load(stream);
                return document;
            }
        }

        private static XmlNamespaceManager CreateNamespaceManager(XmlDocument document)
        {
            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("m", SpreadsheetNamespace);
            manager.AddNamespace("r", RelationshipNamespace);
            return manager;
        }

        private static string GetAttribute(XmlNode node, string attributeName)
        {
            var attribute = node.Attributes != null ? node.Attributes[attributeName] : null;
            return attribute != null ? attribute.Value : string.Empty;
        }

        private static string GetAttribute(XmlNode node, string attributeName, string namespaceUri)
        {
            var attribute = node.Attributes != null ? node.Attributes[attributeName, namespaceUri] : null;
            return attribute != null ? attribute.Value : string.Empty;
        }

        private static string NormalizeSheetTarget(string target)
        {
            var normalized = (target ?? string.Empty).Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(3);
            }

            return normalized;
        }

        private static int GetColumnIndexFromCellReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return 0;
            }

            var column = 0;
            for (var i = 0; i < reference.Length; i++)
            {
                var c = reference[i];
                if (c < 'A' || c > 'Z')
                {
                    break;
                }

                column = (column * 26) + (c - 'A' + 1);
            }

            return column;
        }

        private static string CleanText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("|", "¦")
                .Trim();
        }

        private sealed class SheetInfo
        {
            public SheetInfo(string name, string targetPath)
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Sheet" : name;
                TargetPath = targetPath;
            }

            public string Name { get; }

            public string TargetPath { get; }
        }

        internal sealed class TutorialExcelWorkbookDocument
        {
            public TutorialExcelWorkbookDocument(string title, string sourceUrl)
            {
                Title = title;
                SourceUrl = sourceUrl;
                Sheets = new List<TutorialExcelSheetDocument>();
            }

            public string Title { get; }

            public string SourceUrl { get; }

            public List<TutorialExcelSheetDocument> Sheets { get; }
        }

        internal sealed class TutorialExcelSheetDocument
        {
            public TutorialExcelSheetDocument(string name, string markdown)
            {
                Name = name;
                Markdown = markdown;
            }

            public string Name { get; }

            public string Markdown { get; }
        }

        private sealed class RowData
        {
            public Dictionary<int, string> Cells { get; } = new Dictionary<int, string>();

            public int MaxColumnIndex { get; set; }

            public int NonEmptyCount { get; set; }

            public bool ContainsText(string text)
            {
                foreach (var value in Cells.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value) &&
                        value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
