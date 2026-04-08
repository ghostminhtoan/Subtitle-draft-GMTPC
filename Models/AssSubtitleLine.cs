using System;
using System.Collections.Generic;
using System.Text;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Models
{
    /// <summary>
    /// Dòng phụ đề định dạng ASS
    /// Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
    /// Ví dụ: 0,0:05:13.51,0:05:14.98,Default,,0000,0000,0000,,Where's my bike?
    /// Chú ý: ",," giữa MarginV và Text có thể là empty effect
    /// </summary>
    public class AssSubtitleLine : SubtitleLine
    {
        public string Layer { get; set; }
        public string Style { get; set; }
        public string Name { get; set; }
        public string MarginL { get; set; }
        public string MarginR { get; set; }
        public string MarginV { get; set; }
        public string Effect { get; set; }
        public string DialogText { get; set; }

        /// <summary>
        /// Dòng Dialogue header (thường là "Dialogue")
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Constructor rỗng
        /// </summary>
        public AssSubtitleLine()
        {
        }

        /// <summary>
        /// Constructor từ dòng raw text
        /// </summary>
        public AssSubtitleLine(string originalLine)
        {
            OriginalText = originalLine;
            ParseLine(originalLine);
        }

        /// <summary>
        /// Parse dòng ASS để lấy thông tin
        /// </summary>
        private void ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Tìm "Dialogue:" hoặc "Dialogue: "
            int dialogueIndex = line.IndexOf("Dialogue:");
            if (dialogueIndex >= 0)
            {
                Header = "Dialogue";
                string rest = line.Substring(dialogueIndex + 9).Trim();
                ParseDialogueContent(rest);
            }
            else
            {
                // Nếu không có "Dialogue:" thì coi như toàn bộ là content
                Content = line;
            }
        }

        /// <summary>
        /// Parse phần sau "Dialogue:"
        /// Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
        /// Text có thể chứa dấu "," nên phải tìm ",," cuối cùng trước text
        /// </summary>
        private void ParseDialogueContent(string content)
        {
            Content = content;

            // ASS format có 9 fields cố định, field thứ 10 là text (có thể chứa ",")
            // Tách bằng cách đếm dấu "," - lấy 9 fields đầu
            // Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
            // Index:   0    1     2    3     4     5       6       7       8      9+

            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            int commaCount = 0;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (c == ',' && commaCount < 9)
                {
                    // Dấu "," phân cách field
                    parts.Add(current.ToString());
                    current.Clear();
                    commaCount++;
                }
                else
                {
                    current.Append(c);
                }
            }

            // Phần còn lại là text (field thứ 10 trở đi)
            parts.Add(current.ToString());

            if (parts.Count >= 10)
            {
                Layer = parts[0];
                string startTimeStr = parts[1];
                string endTimeStr = parts[2];
                Style = parts[3];
                Name = parts[4];
                MarginL = parts[5];
                MarginR = parts[6];
                MarginV = parts[7];
                Effect = parts[8];
                DialogText = parts[9];

                StartTime = ParseAssTime(startTimeStr);
                EndTime = ParseAssTime(endTimeStr);
            }
        }

        /// <summary>
        /// Rebuild lại dòng ASS với time mới
        /// </summary>
        public override string RebuildLine()
        {
            string timePart = $"{Layer},{FormatAssTime(StartTime)},{FormatAssTime(EndTime)},{Style},{Name},{MarginL},{MarginR},{MarginV},{Effect}";
            return $"Dialogue: {timePart},{DialogText}";
        }

        /// <summary>
        /// Clone dòng ASS
        /// </summary>
        public override SubtitleLine Clone()
        {
            return new AssSubtitleLine()
            {
                OriginalText = this.OriginalText,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                Content = this.Content,
                Layer = this.Layer,
                Style = this.Style,
                Name = this.Name,
                MarginL = this.MarginL,
                MarginR = this.MarginR,
                MarginV = this.MarginV,
                Effect = this.Effect,
                DialogText = this.DialogText,
                Header = this.Header
            };
        }
    }
}
