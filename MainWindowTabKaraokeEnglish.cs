using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Threading;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

        #region Karaoke English - Fields

        private bool _isKaraokeEngUpdating = false;
        private DispatcherTimer _karaokeEngRulesDebounceTimer;
        private string _pendingKaraokeEngRules;
        private string _karaokeEngRulesDirectory;

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        /// <summary>
        /// Load và gộp tất cả file A-Z.txt từ thư mục english word rules karaoke
        /// </summary>
        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                // Tìm thư mục chứa word rules - thử nhiều vị trí khác nhau
                _karaokeEngRulesDirectory = FindWordRulesDirectory();
                
                if (_karaokeEngRulesDirectory == null || !Directory.Exists(_karaokeEngRulesDirectory))
                {
                    TxtKaraokeEngSplitRules.Text = "// Không tìm thấy thư mục 'english word rules karaoke'\n// Vui lòng đảm bảo folder này tồn tại trong thư mục ứng dụng";
                    return;
                }

                // Tìm tất cả file A-Z.txt
                var ruleFiles = new List<string>();
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    var fileName = $"{c}.txt";
                    var filePath = Path.Combine(_karaokeEngRulesDirectory, fileName);
                    if (File.Exists(filePath))
                    {
                        ruleFiles.Add(filePath);
                    }
                }

                if (ruleFiles.Count == 0)
                {
                    TxtKaraokeEngSplitRules.Text = "// Không tìm thấy file A-Z.txt trong thư mục word rules";
                    return;
                }

                // Gộp nội dung tất cả file
                var allRules = new List<string>();
                foreach (var file in ruleFiles)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        allRules.AddRange(lines);
                    }
                    catch (Exception ex)
                    {
                        // Bỏ qua file không đọc được
                        System.Diagnostics.Debug.WriteLine($"Lỗi đọc file {file}: {ex.Message}");
                    }
                }

                TxtKaraokeEngSplitRules.Text = string.Join(Environment.NewLine, allRules);
            }
            catch (Exception ex)
            {
                TxtKaraokeEngSplitRules.Text = $"// Lỗi load rules: {ex.Message}";
            }
        }

        /// <summary>
        /// Tìm thư mục chứa word rules
        /// </summary>
        private string FindWordRulesDirectory()
        {
            var folderName = "english word rules karaoke";
            
            // Cách 1: Tìm từ thư mục executable
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidateDir = Path.Combine(appDir, folderName);
            if (Directory.Exists(candidateDir))
            {
                return candidateDir;
            }

            // Cách 2: Đi lên các thư mục cha từ executable (tối đa 5 cấp)
            var currentPath = appDir.TrimEnd('\\', '/');
            for (int i = 0; i < 5; i++)
            {
                var parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath)) break;
                
                candidateDir = Path.Combine(parentPath, folderName);
                if (Directory.Exists(candidateDir))
                {
                    return candidateDir;
                }
                currentPath = parentPath;
            }

            // Cách 3: Tìm từ thư mục làm việc hiện tại
            var workDir = Environment.CurrentDirectory;
            candidateDir = Path.Combine(workDir, folderName);
            if (Directory.Exists(candidateDir))
            {
                return candidateDir;
            }

            // Cách 4: Đi lên các thư mục cha từ working directory
            currentPath = workDir.TrimEnd('\\', '/');
            for (int i = 0; i < 5; i++)
            {
                var parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath)) break;
                
                candidateDir = Path.Combine(parentPath, folderName);
                if (Directory.Exists(candidateDir))
                {
                    return candidateDir;
                }
                currentPath = parentPath;
            }

            return null;
        }

        #endregion

        #region Karaoke English - Event Handlers

        private void TxtKaraokeEngInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;
            ProcessKaraokeEngInput();
        }

        private void TxtKaraokeEngSplitRules_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;

            // Lưu rules pending và debounce để xử lý
            _pendingKaraokeEngRules = TxtKaraokeEngSplitRules.Text;

            if (_karaokeEngRulesDebounceTimer == null)
            {
                _karaokeEngRulesDebounceTimer = new DispatcherTimer();
                _karaokeEngRulesDebounceTimer.Interval = TimeSpan.FromMilliseconds(300);
                _karaokeEngRulesDebounceTimer.Tick += (s, args) =>
                {
                    _karaokeEngRulesDebounceTimer.Stop();
                    ProcessKaraokeEngInput();
                };
            }
            else
            {
                _karaokeEngRulesDebounceTimer.Stop();
            }
            _karaokeEngRulesDebounceTimer.Start();
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

                // Xử lý karaoke English với quy tắc tách từ tùy chỉnh
                var splitRules = _pendingKaraokeEngRules ?? TxtKaraokeEngSplitRules.Text;
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
