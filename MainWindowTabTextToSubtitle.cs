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
            if (string.IsNullOrWhiteSpace(TxtTextToSubOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtTextToSubOutput.Text);
                ShowToastTextToSub("\ud83d\udccb Đã copy phụ đề!");
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

                // Validate
                if (maxChars < 50) maxChars = 50;
                if (maxCps < 1.0) maxCps = 17.0;
                if (gapMs < 0) gapMs = 0;

                // Bước 1: Chia văn bản thành các segment
                _textToSubSegments = SplitTextIntoSegments(inputText, maxChars, ignorePunctuation);

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

        /// <summary>
        /// Chia văn bản thành các segment dựa trên số ký tự tối đa
        /// Ưu tiên cắt tại dấu câu (. ! ? 。) để câu trọn vẹn
        /// </summary>
        private List<string> SplitTextIntoSegments(string text, int maxChars, bool ignorePunctuation)
        {
            var segments = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return segments;

            // Loại bỏ khoảng trắng thừa ở đầu/cuối và các dòng trống liên tiếp
            text = Regex.Replace(text.Trim(), @"\s+", " ").Trim();

            int pos = 0;
            int length = text.Length;

            while (pos < length)
            {
                // Tính giới hạn kết thúc cho segment này
                int endPos = Math.Min(pos + maxChars, length);

                if (endPos >= length)
                {
                    // Đoạn còn lại nhỏ hơn maxChars → lấy hết
                    var segment = text.Substring(pos).Trim();
                    if (!string.IsNullOrWhiteSpace(segment))
                    {
                        segments.Add(segment);
                    }
                    break;
                }

                // Tìm vị trí cắt tốt nhất: dấu câu gần endPos nhất về phía trước
                int cutPos = FindBestCutPosition(text, pos, endPos);

                var segmentText = text.Substring(pos, cutPos - pos).Trim();
                if (!string.IsNullOrWhiteSpace(segmentText))
                {
                    segments.Add(segmentText);
                }

                pos = cutPos;
            }

            return segments;
        }

        /// <summary>
        /// Tìm vị trí cắt tốt nhất trong khoảng [startPos, endPos)
        /// Ưu tiên: dấu chấm câu (. ! ? 。) gần endPos nhất
        /// Nếu không có → tìm khoảng trắng gần nhất
        /// Nếu vẫn không có → cắt cứng tại endPos
        /// BỎ QUA: dấu chấm thập phân (3.3%), viết tắt (Mr., Mrs., Dr., etc.)
        /// </summary>
        private int FindBestCutPosition(string text, int startPos, int endPos)
        {
            // Các ký tự dấu câu ưu tiên (từ gần endPos nhất về trước)
            char[] sentenceEnders = { '!', '?', '。', '…' };

            // Tìm dấu câu KHÔNG phải dấu chấm (từ gần endPos nhất về trước)
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (Array.IndexOf(sentenceEnders, text[i]) >= 0)
                {
                    // Cắt SAU dấu câu (bao gồm dấu câu)
                    return i + 1;
                }
            }

            // Tìm dấu chấm "." nhưng KHÔNG phải dấu chấm thập phân hay viết tắt
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (text[i] == '.')
                {
                    // 1. Kiểm tra dấu chấm thập phân: ký tự trước và sau đều là số (0-9)
                    bool prevIsDigit = i > 0 && char.IsDigit(text[i - 1]);
                    bool nextIsDigit = i < text.Length - 1 && char.IsDigit(text[i + 1]);
                    if (prevIsDigit && nextIsDigit) continue; // Dấu chấm thập phân → bỏ qua

                    // 2. Kiểm tra viết tắt: trích word trước dấu chấm, kiểm tra trong dictionary
                    string wordBeforeDot = ExtractWordBefore(text, i);
                    if (!string.IsNullOrEmpty(wordBeforeDot) && Abbreviations.Contains(wordBeforeDot))
                    {
                        continue; // Viết tắt → bỏ qua
                    }

                    // 3. Rule bổ sung: nếu sau dấu chấm là space + chữ thường → khả năng cao viết tắt
                    // Ví dụ: "Mr. johnson" (chữ thường sau) vs "end. Start" (chữ HOA sau)
                    if (i + 2 < text.Length && char.IsWhiteSpace(text[i + 1]) && char.IsLower(text[i + 2]))
                    {
                        continue; // Khả năng cao là viết tắt → bỏ qua
                    }

                    // Còn lại → kết thúc câu thực sự → cắt tại đây
                    return i + 1;
                }
            }

            // Không có dấu câu → tìm khoảng trắng gần endPos nhất
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    // Cắt tại khoảng trắng (không bao gồm khoảng trắng)
                    return i + 1;
                }
            }

            // Không tìm được gì → cắt cứng tại endPos
            return endPos;
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
