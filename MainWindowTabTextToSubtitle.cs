using System;
using System.Collections.Generic;
using System.IO;
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

        private void BtnTextToSubExport_Click(object sender, RoutedEventArgs e)
        {
            if (BtnTextToSubExport.ContextMenu != null)
            {
                BtnTextToSubExport.ContextMenu.PlacementTarget = BtnTextToSubExport;
                BtnTextToSubExport.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                BtnTextToSubExport.ContextMenu.IsOpen = true;
            }
        }

        private void MenuTextToSubExportAss_Click(object sender, RoutedEventArgs e)
        {
            ExportTextToSubtitleAss();
        }

        private void MenuTextToSubExportSrt_Click(object sender, RoutedEventArgs e)
        {
            ExportTextToSubtitleSrt();
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

                int cutPos = FindBestCutPosition(text, pos, endPos, keepContinuous, autoBreak, maxChars);
                if (cutPos <= pos)
                {
                    cutPos = Math.Min(pos + 1, length);
                }

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
            if (last == '!' || last == '?' || last == ':' || last == '。' || trimmed.EndsWith("…") || trimmed.EndsWith("..."))
            {
                return true;
            }
            if (last == '.')
            {
                string wordBefore = ExtractWordBefore(trimmed, trimmed.Length - 1);
                if (!string.IsNullOrEmpty(wordBefore))
                {
                    if (Abbreviations.Contains(wordBefore) || IsIndexOrOutlineMarker(wordBefore))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tìm vị trí cắt tốt nhất trong khoảng [startPos, endPos)
        /// Theo 2 quy tắc Checkbox:
        /// 1. Keep continuous sentence: không ngắt giữa chừng. Nếu không có dấu câu phù hợp thì cố gắng mở rộng hoặc ghép các đoạn.
        /// 2. Auto split sentence (khi bật autoBreak = true): ngắt ngay khi kết thúc câu (. ! ? : 。 hoặc ... + Chữ HOA) theo thứ tự từ trái sang phải.
        ///    Đặc biệt: Không được phép split tại số đếm và mục lục (1., 2., I., II., a., b., A., B., A1., A2., a1., a2...).
        /// Khi KHÔNG bật autoBreak: Chia theo độ dài Max Chars bình thường (quét lùi từ endPos - 1 về startPos).
        /// </summary>
        private int FindBestCutPosition(string text, int startPos, int endPos, bool keepContinuous, bool autoBreak, int maxChars = 500)
        {
            char[] standardEnders = { '!', '?', ':', '。' };

            // =========================================================================
            // TRƯỜNG HỢP 1: BẬT "Auto split sentence"
            // Quét TỪ TRÁI SANG PHẢI để tìm điểm kết thúc câu ĐẦU TIÊN và ngắt dòng ngay
            // =========================================================================
            if (autoBreak)
            {
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
                        // Nếu là ký tự '.' đứng trước ký tự '.' khác (thuộc dấu ba chấm "...") -> bỏ qua để bắt dấu chấm cuối của cụm
                        if (i + 1 < text.Length && text[i + 1] == '.')
                        {
                            continue;
                        }

                        // Kiểm tra dấu ba chấm "..." hoặc '…'
                        bool isEllipsis = (i >= 1 && text[i - 1] == '.') || text[i] == '…';
                        if (isEllipsis)
                        {
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
                                else
                                {
                                    // Kiểm tra xem từ tiếp theo có phải là mục lục (ví dụ b1., 2., a2.) không
                                    string nextWord = ExtractWordAt(text, nextCharIdx);
                                    if (!string.IsNullOrEmpty(nextWord) && IsIndexOrOutlineMarker(nextWord))
                                    {
                                        return i + 1;
                                    }
                                    if (char.IsLower(nextChar))
                                    {
                                        // Viết thường → Bỏ qua (không tự ngắt tại đây)
                                        continue;
                                    }
                                }
                            }
                            return i + 1;
                        }

                        // Dấu chấm đơn: Kiểm tra số thập phân (1.5)
                        bool prevIsDigit = i > 0 && char.IsDigit(text[i - 1]);
                        bool nextIsDigit = i < text.Length - 1 && char.IsDigit(text[i + 1]);
                        if (prevIsDigit && nextIsDigit) continue;

                        int wordBeforeStart;
                        string wordBeforeDot = ExtractWordBefore(text, i, out wordBeforeStart);

                        // Kiểm tra từ viết tắt (Mr., Dr., etc.)
                        if (!string.IsNullOrEmpty(wordBeforeDot) && Abbreviations.Contains(wordBeforeDot))
                        {
                            continue;
                        }

                        // Kiểm tra số đếm và mục lục (1., 2., I., a., A., A1., a1., ...) -> KHÔNG ĐƯỢC PHÉP SPLIT
                        if (!string.IsNullOrEmpty(wordBeforeDot) && IsIndexOrOutlineMarker(wordBeforeDot))
                        {
                            // 1. Nếu nằm ở đầu segment (ví dụ "1. Nội dung" hoặc "A1. Mục A1") -> Chắc chắn là bullet, không split
                            bool isAtStart = wordBeforeStart <= startPos || string.IsNullOrWhiteSpace(text.Substring(startPos, wordBeforeStart - startPos).Trim('(', '[', '{', '§', '#', '-', '*'));
                            if (isAtStart)
                            {
                                continue;
                            }

                            // 2. Nếu đoạn trước đó không có từ nội dung nào (chỉ gồm các số đếm / mục lục liên tiếp như "1. 2. 3..." hay "a. b. c.") -> Không split
                            if (!HasContentWords(text, startPos, wordBeforeStart))
                            {
                                continue;
                            }

                            // 3. Nếu là marker đi sau nội dung (như "Mục A1." trong "A1. Mục A1. A2. Mục A2."):
                            // Kiểm tra từ tiếp theo sau dấu chấm có phải là marker mới (A2., 2., b.) không
                            int nextCharIdx = i + 1;
                            while (nextCharIdx < text.Length && char.IsWhiteSpace(text[nextCharIdx]))
                            {
                                nextCharIdx++;
                            }
                            if (nextCharIdx < text.Length)
                            {
                                string nextWord = ExtractWordAt(text, nextCharIdx);
                                if (!string.IsNullOrEmpty(nextWord) && IsIndexOrOutlineMarker(nextWord))
                                {
                                    // Đằng sau bắt đầu mục lục mới -> Ngắt câu tại đây
                                    return i + 1;
                                }
                            }

                            // Mặc định không split tại số đếm/mục lục
                            continue;
                        }

                        // Auto split sentence: ngắt ngay tại dấu chấm kết thúc câu này
                        return i + 1;
                    }
                }

                // Nếu trong khoảng không có dấu câu nào:
                // Chỉ ngắt tại khoảng trắng nếu độ dài vượt quá maxChars
                if (endPos - startPos > maxChars)
                {
                    for (int i = Math.Min(startPos + maxChars, endPos) - 1; i > startPos; i--)
                    {
                        if (char.IsWhiteSpace(text[i]))
                        {
                            return i + 1;
                        }
                    }
                }

                return endPos;
            }

            // =========================================================================
            // TRƯỜNG HỢP 2: KHÔNG BẬT "Auto split sentence" (CHƯA TICK)
            // Chia theo độ dài Max Chars mặc định: Quét LÙI từ endPos - 1 về startPos
            // =========================================================================

            // 1. Kiểm tra dấu câu thông thường (! ? : 。) từ endPos - 1 lùi về startPos
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (Array.IndexOf(standardEnders, text[i]) >= 0)
                {
                    return i + 1;
                }
            }

            // 2. Xử lý dấu chấm "." và dấu ba chấm "..." từ endPos - 1 lùi về startPos
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (text[i] == '.')
                {
                    // Bỏ qua nếu là chấm ở giữa chuỗi '...' (tiếp theo vẫn là '.')
                    if (i + 1 < text.Length && text[i + 1] == '.')
                    {
                        continue;
                    }

                    // Kiểm tra dấu ba chấm "..."
                    bool isEllipsis = (i >= 1 && text[i - 1] == '.') || text[i] == '…';
                    if (isEllipsis)
                    {
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
                                return i + 1;
                            }
                            else
                            {
                                string nextWord = ExtractWordAt(text, nextCharIdx);
                                if (!string.IsNullOrEmpty(nextWord) && IsIndexOrOutlineMarker(nextWord))
                                {
                                    return i + 1;
                                }
                                if (char.IsLower(nextChar))
                                {
                                    continue;
                                }
                            }
                        }
                        return i + 1;
                    }

                    // Dấu chấm đơn
                    bool prevIsDigit = i > 0 && char.IsDigit(text[i - 1]);
                    bool nextIsDigit = i < text.Length - 1 && char.IsDigit(text[i + 1]);
                    if (prevIsDigit && nextIsDigit) continue;

                    string wordBeforeDot = ExtractWordBefore(text, i);
                    if (!string.IsNullOrEmpty(wordBeforeDot) && Abbreviations.Contains(wordBeforeDot))
                    {
                        continue;
                    }

                    // Số đếm và mục lục: không ngắt tại đây
                    if (!string.IsNullOrEmpty(wordBeforeDot) && IsIndexOrOutlineMarker(wordBeforeDot))
                    {
                        continue;
                    }

                    if (i + 2 < text.Length && char.IsWhiteSpace(text[i + 1]) && char.IsLower(text[i + 2]))
                    {
                        continue;
                    }

                    return i + 1;
                }
            }

            // 3. Ưu tiên ngắt tại khoảng trắng gần endPos nhất
            for (int i = endPos - 1; i > startPos; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    return i + 1;
                }
            }

            return endPos;
        }

        /// <summary>
        /// Trích xuất từ bắt đầu tại vị trí pos (dừng tại khoảng trắng hoặc dấu câu)
        /// </summary>
        private string ExtractWordAt(string text, int pos)
        {
            if (string.IsNullOrEmpty(text) || pos < 0 || pos >= text.Length) return "";
            int start = pos;
            int end = pos;
            while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '.' && text[end] != ':' && text[end] != '?' && text[end] != '!')
            {
                end++;
            }
            if (end <= start) return "";
            return text.Substring(start, end - start).Trim();
        }

        /// <summary>
        /// Trích xuất word ngay trước vị trí pos (dừng tại khoảng trắng hoặc đầu chuỗi)
        /// Ví dụ: text="Hello Mr. Smith", pos=8 (vị trí dấu chấm) → trả về "Mr"
        /// </summary>
        private string ExtractWordBefore(string text, int pos, out int wordStart)
        {
            wordStart = -1;
            if (string.IsNullOrEmpty(text) || pos <= 0 || pos > text.Length) return "";

            int start = pos - 1;

            // Lùi về trước đến khi gặp khoảng trắng hoặc đầu chuỗi
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]) && text[start - 1] != '.' && text[start - 1] != ':' && text[start - 1] != '?' && text[start - 1] != '!' && text[start - 1] != ';')
            {
                start--;
            }

            if (start >= pos) return "";

            wordStart = start;
            return text.Substring(start, pos - start).Trim();
        }

        private string ExtractWordBefore(string text, int pos)
        {
            int dummy;
            return ExtractWordBefore(text, pos, out dummy);
        }

        /// <summary>
        /// Kiểm tra token có phải số đếm hoặc mục lục không (1., 2., I., a., A., A1., a1., ...)
        /// Không được phép split câu tại các vị trí này.
        /// </summary>
        private static bool IsIndexOrOutlineMarker(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Loại bỏ các dấu mở/đóng ngoặc hoặc bullet bao quanh (nếu có): (1), [A], 1), a)
            string clean = token.Trim().TrimStart('(', '[', '{', '§', '#', '-', '*').TrimEnd(')', ']', '}');
            if (string.IsNullOrEmpty(clean)) return false;

            // 1. Số đếm: 1, 2, 3, 10, ... hoặc nhiều cấp: 1.1, 1.2.3, ...
            if (Regex.IsMatch(clean, @"^\d+(\.\d+)*$"))
            {
                return true;
            }

            // 2. Chữ số La Mã phổ biến trong mục lục (I..L / i..l từ 1 đến 50): I, II, III, IV, V, VI, VII, VIII, IX, X, XI, XII, XX, etc.
            if (Regex.IsMatch(clean, @"^(?i:X{0,4}(?:IX|IV|V?I{0,3})|XL|L)$") && clean.Length > 0)
            {
                return true;
            }

            // 3. Chữ cái đơn mục lục (a, b, c, ... z hoặc A, B, C, ... Z)
            if (clean.Length == 1 && char.IsLetter(clean[0]))
            {
                return true;
            }

            // 4. Chữ cái kèm số (A1, A2, B1, B2, a1, a2, b1, b2, C10, c01...)
            if (Regex.IsMatch(clean, @"^[a-zA-Z]{1,3}\d{1,4}$"))
            {
                return true;
            }

            // 5. Số kèm chữ cái (1a, 1b, 2a, 2b, 1A, 1B...)
            if (Regex.IsMatch(clean, @"^\d{1,4}[a-zA-Z]{1,3}$"))
            {
                return true;
            }

            // 6. Mục lục phân cấp kết hợp (A.1, a.1, 1.a...)
            if (Regex.IsMatch(clean, @"^[a-zA-Z0-9]+(\.[a-zA-Z0-9]+)+$"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra trong khoảng [startPos, endPos) có chứa từ vựng thực tế (content words)
        /// không phải là số đếm hoặc mục lục hay không.
        /// </summary>
        private static bool HasContentWords(string text, int startPos, int endPos)
        {
            if (string.IsNullOrEmpty(text) || endPos <= startPos) return false;
            int len = Math.Min(endPos, text.Length) - startPos;
            if (len <= 0) return false;

            string sub = text.Substring(startPos, len);
            string[] tokens = sub.Split(new char[] { ' ', '\t', '\r', '\n', '.', '…', ':', '!', '?', ',', ';', '(', ')', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                if (!IsIndexOrOutlineMarker(t))
                {
                    return true;
                }
            }
            return false;
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

        /// <summary>
        /// Xuất file phụ đề ASS có đầy đủ header [Script Info], [V4+ Styles], [Events]
        /// </summary>
        private void ExportTextToSubtitleAss()
        {
            if (string.IsNullOrWhiteSpace(TxtTextToSubOutput.Text))
            {
                System.Windows.MessageBox.Show("Chưa có phụ đề để export. Vui lòng nhập văn bản và nhấn Convert trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Advanced SubStation Alpha (*.ass)|*.ass|All Files (*.*)|*.*",
                    DefaultExt = "ass",
                    FileName = "subtitle.ass",
                    Title = "Export to ASS"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var content = BuildFullAssContent(TxtTextToSubOutput.Text);
                    File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);
                    ShowToastTextToSub("💾 Đã export file ASS thành công!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi export ASS: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Tạo nội dung hoàn chỉnh của file ASS kèm header chuẩn nếu chưa có
        /// </summary>
        private string BuildFullAssContent(string dialogueLines)
        {
            if (string.IsNullOrWhiteSpace(dialogueLines)) return "";

            if (dialogueLines.Contains("[Script Info]") || dialogueLines.Contains("[Events]"))
            {
                return dialogueLines;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[Script Info]");
            sb.AppendLine("; Script generated by Subtitle draft GMTPC");
            sb.AppendLine("Title: Subtitle draft GMTPC");
            sb.AppendLine("ScriptType: v4.00+");
            sb.AppendLine("WrapStyle: 0");
            sb.AppendLine("ScaledBorderAndShadow: yes");
            sb.AppendLine("YCbCr Matrix: None");
            sb.AppendLine();
            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
            sb.AppendLine("Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1");
            sb.AppendLine();
            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
            sb.AppendLine(dialogueLines.Trim());

            return sb.ToString();
        }

        /// <summary>
        /// Xuất file phụ đề SRT
        /// </summary>
        private void ExportTextToSubtitleSrt()
        {
            if (string.IsNullOrWhiteSpace(TxtTextToSubOutput.Text))
            {
                System.Windows.MessageBox.Show("Chưa có phụ đề để export. Vui lòng nhập văn bản và nhấn Convert trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SubRip Subtitle (*.srt)|*.srt|All Files (*.*)|*.*",
                    DefaultExt = "srt",
                    FileName = "subtitle.srt",
                    Title = "Export to SRT"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string srtContent;
                    if (_textToSubSegments != null && _textToSubSegments.Count > 0)
                    {
                        double maxCps = GetTextToSubCps();
                        bool ignorePunctuation = GetTextToSubIgnorePunctuation();
                        int gapMs = GetTextToSubGap();
                        srtContent = BuildSrtOutput(_textToSubSegments, maxCps, ignorePunctuation, gapMs);
                    }
                    else
                    {
                        var lines = SubtitleParser.ParseAss(TxtTextToSubOutput.Text);
                        srtContent = SubtitleParser.ToText(lines, SubtitleFormat.Srt);
                    }

                    File.WriteAllText(saveDialog.FileName, srtContent, Encoding.UTF8);
                    ShowToastTextToSub("💾 Đã export file SRT thành công!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi export SRT: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Build nội dung format SRT từ các segment với time code đồng bộ
        /// </summary>
        private string BuildSrtOutput(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs)
        {
            if (segments == null || segments.Count == 0) return "";

            var sb = new StringBuilder();
            TimeSpan currentTime = TimeSpan.Zero;
            int index = 1;

            foreach (var segment in segments)
            {
                int charCount = CountCharacters(segment, ignorePunctuation);
                double durationSeconds = charCount / maxCps;
                TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);

                TimeSpan startTime = currentTime;
                TimeSpan endTime = startTime + duration;

                sb.AppendLine(index.ToString());
                sb.AppendLine(string.Format("{0} --> {1}", SubtitleLine.FormatSrtTime(startTime), SubtitleLine.FormatSrtTime(endTime)));
                sb.AppendLine(segment);
                sb.AppendLine();

                currentTime = endTime + TimeSpan.FromMilliseconds(gapMs);
                index++;
            }

            return sb.ToString().TrimEnd();
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
