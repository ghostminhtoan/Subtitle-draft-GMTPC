using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region Karaoke Vietnamese - Fields

        private bool _isKaraokeUpdating = false;
        private bool _isKaraokeSyncingSelection = false;
        private KaraokeVietnameseService.KaraokeMappingResult _currentKaraokeVietMappingResult;

        #endregion

        #region Karaoke Vietnamese - Event Handlers

        private void TxtKaraokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeUpdating) return;
            try
            {
                _isKaraokeUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtKaraokeInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtKaraokeCount.Text = "";
                    TxtKaraokeOutput.Text = "";
                    TxtKaraokeEditable.Text = "";
                    _currentKaraokeVietMappingResult = null;
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeCount.Text = string.Format("({0} dòng)", lines.Length);

                // Xử lý karaoke với Mapping
                var mappingResult = KaraokeVietnameseService.ProcessLyricsWithMapping(content, null, null);
                _currentKaraokeVietMappingResult = mappingResult;

                TxtKaraokeOutput.Text = mappingResult.FormattedOutput;
                TxtKaraokeEditable.Text = mappingResult.FormattedOutput;
            }
            catch (Exception ex)
            {
                TxtKaraokeCount.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                _isKaraokeUpdating = false;
            }
        }

        #endregion

        #region Karaoke Vietnamese - Selection & Double Click Synchronization

        private void TxtKaraokeInput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeSyncingSelection || _isKaraokeUpdating) return;
            if (TxtKaraokeInput.IsFocused || TxtKaraokeInput.SelectionLength > 0)
            {
                SyncSelectionFromInputViet();
            }
        }

        private void TxtKaraokeInput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromInputViet();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeOutput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeSyncingSelection || _isKaraokeUpdating) return;
            if (TxtKaraokeOutput.IsFocused)
            {
                SyncSelectionFromOutputOrEditableViet(TxtKaraokeOutput);
            }
        }

        private void TxtKaraokeOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditableViet(TxtKaraokeOutput);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeEditable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeSyncingSelection || _isKaraokeUpdating) return;
            if (TxtKaraokeEditable.IsFocused)
            {
                SyncSelectionFromOutputOrEditableViet(TxtKaraokeEditable);
            }
        }

        private void TxtKaraokeEditable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditableViet(TxtKaraokeEditable);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Đồng bộ từ Panel 1 (Input) sang Panel 2 và Panel 3 dựa trên Mapping chính xác 1-1
        /// </summary>
        private void SyncSelectionFromInputViet()
        {
            if (_isKaraokeSyncingSelection) return;
            try
            {
                _isKaraokeSyncingSelection = true;
                var text = TxtKaraokeInput.Text;
                if (string.IsNullOrEmpty(text))
                {
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeOutput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeEditable);
                    return;
                }

                // Xóa highlight Adorner cũ ở chính Input nếu có
                Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeInput);

                int selStart = TxtKaraokeInput.SelectionStart;
                int selLen = TxtKaraokeInput.SelectionLength;

                if (_currentKaraokeVietMappingResult != null && _currentKaraokeVietMappingResult.Mappings.Count > 0)
                {
                    var targetMappings = _currentKaraokeVietMappingResult.Mappings
                        .Where(m => (selStart >= m.InputStart && selStart <= m.InputStart + m.InputLength) ||
                                    (m.InputStart >= selStart && m.InputStart < selStart + Math.Max(1, selLen)))
                        .ToList();

                    if (targetMappings.Count > 0)
                    {
                        var firstMap = targetMappings[0];
                        var lastMap = targetMappings[targetMappings.Count - 1];

                        HighlightLineRangeInOutputViet(TxtKaraokeOutput, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        HighlightLineRangeInOutputViet(TxtKaraokeEditable, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        return;
                    }
                }

                // Fallback scroll theo tỷ lệ dòng
                int lineIndex = TxtKaraokeInput.GetLineIndexFromCharacterIndex(selStart);
                if (lineIndex >= 0)
                {
                    int totalInputLines = TxtKaraokeInput.LineCount;
                    if (totalInputLines > 0)
                    {
                        int totalOutLines = TxtKaraokeOutput.LineCount;
                        int estimatedOutLine = (int)(((double)lineIndex / totalInputLines) * totalOutLines);
                        HighlightLineInBoxByIndexViet(TxtKaraokeOutput, estimatedOutLine);
                        HighlightLineInBoxByIndexViet(TxtKaraokeEditable, estimatedOutLine);
                    }
                }
            }
            finally
            {
                _isKaraokeSyncingSelection = false;
            }
        }

        /// <summary>
        /// Đồng bộ từ Panel 2 (Output) hoặc Panel 3 (Editable) sang Panel 1 và Panel còn lại
        /// </summary>
        private void SyncSelectionFromOutputOrEditableViet(TextBox sourceBox)
        {
            if (_isKaraokeSyncingSelection) return;
            try
            {
                _isKaraokeSyncingSelection = true;
                var text = sourceBox.Text;
                if (string.IsNullOrEmpty(text))
                {
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeInput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeOutput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeEditable);
                    return;
                }

                // Xóa Adorner highlight ở chính sourceBox đang focus để hiển thị selection native
                Helpers.TextHighlightAdorner.ClearHighlight(sourceBox);

                int caret = sourceBox.SelectionStart;
                int lineIndex = sourceBox.GetLineIndexFromCharacterIndex(caret);
                if (lineIndex < 0) return;

                if (_currentKaraokeVietMappingResult != null && _currentKaraokeVietMappingResult.Mappings.Count > 0)
                {
                    var map = _currentKaraokeVietMappingResult.Mappings.FirstOrDefault(m => m.OutputLineIndex == lineIndex);
                    if (map != null)
                    {
                        // Highlight trực quan Panel 1 (Input)
                        TxtKaraokeInput.Select(map.InputStart, map.InputLength);
                        Helpers.TextHighlightAdorner.SetHighlight(TxtKaraokeInput, map.InputStart, map.InputLength);

                        int inputLineIdx = TxtKaraokeInput.GetLineIndexFromCharacterIndex(map.InputStart);
                        if (inputLineIdx >= 0)
                        {
                            TxtKaraokeInput.ScrollToLine(Math.Max(0, inputLineIdx - 2));
                        }

                        var otherBox = (sourceBox == TxtKaraokeOutput) ? TxtKaraokeEditable : TxtKaraokeOutput;
                        HighlightLineRangeInOutputViet(otherBox, lineIndex, lineIndex);
                        return;
                    }
                }

                var otherTargetBox = (sourceBox == TxtKaraokeOutput) ? TxtKaraokeEditable : TxtKaraokeOutput;
                HighlightLineInBoxByIndexViet(otherTargetBox, lineIndex);

                int totalSourceLines = sourceBox.LineCount;
                if (totalSourceLines > 0)
                {
                    int totalInputLines = TxtKaraokeInput.LineCount;
                    int estimatedInputLine = (int)(((double)lineIndex / totalSourceLines) * totalInputLines);
                    ScrollToLineInBoxViet(TxtKaraokeInput, estimatedInputLine);
                }
            }
            finally
            {
                _isKaraokeSyncingSelection = false;
            }
        }

        private void HighlightLineRangeInOutputViet(TextBox targetBox, int startLineIndex, int endLineIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || startLineIndex < 0) return;

            int lineCount = targetBox.LineCount;
            if (startLineIndex >= lineCount) startLineIndex = lineCount - 1;
            if (endLineIndex >= lineCount) endLineIndex = lineCount - 1;
            if (startLineIndex < 0) return;

            int charStart = targetBox.GetCharacterIndexFromLineIndex(startLineIndex);
            int charEnd = targetBox.GetCharacterIndexFromLineIndex(endLineIndex) + targetBox.GetLineLength(endLineIndex);
            if (charStart >= 0 && charEnd >= charStart)
            {
                int length = charEnd - charStart;
                targetBox.Select(charStart, length);
                Helpers.TextHighlightAdorner.SetHighlight(targetBox, charStart, length);
                targetBox.ScrollToLine(Math.Max(0, startLineIndex - 2));
            }
        }

        private void HighlightLineInBoxByIndexViet(TextBox targetBox, int lineIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || lineIndex < 0) return;
            if (lineIndex >= targetBox.LineCount) lineIndex = targetBox.LineCount - 1;
            if (lineIndex < 0) return;

            int charStart = targetBox.GetCharacterIndexFromLineIndex(lineIndex);
            int charLen = targetBox.GetLineLength(lineIndex);
            if (charStart >= 0 && charLen >= 0)
            {
                targetBox.Select(charStart, charLen);
                Helpers.TextHighlightAdorner.SetHighlight(targetBox, charStart, charLen);
                targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
            }
        }

        private void ScrollToLineInBoxViet(TextBox targetBox, int lineIndex)
        {
            if (targetBox == null || lineIndex < 0) return;
            if (lineIndex >= targetBox.LineCount) lineIndex = targetBox.LineCount - 1;
            if (lineIndex >= 0)
            {
                targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
            }
        }

        #endregion

        #region Karaoke Vietnamese - Toast

        private async void ShowToastKaraoke(string message)
        {
            ToastTextKaraoke.Text = message;
            ToastBorderKaraoke.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderKaraoke.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Karaoke Vietnamese - Copy Button

        private void BtnCopyKaraoke_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtKaraokeEditable.Text)) return;
            try
            {
                Clipboard.SetText(TxtKaraokeEditable.Text);
                ShowToastKaraoke("📋 Đã copy Karaoke!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

    }
}

