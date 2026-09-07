using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region "Text to Subtitle - Fields"

        private bool _isTextToSubUpdating = false;
        private List<string> _textToSubSegments = new List<string>();
        private DispatcherTimer _textToSubDebounceTimer = new DispatcherTimer();
        private bool _textToSubPendingConvert = false;

        // Danh sách viết tắt tiếng Anh phổ biến (không coi là kết thúc câu)
        private static readonly HashSet<string> Abbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Danh xưng
            "Mr", "Mrs", "Ms", "Dr", "Prof", "Sr", "Jr",
            // Học vị, chức danh
            "PhD", "MD", "BA", "MA", "BSc", "MSc", "LLB", "JD",
            // Địa lý, địa chỉ
            "St", "Ave", "Blvd", "Rd", "Dr", "Ln", "Ct", "Pl",
            "Mt", "Ft", "Pres", "Sec", "Gen", "Col", "Lt", "Maj", "Capt", "Sgt",
            // Thời gian
            "Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Sept", "Oct", "Nov", "Dec",
            "Sun", "Mon", "Tue", "Tue", "Wed", "Thu", "Fri", "Sat",
            "AM", "PM", "am", "pm", "AM", "PM",
            // Khác phổ biến
            "e", "g", "i", "e", "vs", "etc", "al", "nr", "no",
            "vol", "ed", "pp", "p", "cf", "ca", "cir", "c",
            // Viết tắt khác
            "Inc", "Ltd", "Corp", "Co", "Dept", "Univ", "Assn", "Intl"
        };

        #endregion

        #region "Text to Subtitle - Initialization"

        private void InitializeTextToSubtitleDebounce()
        {
            _textToSubDebounceTimer.Interval = TimeSpan.FromMilliseconds(300);
            _textToSubDebounceTimer.Tick += (sender, e) =>
            {
                _textToSubDebounceTimer.Stop();
                if (_textToSubPendingConvert)
                {
                    _textToSubPendingConvert = false;
                    ConvertTextToSubtitle();
                }
            };
        }

        #endregion

        #region "Text to Subtitle - Event Handlers"

        private void TxtTextToSubInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isTextToSubUpdating) return;
            _textToSubPendingConvert = true;
            _textToSubDebounceTimer.Stop();
            _textToSubDebounceTimer.Start();
        }

        private void BtnTextToSubConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _textToSubDebounceTimer.Stop();
                _textToSubPendingConvert = false;
                ConvertTextToSubtitle();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnTextToSubCopy_Click(object sender, RoutedEventArgs e)
        {
            if (BtnTextToSubCopy.ContextMenu != null)
            {
                BtnTextToSubCopy.ContextMenu.PlacementTarget = BtnTextToSubCopy;
                BtnTextToSubCopy.ContextMenu.IsOpen = true;
            }
            else
            {
                CopyTextToSubtitleFull();
            }
        }

        private void MenuTextToSubCopySubtitle_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToSubtitleFull();
        }

        private void MenuTextToSubCopySentence_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToSubtitleSentencesOnly();
        }

        private void CopyTextToSubtitleFull()
        {
            if (string.IsNullOrWhiteSpace(TxtTextToSubOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtTextToSubOutput.Text);
                ShowToastTextToSub("📋 Đã copy subtitle!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CopyTextToSubtitleSentencesOnly()
        {
            if (_textToSubSegments == null || _textToSubSegments.Count == 0) return;
            try
            {
                string textOnly = string.Join(Environment.NewLine, _textToSubSegments);
                if (string.IsNullOrWhiteSpace(textOnly)) return;

                Clipboard.SetText(textOnly);
                ShowToastTextToSub("📋 Đã copy sentence!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void TxtTextToSubSetting_Changed(object sender, EventArgs e)
        {
            if (_isTextToSubUpdating) return;
            _textToSubPendingConvert = true;
            _textToSubDebounceTimer.Stop();
            _textToSubDebounceTimer.Start();
        }

        #endregion

        #region "Text to Subtitle - Core Logic"

        private void ConvertTextToSubtitle()
        {
            try
            {
                _isTextToSubUpdating = true;

                var inputText = TxtTextToSubInput.Text?.Trim();
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    _textToSubSegments.Clear();
                    TxtTextToSubOutput.Text = "";
                    TxtTextToSubStats.Text = "";
                    return;
                }

                // Đọc settings
                int maxChars = GetTextToSubMaxChars();
                double maxCps = GetTextToSubCps();
                bool ignorePunctuation = GetTextToSubIgnorePunctuation();
                int gapMs = GetTextToSubGap();
                bool keepContinuous = GetTextToSubKeepContinuous();
                bool autoBreak = GetTextToSubAutoBreak();

                // Validate
                if (maxChars < 50) maxChars = 50;
                if (maxCps < 1.0) maxCps = 17.0;
                if (gapMs < 0) gapMs = 0;

                // Bước 1: Chia văn bản thành các segment
                _textToSubSegments = SplitTextIntoSegments(inputText, maxChars, ignorePunctuation, keepContinuous, autoBreak);

                // Bước 2: Tính toán time codes
                var assOutput = BuildAssOutput(_textToSubSegments, maxCps, ignorePunctuation, gapMs);

                // Bước 3: Hiển thị output
                TxtTextToSubOutput.Text = assOutput;

                // Bước 4: Thống kê
                UpdateTextToSubStats(_textToSubSegments, maxCps, ignorePunctuation, gapMs);

                // Lưu settings
                SaveTextToSubSettings();
            }
            catch (Exception ex)
            {
                TxtTextToSubOutput.Text = string.Format("(Lỗi: {0})", ex.Message);
                TxtTextToSubStats.Text = "";
            }
            finally
            {
                _isTextToSubUpdating = false;
            }
        }

        private bool GetTextToSubKeepContinuous()
        {
            if (ChkTextToSubKeepContinuous == null) return false;
            return ChkTextToSubKeepContinuous.IsChecked == true;
        }

        private bool GetTextToSubAutoBreak()
        {
            if (ChkTextToSubAutoBreak == null) return false;
            return ChkTextToSubAutoBreak.IsChecked == true;
        }

        /// <summary>
        /// Chia văn bản thành các segment dựa trên các thiết lập Max Chars, Keep Continuous, Auto Split Sentence
        /// </summary>
        private List<string> SplitTextIntoSegments(string text, int maxChars, bool ignorePunctuation, bool keepContinuous, bool autoBreak)
        {
            var segments = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return segments;

            // Đảm bảo khoảng trắng chuẩn hóa
            text = Regex.Replace(text.Trim(), @"\s+", " ").Trim();

            int pos = 0;
            int length = text.Length;

            while (pos < length)
            {
                // Khi bật autoBreak (Auto split sentence), quét trên TOÀN BỘ chiều dài còn lại của text để tìm kết thúc câu
                int endPos = autoBreak ? length : Math.Min(pos + maxChars, length);

                if (!autoBreak && endPos >= length)
                {
                    var segment = text.Substring(pos).Trim();
                    if (!string.IsNullOrWhiteSpace(segment))
                    {
                        segments.Add(segment);
                    }
                    break;
                }

                int cutPos = FindBestCutPosition(text, pos, endPos, keepContinuous, autoBreak);

                var segmentText = text.Substring(pos, cutPos - pos).Trim();
                if (!string.IsNullOrWhiteSpace(segmentText))
                {
                    segments.Add(segmentText);
                }

                pos = cutPos;
            }

            // Nếu bật keepContinuous: ghép các segment không kết thúc bằng dấu chấm câu vào segment kế tiếp
            if (keepContinuous && segments.Count > 1)
            {
                var mergedSegments = new List<string>();
                string currentMerged = "";

                for (int i = 0; i < segments.Count; i++)
                {
                    if (string.IsNullOrEmpty(currentMerged))
                    {
                        currentMerged = segments[i];
                    }
                    else
                    {
                        currentMerged += " " + segments[i];
                    }

                    bool endsWithSentencePunct = IsSentenceEndPunctuation(segments[i]);
                    // Nếu đã kết thúc câu hoặc là segment cuối cùng thì chốt segment này
                    if (endsWithSentencePunct || i == segments.Count - 1)
                    {
                        mergedSegments.Add(currentMerged);
                        currentMerged = "";
                    }
                }

                if (!string.IsNullOrEmpty(currentMerged))
                {
                    mergedSegments.Add(currentMerged);
                }

                segments = mergedSegments;
            }

            return segments;
        }

        private bool IsSentenceEndPunctuation(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return false;
            string trimmed = str.TrimEnd();
            if (trimmed.Length == 0) return false;
            char last = trimmed[trimmed.Length - 1];
            return last == '.' || last == '!' || last == '?' || last == ':' || last == '。' || trimmed.EndsWith("…") || trimmed.EndsWith("...");
        }

        /// <summary>
        /// Tìm vị trí cắt tốt nhất trong khoảng [startPos, endPos)
        /// Theo 2 quy tắc Checkbox:
        /// 1. Keep continuous sentence: không ngắt giữa chừng. Nếu không có dấu câu phù hợp thì cố gắng mở rộng hoặc ghép các đoạn.
        /// 2. Auto split sentence: ngắt ngay khi kết thúc câu (. ! ? : 。 hoặc ... + Chữ HOA). Nếu ... + chữ thường thì không tự ngắt.
        /// </summary>
        private int FindBestCutPosition(string text, int startPos, int endPos, bool keepContinuous, bool autoBreak)
        {
            char[] standardEnders = { '!', '?', ':', '。' };

            // Quét từng ký tự từ startPos + 1 đến endPos - 1 từ TRÁI SANG PHẢI để tìm điểm kết thúc câu ĐẦU TIÊN
            for (int i = startPos + 1; i < endPos; i++)
            {
                // Dấu câu thông thường (! ? : 。)
                if (Array.IndexOf(standardEnders, text[i]) >= 0)
                {
                    return i + 1;
                }

                // Dấu chấm "." hoặc dấu ba chấm "..."
                if (text[i] == '.')
                {
                    // Kiểm tra dấu ba chấm "..."
                    bool isEllipsis = (i >= 2 && text[i - 1] == '.' && text[i - 2] == '.') || text[i] == '…';
                    
                    if (isEllipsis)
                    {
                        // Kiểm tra ký tự theo sau dấu ba chấm
                        int nextCharIdx = i + 1;
                        while (nextCharIdx < text.Length && char.IsWhiteSpace(text[nextCharIdx]))
                        {
                            nextCharIdx++;
                        }

                        if (nextCharIdx < text.Length)
                        {
                            char nextChar = text[nextCharIdx];
                            if (char.IsUpper(nextChar))
                            {
                                // Viết HOA → Tự ngắt
                                return i + 1;
                            }
                            else if (char.IsLower(nextChar))
                            {
                                // Viết thường → Bỏ qua (không tự ngắt tại đây)
                                continue;
                            }
                        }
                        return i + 1;
                    }

                    // Dấu chấm đơn: Kiểm tra số (1.5)
                    bool prevIsDigit = i > 0 && char.IsDigit(text[i - 1]);
                    bool nextIsDigit = i < text.Length - 1 && char.IsDigit(text[i + 1]);
                    if (prevIsDigit && nextIsDigit) continue;

                    // Kiểm tra từ viết tắt (Mr., Dr., etc.)
                    string wordBeforeDot = ExtractWordBefore(text, i);
                    if (!string.IsNullOrEmpty(wordBeforeDot) && Abbreviations.Contains(wordBeforeDot))
                    {
                        continue;
                    }

                    if (autoBreak)
                    {
                        // Auto split sentence: ngắt ngay tại dấu chấm này
                        return i + 1;
                    }

                    if (i + 2 < text.Length && char.IsWhiteSpace(text[i + 1]) && char.IsLower(text[i + 2]))
                    {
                        continue;
                    }

                    return i + 1;
                }
            }

            // 3. Nếu không có dấu câu nào trong khoảng và không bật autoBreak hoặc đã hết giới hạn:
            // Nếu bật autoBreak mà cả đoạn không có dấu ngắt câu nào, dùng Max Chars làm điểm cắt mặc định
            int maxCutLimit = Math.Min(startPos + 1000, endPos);
            for (int i = maxCutLimit - 1; i > startPos; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    return i + 1;
                }
            }

            return maxCutLimit;
        }

        /// <summary>
        /// Trích xuất word ngay trước vị trí pos (dừng tại khoảng trắng hoặc đầu chuỗi)
        /// Ví dụ: text="Hello Mr. Smith", pos=8 (vị trí dấu chấm) → trả về "Mr"
        /// </summary>
        private string ExtractWordBefore(string text, int pos)
        {
            if (pos <= 0) return "";

            int end = pos - 1;
            int start = end;

            // Lùi về trước đến khi gặp khoảng trắng hoặc đầu chuỗi
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]) && text[start - 1] != '.')
            {
                start--;
            }

            if (start >= end) return "";

            return text.Substring(start, end - start);
        }

        /// <summary>
        /// Đếm số ký tự (có hoặc không bỏ punctuation)
        /// </summary>
        private int CountCharacters(string text, bool ignorePunctuation)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            if (ignorePunctuation)
            {
                // Bỏ punctuation: ()[]-:;,.!?'" \t\n\r và các ký tự tương tự
                int count = 0;
                foreach (char c in text)
                {
                    if (!IsPunctuation(c) && !char.IsWhiteSpace(c))
                    {
                        count++;
                    }
                }
                return count;
            }
            else
            {
                // Đếm tất cả ký tự (bao gồm space)
                return text.Length;
            }
        }

        /// <summary>
        /// Kiểm tra ký tự có phải punctuation không
        /// </summary>
        private bool IsPunctuation(char c)
        {
            return "()-[]:;,.!?'\u201C\u201D\u2018\u2019\u2026\u2014\u2013\"".IndexOf(c) >= 0;
        }

        /// <summary>
        /// Build output ASS format với time codes
        /// </summary>
        private string BuildAssOutput(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs)
        {
            if (segments.Count == 0) return "";

            var sb = new StringBuilder();
            TimeSpan currentTime = TimeSpan.Zero;

            foreach (var segment in segments)
            {
                int charCount = CountCharacters(segment, ignorePunctuation);

                // Tính duration: charCount / CPS = seconds
                double durationSeconds = charCount / maxCps;
                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);

                TimeSpan startTime = currentTime;
                TimeSpan endTime = startTime + duration;

                // Format ASS line
                string startStr = SubtitleLine.FormatAssTime(startTime);
                string endStr = SubtitleLine.FormatAssTime(endTime);

                sb.AppendFormat("Dialogue: 0,{0},{1},Default,,0,0,0,,{2}", startStr, endStr, segment);
                sb.AppendLine();

                // Tính start time cho dòng tiếp theo = end time + gap
                currentTime = endTime + TimeSpan.FromMilliseconds(gapMs);
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Cập nhật thống kê
        /// </summary>
        private void UpdateTextToSubStats(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs)
        {
            if (segments.Count == 0)
            {
                TxtTextToSubStats.Text = "";
                return;
            }

            int totalChars = 0;
            int maxSegmentChars = 0;
            double avgCps = 0;

            foreach (var seg in segments)
            {
                int count = CountCharacters(seg, ignorePunctuation);
                totalChars += count;
                if (count > maxSegmentChars) maxSegmentChars = count;
            }

            // Tính avg CPS thực tế
            double totalDurationSec = totalChars / maxCps;
            double totalGapSec = (segments.Count - 1) * gapMs / 1000.0;
            double totalTimeSec = totalDurationSec + totalGapSec;

            if (totalTimeSec > 0)
            {
                avgCps = totalChars / totalTimeSec;
            }

            TimeSpan totalTime = TimeSpan.FromSeconds(totalTimeSec);

            TxtTextToSubStats.Text = string.Format(
                "📊 {0} segments | Max: {1} chars/segment | Tổng: {2} chars | CPS trung bình: {3:F1} | Tổng thời lượng: {4}",
                segments.Count,
                maxSegmentChars,
                totalChars,
                avgCps,
                SubtitleLine.FormatAssTime(totalTime)
            );
        }

        #endregion

        #region "Text to Subtitle - Settings"

        private int GetTextToSubMaxChars()
        {
            string text = TxtTextToSubMaxChars.Text?.Trim();
            int val = 500;
            if (int.TryParse(text, out val) && val > 0) return val;

            // Thử đọc từ AppSettings
            try
            {
                var saved = Properties.Settings.Default.TextToSubMaxChars;
                if (saved > 0) return saved;
            }
            catch { }
            return 500;
        }

        private double GetTextToSubCps()
        {
            string text = TxtTextToSubCps.Text?.Trim();
            double val = 17.0;
            if (double.TryParse(text, out val) && val > 0) return val;

            try
            {
                var saved = Properties.Settings.Default.TextToSubCps;
                if (saved > 0) return saved;
            }
            catch { }
            return 17.0;
        }

        private bool GetTextToSubIgnorePunctuation()
        {
            // Default: Ignore punctuation
            if (CmbTextToSubPunctuation == null) return true;
            return CmbTextToSubPunctuation.SelectedIndex == 1; // Index 1 = Ignore punctuation
        }

        private int GetTextToSubGap()
        {
            string text = TxtTextToSubGap.Text?.Trim();
            int val = 200;
            if (int.TryParse(text, out val) && val >= 0) return val;

            try
            {
                var saved = Properties.Settings.Default.TextToSubGap;
                if (saved >= 0) return saved;
            }
            catch { }
            return 200;
        }

        private void SaveTextToSubSettings()
        {
            try
            {
                int maxChars;
                if (int.TryParse(TxtTextToSubMaxChars.Text?.Trim(), out maxChars) && maxChars > 0)
                    Properties.Settings.Default.TextToSubMaxChars = maxChars;

                double cps;
                if (double.TryParse(TxtTextToSubCps.Text?.Trim(), out cps) && cps > 0)
                    Properties.Settings.Default.TextToSubCps = cps;

                int gap;
                if (int.TryParse(TxtTextToSubGap.Text?.Trim(), out gap) && gap >= 0)
                    Properties.Settings.Default.TextToSubGap = gap;

                Properties.Settings.Default.Save();
            }
            catch { }
        }

        private void LoadTextToSubSettings()
        {
            try
            {
                int maxChars = Properties.Settings.Default.TextToSubMaxChars;
                if (maxChars > 0) TxtTextToSubMaxChars.Text = maxChars.ToString();

                double cps = Properties.Settings.Default.TextToSubCps;
                if (cps > 0) TxtTextToSubCps.Text = cps.ToString("F1");

                int gap = Properties.Settings.Default.TextToSubGap;
                if (gap >= 0) TxtTextToSubGap.Text = gap.ToString();
            }
            catch { }
        }

        #endregion

        #region "Text to Subtitle - Toast"

        private async void ShowToastTextToSub(string message)
        {
            ToastTextTextToSub.Text = message;
            ToastBorderTextToSub.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderTextToSub.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}
