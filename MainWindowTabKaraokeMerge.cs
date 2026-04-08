using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region Karaoke Merge - Fields

        private bool _isKaraokeMergeUpdating = false;

        #endregion

        #region Karaoke Merge - Event Handlers

        private void TxtKaraokeMergeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeMergeUpdating) return;
            try
            {
                _isKaraokeMergeUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtKaraokeMergeInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtKaraokeMergeCount.Text = "";
                    TxtKaraokeMergeOutput.Text = "";
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var dialogueCount = lines.Count(l => l.Trim().StartsWith("Dialogue:"));
                TxtKaraokeMergeCount.Text = string.Format("({0} dialogue lines)", dialogueCount);

                // Xu ly merge karaoke
                TxtKaraokeMergeOutput.Text = KaraokeMergeService.ProcessKaraokeMerge(content);
            }
            catch (Exception ex)
            {
                TxtKaraokeMergeCount.Text = string.Format("(Error: {0})", ex.Message);
            }
            finally
            {
                _isKaraokeMergeUpdating = false;
            }
        }

        #endregion

        #region Karaoke Merge - Toast

        private async void ShowToastKaraokeMerge(string message)
        {
            ToastTextKaraokeMerge.Text = message;
            ToastBorderKaraokeMerge.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderKaraokeMerge.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Karaoke Merge - Copy Button

        private void BtnCopyKaraokeMerge_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtKaraokeMergeOutput.Text)) return;
            try
            {
                Clipboard.SetText(TxtKaraokeMergeOutput.Text);
                ShowToastKaraokeMerge("📋 Copied Merged Karaoke!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
