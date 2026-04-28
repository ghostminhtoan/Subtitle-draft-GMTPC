using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace Subtitle_draft_GMTPC.Services
{
    internal static class GoogleSheetsWorkbookRenderer
    {
        private static readonly HttpClient _httpClient = CreateHttpClient();

        public static async Task<string> LoadWorkbookHtmlAsync(string workbookUrl)
        {
            var csvUrl = BuildCsvExportUrl(workbookUrl);
            using (var response = await _httpClient.GetAsync(csvUrl))
            {
                response.EnsureSuccessStatusCode();
                var csvBytes = await response.Content.ReadAsByteArrayAsync();
                return BuildHtmlFromCsv(csvBytes, workbookUrl, csvUrl);
            }
        }

        public static string BuildExternalBrowserUrl(string workbookUrl)
        {
            return workbookUrl ?? string.Empty;
        }

        private static string BuildCsvExportUrl(string workbookUrl)
        {
            var spreadsheetId = ExtractSpreadsheetId(workbookUrl);
            var gid = ExtractSheetGid(workbookUrl);
            return "https://docs.google.com/spreadsheets/d/" + spreadsheetId + "/export?format=csv&gid=" + gid;
        }

        private static string ExtractSpreadsheetId(string workbookUrl)
        {
            if (string.IsNullOrWhiteSpace(workbookUrl))
            {
                throw new InvalidOperationException("Thiếu link Google Sheets.");
            }

            var marker = "/spreadsheets/d/";
            var index = workbookUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                throw new InvalidOperationException("Link không phải Google Sheets hợp lệ.");
            }

            var start = index + marker.Length;
            var end = workbookUrl.IndexOf('/', start);
            if (end < 0)
            {
                end = workbookUrl.Length;
            }

            var id = workbookUrl.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Không lấy được spreadsheet id.");
            }

            return id;
        }

        private static string ExtractSheetGid(string workbookUrl)
        {
            if (string.IsNullOrWhiteSpace(workbookUrl))
            {
                return "0";
            }

            var fragmentIndex = workbookUrl.IndexOf("#gid=", StringComparison.OrdinalIgnoreCase);
            if (fragmentIndex >= 0)
            {
                return ReadDigits(workbookUrl, fragmentIndex + 5);
            }

            var queryIndex = workbookUrl.IndexOf("gid=", StringComparison.OrdinalIgnoreCase);
            if (queryIndex >= 0)
            {
                return ReadDigits(workbookUrl, queryIndex + 4);
            }

            return "0";
        }

        private static string ReadDigits(string text, int startIndex)
        {
            var builder = new StringBuilder();
            for (var i = startIndex; i < text.Length; i++)
            {
                var ch = text[i];
                if (!char.IsDigit(ch))
                {
                    break;
                }

                builder.Append(ch);
            }

            return builder.Length > 0 ? builder.ToString() : "0";
        }

        private static string BuildHtmlFromCsv(byte[] csvBytes, string workbookUrl, string csvUrl)
        {
            var rows = ParseCsvRows(csvBytes);
            var html = new StringBuilder();
            html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\" />");
            html.Append("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
            html.Append("<style>");
            html.Append("body{margin:0;background:#f3f4f6;color:#111827;font-family:'Segoe UI',Arial,sans-serif;}");
            html.Append(".top{background:#1f2330;color:#fff;padding:12px 16px;border-bottom:1px solid #3e4e6a;}");
            html.Append(".title{font-size:16px;font-weight:600;}");
            html.Append(".meta{font-size:12px;color:#c7d2fe;margin-top:4px;word-break:break-all;}");
            html.Append(".wrap{padding:12px;overflow:auto;}");
            html.Append("table{border-collapse:collapse;width:100%;background:#fff;}");
            html.Append("th,td{border:1px solid #d1d5db;padding:6px 8px;font-size:12px;vertical-align:top;}");
            html.Append("th{background:#111827;color:#fff;position:sticky;top:0;z-index:1;}");
            html.Append("tr:nth-child(even) td{background:#f9fafb;}");
            html.Append(".empty{color:#6b7280;font-style:italic;}");
            html.Append("</style></head><body>");
            html.Append("<div class=\"top\"><div class=\"title\">Google Sheets workbook</div>");
            html.Append("<div class=\"meta\">");
            html.Append(SecurityElement.Escape(workbookUrl ?? string.Empty));
            html.Append("</div>");
            html.Append("<div class=\"meta\">CSV export: ");
            html.Append(SecurityElement.Escape(csvUrl));
            html.Append("</div></div>");
            html.Append("<div class=\"wrap\"><table>");

            var firstRow = true;
            foreach (var row in rows)
            {
                html.Append(firstRow ? "<thead><tr>" : "<tr>");
                for (var i = 0; i < row.Count; i++)
                {
                    var cell = row[i] ?? string.Empty;
                    var tag = firstRow ? "th" : "td";
                    html.Append("<").Append(tag).Append(">");
                    html.Append(string.IsNullOrWhiteSpace(cell) ? "<span class=\"empty\">&nbsp;</span>" : SecurityElement.Escape(cell));
                    html.Append("</").Append(tag).Append(">");
                }

                html.Append(firstRow ? "</tr></thead><tbody>" : "</tr>");
                firstRow = false;
            }

            if (!firstRow)
            {
                html.Append("</tbody>");
            }
            else
            {
                html.Append("<tr><td class=\"empty\">Không có dữ liệu.</td></tr>");
            }

            html.Append("</table></div></body></html>");
            return html.ToString();
        }

        private static List<List<string>> ParseCsvRows(byte[] csvBytes)
        {
            var rows = new List<List<string>>();
            using (var stream = new MemoryStream(csvBytes))
            using (var parser = new TextFieldParser(stream, Encoding.UTF8))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;

                while (!parser.EndOfData)
                {
                    var fields = parser.ReadFields();
                    rows.Add(fields != null ? new List<string>(fields) : new List<string>());
                }
            }

            return rows;
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            return client;
        }
    }
}
