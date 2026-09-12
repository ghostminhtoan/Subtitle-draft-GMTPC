using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        #region Karaoke Japanese - Fields

        private bool _isKaraokeJapUpdating = false;
        private bool _isKaraokeJapSyncingSelection = false;
        private string _pendingKaraokeJapRules;
        private string _pendingCustomSongJapRules;
        private string _japWordListFilePath;
        private string _japCustomSongListFilePath;
        private DateTime _lastJapWordListModifiedUtc = DateTime.MinValue;
        private DateTime _lastJapCustomSongListModifiedUtc = DateTime.MinValue;
        private System.Windows.Threading.DispatcherTimer _japWatcherDebounceTimer;
        private FileSystemWatcher _japWordListWatcher;
        private FileSystemWatcher _japCustomSongListWatcher;
        private KaraokeVietnameseService.KaraokeMappingResult _currentKaraokeJapMappingResult;

        #endregion

        #region Karaoke Japanese - Initialize Word Split Rules

        private static string SafeReadAllTextWithRetry(string filePath, int maxRetries = 6, int delayMs = 150)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (!File.Exists(filePath)) return null;
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1) return null;
                    System.Threading.Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    if (i == maxRetries - 1) return null;
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
            return null;
        }

        private void LoadKaraokeJapSplitRules()
        {
            try
            {
                _pendingKaraokeJapRules = JapaneseWordListRules.DefaultRules;
                _pendingCustomSongJapRules = "";

                var appDir = AppRuntimePaths.BaseDirectory;
                var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                if (!Directory.Exists(japRulesDir))
                {
                    Directory.CreateDirectory(japRulesDir);
                }

                _japWordListFilePath = Path.Combine(japRulesDir, "japanese word list rules.txt");
                _japCustomSongListFilePath = Path.Combine(japRulesDir, "japanese custom song list rules.txt");

                if (File.Exists(_japWordListFilePath))
                {
                    _pendingKaraokeJapRules = SafeReadAllTextWithRetry(_japWordListFilePath) ?? JapaneseWordListRules.DefaultRules;
                    _lastJapWordListModifiedUtc = File.GetLastWriteTimeUtc(_japWordListFilePath);
                }
                else
                {
                    File.WriteAllText(_japWordListFilePath, JapaneseWordListRules.DefaultRules);
                    _lastJapWordListModifiedUtc = File.GetLastWriteTimeUtc(_japWordListFilePath);
                }

                if (File.Exists(_japCustomSongListFilePath))
                {
                    _pendingCustomSongJapRules = SafeReadAllTextWithRetry(_japCustomSongListFilePath) ?? "";
                    _lastJapCustomSongListModifiedUtc = File.GetLastWriteTimeUtc(_japCustomSongListFilePath);
                }
                else
                {
                    File.WriteAllText(_japCustomSongListFilePath, "// Nhập các quy tắc tách từ Romaji riêng cho bài hát tại đây\r\n// Định dạng: (word:part1/part2) hoặc word:part1/part2\r\n// Các quy tắc này sẽ ghi đè quy tắc trong Japanese Word List\r\n");
                    _lastJapCustomSongListModifiedUtc = File.GetLastWriteTimeUtc(_japCustomSongListFilePath);
                }

                // Setup file watcher cho cả 2 file rules
                SetupJapWordListWatcher();
                SetupJapCustomSongListWatcher();
            }
            catch (Exception)
            {
                _pendingKaraokeJapRules = JapaneseWordListRules.DefaultRules;
                _pendingCustomSongJapRules = "";
            }
        }

        /// <summary>
        /// Kiểm tra và reload rules tiếng Nhật nếu file trên đĩa có thay đổi
        /// </summary>
        public bool CheckAndReloadKaraokeJapRulesIfModified(bool force = false, bool showToast = false)
        {
            bool hasChanged = false;
            try
            {
                if (!string.IsNullOrEmpty(_japWordListFilePath) && File.Exists(_japWordListFilePath))
                {
                    var writeTime = File.GetLastWriteTimeUtc(_japWordListFilePath);
                    if (force || writeTime != _lastJapWordListModifiedUtc)
                    {
                        var content = SafeReadAllTextWithRetry(_japWordListFilePath);
                        if (content != null)
                        {
                            _pendingKaraokeJapRules = content;
                            _lastJapWordListModifiedUtc = writeTime;
                            hasChanged = true;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_japCustomSongListFilePath) && File.Exists(_japCustomSongListFilePath))
                {
                    var writeTime = File.GetLastWriteTimeUtc(_japCustomSongListFilePath);
                    if (force || writeTime != _lastJapCustomSongListModifiedUtc)
                    {
                        var content = SafeReadAllTextWithRetry(_japCustomSongListFilePath);
                        if (content != null)
                        {
                            _pendingCustomSongJapRules = content;
                            _lastJapCustomSongListModifiedUtc = writeTime;
                            hasChanged = true;
                        }
                    }
                }

                if (hasChanged)
                {
                    ProcessKaraokeJapInput();
                    if (showToast)
                    {
                        ShowToastKaraokeJap("🔄 Auto-reloaded Japanese Rules!");
                    }
                }
            }
            catch
            {
            }

            return hasChanged;
        }

        private void TriggerJapDebouncedReload()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_japWatcherDebounceTimer == null)
                {
                    _japWatcherDebounceTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(250)
                    };
                    _japWatcherDebounceTimer.Tick += (s, e) =>
                    {
                        _japWatcherDebounceTimer.Stop();
                        CheckAndReloadKaraokeJapRulesIfModified(force: true, showToast: true);
                    };
                }
                else
                {
                    _japWatcherDebounceTimer.Stop();
                }

                _japWatcherDebounceTimer.Start();
            }));
        }

        /// <summary>
        /// Theo dõi file japanese word list rules để tự động reload khi có thay đổi
        /// </summary>
        private void SetupJapWordListWatcher()
        {
            try
            {
                var dir = Path.GetDirectoryName(_japWordListFilePath);
                var file = Path.GetFileName(_japWordListFilePath);

                if (Directory.Exists(dir))
                {
                    _japWordListWatcher?.Dispose();
                    _japWordListWatcher = new FileSystemWatcher(dir, file)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true
                    };
                    _japWordListWatcher.Changed += (s, e) => TriggerJapDebouncedReload();
                    _japWordListWatcher.Created += (s, e) => TriggerJapDebouncedReload();
                    _japWordListWatcher.Renamed += (s, e) => TriggerJapDebouncedReload();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Theo dõi file japanese custom song list rules để tự động reload khi có thay đổi
        /// </summary>
        private void SetupJapCustomSongListWatcher()
        {
            try
            {
                var dir = Path.GetDirectoryName(_japCustomSongListFilePath);
                var file = Path.GetFileName(_japCustomSongListFilePath);

                if (Directory.Exists(dir))
                {
                    _japCustomSongListWatcher?.Dispose();
                    _japCustomSongListWatcher = new FileSystemWatcher(dir, file)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true
                    };
                    _japCustomSongListWatcher.Changed += (s, e) => TriggerJapDebouncedReload();
                    _japCustomSongListWatcher.Created += (s, e) => TriggerJapDebouncedReload();
                    _japCustomSongListWatcher.Renamed += (s, e) => TriggerJapDebouncedReload();
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Karaoke Japanese - Input Processing

        private void TxtKaraokeJapInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isKaraokeJapUpdating) return;
            ProcessKaraokeJapInput();
        }

        private void ProcessKaraokeJapInput()
        {
            if (_isKaraokeJapUpdating) return;
            try
            {
                _isKaraokeJapUpdating = true;

                // Luôn kiểm tra xem file rules trên ổ đĩa có thay đổi không trước khi chạy
                CheckAndReloadKaraokeJapRulesIfModified(force: false, showToast: false);
                var content = SubtitleParser.SanitizeContent(TxtKaraokeJapInput.Text);
                if (string.IsNullOrWhiteSpace(content))
                {
                    TxtKaraokeJapCount.Text = "";
                    TxtKaraokeJapOutput.Text = "";
                    TxtKaraokeJapEditable.Text = "";
                    _currentKaraokeJapMappingResult = null;
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeJapCount.Text = string.Format("({0} lines)", lines.Length);

                var splitRules = _pendingKaraokeJapRules ?? "";
                var customRules = _pendingCustomSongJapRules ?? "";
                var mappingResult = KaraokeVietnameseService.ProcessLyricsWithMapping(content, splitRules, customRules, isJapaneseRomajiMode: true);
                _currentKaraokeJapMappingResult = mappingResult;

                TxtKaraokeJapOutput.Text = mappingResult.FormattedOutput;
                TxtKaraokeJapEditable.Text = mappingResult.FormattedOutput;
            }
            catch (Exception ex)
            {
                TxtKaraokeJapCount.Text = string.Format("(Error: {0})", ex.Message);
            }
            finally
            {
                _isKaraokeJapUpdating = false;
            }
        }

        #endregion

        #region Karaoke Japanese - Selection & Double Click Synchronization

        private void TxtKaraokeJapInput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeJapSyncingSelection || _isKaraokeJapUpdating) return;
            if (TxtKaraokeJapInput.IsFocused || TxtKaraokeJapInput.SelectionLength > 0)
            {
                SyncSelectionFromInputJap();
            }
        }

        private void TxtKaraokeJapInput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeJapUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromInputJap();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeJapOutput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeJapSyncingSelection || _isKaraokeJapUpdating) return;
            if (TxtKaraokeJapOutput.IsFocused)
            {
                SyncSelectionFromOutputOrEditableJap(TxtKaraokeJapOutput);
            }
        }

        private void TxtKaraokeJapOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeJapUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditableJap(TxtKaraokeJapOutput);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeJapEditable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeJapSyncingSelection || _isKaraokeJapUpdating) return;
            if (TxtKaraokeJapEditable.IsFocused)
            {
                SyncSelectionFromOutputOrEditableJap(TxtKaraokeJapEditable);
            }
        }

        private void TxtKaraokeJapEditable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeJapUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditableJap(TxtKaraokeJapEditable);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Đồng bộ từ Panel 1 (Input) sang Panel 2 và Panel 3 dựa trên Mapping chính xác 1-1
        /// </summary>
        private void SyncSelectionFromInputJap()
        {
            if (_isKaraokeJapSyncingSelection) return;
            try
            {
                _isKaraokeJapSyncingSelection = true;
                var text = TxtKaraokeJapInput.Text;
                if (string.IsNullOrEmpty(text))
                {
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapOutput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapEditable);
                    return;
                }

                // Xóa highlight Adorner cũ ở chính Input nếu có
                Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapInput);

                int selStart = TxtKaraokeJapInput.SelectionStart;
                int selLen = TxtKaraokeJapInput.SelectionLength;

                if (_currentKaraokeJapMappingResult != null && _currentKaraokeJapMappingResult.Mappings.Count > 0)
                {
                    var targetMappings = _currentKaraokeJapMappingResult.Mappings
                        .Where(m => (selStart >= m.InputStart && selStart < m.InputStart + m.InputLength) ||
                                    (selStart == m.InputStart + m.InputLength && selLen == 0) ||
                                    (m.InputStart >= selStart && m.InputStart < selStart + Math.Max(1, selLen)))
                        .ToList();

                    if (targetMappings.Count == 0 && selLen == 0)
                    {
                        targetMappings = _currentKaraokeJapMappingResult.Mappings
                            .Where(m => Math.Abs(m.InputStart - selStart) <= 2 || Math.Abs(m.InputStart + m.InputLength - selStart) <= 2)
                            .Take(1)
                            .ToList();
                    }

                    if (targetMappings.Count > 0)
                    {
                        var firstMap = targetMappings[0];
                        var lastMap = targetMappings[targetMappings.Count - 1];

                        HighlightLineRangeInOutputJap(TxtKaraokeJapOutput, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        HighlightLineRangeInOutputJap(TxtKaraokeJapEditable, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        return;
                    }
                }

                // Fallback theo vị trí dòng tương đối
                int lineIndex = TxtKaraokeJapInput.GetLineIndexFromCharacterIndex(selStart);
                if (lineIndex >= 0)
                {
                    int totalInputLines = TxtKaraokeJapInput.LineCount;
                    if (totalInputLines > 0)
                    {
                        int totalOutLines = TxtKaraokeJapOutput.LineCount;
                        int estimatedOutLine = (int)(((double)lineIndex / totalInputLines) * totalOutLines);
                        HighlightLineInBoxByIndexJap(TxtKaraokeJapOutput, estimatedOutLine);
                        HighlightLineInBoxByIndexJap(TxtKaraokeJapEditable, estimatedOutLine);
                    }
                }
            }
            finally
            {
                _isKaraokeJapSyncingSelection = false;
            }
        }

        /// <summary>
        /// Đồng bộ từ Panel 2 (Output) hoặc Panel 3 (Editable) sang Panel 1 và Panel còn lại
        /// </summary>
        private void SyncSelectionFromOutputOrEditableJap(TextBox sourceBox)
        {
            if (_isKaraokeJapSyncingSelection) return;
            try
            {
                _isKaraokeJapSyncingSelection = true;
                var text = sourceBox.Text;
                if (string.IsNullOrEmpty(text))
                {
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapInput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapOutput);
                    Helpers.TextHighlightAdorner.ClearHighlight(TxtKaraokeJapEditable);
                    return;
                }

                Helpers.TextHighlightAdorner.ClearHighlight(sourceBox);

                int caret = sourceBox.SelectionStart;
                int lineIndex = sourceBox.GetLineIndexFromCharacterIndex(caret);
                if (lineIndex < 0) return;

                // Lấy nội dung dòng và làm sạch (bỏ ký tự ∞ ở đầu và ♫ ở cuối)
                string lineText = "";
                int lineStart = sourceBox.GetCharacterIndexFromLineIndex(lineIndex);
                int lineLen = sourceBox.GetLineLength(lineIndex);
                if (lineStart >= 0 && lineLen > 0)
                {
                    lineText = sourceBox.Text.Substring(lineStart, lineLen).TrimEnd('\r', '\n').Trim();
                }
                string cleanWord = lineText.TrimStart('∞').TrimEnd('♫').Trim();

                if (_currentKaraokeJapMappingResult != null && _currentKaraokeJapMappingResult.Mappings.Count > 0)
                {
                    var map = _currentKaraokeJapMappingResult.Mappings.FirstOrDefault(m => m.OutputLineIndex == lineIndex);
                    
                    if (map == null && !string.IsNullOrEmpty(cleanWord))
                    {
                        map = _currentKaraokeJapMappingResult.Mappings.FirstOrDefault(m => 
                            m.SyllableText.Equals(cleanWord, StringComparison.OrdinalIgnoreCase) ||
                            m.InputWord.Equals(cleanWord, StringComparison.OrdinalIgnoreCase));
                    }

                    if (map != null)
                    {
                        TxtKaraokeJapInput.Select(map.InputStart, map.InputLength);
                        Helpers.TextHighlightAdorner.SetHighlight(TxtKaraokeJapInput, map.InputStart, map.InputLength);

                        int inputLineIdx = TxtKaraokeJapInput.GetLineIndexFromCharacterIndex(map.InputStart);
                        if (inputLineIdx >= 0)
                        {
                            TxtKaraokeJapInput.ScrollToLine(Math.Max(0, inputLineIdx - 2));
                        }

                        var otherBox = (sourceBox == TxtKaraokeJapOutput) ? TxtKaraokeJapEditable : TxtKaraokeJapOutput;
                        HighlightLineRangeInOutputJap(otherBox, lineIndex, lineIndex);
                        return;
                    }
                }

                var otherTargetBox = (sourceBox == TxtKaraokeJapOutput) ? TxtKaraokeJapEditable : TxtKaraokeJapOutput;
                HighlightLineInBoxByIndexJap(otherTargetBox, lineIndex);

                int totalSourceLines = sourceBox.LineCount;
                if (totalSourceLines > 0)
                {
                    int totalInputLines = TxtKaraokeJapInput.LineCount;
                    int estimatedInputLine = (int)(((double)lineIndex / totalSourceLines) * totalInputLines);
                    ScrollToLineInBox(TxtKaraokeJapInput, estimatedInputLine);
                }
            }
            finally
            {
                _isKaraokeJapSyncingSelection = false;
            }
        }

        private void HighlightLineRangeInOutputJap(TextBox targetBox, int startLineIndex, int endLineIndex)
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

        private void HighlightLineInBoxByIndexJap(TextBox targetBox, int lineIndex)
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

        #endregion

        #region Karaoke Japanese - Toast Notifications

        private void ShowToastKaraokeJap(string message)
        {
            try
            {
                if (ToastBorderKaraokeJap != null && ToastTextKaraokeJap != null)
                {
                    ToastTextKaraokeJap.Text = message;
                    ToastBorderKaraokeJap.Visibility = Visibility.Visible;

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, e) =>
                    {
                        ToastBorderKaraokeJap.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Karaoke Japanese - Buttons (Load Default, Edit, Custom Song List, Add To Word List, Save, Load)

        /// <summary>
        /// Reset / Load Default rules cho Karaoke Japanese
        /// </summary>
        private void BtnLoadDefaultWordListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _pendingKaraokeJapRules = JapaneseWordListRules.DefaultRules;
                
                var appDir = AppRuntimePaths.BaseDirectory;
                var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                if (!Directory.Exists(japRulesDir)) Directory.CreateDirectory(japRulesDir);
                _japWordListFilePath = Path.Combine(japRulesDir, "japanese word list rules.txt");

                File.WriteAllText(_japWordListFilePath, JapaneseWordListRules.DefaultRules);

                ProcessKaraokeJapInput();
                ShowToastKaraokeJap("🔄 Đã reset Japanese Word List về mặc định!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Edit Word List: Mở file japanese word list rules.txt bằng Notepad
        /// </summary>
        private void BtnEditWordListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                if (!Directory.Exists(japRulesDir)) Directory.CreateDirectory(japRulesDir);
                _japWordListFilePath = Path.Combine(japRulesDir, "japanese word list rules.txt");

                if (!File.Exists(_japWordListFilePath))
                {
                    File.WriteAllText(_japWordListFilePath, JapaneseWordListRules.DefaultRules);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _japWordListFilePath,
                    UseShellExecute = true
                });

                ShowToastKaraokeJap("✏️ Đang mở Japanese Word List để chỉnh sửa!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Custom Song List: Mở file japanese custom song list rules.txt bằng Notepad
        /// </summary>
        private void BtnCustomSongListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                if (!Directory.Exists(japRulesDir)) Directory.CreateDirectory(japRulesDir);
                _japCustomSongListFilePath = Path.Combine(japRulesDir, "japanese custom song list rules.txt");

                if (!File.Exists(_japCustomSongListFilePath))
                {
                    File.WriteAllText(_japCustomSongListFilePath, "// Nhập các quy tắc tách từ Romaji riêng cho bài hát tại đây\r\n// Định dạng: (word:part1/part2) hoặc word:part1/part2\r\n// Các quy tắc này sẽ ghi đè quy tắc trong Japanese Word List\r\n");
                }

                // Đọc ngay nội dung hiện tại nếu có
                var currentContent = SafeReadAllTextWithRetry(_japCustomSongListFilePath);
                if (currentContent != null)
                {
                    _pendingCustomSongJapRules = currentContent;
                    _lastJapCustomSongListModifiedUtc = File.GetLastWriteTimeUtc(_japCustomSongListFilePath);
                    ProcessKaraokeJapInput();
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _japCustomSongListFilePath,
                    UseShellExecute = true
                });

                ShowToastKaraokeJap("🎵 Đang mở Japanese Custom Song List (Lưu file để tự động áp dụng)!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add To Word List: Mở cửa sổ popup nhập quy tắc thêm vào Japanese Word List
        /// </summary>
        private void BtnAddToWordListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                if (!Directory.Exists(japRulesDir)) Directory.CreateDirectory(japRulesDir);
                _japWordListFilePath = Path.Combine(japRulesDir, "japanese word list rules.txt");

                var dialog = new AddWordListRulesWindow(_japWordListFilePath, (updatedRules) =>
                {
                    _pendingKaraokeJapRules = updatedRules;
                    ProcessKaraokeJapInput();
                    ShowToastKaraokeJap("✨ Đã cập nhật Japanese Word List!");
                });
                dialog.Owner = this;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Save: Lưu rules hiện tại vào file do user chọn
        /// </summary>
        private void BtnSaveWordListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = "japanese word list rules.txt",
                    Title = "Lưu Japanese Word List Rules"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var rulesToSave = _pendingKaraokeJapRules ?? JapaneseWordListRules.DefaultRules;
                    File.WriteAllText(saveDialog.FileName, rulesToSave);
                    ShowToastKaraokeJap("💾 Đã lưu Japanese rules!");
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
        private void BtnLoadWordListJap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    Title = "Chọn file Japanese Word List Rules"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var content = File.ReadAllText(openDialog.FileName);
                    _pendingKaraokeJapRules = content;
                    
                    var appDir = AppRuntimePaths.BaseDirectory;
                    var japRulesDir = Path.Combine(appDir, "japanese word rules karaoke");
                    if (!Directory.Exists(japRulesDir)) Directory.CreateDirectory(japRulesDir);
                    _japWordListFilePath = Path.Combine(japRulesDir, "japanese word list rules.txt");
                    File.WriteAllText(_japWordListFilePath, content);
                    
                    ProcessKaraokeJapInput();
                    ShowToastKaraokeJap("📂 Đã load Japanese rules từ file!");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi load: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Karaoke Japanese - Copy Button

        private void BtnCopyKaraokeJap_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtKaraokeJapEditable.Text)) return;
            try
            {
                Clipboard.SetText(TxtKaraokeJapEditable.Text);
                ShowToastKaraokeJap("📋 Copied Japanese Karaoke!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
