using System;
using System.Collections.Generic;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service xử lý time code: shift time, connect gap
    /// </summary>
    public class TimeCodeService
    {
        /// <summary>
        /// Dịch chuyển tất cả start time và end time thêm (milliseconds)
        /// Nếu thời gian nhỏ hơn 0 thì giữ nguyên 0
        /// </summary>
        public static List<SubtitleLine> ShiftTime(List<SubtitleLine> lines, int milliseconds)
        {
            if (lines == null || lines.Count == 0) return lines;

            var result = new List<SubtitleLine>();

            foreach (var line in lines)
            {
                var cloned = line.Clone();

                if (IsZeroTimeLine(cloned))
                {
                    result.Add(cloned);
                    continue;
                }

                // Shift start time
                var newStartMs = SubtitleLine.ToMilliseconds(cloned.StartTime) + milliseconds;
                if (newStartMs < 0) newStartMs = 0;
                cloned.StartTime = SubtitleLine.FromMilliseconds(newStartMs);

                // Shift end time
                var newEndMs = SubtitleLine.ToMilliseconds(cloned.EndTime) + milliseconds;
                if (newEndMs < 0) newEndMs = 0;
                cloned.EndTime = SubtitleLine.FromMilliseconds(newEndMs);

                // Rebuild content
                cloned.Content = cloned.RebuildLine();

                result.Add(cloned);
            }

            return result;
        }

        /// <summary>
        /// Nối gap giữa các dòng phụ đề
        /// Gap = StartTime dòng dưới - EndTime dòng trên
        /// Nếu gap > 0 → extend EndTime dòng trên = StartTime dòng dưới
        /// Start time luôn giữ nguyên, chỉ sửa End time dòng trên
        /// </summary>
        public static List<SubtitleLine> ConnectGap(List<SubtitleLine> lines)
        {
            if (lines == null || lines.Count == 0) return lines;

            var result = new List<SubtitleLine>();

            // Clone tất cả dòng trước
            foreach (var line in lines)
            {
                result.Add(line.Clone());
            }

            // Duyệt từ trên xuống dưới
            for (int i = 0; i <= result.Count - 2; i++)
            {
                var currentLine = result[i];
                var nextLine = result[i + 1];

                if (IsZeroTimeLine(currentLine) || IsZeroTimeLine(nextLine))
                {
                    continue;
                }

                // Tính gap = Start dòng dưới - End dòng trên
                var currentEndMs = SubtitleLine.ToMilliseconds(currentLine.EndTime);
                var nextStartMs = SubtitleLine.ToMilliseconds(nextLine.StartTime);
                var gap = nextStartMs - currentEndMs;

                // Nếu gap > 0 → extend EndTime dòng trên đến StartTime dòng dưới
                if (gap > 0)
                {
                    currentLine.EndTime = nextLine.StartTime;
                    currentLine.Content = currentLine.RebuildLine();
                }
            }

            return result;
        }

        private static bool IsZeroTimeLine(SubtitleLine line)
        {
            return line != null && line.StartTime == TimeSpan.Zero && line.EndTime == TimeSpan.Zero;
        }
    }
}
