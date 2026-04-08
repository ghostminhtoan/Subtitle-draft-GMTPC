using System;
using System.Collections.Generic;
using System.Linq;
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

        #region Zero Time - Fields

        private List<SubtitleLine> _zeroLines = new List<SubtitleLine>();
        private SubtitleFormat _zeroFormat = SubtitleFormat.Unknown;
        private bool _isZeroUpdating = false;
        private List<string> _zeroPlainTexts = new List<string>();

        #endregion

        #region Zero Time - Event Handlers

        private void TxtZeroInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isZeroUpdating) return;
            try
            {
                _isZeroUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtZeroInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    _zeroLines.Clear();
                    _zeroFormat = SubtitleFormat.Unknown;
                    _zeroPlainTexts.Clear();
                    TxtZeroFormat.Text = "";
                    TxtZeroOutput.Text = "";
                    return;
                }
                _zeroFormat = SubtitleParser.DetectFormat(content);
                if (_zeroFormat != SubtitleFormat.Unknown)
                {
                    // Phụ đề chuẩn (SRT/ASS)
                    _zeroLines = SubtitleParser.Parse(content);
                    _zeroPlainTexts.Clear();
                    TxtZeroFormat.Text = string.Format("({0} - {1} dòng)", _zeroFormat.ToString().ToUpper(), _zeroLines.Count);
                }
                else
                {
                    // Plain text: mỗi dòng là 1 entry
                    _zeroLines.Clear();
                    _zeroPlainTexts = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    TxtZeroFormat.Text = string.Format("(Text - {0} dòng)", _zeroPlainTexts.Count);
                }
                UpdateZeroOutput();
            }
            catch (Exception ex)
            {
                TxtZeroFormat.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                _isZeroUpdating = false;
            }
        }

        private void BtnCopyZero_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtZeroOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtZeroOutput.Text);
                ShowToastZero("📋 Đã copy Zero Time!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region Zero Time - Display Methods

        /// <summary>
        /// Set toàn bộ StartTime và EndTime về 0 rồi xuất ra
        /// Hỗ trợ cả phụ đề SRT/ASS và plain text
        /// </summary>
        private void UpdateZeroOutput()
        {
            if (_zeroLines.Count == 0 && _zeroPlainTexts.Count == 0)
            {
                TxtZeroOutput.Text = "";
                return;
            }

            var sb = new StringBuilder();
            var zeroTimeAss = "0:00:00.00";

            // Nếu là plain text → xuất ra ASS format với time = 0
            if (_zeroPlainTexts.Count > 0)
            {
                for (int i = 0; i < _zeroPlainTexts.Count; i++)
                {
                    var text = _zeroPlainTexts[i].Replace("\\N", "\\\\N").Replace("\n", "\\N").Replace(Environment.NewLine, "\\N");
                    sb.AppendLine(string.Format("Dialogue: 0,{0},{1},Default,,0000,0000,0000,,{2}", zeroTimeAss, zeroTimeAss, text));
                }
                TxtZeroOutput.Text = sb.ToString().TrimEnd();
                return;
            }

            // Nếu là phụ đề SRT/ASS → xuất ASS format
            foreach (var line in _zeroLines)
            {
                var assLine = line as AssSubtitleLine;
                if (assLine != null)
                {
                    // ASS: giữ nguyên cấu trúc, thay time bằng 0
                    sb.AppendLine(string.Format("Dialogue: {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                        assLine.Layer, zeroTimeAss, zeroTimeAss, assLine.Style,
                        assLine.Name, assLine.MarginL, assLine.MarginR, assLine.MarginV,
                        assLine.Effect, assLine.DialogText));
                }
                else
                {
                    var srtLine = line as SrtSubtitleLine;
                    if (srtLine != null)
                    {
                        // SRT → chuyển sang ASS format
                        var text = srtLine.Text.Replace("\\N", "\\\\N").Replace("\n", "\\N").Replace(Environment.NewLine, "\\N");
                        sb.AppendLine(string.Format("Dialogue: 0,{0},{1},Default,,0000,0000,0000,,{2}", zeroTimeAss, zeroTimeAss, text));
                    }
                }
            }

            TxtZeroOutput.Text = sb.ToString().TrimEnd();
        }

        #endregion

        #region Zero Time - Toast

        private async void ShowToastZero(string message)
        {
            ToastTextZero.Text = message;
            ToastBorderZero.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderZero.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}
