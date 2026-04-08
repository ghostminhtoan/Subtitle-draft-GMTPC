using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
