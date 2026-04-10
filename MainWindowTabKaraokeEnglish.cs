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

                _isKaraokeEngUpdating = true;
                TxtKaraokeEngSplitRules.Text = string.Join(Environment.NewLine, allRules);
                _isKaraokeEngUpdating = false;
                _pendingKaraokeEngRules = TxtKaraokeEngSplitRules.Text;
                
                // Nếu đang có search text, tìm lại
                ReapplySearchAfterLoad();
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

        #region Karaoke English - Word Split Rules Search

        /// <summary>
        /// Search text changed - KHÔNG auto-search, chỉ lưu text
        /// </summary>
        private void TxtKaraokeEngRulesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _rulesSearchText = TxtKaraokeEngRulesSearch.Text;
            // Không auto-search, đợi Enter hoặc nút Prev/Next
        }

        /// <summary>
        /// Search key down - Enter để tìm kiếm
        /// </summary>
        private void TxtKaraokeEngRulesSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PerformRuleSearch();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                TxtKaraokeEngRulesSearch.Text = "";
                _rulesSearchText = "";
                _rulesSearchPositions.Clear();
                TxtKaraokeEngSplitRules.Focus();
            }
        }

        /// <summary>
        /// Thực hiện tìm kiếm
        /// </summary>
        private void PerformRuleSearch()
        {
            if (string.IsNullOrWhiteSpace(_rulesSearchText))
            {
                _rulesSearchPositions.Clear();
                return;
            }

            _rulesSearchIndex = -1;
            _rulesSearchPositions.Clear();

            // Tìm tất cả vị trí
            FindAllRuleSearchMatches();

            // Hiển thị kết quả
            if (_rulesSearchPositions.Count > 0)
            {
                _rulesSearchIndex = 0;
                SelectRuleSearchMatch(0);
                ShowToastKaraokeEng($"🔍 {_rulesSearchPositions.Count} kết quả cho '{_rulesSearchText}'");
            }
            else
            {
                ShowToastKaraokeEng($"❌ Không tìm thấy '{_rulesSearchText}'");
            }
        }

        /// <summary>
        /// Tìm previous match
        /// </summary>
        private void BtnKaraokeEngRulesSearchPrev_Click(object sender, RoutedEventArgs e)
        {
            // Nếu chưa search, thực hiện search trước
            if (_rulesSearchPositions.Count == 0)
            {
                PerformRuleSearch();
                return;
            }

            _rulesSearchIndex--;
            if (_rulesSearchIndex < 0)
                _rulesSearchIndex = _rulesSearchPositions.Count - 1;

            SelectRuleSearchMatch(_rulesSearchIndex);
        }

        /// <summary>
        /// Tìm next match
        /// </summary>
        private void BtnKaraokeEngRulesSearchNext_Click(object sender, RoutedEventArgs e)
        {
            // Nếu chưa search, thực hiện search trước
            if (_rulesSearchPositions.Count == 0)
            {
                PerformRuleSearch();
                return;
            }

            _rulesSearchIndex++;
            if (_rulesSearchIndex >= _rulesSearchPositions.Count)
                _rulesSearchIndex = 0;

            SelectRuleSearchMatch(_rulesSearchIndex);
        }

        /// <summary>
        /// Tìm tất cả vị trí xuất hiện của search text - Luôn dùng content mới nhất từ TextBox
        /// </summary>
        private void FindAllRuleSearchMatches()
        {
            if (string.IsNullOrWhiteSpace(_rulesSearchText)) return;
            
            // Đảm bảo TextBox có dữ liệu
            if (TxtKaraokeEngSplitRules == null) return;
            
            // Force lấy text mới nhất từ TextBox
            var content = TxtKaraokeEngSplitRules.Text;
            if (string.IsNullOrWhiteSpace(content)) return;
            
            var lowerContent = content.ToLower();
            var lowerSearch = _rulesSearchText.ToLower();

            _rulesSearchPositions.Clear();
            int startIndex = 0;

            while ((startIndex = lowerContent.IndexOf(lowerSearch, startIndex)) != -1)
            {
                _rulesSearchPositions.Add(startIndex);
                startIndex += _rulesSearchText.Length;
            }
        }

        /// <summary>
        /// Select và scroll đến match - Highlight bằng cách focus tạm thời
        /// </summary>
        private void SelectRuleSearchMatch(int index)
        {
            if (index < 0 || index >= _rulesSearchPositions.Count) return;

            int pos = _rulesSearchPositions[index];
            
            // Focus để highlight, scroll đến dòng
            TxtKaraokeEngSplitRules.Focus();
            TxtKaraokeEngSplitRules.Select(pos, _rulesSearchText.Length);
            TxtKaraokeEngSplitRules.ScrollToLine(GetLineFromPosition(TxtKaraokeEngSplitRules.Text, pos));
            
            // Focus lại search box sau 100ms để người dùng tiếp tục search
            System.Windows.Threading.DispatcherTimer restoreFocusTimer = new System.Windows.Threading.DispatcherTimer();
            restoreFocusTimer.Interval = TimeSpan.FromMilliseconds(100);
            restoreFocusTimer.Tick += (s, e) =>
            {
                restoreFocusTimer.Stop();
                TxtKaraokeEngRulesSearch.Focus();
                // Đặt cursor ở cuối text trong search box
                TxtKaraokeEngRulesSearch.CaretIndex = TxtKaraokeEngRulesSearch.Text.Length;
            };
            restoreFocusTimer.Start();
        }

        /// <summary>
        /// Tính số dòng từ vị trí character
        /// </summary>
        private int GetLineFromPosition(string text, int position)
        {
            int line = 0;
            for (int i = 0; i < position && i < text.Length; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line;
        }

        /// <summary>
        /// Tìm lại sau khi load/reset rules - Đơn giản, không dùng Dispatcher
        /// </summary>
        private void ReapplySearchAfterLoad()
        {
            if (string.IsNullOrWhiteSpace(_rulesSearchText)) return;

            // Reset và tìm lại
            PerformRuleSearch();
        }

        #endregion

        #region Karaoke English - Word Split Rules Buttons

        /// <summary>
        /// Reset về rules mặc định từ folder
        /// </summary>
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

        /// <summary>
        /// Save rules vào file .txt
        /// </summary>
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

        /// <summary>
        /// Load rules - Hiện menu chọn Load Default hoặc Load from File
        /// </summary>
        private void BtnKaraokeEngRulesLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo context menu với 2 lựa chọn
                var contextMenu = new ContextMenu();

                // Option 1: Load Default
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

                // Option 2: Load from File
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
                            _isKaraokeEngUpdating = true;
                            TxtKaraokeEngSplitRules.Text = content;
                            _isKaraokeEngUpdating = false;
                            _pendingKaraokeEngRules = content;
                            ProcessKaraokeEngInput();
                            
                            // Tìm lại nếu có search text
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

                // Hiển thị menu ngay dưới nút Load
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
