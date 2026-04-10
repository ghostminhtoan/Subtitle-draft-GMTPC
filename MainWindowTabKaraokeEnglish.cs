using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Karaoke English - Fields

        private bool _isKaraokeEngUpdating = false;
        private string _pendingKaraokeEngRules;
        private string _wordListFilePath;

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                // Lấy rules từ hardcoded string
                _pendingKaraokeEngRules = WordListRules.DefaultRules;
            }
            catch (Exception ex)
            {
                _pendingKaraokeEngRules = "";
            }
        }

        #endregion

        #region Karaoke English - Event Handlers

        private void TxtKaraokeEngInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;
            ProcessKaraokeEngInput();
        }

        private void ProcessKaraokeEngInput()
        {
            if (_isKaraokeEngUpdating) return;
            try
            {
                _isKaraokeEngUpdating = true;
                var content = SubtitleParser.SanitizeContent(TxtKaraokeEngInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtKaraokeEngCount.Text = "";
                    TxtKaraokeEngOutput.Text = "";
                    TxtKaraokeEngEditable.Text = "";
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeEngCount.Text = string.Format("({0} lines)", lines.Length);

                var splitRules = _pendingKaraokeEngRules ?? "";
                var karaokeResult = KaraokeVietnameseService.ProcessLyricsWithSplitRules(content, splitRules);
                TxtKaraokeEngOutput.Text = karaokeResult;
                TxtKaraokeEngEditable.Text = karaokeResult;
            }
            catch (Exception ex)
            {
                TxtKaraokeEngCount.Text = string.Format("(Error: {0})", ex.Message);
            }
            finally
            {
                _isKaraokeEngUpdating = false;
            }
        }

        #endregion

        #region Karaoke English - Toast

        private async void ShowToastKaraokeEng(string message)
        {
            ToastTextKaraokeEng.Text = message;
            ToastBorderKaraokeEng.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderKaraokeEng.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Karaoke English - Open Word List

        /// <summary>
        /// Export hardcoded rules to file và mở bằng Notepad
        /// </summary>
        private void BtnOpenWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo file tạm trong thư mục app
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");

                // Export rules từ hardcoded string ra file
                File.WriteAllText(_wordListFilePath, WordListRules.DefaultRules);

                // Mở file bằng Notepad
                Process.Start("notepad.exe", $"\"{_wordListFilePath}\"");
                ShowToastKaraokeEng("📂 Đã mở Word List trong Notepad!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reload rules từ file đã chỉnh sửa
        /// </summary>
        private void ReloadWordListRules()
        {
            try
            {
                if (_wordListFilePath != null && File.Exists(_wordListFilePath))
                {
                    _pendingKaraokeEngRules = File.ReadAllText(_wordListFilePath);
                    ProcessKaraokeEngInput();
                    ShowToastKaraokeEng("🔄 Đã reload rules từ file!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi reload: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Karaoke English - Copy Button

        private void BtnCopyKaraokeEng_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtKaraokeEngEditable.Text)) return;
            try
            {
                Clipboard.SetText(TxtKaraokeEngEditable.Text);
                ShowToastKaraokeEng("📋 Copied Karaoke!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
