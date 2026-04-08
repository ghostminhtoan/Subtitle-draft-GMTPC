using System;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Models
{
    /// <summary>
    /// Dòng phụ đề định dạng SRT
    /// Format:
    ///   Index (số thứ tự)
    ///   HH:MM:SS,fff --> HH:MM:SS,fff
    ///   Text
    /// </summary>
    public class SrtSubtitleLine : SubtitleLine
    {
        public int Index { get; set; }
        public string Text { get; set; }

        /// <summary>
        /// Constructor rỗng
        /// </summary>
        public SrtSubtitleLine()
        {
        }

        /// <summary>
        /// Constructor từ các thành phần
        /// </summary>
        public SrtSubtitleLine(int index, TimeSpan startTime, TimeSpan endTime, string text)
        {
            Index = index;
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
        }

        /// <summary>
        /// Rebuild lại dòng SRT với time mới
        /// </summary>
        public override string RebuildLine()
        {
            string timeLine = $"{FormatSrtTime(StartTime)} --> {FormatSrtTime(EndTime)}";
            return $"{Index}{Environment.NewLine}{timeLine}{Environment.NewLine}{Text}";
        }

        /// <summary>
        /// Clone dòng SRT
        /// </summary>
        public override SubtitleLine Clone()
        {
            return new SrtSubtitleLine()
            {
                Index = this.Index,
                StartTime = this.StartTime,
                EndTime = this.EndTime,
                Text = this.Text,
                OriginalText = this.OriginalText,
                Content = this.Content
            };
        }
    }
}
