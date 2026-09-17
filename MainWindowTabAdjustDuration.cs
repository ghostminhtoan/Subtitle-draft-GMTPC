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
        #region "Adjust Duration - Fields"

        private bool _isAdjustDurationUpdating = false;
        private List<string> _adjustDurationSegments = new List<string>();
        private DispatcherTimer _adjustDurationDebounceTimer = new DispatcherTimer();
        private bool _adjustDurationPendingConvert = false;
        private TimeSpan _adjustDurationInitialStartTime = TimeSpan.Zero;

        #endregion

        #region "Adjust Duration - Initialization"

        private void InitializeAdjustDurationDebounce()
        {
            _adjustDurationDebounceTimer.Interval = TimeSpan.FromMilliseconds(300);
            _adjustDurationDebounceTimer.Tick += (sender, e) =>
            {
                _adjustDurationDebounceTimer.Stop();
                if (_adjustDurationPendingConvert)
                {
                    _adjustDurationPendingConvert = false;
                    ConvertAdjustDuration();
                }
            };
        }

        #endregion

        #region "Adjust Duration - Event Handlers"

        private void TxtAdjustDurationInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isAdjustDurationUpdating) return;
            _adjustDurationPendingConvert = true;
            _adjustDurationDebounceTimer.Stop();
            _adjustDurationDebounceTimer.Start();
        }

        private void BtnAdjustDurationConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _adjustDurationDebounceTimer.Stop();
                _adjustDurationPendingConvert = false;
                ConvertAdjustDuration();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnAdjustDurationCopy_Click(object sender, RoutedEventArgs e)
        {
            if (BtnAdjustDurationCopy.ContextMenu != null)
            {
                BtnAdjustDurationCopy.ContextMenu.PlacementTarget = BtnAdjustDurationCopy;
                BtnAdjustDurationCopy.ContextMenu.IsOpen = true;
            }
            else
            {
                CopyAdjustDurationFull();
            }
        }

        private void MenuAdjustDurationCopySubtitle_Click(object sender, RoutedEventArgs e)
        {
            CopyAdjustDurationFull();
        }

        private void MenuAdjustDurationCopySentence_Click(object sender, RoutedEventArgs e)
        {
            CopyAdjustDurationSentencesOnly();
        }

        private void CopyAdjustDurationFull()
        {
            if (string.IsNullOrWhiteSpace(TxtAdjustDurationOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtAdjustDurationOutput.Text);
                ShowToastAdjustDuration("📋 Đã copy subtitle!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void CopyAdjustDurationSentencesOnly()
        {
            if (_adjustDurationSegments == null || _adjustDurationSegments.Count == 0) return;
            try
            {
                string textOnly = string.Join(Environment.NewLine, _adjustDurationSegments);
                if (string.IsNullOrWhiteSpace(textOnly)) return;

                Clipboard.SetText(textOnly);
                ShowToastAdjustDuration("📋 Đã copy sentence!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void TxtAdjustDurationSetting_Changed(object sender, EventArgs e)
        {
            if (_isAdjustDurationUpdating) return;
            _adjustDurationPendingConvert = true;
            _adjustDurationDebounceTimer.Stop();
            _adjustDurationDebounceTimer.Start();
        }

        private void BtnAdjustDurationExport_Click(object sender, RoutedEventArgs e)
        {
            if (BtnAdjustDurationExport.ContextMenu != null)
            {
                BtnAdjustDurationExport.ContextMenu.PlacementTarget = BtnAdjustDurationExport;
                BtnAdjustDurationExport.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                BtnAdjustDurationExport.ContextMenu.IsOpen = true;
            }
        }

        private void MenuAdjustDurationExportAss_Click(object sender, RoutedEventArgs e)
        {
            ExportAdjustDurationAss();
        }

        private void MenuAdjustDurationExportSrt_Click(object sender, RoutedEventArgs e)
        {
            ExportAdjustDurationSrt();
        }

        #endregion

        #region "Adjust Duration - Core Logic"

        private void ConvertAdjustDuration()
        {
            try
            {
                _isAdjustDurationUpdating = true;

                var inputContent = TxtAdjustDurationInput.Text?.Trim();
                if (string.IsNullOrWhiteSpace(inputContent))
                {
                    _adjustDurationSegments.Clear();
                    _adjustDurationInitialStartTime = TimeSpan.Zero;
                    TxtAdjustDurationOutput.Text = "";
                    TxtAdjustDurationStats.Text = "";
                    return;
                }

                // Đọc settings
                int maxChars = GetAdjustDurationMaxChars();
                double maxCps = GetAdjustDurationCps();
                bool ignorePunctuation = GetAdjustDurationIgnorePunctuation();
                int gapMs = GetAdjustDurationGap();
                bool keepContinuous = GetAdjustDurationKeepContinuous();
                bool autoBreak = GetAdjustDurationAutoBreak();
                bool eachLine = GetAdjustDurationEachLine();

                // Validate
                if (maxChars < 50) maxChars = 50;
                if (maxCps < 1.0) maxCps = 17.0;
                if (gapMs < 0) gapMs = 0;

                // Phát hiện định dạng subtitle đầu vào (ASS hoặc SRT hoặc Plain text)
                var format = SubtitleParser.DetectFormat(inputContent);
                var extractedLines = new List<string>();
                TimeSpan initialStartTime = TimeSpan.Zero;

                if (format == SubtitleFormat.Ass)
                {
                    var assLines = SubtitleParser.ParseAss(inputContent);
                    if (assLines.Count > 0)
                    {
                        initialStartTime = assLines[0].StartTime;
                        foreach (var line in assLines)
                        {
                            string txt = GetCleanSubtitleLineText(line);
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                extractedLines.Add(txt);
                            }
                        }
                    }
                }
                else if (format == SubtitleFormat.Srt)
                {
                    var srtLines = SubtitleParser.ParseSrt(inputContent);
                    if (srtLines.Count > 0)
                    {
                        initialStartTime = srtLines[0].StartTime;
                        foreach (var line in srtLines)
                        {
                            string txt = GetCleanSubtitleLineText(line);
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                extractedLines.Add(txt);
                            }
                        }
                    }
                }

                _adjustDurationInitialStartTime = initialStartTime;

                // Bước 1: Chia văn bản thành các segment
                if (eachLine)
                {
                    if (extractedLines.Count > 0)
                    {
                        _adjustDurationSegments = new List<string>(extractedLines);
                    }
                    else
                    {
                        var rawLines = inputContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        var segments = new List<string>();
                        foreach (var rLine in rawLines)
                        {
                            var trimmed = rLine.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                            {
                                segments.Add(trimmed);
                            }
                        }
                        _adjustDurationSegments = segments;
                    }
                }
                else
                {
                    // Nếu không parse được line nào hoặc là text thường
                    string fullText;
                    if (extractedLines.Count > 0)
                    {
                        fullText = string.Join(" ", extractedLines);
                    }
                    else
                    {
                        fullText = inputContent;
                        initialStartTime = TimeSpan.Zero;
                    }

                    _adjustDurationSegments = SplitTextIntoSegments(fullText, maxChars, ignorePunctuation, keepContinuous, autoBreak);
                }

                // Bước 2: Tính toán time codes ASS bắt đầu từ initialStartTime
                var assOutput = BuildAssOutputForAdjustDuration(_adjustDurationSegments, maxCps, ignorePunctuation, gapMs, initialStartTime);

                // Bước 3: Hiển thị output
                TxtAdjustDurationOutput.Text = assOutput;

                // Bước 4: Thống kê
                UpdateAdjustDurationStats(_adjustDurationSegments, maxCps, ignorePunctuation, gapMs, initialStartTime);

                // Lưu settings
                SaveAdjustDurationSettings();
            }
            catch (Exception ex)
            {
                TxtAdjustDurationOutput.Text = string.Format("(Lỗi: {0})", ex.Message);
                TxtAdjustDurationStats.Text = "";
            }
            finally
            {
                _isAdjustDurationUpdating = false;
            }
        }

        private string GetCleanSubtitleLineText(SubtitleLine line)
        {
            if (line == null) return string.Empty;
            string raw = "";
            if (line is AssSubtitleLine ass)
            {
                raw = !string.IsNullOrEmpty(ass.DialogText) ? ass.DialogText : ass.Content;
            }
            else if (line is SrtSubtitleLine srt)
            {
                raw = !string.IsNullOrEmpty(srt.Text) ? srt.Text : srt.Content;
            }
            else
            {
                raw = line.Content ?? "";
            }

            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            // Loại bỏ các ASS tag {\...}
            raw = Regex.Replace(raw, @"\{[^}]*\}", "");
            // Thay thế ngắt dòng ASS \N hoặc \n bằng space
            raw = Regex.Replace(raw, @"\\[Nn]", " ");
            raw = raw.Replace("\r", " ").Replace("\n", " ");
            // Chuẩn hóa khoảng trắng
            return Regex.Replace(raw, @"\s+", " ").Trim();
        }

        private string BuildAssOutputForAdjustDuration(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs, TimeSpan startOffset)
        {
            if (segments == null || segments.Count == 0) return "";

            var sb = new StringBuilder();
            TimeSpan currentTime = startOffset;

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

        private string BuildSrtOutputForAdjustDuration(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs, TimeSpan startOffset)
        {
            if (segments == null || segments.Count == 0) return "";

            var sb = new StringBuilder();
            TimeSpan currentTime = startOffset;
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

        private void UpdateAdjustDurationStats(List<string> segments, double maxCps, bool ignorePunctuation, int gapMs, TimeSpan startOffset)
        {
            if (segments == null || segments.Count == 0)
            {
                TxtAdjustDurationStats.Text = "";
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

            double totalDurationSec = totalChars / maxCps;
            double totalGapSec = (segments.Count - 1) * gapMs / 1000.0;
            double totalTimeSec = totalDurationSec + totalGapSec;

            if (totalTimeSec > 0)
            {
                avgCps = totalChars / totalTimeSec;
            }

            TimeSpan totalTime = TimeSpan.FromSeconds(totalTimeSec);

            TxtAdjustDurationStats.Text = string.Format(
                "📊 {0} segments | Max: {1} chars/segment | Tổng: {2} chars | CPS trung bình: {3:F1} | Tổng thời lượng: {4}",
                segments.Count,
                maxSegmentChars,
                totalChars,
                avgCps,
                SubtitleLine.FormatAssTime(totalTime)
            );
        }

        private void ExportAdjustDurationAss()
        {
            if (string.IsNullOrWhiteSpace(TxtAdjustDurationOutput.Text))
            {
                System.Windows.MessageBox.Show("Chưa có phụ đề để export. Vui lòng nhập phụ đề và nhấn Convert trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Advanced SubStation Alpha (*.ass)|*.ass|All Files (*.*)|*.*",
                    DefaultExt = "ass",
                    FileName = "adjusted_subtitle.ass",
                    Title = "Export to ASS"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var content = BuildFullAssContent(TxtAdjustDurationOutput.Text);
                    File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);
                    ShowToastAdjustDuration("💾 Đã export file ASS thành công!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi export ASS: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAdjustDurationSrt()
        {
            if (string.IsNullOrWhiteSpace(TxtAdjustDurationOutput.Text))
            {
                System.Windows.MessageBox.Show("Chưa có phụ đề để export. Vui lòng nhập phụ đề và nhấn Convert trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SubRip Subtitle (*.srt)|*.srt|All Files (*.*)|*.*",
                    DefaultExt = "srt",
                    FileName = "adjusted_subtitle.srt",
                    Title = "Export to SRT"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string srtContent;
                    if (_adjustDurationSegments != null && _adjustDurationSegments.Count > 0)
                    {
                        double maxCps = GetAdjustDurationCps();
                        bool ignorePunctuation = GetAdjustDurationIgnorePunctuation();
                        int gapMs = GetAdjustDurationGap();
                        srtContent = BuildSrtOutputForAdjustDuration(_adjustDurationSegments, maxCps, ignorePunctuation, gapMs, _adjustDurationInitialStartTime);
                    }
                    else
                    {
                        var lines = SubtitleParser.ParseAss(TxtAdjustDurationOutput.Text);
                        srtContent = SubtitleParser.ToText(lines, SubtitleFormat.Srt);
                    }

                    File.WriteAllText(saveDialog.FileName, srtContent, Encoding.UTF8);
                    ShowToastAdjustDuration("💾 Đã export file SRT thành công!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi export SRT: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region "Adjust Duration - Settings"

        private int GetAdjustDurationMaxChars()
        {
            string text = TxtAdjustDurationMaxChars.Text?.Trim();
            int val = 500;
            if (int.TryParse(text, out val) && val > 0) return val;

            try
            {
                string saved = AppSettings.GetString("AdjustDurationMaxChars", "500");
                if (int.TryParse(saved, out val) && val > 0) return val;
            }
            catch { }
            return 500;
        }

        private double GetAdjustDurationCps()
        {
            string text = TxtAdjustDurationCps.Text?.Trim();
            double val = 17.0;
            if (double.TryParse(text, out val) && val > 0) return val;

            try
            {
                string saved = AppSettings.GetString("AdjustDurationCps", "17.0");
                if (double.TryParse(saved, out val) && val > 0) return val;
            }
            catch { }
            return 17.0;
        }

        private bool GetAdjustDurationIgnorePunctuation()
        {
            if (CmbAdjustDurationPunctuation == null) return true;
            return CmbAdjustDurationPunctuation.SelectedIndex == 1;
        }

        private int GetAdjustDurationGap()
        {
            string text = TxtAdjustDurationGap.Text?.Trim();
            int val = 200;
            if (int.TryParse(text, out val) && val >= 0) return val;

            try
            {
                string saved = AppSettings.GetString("AdjustDurationGap", "200");
                if (int.TryParse(saved, out val) && val >= 0) return val;
            }
            catch { }
            return 200;
        }

        private bool GetAdjustDurationKeepContinuous()
        {
            if (ChkAdjustDurationKeepContinuous == null) return false;
            return ChkAdjustDurationKeepContinuous.IsChecked == true;
        }

        private bool GetAdjustDurationAutoBreak()
        {
            if (ChkAdjustDurationAutoBreak == null) return false;
            return ChkAdjustDurationAutoBreak.IsChecked == true;
        }

        private bool GetAdjustDurationEachLine()
        {
            if (ChkAdjustDurationEachLine == null) return false;
            return ChkAdjustDurationEachLine.IsChecked == true;
        }

        private void SaveAdjustDurationSettings()
        {
            try
            {
                AppSettings.SetString("AdjustDurationMaxChars", TxtAdjustDurationMaxChars.Text?.Trim() ?? "500");
                AppSettings.SetString("AdjustDurationCps", TxtAdjustDurationCps.Text?.Trim() ?? "17.0");
                AppSettings.SetString("AdjustDurationGap", TxtAdjustDurationGap.Text?.Trim() ?? "200");
                AppSettings.SetString("AdjustDurationEachLine", ChkAdjustDurationEachLine?.IsChecked == true ? "1" : "0");
                Properties.Settings.Default.Save();
            }
            catch { }
        }

        private void LoadAdjustDurationSettings()
        {
            try
            {
                string maxChars = AppSettings.GetString("AdjustDurationMaxChars", "500");
                if (!string.IsNullOrEmpty(maxChars)) TxtAdjustDurationMaxChars.Text = maxChars;

                string cps = AppSettings.GetString("AdjustDurationCps", "17.0");
                if (!string.IsNullOrEmpty(cps)) TxtAdjustDurationCps.Text = cps;

                string gap = AppSettings.GetString("AdjustDurationGap", "200");
                if (!string.IsNullOrEmpty(gap)) TxtAdjustDurationGap.Text = gap;

                string eachLineStr = AppSettings.GetString("AdjustDurationEachLine", "0");
                if (ChkAdjustDurationEachLine != null)
                {
                    ChkAdjustDurationEachLine.IsChecked = eachLineStr == "1";
                }
            }
            catch { }
        }

        #endregion

        #region "Adjust Duration - Toast"

        private async void ShowToastAdjustDuration(string message)
        {
            ToastTextAdjustDuration.Text = message;
            ToastBorderAdjustDuration.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderAdjustDuration.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
