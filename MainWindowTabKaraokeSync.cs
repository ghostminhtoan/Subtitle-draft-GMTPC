using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Text;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region Karaoke Sync - Fields

        private bool _isSyncUpdating = false;

        #endregion

        #region Karaoke Sync - Event Handlers

        /// <summary>
        /// Khi input thay đổi, cập nhật số dòng
        /// </summary>
        private void TxtSyncInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncUpdating) return;
            try
            {
                _isSyncUpdating = true;
                var content = TxtSyncInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtSyncInputCount.Text = "";
                    TxtSyncOutput.Text = "";
                    TxtSyncOutputCount.Text = "";
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtSyncInputCount.Text = string.Format("({0} dòng)", lines.Length);

                // Tự động sync khi có input
                SyncTimeCodes();
            }
            catch (Exception ex)
            {
                TxtSyncInputCount.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                _isSyncUpdating = false;
            }
        }

        /// <summary>
        /// Khi time input thay đổi, auto sync
        /// </summary>
        private void TxtSyncTimeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncUpdating) return;
            try
            {
                SyncTimeCodes();
            }
            catch (Exception)
            {
                // Ignore parse errors during typing
            }
        }

        /// <summary>
        /// Offset toàn bộ time code theo desired start time (giữ nguyên duration)
        /// </summary>
        private void BtnSyncTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncTimeCodes();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi sync time: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Copy kết quả sync
        /// </summary>
        private void BtnCopySync_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtSyncOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtSyncOutput.Text);
                ShowToastSync("📋 Đã copy kết quả sync!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region Karaoke Sync - Sync Logic

        /// <summary>
        /// Thực hiện sync time codes
        /// Logic: Tính offset từ dòng đầu tiên, áp dụng offset đó cho tất cả các dòng
        /// </summary>
        private void SyncTimeCodes()
        {
            var inputContent = TxtSyncInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputContent))
            {
                TxtSyncOutput.Text = "";
                TxtSyncOutputCount.Text = "";
                return;
            }

            // Parse desired start time từ textbox (format: HH:MM:SS.mmm hoặc HH:MM:SS,mmm)
            var desiredStartMs = ParseTimeInputToMs(TxtSyncTimeInput.Text);
            if (desiredStartMs < 0)
            {
                TxtSyncOutput.Text = inputContent;
                TxtSyncOutputCount.Text = "(Lỗi: Time format không hợp lệ)";
                return;
            }

            var lines = inputContent.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Tìm dòng đầu tiên có time code (Dialogue hoặc Comment)
            long? firstLineStartTimeMs = null;
            long offsetMs = 0;

            foreach (var line in lines)
            {
                // Thử parse SRT format
                var srtMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})");
                if (srtMatch.Success)
                {
                    var startTimeStr = srtMatch.Groups[1].Value;
                    firstLineStartTimeMs = ParseSrtTimeToMs(startTimeStr);
                    break;
                }

                // Thử parse ASS format (Dialogue hoặc Comment)
                var assMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(?:Dialogue|Comment):\s*\d+,(\d+:\d{2}:\d{2}\.\d{2}),(\d+:\d{2}:\d{2}\.\d{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (assMatch.Success)
                {
                    var startTimeStr = assMatch.Groups[1].Value;
                    firstLineStartTimeMs = ParseAssTimeToMs(startTimeStr);
                    break;
                }
            }

            // Nếu không tìm thấy time code nào
            if (firstLineStartTimeMs == null)
            {
                TxtSyncOutput.Text = inputContent;
                TxtSyncOutputCount.Text = "(0 dòng đã sync)";
                return;
            }

            // Tính offset: desired - actual
            offsetMs = desiredStartMs - firstLineStartTimeMs.Value;

            // Áp dụng offset cho tất cả các dòng
            var sb = new StringBuilder();
            int processedCount = 0;

            foreach (var line in lines)
            {
                var processedLine = line;

                // Xử lý SRT format: 00:00:00,000 --> 00:00:00,000
                var srtMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})");
                if (srtMatch.Success)
                {
                    var startTimeStr = srtMatch.Groups[1].Value;
                    var endTimeStr = srtMatch.Groups[2].Value;

                    var startMs = ParseSrtTimeToMs(startTimeStr);
                    var endMs = ParseSrtTimeToMs(endTimeStr);

                    // Offset time, đảm bảo không âm
                    var newStartMs = Math.Max(0L, startMs + offsetMs);
                    var newEndMs = Math.Max(0L, endMs + offsetMs);

                    // Giữ nguyên duration
                    var duration = endMs - startMs;
                    newEndMs = newStartMs + duration;

                    var newStartStr = MsToSrtTime(newStartMs);
                    var newEndStr = MsToSrtTime(newEndMs);

                    processedLine = line.Replace(startTimeStr, newStartStr).Replace(endTimeStr, newEndStr);
                    processedCount += 1;
                }
                else
                {
                    // Xử lý ASS format: Dialogue hoặc Comment
                    var assMatch = System.Text.RegularExpressions.Regex.Match(line, @"^((?:Dialogue|Comment):\s*\d+,)(\d+:\d{2}:\d{2}\.\d{2}),(\d+:\d{2}:\d{2}\.\d{2})(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (assMatch.Success)
                    {
                        var prefix = assMatch.Groups[1].Value;
                        var startTimeStr = assMatch.Groups[2].Value;
                        var endTimeStr = assMatch.Groups[3].Value;
                        var suffix = assMatch.Groups[4].Value;

                        var startMs = ParseAssTimeToMs(startTimeStr);
                        var endMs = ParseAssTimeToMs(endTimeStr);

                        // Offset time, đảm bảo không âm
                        var newStartMs = Math.Max(0L, startMs + offsetMs);
                        var newEndMs = Math.Max(0L, endMs + offsetMs);

                        // Giữ nguyên duration
                        var duration = endMs - startMs;
                        newEndMs = newStartMs + duration;

                        var newStartStr = MsToAssTime(newStartMs);
                        var newEndStr = MsToAssTime(newEndMs);

                        processedLine = prefix + newStartStr + "," + newEndStr + suffix;
                        processedCount += 1;
                    }
                }

                sb.AppendLine(processedLine);
            }

            TxtSyncOutput.Text = sb.ToString().TrimEnd();
            TxtSyncOutputCount.Text = string.Format("({0} dòng đã sync)", processedCount);
        }

        #endregion

        #region Karaoke Sync - Time Parsing Helpers

        /// <summary>
        /// Parse time input string (HH:MM:SS.mmm hoặc HH:MM:SS,mmm) sang milliseconds
        /// Trả về -1 nếu format không hợp lệ
        /// </summary>
        private long ParseTimeInputToMs(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return 0L;

            // Chuẩn hóa: thay comma bằng dot để thống nhất xử lý
            timeStr = timeStr.Trim().Replace(',', '.');

            // Regex: HH:MM:SS.mmm (có thể bỏ HH hoặc MM)
            var match = System.Text.RegularExpressions.Regex.Match(timeStr, @"^(\d+):(\d{2}):(\d{2})\.(\d+)$");
            if (!match.Success)
            {
                return -1L;
            }

            try
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var fracSeconds = match.Groups[4].Value;

                // Parse fractional seconds (có thể là 1-3 digits)
                int milliseconds = 0;
                if (fracSeconds.Length == 1)
                {
                    milliseconds = int.Parse(fracSeconds) * 100;
                }
                else if (fracSeconds.Length == 2)
                {
                    milliseconds = int.Parse(fracSeconds) * 10;
                }
                else
                {
                    milliseconds = int.Parse(fracSeconds.Substring(0, 3));
                }

                return (hours * 3600L + minutes * 60L + seconds) * 1000L + milliseconds;
            }
            catch
            {
                return -1L;
            }
        }

        /// <summary>
        /// Parse SRT time string (00:00:00,000) sang milliseconds
        /// </summary>
        private long ParseSrtTimeToMs(string timeStr)
        {
            var parts = timeStr.Split(':', ',');
            if (parts.Length != 4) return 0L;

            var hours = int.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);
            var seconds = int.Parse(parts[2]);
            var milliseconds = int.Parse(parts[3]);

            return hours * 3600000L + minutes * 60000L + seconds * 1000L + milliseconds;
        }

        /// <summary>
        /// Convert milliseconds sang SRT time string (00:00:00,000)
        /// </summary>
        private string MsToSrtTime(long ms)
        {
            var isNegative = ms < 0;
            ms = Math.Abs(ms);

            var hours = ms / 3600000L;
            ms = ms % 3600000L;
            var minutes = ms / 60000L;
            ms = ms % 60000L;
            var seconds = ms / 1000L;
            var milliseconds = ms % 1000L;

            return string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", hours, minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Parse ASS time string (H:MM:SS.cc) sang milliseconds
        /// </summary>
        private long ParseAssTimeToMs(string timeStr)
        {
            var parts = timeStr.Split(':', '.');
            if (parts.Length != 4) return 0L;

            var hours = int.Parse(parts[0]);
            var minutes = int.Parse(parts[1]);
            var seconds = int.Parse(parts[2]);
            var centiseconds = int.Parse(parts[3]);

            return hours * 3600000L + minutes * 60000L + seconds * 1000L + centiseconds * 10L;
        }

        /// <summary>
        /// Convert milliseconds sang ASS time string (H:MM:SS.cc)
        /// </summary>
        private string MsToAssTime(long ms)
        {
            var isNegative = ms < 0;
            ms = Math.Abs(ms);

            var hours = ms / 3600000L;
            ms = ms % 3600000L;
            var minutes = ms / 60000L;
            ms = ms % 60000L;
            var seconds = ms / 1000L;
            var centiseconds = (ms % 1000L) / 10L;

            return string.Format("{0}:{1:D2}:{2:D2}.{3:D2}", hours, minutes, seconds, centiseconds);
        }

        #endregion

        #region Karaoke Sync - Toast

        private async void ShowToastSync(string message)
        {
            ToastTextSync.Text = message;
            ToastBorderSync.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderSync.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}
