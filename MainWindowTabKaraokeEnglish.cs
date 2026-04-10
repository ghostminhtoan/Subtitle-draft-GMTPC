using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        // Search fields cho Word Split Rules
        private int _rulesSearchIndex = -1;
        private string _rulesSearchText = "";
        private List<int> _rulesSearchPositions = new List<int>();

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        /// <summary>
        /// Load và gộp tất cả file A-Z.txt từ thư mục english word rules karaoke
        /// </summary>
        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                _karaokeEngRulesDirectory = FindWordRulesDirectory();

                if (_karaokeEngRulesDirectory == null || !Directory.Exists(_karaokeEngRulesDirectory))
                {
                    TxtKaraokeEngSplitRules.Text = "// Không tìm thấy thư mục 'english word rules karaoke'\n// Vui lòng đảm bảo folder này tồn tại trong thư mục ứng dụng";
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
                    TxtKaraokeEngSplitRules.Text = "// Không tìm thấy file A-Z.txt trong thư mục word rules";
                    return;
                }

                var allRules = new List<string>();
                foreach (var file in ruleFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        // Format mới: (word:part1/part2), (word2:part1/part2)
                        // Xóa (), và dấu phẩy, mỗi rule xuống dòng
                        var rules = ExtractRulesFromParens(content);
                        allRules.AddRange(rules);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi đọc file {file}: {ex.Message}");
                    }
                }

                _isKaraokeEngUpdating = true;
                TxtKaraokeEngSplitRules.Text = string.Join(Environment.NewLine, allRules);
                _isKaraokeEngUpdating = false;
                _pendingKaraokeEngRules = TxtKaraokeEngSplitRules.Text;

                TxtKaraokeEngSplitRules.PreviewKeyDown += TxtKaraokeEngSplitRules_PreviewKeyDown;

                ReapplySearchAfterLoad();
            }
            catch (Exception ex)
            {
                TxtKaraokeEngSplitRules.Text = $"// Lỗi load rules: {ex.Message}";
            }
        }

        /// <summary>
        /// Trích xuất rules từ format: (word:part1/part2), (word2:part1/part2)
        /// </summary>
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

        /// <summary>
        /// Handler cho Ctrl+F và F3 khi focus trong rules TextBox
        /// </summary>
        private void TxtKaraokeEngSplitRules_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                TxtKaraokeEngRulesSearch.Focus();
                TxtKaraokeEngRulesSearch.SelectAll();
                return;
            }

            if (e.Key == Key.F3)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    SearchPrev();
                else
                    SearchNext();
                return;
            }
        }

        /// <summary>
        /// Tìm thư mục chứa word rules
        /// </summary>
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

        private void TxtKaraokeEngSplitRules_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;

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

        #region Karaoke English - Word Split Rules Search

        private void TxtKaraokeEngRulesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _rulesSearchText = TxtKaraokeEngRulesSearch.Text;
        }

        private void TxtKaraokeEngRulesSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    SearchPrev();
                else
                    PerformRuleSearchAndHighlightAll();
            }
            else if (e.Key == Key.F3)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    SearchPrev();
                else
                    SearchNext();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                TxtKaraokeEngRulesSearch.Text = "";
                _rulesSearchText = "";
                _rulesSearchPositions.Clear();
                _rulesSearchIndex = -1;
                TxtKaraokeEngSplitRules.SelectionStart = 0;
                TxtKaraokeEngSplitRules.SelectionLength = 0;
            }
        }

        private void PerformRuleSearchAndHighlightAll()
        {
            if (string.IsNullOrWhiteSpace(_rulesSearchText) || TxtKaraokeEngSplitRules == null)
            {
                _rulesSearchPositions.Clear();
                _rulesSearchIndex = -1;
                return;
            }

            var content = TxtKaraokeEngSplitRules.Text;
            if (string.IsNullOrWhiteSpace(content))
            {
                _rulesSearchPositions.Clear();
                _rulesSearchIndex = -1;
                return;
            }

            _rulesSearchPositions.Clear();
            var lowerContent = content.ToLowerInvariant();
            var lowerSearch = _rulesSearchText.ToLowerInvariant();
            int startIndex = 0;

            while ((startIndex = lowerContent.IndexOf(lowerSearch, startIndex)) != -1)
            {
                _rulesSearchPositions.Add(startIndex);
                startIndex += _rulesSearchText.Length;
            }

            if (_rulesSearchPositions.Count > 0)
            {
                _rulesSearchIndex = 0;
                SelectRuleSearchMatch(0);
                ShowToastKaraokeEng($"🔍 {_rulesSearchPositions.Count} kết quả");
            }
            else
            {
                _rulesSearchIndex = -1;
                ShowToastKaraokeEng($"❌ Không tìm thấy");
            }
        }

        private void SearchPrev()
        {
            if (_rulesSearchPositions.Count == 0) { PerformRuleSearchAndHighlightAll(); return; }
            _rulesSearchIndex--;
            if (_rulesSearchIndex < 0) _rulesSearchIndex = _rulesSearchPositions.Count - 1;
            SelectRuleSearchMatch(_rulesSearchIndex);
        }

        private void SearchNext()
        {
            if (_rulesSearchPositions.Count == 0) { PerformRuleSearchAndHighlightAll(); return; }
            _rulesSearchIndex++;
            if (_rulesSearchIndex >= _rulesSearchPositions.Count) _rulesSearchIndex = 0;
            SelectRuleSearchMatch(_rulesSearchIndex);
        }

        private void BtnKaraokeEngRulesSearchPrev_Click(object sender, RoutedEventArgs e) => SearchPrev();
        private void BtnKaraokeEngRulesSearchNext_Click(object sender, RoutedEventArgs e) => SearchNext();

        private void SelectRuleSearchMatch(int index)
        {
            if (index < 0 || index >= _rulesSearchPositions.Count) return;
            int pos = _rulesSearchPositions[index];
            TxtKaraokeEngSplitRules.SelectionStart = pos;
            TxtKaraokeEngSplitRules.SelectionLength = _rulesSearchText.Length;
            TxtKaraokeEngSplitRules.ScrollToLine(GetLineFromPosition(TxtKaraokeEngSplitRules.Text, pos));
        }

        private int GetLineFromPosition(string text, int position)
        {
            int line = 0;
            for (int i = 0; i < position && i < text.Length; i++)
                if (text[i] == '\n') line++;
            return line;
        }

        private void ReapplySearchAfterLoad()
        {
            if (string.IsNullOrWhiteSpace(_rulesSearchText)) return;
            PerformRuleSearchAndHighlightAll();
        }

        #endregion

        #region Karaoke English - Word Split Rules Buttons

        private void BtnKaraokeEngRulesReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadKaraokeEngSplitRules();
                ShowToastKaraokeEng("🔄 Reset về rules mặc định!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi reset: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnKaraokeEngRulesSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = "word-split-rules.txt",
                    Title = "Lưu Word Split Rules"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, TxtKaraokeEngSplitRules.Text, System.Text.Encoding.UTF8);
                    ShowToastKaraokeEng("💾 Đã lưu rules!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi lưu: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnKaraokeEngRulesLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contextMenu = new ContextMenu();

                var loadDefaultItem = new MenuItem { Header = "🔄 Load Default (từ thư mục app)" };
                loadDefaultItem.Click += (s, args) =>
                {
                    try
                    {
                        LoadKaraokeEngSplitRules();
                        ShowToastKaraokeEng("🔄 Đã load rules mặc định!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Lỗi load default: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(loadDefaultItem);

                var loadFileItem = new MenuItem { Header = "📂 Load from File (.txt)" };
                loadFileItem.Click += (s, args) =>
                {
                    try
                    {
                        var openDialog = new Microsoft.Win32.OpenFileDialog
                        {
                            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                            DefaultExt = "txt",
                            Title = "Chọn file Word Split Rules"
                        };

                        if (openDialog.ShowDialog() == true)
                        {
                            var content = File.ReadAllText(openDialog.FileName, System.Text.Encoding.UTF8);
                            var rules = ExtractRulesFromParens(content);
                            _isKaraokeEngUpdating = true;
                            TxtKaraokeEngSplitRules.Text = string.Join(Environment.NewLine, rules);
                            _isKaraokeEngUpdating = false;
                            _pendingKaraokeEngRules = TxtKaraokeEngSplitRules.Text;
                            ProcessKaraokeEngInput();
                            ReapplySearchAfterLoad();
                            ShowToastKaraokeEng("📂 Đã load rules từ file!");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Lỗi load file: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(loadFileItem);

                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.PlacementTarget = BtnKaraokeEngRulesLoad;
                contextMenu.HorizontalOffset = 0;
                contextMenu.VerticalOffset = 0;
                contextMenu.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
