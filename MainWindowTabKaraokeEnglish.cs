using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private string _pendingKaraokeEngRules;
        private string _karaokeEngRulesDirectory;

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                _karaokeEngRulesDirectory = FindWordRulesDirectory();

                if (_karaokeEngRulesDirectory == null || !Directory.Exists(_karaokeEngRulesDirectory))
                {
                    _pendingKaraokeEngRules = "";
                    return;
                }

                var ruleFiles = new List<string>();
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    var fileName = $"{c}.txt";
                    var filePath = Path.Combine(_karaokeEngRulesDirectory, fileName);
                    if (File.Exists(filePath))
                        ruleFiles.Add(filePath);
                }

                if (ruleFiles.Count == 0)
                {
                    _pendingKaraokeEngRules = "";
                    return;
                }

                var allRules = new List<string>();
                foreach (var file in ruleFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var rules = ExtractRulesFromParens(content);
                        allRules.AddRange(rules);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi đọc file {file}: {ex.Message}");
                    }
                }

                _pendingKaraokeEngRules = string.Join(Environment.NewLine, allRules);
            }
            catch (Exception ex)
            {
                _pendingKaraokeEngRules = "";
            }
        }

        private List<string> ExtractRulesFromParens(string content)
        {
            var rules = new List<string>();
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"\(([^)]+)\)");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                rules.Add(match.Groups[1].Value);
            }
            return rules.Count > 0 ? rules : new List<string> { content };
        }

        private string FindWordRulesDirectory()
        {
            var folderName = "english word rules karaoke";

            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidateDir = Path.Combine(appDir, folderName);
            if (Directory.Exists(candidateDir)) return candidateDir;

            var currentPath = appDir.TrimEnd('\\', '/');
            for (int i = 0; i < 5; i++)
            {
                var parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath)) break;
                candidateDir = Path.Combine(parentPath, folderName);
                if (Directory.Exists(candidateDir)) return candidateDir;
                currentPath = parentPath;
            }

            var workDir = Environment.CurrentDirectory;
            candidateDir = Path.Combine(workDir, folderName);
            if (Directory.Exists(candidateDir)) return candidateDir;

            currentPath = workDir.TrimEnd('\\', '/');
            for (int i = 0; i < 5; i++)
            {
                var parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath)) break;
                candidateDir = Path.Combine(parentPath, folderName);
                if (Directory.Exists(candidateDir)) return candidateDir;
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
        /// Mở thư mục chứa word split rules bằng Notepad
        /// </summary>
        private void BtnOpenWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_karaokeEngRulesDirectory == null || !Directory.Exists(_karaokeEngRulesDirectory))
                {
                    LoadKaraokeEngSplitRules();
                    if (_karaokeEngRulesDirectory == null || !Directory.Exists(_karaokeEngRulesDirectory))
                    {
                        System.Windows.MessageBox.Show("Không tìm thấy thư mục 'english word rules karaoke'.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Mở thư mục chứa file
                Process.Start("explorer.exe", $"\"{_karaokeEngRulesDirectory}\"");
                ShowToastKaraokeEng("📂 Đã mở thư mục Word List!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
