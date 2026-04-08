using System;
using System.Collections.Generic;
using Subtitle_draft_GMTPC.Models;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service xử lý merge phụ đề: gộp Engsub và Vietsub
    /// QUY TẮC: Panel 2 (Vietsub) LUÔN ở trên, Panel 1 (Engsub) LUÔN ở dưới
    /// </summary>
    public class MergeService
    {
        /// <summary>
        /// Merge: Panel 2 (Vietsub) TRƯỚC → Panel 1 (Engsub) SAU
        /// KHÔNG QUAN TÂM ngôn ngữ, chỉ quan trọng panel nào
        /// </summary>
        public static List<SubtitleLine> MergeSubtitles(List<SubtitleLine> engLines, List<SubtitleLine> vietLines, SubtitleFormat format)
        {
            if (engLines == null) engLines = new List<SubtitleLine>();
            if (vietLines == null) vietLines = new List<SubtitleLine>();

            if (engLines.Count == 0 && vietLines.Count == 0)
            {
                return new List<SubtitleLine>();
            }

            var result = new List<SubtitleLine>();

            // LUÔN thêm Panel 2 (Vietsub) TRƯỚC
            foreach (var line in vietLines)
            {
                result.Add(line.Clone());
            }

            // SAU ĐÓ mới thêm Panel 1 (Engsub)
            foreach (var line in engLines)
            {
                result.Add(line.Clone());
            }

            return result;
        }

        /// <summary>
        /// Merge (no break line): Giống Merge nhưng xóa newline trong mỗi dòng
        /// Panel 2 (Vietsub) TRƯỚC → Panel 1 (Engsub) SAU
        /// </summary>
        public static List<SubtitleLine> MergeUnbreak(List<SubtitleLine> engLines, List<SubtitleLine> vietLines, SubtitleFormat format)
        {
            // Gọi MergeSubtitles trước (đã đúng thứ tự Viet → Eng)
            var mergedLines = MergeSubtitles(engLines, vietLines, format);

            // Xóa newline trong mỗi dòng (xử lý null an toàn)
            foreach (var line in mergedLines)
            {
                var assLine = line as AssSubtitleLine;
                if (assLine != null)
                {
                    if (assLine.DialogText != null)
                    {
                        assLine.DialogText = assLine.DialogText.Replace("\\N", " ").Replace("\n", " ");
                    }
                    assLine.Content = assLine.RebuildLine();
                    continue;
                }

                var srtLine = line as SrtSubtitleLine;
                if (srtLine != null)
                {
                    if (srtLine.Text != null)
                    {
                        srtLine.Text = srtLine.Text.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");
                    }
                    srtLine.Content = srtLine.RebuildLine();
                }
            }

            return mergedLines;
        }
    }
}
