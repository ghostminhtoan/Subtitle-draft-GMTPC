using System;
using System.Text;

namespace Subtitle_draft_GMTPC.Models
{
    /// <summary>
    /// Lớp trừu tượng đại diện cho một dòng phụ đề
    /// </summary>
    public abstract class SubtitleLine
    {
        /// <summary>
        /// Text gốc của dòng (dòng raw trong file)
        /// </summary>
        public string OriginalText { get; set; }

        /// <summary>
        /// Thời gian bắt đầu
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Nội dung text sẽ được rebuild (có thể bị thay đổi time)
        /// </summary>
        public string Content { get; set; }

        protected SubtitleLine()
        {
        }

        /// <summary>
        /// Chuyển TimeSpan về milliseconds (long)
        /// </summary>
        public static long ToMilliseconds(TimeSpan ts)
        {
            return (long)ts.TotalMilliseconds;
        }

        /// <summary>
        /// Chuyển milliseconds về TimeSpan
        /// </summary>
        public static TimeSpan FromMilliseconds(long ms)
        {
            if (ms < 0) ms = 0;
            return TimeSpan.FromMilliseconds(ms);
        }

        /// <summary>
        /// Format time theo định dạng ASS: H:MM:SS.ff
        /// </summary>
        public static string FormatAssTime(TimeSpan ts)
        {
            long totalMs = (long)ts.TotalMilliseconds;
            if (totalMs < 0) totalMs = 0;
            long hours = totalMs / 3600000;
            long remaining = totalMs % 3600000;
            long minutes = remaining / 60000;
            remaining = remaining % 60000;
            long seconds = remaining / 1000;
            remaining = remaining % 1000;
            long centiseconds = remaining / 10;
            return $"{hours}:{minutes:D2}:{seconds:D2}.{centiseconds:D2}";
        }

        /// <summary>
        /// Format time theo định dạng SRT: HH:MM:SS,fff
        /// </summary>
        public static string FormatSrtTime(TimeSpan ts)
        {
            long totalMs = (long)ts.TotalMilliseconds;
            if (totalMs < 0) totalMs = 0;
            long hours = totalMs / 3600000;
            long remaining = totalMs % 3600000;
            long minutes = remaining / 60000;
            remaining = remaining % 60000;
            long seconds = remaining / 1000;
            long milliseconds = remaining;
            return $"{hours:D2}:{minutes:D2}:{seconds:D3},{milliseconds:D3}";
        }

        /// <summary>
        /// Parse time từ string định dạng ASS: H:MM:SS.ff hoặc HH:MM:SS.ff
        /// </summary>
        public static TimeSpan ParseAssTime(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return TimeSpan.Zero;
            timeStr = timeStr.Trim();
            try
            {
                // Format: H:MM:SS.ff hoặc HH:MM:SS.ff
                string[] parts = timeStr.Split(':');
                if (parts.Length != 3) return TimeSpan.Zero;

                int hours = int.Parse(parts[0]);
                int minutes = int.Parse(parts[1]);
                string[] secParts = parts[2].Split('.');
                int seconds = int.Parse(secParts[0]);
                int centiseconds = 0;
                if (secParts.Length > 1)
                {
                    centiseconds = int.Parse(secParts[1]);
                }

                long totalMs = hours * 3600000L + minutes * 60000L + seconds * 1000L + centiseconds * 10L;
                return FromMilliseconds(totalMs);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Parse time từ string định dạng SRT: HH:MM:SS,fff
        /// </summary>
        public static TimeSpan ParseSrtTime(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return TimeSpan.Zero;
            timeStr = timeStr.Trim();
            try
            {
                string[] parts = timeStr.Split(':');
                if (parts.Length != 3) return TimeSpan.Zero;

                int hours = int.Parse(parts[0]);
                int minutes = int.Parse(parts[1]);
                string[] secParts = parts[2].Split(',');
                int seconds = int.Parse(secParts[0]);
                int milliseconds = 0;
                if (secParts.Length > 1)
                {
                    milliseconds = int.Parse(secParts[1]);
                }

                long totalMs = hours * 3600000L + minutes * 60000L + seconds * 1000L + milliseconds;
                return FromMilliseconds(totalMs);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Rebuild lại dòng với time mới (phải override)
        /// </summary>
        public abstract string RebuildLine();

        /// <summary>
        /// Clone dòng phụ đề
        /// </summary>
        public abstract SubtitleLine Clone();
    }
}
