using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service phân tích cú pháp phụ đề SRT và ASS
    /// </summary>
    public class SubtitleParser
    {
        /// <summary>
        /// Làm sạch text: loại bỏ BOM, zero-width chars, ký tự điều khiển
        /// Cần thiết khi copy từ trình duyệt (Chrome/Edge) hoặc các nguồn khác
        /// </summary>
        public static string SanitizeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var sb = new StringBuilder(content.Length);
            foreach (var ch in content)
            {
                var code = (int)ch;
                // Giữ lại các ký tự bình thường
                if (code == 0x9) { // Tab
                    sb.Append(ch);
                }
                else if (code == 0xA) { // Line feed
                    sb.Append(ch);
                }
                else if (code == 0xD) { // Carriage return
                    sb.Append(ch);
                }
                else if (code >= 0x20 && code <= 0xFFFF) { // Ký tự in được
                    // Loại bỏ BOM và zero-width chars
                    if (code != 0xFEFF && code != 0x200B && code != 0x200C && code != 0x200D && code != 0x200E && code != 0x200F && code != 0x202A && code != 0x202B && code != 0x202C && code != 0x202D && code != 0x202E && code != 0xFEFF && code != 0xFFFC)
                    {
                        sb.Append(ch);
                    }
                }
                else if (code > 0xFFFF) { // Emoji, ký tự đặc biệt > U+FFFF
                    sb.Append(ch);
                }
                // Các ký tự điều khiển khác (< 0x20) bị bỏ qua
            }
            return sb.ToString();
        }

        /// <summary>
        /// Phát hiện định dạng phụ đề từ nội dung
        /// </summary>
        public static SubtitleFormat DetectFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return SubtitleFormat.Unknown;

            var trimmed = content.Trim();

            // ASS thường có [Script Info] hoặc [Events] hoặc dòng Dialogue:
            if (trimmed.Contains("[Script Info]") || trimmed.Contains("[Events]") || trimmed.Contains("Dialogue:"))
            {
                return SubtitleFormat.Ass;
            }

            // SRT thường có pattern: số --> số
            if (Regex.IsMatch(trimmed, @"\d{1,2}:\d{2}:\d{2}[,.]\d{2,3}\s*-->\s*\d{1,2}:\d{2}:\d{2}[,.]\d{2,3}"))
            {
                return SubtitleFormat.Srt;
            }

            return SubtitleFormat.Unknown;
        }

        /// <summary>
        /// Parse nội dung phụ đề thành danh sách các dòng
        /// </summary>
        public static List<SubtitleLine> Parse(string content)
        {
            var format = DetectFormat(content);
            switch (format)
            {
                case SubtitleFormat.Ass:
                    return ParseAss(content);
                case SubtitleFormat.Srt:
                    return ParseSrt(content);
                default:
                    return new List<SubtitleLine>();
            }
        }

        /// <summary>
        /// Parse phụ đề ASS
        /// </summary>
        public static List<SubtitleLine> ParseAss(string content)
        {
            var result = new List<SubtitleLine>();
            if (string.IsNullOrWhiteSpace(content)) return result;

            var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Chỉ lấy các dòng Dialogue
                if (trimmed.StartsWith("Dialogue:"))
                {
                    try
                    {
                        var assLine = new AssSubtitleLine(line);
                        result.Add(assLine);
                    }
                    catch
                    {
                        // Bo qua dong loi
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Parse phụ đề SRT
        /// </summary>
        public static List<SubtitleLine> ParseSrt(string content)
        {
            var result = new List<SubtitleLine>();
            if (string.IsNullOrWhiteSpace(content)) return result;

            // Chuan hoa line endings
            var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split('\n');

            int i = 0;
            while (i < lines.Length)
            {
                var line = lines[i].Trim();

                // Bo qua dong trong
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Thu parse index (so thu tu)
                if (int.TryParse(line, out int index))
                {
                    // Dong tiep theo phai la time line
                    if (i + 1 < lines.Length)
                    {
                        var timeLine = lines[i + 1].Trim();
                        var timeMatch = Regex.Match(timeLine, @"(.+?)\s*-->\s*(.+)");

                        if (timeMatch.Success)
                        {
                            var startTime = SubtitleLine.ParseSrtTime(timeMatch.Groups[1].Value);
                            var endTime = SubtitleLine.ParseSrtTime(timeMatch.Groups[2].Value);

                            // Gom cac dong text tiep theo cho den khi gap dong trong
                            var textLines = new List<string>();
                            i += 2;
                            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i].Trim()))
                            {
                                textLines.Add(lines[i].TrimEnd());
                                i++;
                            }

                            var text = string.Join(Environment.NewLine, textLines);
                            var srtLine = new SrtSubtitleLine(index, startTime, endTime, text);
                            srtLine.OriginalText = string.Join(Environment.NewLine, new[] { index.ToString(), timeLine, text });
                            srtLine.Content = srtLine.OriginalText;
                            result.Add(srtLine);
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            return result;
        }

        /// <summary>
        /// Xuat danh sach SubtitleLine thanh noi dung text hoan chinh
        /// </summary>
        public static string ToText(List<SubtitleLine> lines, SubtitleFormat format)
        {
            if (lines == null || lines.Count == 0) return string.Empty;

            if (format == SubtitleFormat.Srt)
            {
                return ToSrtText(lines);
            }
            else
            {
                return ToAssText(lines);
            }
        }

        /// <summary>
        /// Xuat ra dinh dang SRT
        /// </summary>
        private static string ToSrtText(List<SubtitleLine> lines)
        {
            var sb = new StringBuilder();
            var index = 1;

            foreach (var line in lines)
            {
                var srtLine = line as SrtSubtitleLine;
                if (srtLine != null)
                {
                    srtLine.Index = index;
                    sb.AppendLine(srtLine.RebuildLine());
                    sb.AppendLine();
                    index++;
                }
                else
                {
                    // Neu la AssSubtitleLine, chuyen doi sang format SRT
                    var timeLine = $"{SubtitleLine.FormatSrtTime(line.StartTime)} --> {SubtitleLine.FormatSrtTime(line.EndTime)}";
                    sb.AppendLine(index.ToString());
                    sb.AppendLine(timeLine);

                    var assLine = line as AssSubtitleLine;
                    if (assLine != null)
                    {
                        sb.AppendLine(assLine.DialogText);
                    }
                    else
                    {
                        sb.AppendLine(line.OriginalText);
                    }
                    sb.AppendLine();
                    index++;
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Xuat ra dinh dang ASS - chi xuat cac dong Dialogue, khong xuat header
        /// </summary>
        private static string ToAssText(List<SubtitleLine> lines)
        {
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var assLine = line as AssSubtitleLine;
                if (assLine != null)
                {
                    sb.AppendLine(assLine.RebuildLine());
                }
                else
                {
                    // Neu la SrtSubtitleLine, chuyen doi sang format ASS
                    var srtLine = line as SrtSubtitleLine;
                    if (srtLine != null)
                    {
                        var dialogueLine = $"Dialogue: 0,{SubtitleLine.FormatAssTime(line.StartTime)},{SubtitleLine.FormatAssTime(line.EndTime)},Default,,0000,0000,0000,,{srtLine.Text.Replace(Environment.NewLine, "\\N")}";
                        sb.AppendLine(dialogueLine);
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Xuat danh sach SubtitleLine thanh noi dung text giu nguyen format goc
        /// </summary>
        public static string ToOriginalText(List<SubtitleLine> lines, SubtitleFormat format)
        {
            return ToText(lines, format);
        }
    }
}
