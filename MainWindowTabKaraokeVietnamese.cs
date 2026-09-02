using System;
using System.Collections.Generic;
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
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeCount.Text = string.Format("({0} dòng)", lines.Length);

                // Xử lý karaoke
                var karaokeResult = KaraokeVietnameseService.ProcessLyrics(content);
                TxtKaraokeOutput.Text = karaokeResult;
                TxtKaraokeEditable.Text = karaokeResult;
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

        /// <summary>
        /// Double click vào bất kỳ chữ nào ở Panel 1 sẽ nhảy đến chữ tương ứng ở Panel 2 và 3
        /// </summary>
        private void TxtKaraokeInput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Cho phép TextBox hoàn thành thao tác double click chọn từ mặc định
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        JumpToCorrespondingWordViet();
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch { }
        }

        private void JumpToCorrespondingWordViet()
        {
            var text = TxtKaraokeInput.Text;
            if (string.IsNullOrEmpty(text)) return;

            int caretIndex = TxtKaraokeInput.SelectionStart;
            if (caretIndex < 0 || caretIndex > text.Length) caretIndex = 0;

            // Tìm từ đang được chọn hoặc tại vị trí caret
            int wordStart = caretIndex;
            while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]))
            {
                wordStart--;
            }

            int wordEnd = caretIndex;
            while (wordEnd < text.Length && !char.IsWhiteSpace(text[wordEnd]))
            {
                wordEnd++;
            }

            if (wordStart >= wordEnd) return;

            string selectedWord = text.Substring(wordStart, wordEnd - wordStart).Trim();
            if (string.IsNullOrEmpty(selectedWord)) return;

            // Đếm số lần selectedWord xuất hiện từ đầu text đến wordStart
            int occurrenceIndex = 0;
            int searchPos = 0;
            while (searchPos <= wordStart && searchPos < text.Length)
            {
                while (searchPos < text.Length && char.IsWhiteSpace(text[searchPos])) searchPos++;
                if (searchPos >= text.Length) break;

                int curWordEnd = searchPos;
                while (curWordEnd < text.Length && !char.IsWhiteSpace(text[curWordEnd])) curWordEnd++;

                string curWord = text.Substring(searchPos, curWordEnd - searchPos);
                if (string.Equals(curWord, selectedWord, StringComparison.OrdinalIgnoreCase))
                {
                    if (searchPos == wordStart)
                    {
                        break;
                    }
                    occurrenceIndex++;
                }

                searchPos = curWordEnd;
            }

            // Đồng bộ nhảy đến Panel 2 và Panel 3
            HighlightWordInTargetTextBoxViet(TxtKaraokeOutput, selectedWord, occurrenceIndex);
            HighlightWordInTargetTextBoxViet(TxtKaraokeEditable, selectedWord, occurrenceIndex);
        }

        /// <summary>
        /// Tìm và chọn dòng tương ứng của từ trong Panel 2 hoặc Panel 3 Karaoke Vietnamese
        /// </summary>
        private void HighlightWordInTargetTextBoxViet(TextBox targetBox, string targetWord, int occurrenceIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || string.IsNullOrEmpty(targetWord)) return;

            var targetText = targetBox.Text;
            string cleanTarget = targetWord.Trim().ToLowerInvariant();

            var lines = targetText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int currentOccurrence = 0;
            int lineStartIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var cleanLine = line.Replace("∞", "").Replace("♫", "").Trim().ToLowerInvariant();

                if (!string.IsNullOrEmpty(cleanLine) && (cleanTarget.StartsWith(cleanLine) || cleanLine.StartsWith(cleanTarget)))
                {
                    if (currentOccurrence == occurrenceIndex)
                    {
                        targetBox.Focus();
                        targetBox.Select(lineStartIndex, line.Length);
                        
                        int lineIndex = targetBox.GetLineIndexFromCharacterIndex(lineStartIndex);
                        if (lineIndex >= 0)
                        {
                            targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
                        }
                        return;
                    }
                    currentOccurrence++;
                }

                lineStartIndex += line.Length;
                if (lineStartIndex < targetText.Length)
                {
                    if (lineStartIndex + 1 < targetText.Length && targetText[lineStartIndex] == '\r' && targetText[lineStartIndex + 1] == '\n')
                    {
                        lineStartIndex += 2;
                    }
                    else
                    {
                        lineStartIndex += 1;
                    }
                }
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

