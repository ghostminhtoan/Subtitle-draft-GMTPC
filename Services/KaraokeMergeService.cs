using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Subtitle_draft_GMTPC.Services
{
    /// <summary>
    /// Service merge karaoke ASS: gop cac dong karaoke rieng le thanh cau hoan chinh voi {\k...} tags
    /// Quy tac:
    /// - Dau cau co ∞, cuoi cau khong co ∞ va ♫
    /// - Xoa dau ∞, xoa khoang cach, thay ♫ bang khoang cach
    /// - Them {\k...} voi thoi gian tinh bang centiseconds
    /// </summary>
    public class KaraokeMergeService
    {
        public static string ProcessKaraokeMerge(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var sb = new StringBuilder();
            var lines = input.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Parse tat ca cac dong Dialogue
            var parsedLines = new List<ParsedDialogueLine>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Dialogue:") || trimmed.StartsWith("Dialogue :"))
                {
                    var parsed = ParseDialogueLine(trimmed);
                    if (parsed != null)
                    {
                        parsedLines.Add(parsed);
                    }
                }
            }

            if (parsedLines.Count == 0) return string.Empty;

            // Gom nhom thanh cac cau
            var sentences = GroupIntoSentences(parsedLines);

            // Xuat ra ket qua
            foreach (var sentence in sentences)
            {
                sb.AppendLine(BuildMergedLine(sentence));
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Parse mot dong Dialogue ASS
        /// Format: Dialogue: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
        /// </summary>
        private static ParsedDialogueLine ParseDialogueLine(string line)
        {
            try
            {
                // Tim "Dialogue:" hoac "Dialogue: "
                var dialogueIdx = line.IndexOf("Dialogue:");
                if (dialogueIdx < 0) return null;

                var rest = line.Substring(dialogueIdx + 9).TrimStart();

                // Tach 9 fields dau, field thu 10 la text (co the chua ",")
                var parts = new List<string>();
                var current = new StringBuilder();
                var commaCount = 0;

                for (int i = 0; i < rest.Length; i++)
                {
                    var ch = rest[i];
                    if (ch == ',' && commaCount < 9)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                        commaCount++;
                    }
                    else
                    {
                        current.Append(ch);
                    }
                }
                parts.Add(current.ToString());

                if (parts.Count < 10) return null;

                var result = new ParsedDialogueLine();
                result.Layer = parts[0];
                result.StartTime = parts[1];
                result.EndTime = parts[2];
                result.Style = parts[3];
                result.Name = parts[4];
                result.MarginL = parts[5];
                result.MarginR = parts[6];
                result.MarginV = parts[7];
                result.Effect = parts[8];
                result.Text = parts[9];

                // Tinh thoi gian duration centiseconds
                result.DurationCs = CalculateDurationCs(result.StartTime, result.EndTime);

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tinh thoi gian duration bang centiseconds (1cs = 10ms)
        /// Format ASS: H:MM:SS.cc
        /// </summary>
        private static int CalculateDurationCs(string startTime, string endTime)
        {
            var startCs = ParseAssTimeToCs(startTime);
            var endCs = ParseAssTimeToCs(endTime);
            return Math.Max(0, endCs - startCs);
        }

        /// <summary>
        /// Parse thoi gian ASS thanh centiseconds
        /// </summary>
        private static int ParseAssTimeToCs(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return 0;

            timeStr = timeStr.Trim();
            var parts = timeStr.Split(':');
            if (parts.Length != 3) return 0;

            var hours = int.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);

            var secParts = parts[2].Split('.');
            var seconds = int.Parse(secParts[0]);
            var centiseconds = 0;
            if (secParts.Length > 1)
            {
                var csStr = secParts[1].PadRight(2, '0').Substring(0, 2);
                centiseconds = int.Parse(csStr);
            }

            return hours * 360000 + minutes * 6000 + seconds * 100 + centiseconds;
        }

        /// <summary>
        /// Gom nhom cac dong thanh cau
        /// - Cau bat dau khi text co ∞
        /// - Cau ket thuc khi text khong ket thuc bang ♫ va khong co ∞
        /// - Trong cung mot cau: extend end time cua moi dong cho trung start time cua dong ke tiep
        ///   (connect gap trong cau, khong extend giua 2 cau)
        /// </summary>
        private static List<KaraokeSentence> GroupIntoSentences(List<ParsedDialogueLine> lines)
        {
            var sentences = new List<KaraokeSentence>();
            KaraokeSentence currentSentence = null;

            for (int idx = 0; idx < lines.Count; idx++)
            {
                var line = lines[idx];
                var cleanText = line.Text.Trim();
                var hasInfinity = cleanText.Contains("∞");
                var endsWithMusicNote = cleanText.EndsWith("♫");
                var hasNextLine = (idx + 1 < lines.Count);
                var nextLine = hasNextLine ? lines[idx + 1] : null;
                var nextCleanText = nextLine != null ? nextLine.Text.Trim() : "";
                var nextHasInfinity = nextCleanText.Contains("∞");

                if (hasInfinity)
                {
                    // Bat dau cau moi
                    if (currentSentence != null && currentSentence.Words.Count > 0)
                    {
                        sentences.Add(currentSentence);
                    }
                    currentSentence = new KaraokeSentence();
                    currentSentence.StartTime = line.StartTime;
                    currentSentence.Style = line.Style;
                    currentSentence.Name = line.Name;
                    currentSentence.Layer = line.Layer;
                    currentSentence.MarginL = line.MarginL;
                    currentSentence.MarginR = line.MarginR;
                    currentSentence.MarginV = line.MarginV;
                    currentSentence.Effect = line.Effect;
                }

                if (currentSentence == null)
                {
                    currentSentence = new KaraokeSentence();
                    currentSentence.StartTime = line.StartTime;
                    currentSentence.Style = line.Style;
                }

                // Them word vao cau
                var word = new KaraokeWord();
                word.Text = line.Text;
                word.DurationCs = line.DurationCs;

                // Connect gap trong cau:
                // Neu dong nay co ♫ HOAC (dong sau khong co ∞ va dong nay khong phai cuoi cau)
                // => extend end time cua dong nay cho trung start time cua dong ke tiep
                var isLastWordInSentence = !endsWithMusicNote && !hasInfinity;
                if (hasNextLine && !isLastWordInSentence)
                {
                    // Dong trong cau (khong phai cuoi cau) => connect gap
                    word.DurationCs = CalculateDurationCs(line.StartTime, nextLine.StartTime);
                    word.ConnectedEndTime = nextLine.StartTime;
                }
                else if (hasNextLine && isLastWordInSentence && !nextHasInfinity)
                {
                    // Dong cuoi cau nhung dong sau cung thuoc cau nay (edge case) => connect gap
                    word.DurationCs = CalculateDurationCs(line.StartTime, nextLine.StartTime);
                    word.ConnectedEndTime = nextLine.StartTime;
                }

                currentSentence.Words.Add(word);

                // Cap nhat end time
                if (word.ConnectedEndTime != null)
                {
                    currentSentence.EndTime = word.ConnectedEndTime;
                }
                else
                {
                    currentSentence.EndTime = line.EndTime;
                }

                // Kiem tra ket thuc cau
                if (isLastWordInSentence)
                {
                    sentences.Add(currentSentence);
                    currentSentence = null;
                }
            }

            // Them cau cuoi cung
            if (currentSentence != null && currentSentence.Words.Count > 0)
            {
                sentences.Add(currentSentence);
            }

            return sentences;
        }

        /// <summary>
        /// Xay dung dong Dialogue da merge
        /// </summary>
        private static string BuildMergedLine(KaraokeSentence sentence)
        {
            var sb = new StringBuilder();

            // Xay dung text voi {\k...} tags
            var textParts = new List<string>();
            for (int i = 0; i < sentence.Words.Count; i++)
            {
                var word = sentence.Words[i];
                var processedText = ProcessWordText(word.Text);

                if (i == 0)
                {
                    // Tu dau tien: {\kX} Word (co space sau {\kX})
                    textParts.Add($"{{\\k{word.DurationCs}}} {processedText}");
                }
                else
                {
                    // Tu tiep theo: {\kX}word (space truoc {\kX}, khong space sau)
                    textParts.Add($" {{\\k{word.DurationCs}}}{processedText}");
                }
            }

            var mergedText = string.Join("", textParts);

            // Xoa spaces trong style name
            var cleanStyle = sentence.Style.Replace(" ", "");

            // Xay dung dong Dialogue (khong co space sau "Dialogue:")
            sb.Append($"Dialogue:{sentence.Layer},{sentence.StartTime},{sentence.EndTime},{cleanStyle},{sentence.Name},{sentence.MarginL},{sentence.MarginR},{sentence.MarginV},{sentence.Effect},{mergedText}");

            return sb.ToString();
        }

        /// <summary>
        /// Xu ly text cua mot word: xoa ∞, xoa ♫, xoa khoang cach
        /// </summary>
        private static string ProcessWordText(string text)
        {
            // Xoa ∞
            text = text.Replace("∞", "");
            // Xoa ♫
            text = text.Replace("♫", "");
            // Xoa khoang cach
            text = text.Replace(" ", "");
            return text;
        }

        #region Models

        private class ParsedDialogueLine
        {
            public string Layer { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Style { get; set; }
            public string Name { get; set; }
            public string MarginL { get; set; }
            public string MarginR { get; set; }
            public string MarginV { get; set; }
            public string Effect { get; set; }
            public string Text { get; set; }
            public int DurationCs { get; set; }
        }

        private class KaraokeSentence
        {
            public string Layer { get; set; } = "0";
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public string Style { get; set; }
            public string Name { get; set; } = "";
            public string MarginL { get; set; } = "0";
            public string MarginR { get; set; } = "0";
            public string MarginV { get; set; } = "0";
            public string Effect { get; set; } = "";
            public List<KaraokeWord> Words { get; set; } = new List<KaraokeWord>();
        }

        private class KaraokeWord
        {
            public string Text { get; set; }
            public int DurationCs { get; set; }
            public string ConnectedEndTime { get; set; }
        }

        #endregion
    }
}
