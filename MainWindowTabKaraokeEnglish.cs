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
        private FileSystemWatcher _wordListWatcher;

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                _pendingKaraokeEngRules = WordListRules.DefaultRules;
                
                // Setup file watcher cho word list rules
                SetupWordListWatcher();
            }
            catch (Exception)
            {
                _pendingKaraokeEngRules = "";
            }
        }

        /// <summary>
        /// Theo dõi file word list rules để tự động reload khi có thay đổi
        /// </summary>
        private void SetupWordListWatcher()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");

                var dir = Path.GetDirectoryName(_wordListFilePath);
                var file = Path.GetFileName(_wordListFilePath);

                if (Directory.Exists(dir))
                {
                    _wordListWatcher = new FileSystemWatcher(dir, file);
                    _wordListWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                    _wordListWatcher.Changed += WordListFile_Changed;
                    _wordListWatcher.EnableRaisingEvents = true;
                }
            }
            catch
            {
                // Bỏ qua nếu không setup được watcher
            }
        }

        private void WordListFile_Changed(object sender, FileSystemEventArgs e)
        {
            // Reload rules từ file khi có thay đổi
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (File.Exists(_wordListFilePath))
                    {
                        _pendingKaraokeEngRules = File.ReadAllText(_wordListFilePath);
                        ProcessKaraokeEngInput();
                        ShowToastKaraokeEng("🔄 Auto-reloaded rules từ file!");
                    }
                }
                catch { }
            }));
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

        #region Karaoke English - Word List Buttons

        /// <summary>
        /// Load Default: Reset về rules mặc định từ embedded string
        /// </summary>
        private void BtnLoadDefaultWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pendingKaraokeEngRules = WordListRules.DefaultRules;
                
                // Ghi đè lên file word list rules.txt
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");
                File.WriteAllText(_wordListFilePath, WordListRules.DefaultRules);
                
                ProcessKaraokeEngInput();
                ShowToastKaraokeEng("🔄 Đã reset về rules mặc định!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edit: Mở file word list rules.txt để chỉnh sửa
        /// </summary>
        private void BtnEditWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");

                if (!File.Exists(_wordListFilePath))
                {
                    // Nếu chưa có file, tạo mới từ embedded rules
                    File.WriteAllText(_wordListFilePath, WordListRules.DefaultRules);
                    ShowToastKaraokeEng("📄 Đã tạo file Word List mới!");
                }

                // Mở file bằng Notepad
                Process.Start("notepad.exe", $"\"{_wordListFilePath}\"");
                ShowToastKaraokeEng("✏️ Đang mở Word List để chỉnh sửa!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save: Lưu rules hiện tại vào file do user chọn
        /// </summary>
        private void BtnSaveWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = "word list rules.txt",
                    Title = "Lưu Word List Rules"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var rulesToSave = _pendingKaraokeEngRules ?? WordListRules.DefaultRules;
                    File.WriteAllText(saveDialog.FileName, rulesToSave);
                    ShowToastKaraokeEng("💾 Đã lưu rules!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi lưu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load: Load rules từ file do user chọn
        /// </summary>
        private void BtnLoadWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    Title = "Chọn file Word List Rules"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var content = File.ReadAllText(openDialog.FileName);
                    _pendingKaraokeEngRules = content;
                    
                    // Lưu vào file word list rules.txt để watcher theo dõi
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    _wordListFilePath = Path.Combine(appDir, "word list rules.txt");
                    File.WriteAllText(_wordListFilePath, content);
                    
                    ProcessKaraokeEngInput();
                    ShowToastKaraokeEng("📂 Đã load rules từ file!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi load: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
