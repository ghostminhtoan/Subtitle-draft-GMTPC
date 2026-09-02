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
        #region Karaoke English - Fields

        private bool _isKaraokeEngUpdating = false;
        private bool _isKaraokeEngSyncingSelection = false;
        private string _pendingKaraokeEngRules;
        private string _pendingCustomSongRules;
        private string _wordListFilePath;
        private string _customSongListFilePath;
        private FileSystemWatcher _wordListWatcher;
        private FileSystemWatcher _customSongListWatcher;
        private KaraokeVietnameseService.KaraokeMappingResult _currentKaraokeEngMappingResult;

        #endregion

        #region Karaoke English - Initialize Word Split Rules

        private void LoadKaraokeEngSplitRules()
        {
            try
            {
                _pendingKaraokeEngRules = WordListRules.DefaultRules;
                _pendingCustomSongRules = "";

                var appDir = AppRuntimePaths.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");
                _customSongListFilePath = Path.Combine(appDir, "custom song list rules.txt");

                if (File.Exists(_wordListFilePath))
                {
                    _pendingKaraokeEngRules = File.ReadAllText(_wordListFilePath);
                }

                if (File.Exists(_customSongListFilePath))
                {
                    _pendingCustomSongRules = File.ReadAllText(_customSongListFilePath);
                }

                // Setup file watcher cho cả 2 file rules
                SetupWordListWatcher();
                SetupCustomSongListWatcher();
            }
            catch (Exception)
            {
                _pendingKaraokeEngRules = "";
                _pendingCustomSongRules = "";
            }
        }

        /// <summary>
        /// Theo dõi file word list rules để tự động reload khi có thay đổi
        /// </summary>
        private void SetupWordListWatcher()
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
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
                        ShowToastKaraokeEng("🔄 Auto-reloaded Word List từ file!");
                    }
                }
                catch { }
            }));
        }

        /// <summary>
        /// Theo dõi file custom song list rules để tự động reload khi có thay đổi
        /// </summary>
        private void SetupCustomSongListWatcher()
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                _customSongListFilePath = Path.Combine(appDir, "custom song list rules.txt");

                var dir = Path.GetDirectoryName(_customSongListFilePath);
                var file = Path.GetFileName(_customSongListFilePath);

                if (Directory.Exists(dir))
                {
                    _customSongListWatcher = new FileSystemWatcher(dir, file);
                    _customSongListWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                    _customSongListWatcher.Changed += CustomSongListFile_Changed;
                    _customSongListWatcher.EnableRaisingEvents = true;
                }
            }
            catch
            {
                // Bỏ qua nếu không setup được watcher
            }
        }

        private void CustomSongListFile_Changed(object sender, FileSystemEventArgs e)
        {
            // Reload custom song rules từ file khi có thay đổi
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (File.Exists(_customSongListFilePath))
                    {
                        _pendingCustomSongRules = File.ReadAllText(_customSongListFilePath);
                        ProcessKaraokeEngInput();
                        ShowToastKaraokeEng("🔄 Auto-reloaded Custom Song List!");
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
                    _currentKaraokeEngMappingResult = null;
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeEngCount.Text = string.Format("({0} lines)", lines.Length);

                var splitRules = _pendingKaraokeEngRules ?? "";
                var customRules = _pendingCustomSongRules ?? "";
                var mappingResult = KaraokeVietnameseService.ProcessLyricsWithMapping(content, splitRules, customRules);
                _currentKaraokeEngMappingResult = mappingResult;

                TxtKaraokeEngOutput.Text = mappingResult.FormattedOutput;
                TxtKaraokeEngEditable.Text = mappingResult.FormattedOutput;
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

        #region Karaoke English - Selection & Double Click Synchronization

        private void TxtKaraokeEngInput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeEngSyncingSelection || _isKaraokeEngUpdating) return;
            if (TxtKaraokeEngInput.SelectionLength > 0)
            {
                SyncSelectionFromInput();
            }
        }

        private void TxtKaraokeEngInput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromInput();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeEngOutput_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeEngSyncingSelection || _isKaraokeEngUpdating) return;
            if (TxtKaraokeEngOutput.SelectionLength > 0)
            {
                SyncSelectionFromOutputOrEditable(TxtKaraokeEngOutput);
            }
        }

        private void TxtKaraokeEngOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditable(TxtKaraokeEngOutput);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void TxtKaraokeEngEditable_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isKaraokeEngSyncingSelection || _isKaraokeEngUpdating) return;
            if (TxtKaraokeEngEditable.SelectionLength > 0)
            {
                SyncSelectionFromOutputOrEditable(TxtKaraokeEngEditable);
            }
        }

        private void TxtKaraokeEngEditable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_isKaraokeEngUpdating) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncSelectionFromOutputOrEditable(TxtKaraokeEngEditable);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Đồng bộ từ Panel 1 (Input) sang Panel 2 và Panel 3 dựa trên Mapping chính xác 1-1
        /// </summary>
        private void SyncSelectionFromInput()
        {
            if (_isKaraokeEngSyncingSelection) return;
            try
            {
                _isKaraokeEngSyncingSelection = true;
                var text = TxtKaraokeEngInput.Text;
                if (string.IsNullOrEmpty(text)) return;

                int selStart = TxtKaraokeEngInput.SelectionStart;
                int selLen = TxtKaraokeEngInput.SelectionLength;

                // Nếu có Mapping Result, tra cứu chính xác từ theo vị trí caret/selection
                if (_currentKaraokeEngMappingResult != null && _currentKaraokeEngMappingResult.Mappings.Count > 0)
                {
                    // Tìm mapping bao phủ vị trí caret/selection
                    var targetMappings = _currentKaraokeEngMappingResult.Mappings
                        .Where(m => (selStart >= m.InputStart && selStart <= m.InputStart + m.InputLength) ||
                                    (m.InputStart >= selStart && m.InputStart < selStart + Math.Max(1, selLen)))
                        .ToList();

                    if (targetMappings.Count > 0)
                    {
                        var firstMap = targetMappings[0];
                        var lastMap = targetMappings[targetMappings.Count - 1];

                        HighlightLineRangeInOutput(TxtKaraokeEngOutput, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        HighlightLineRangeInOutput(TxtKaraokeEngEditable, firstMap.OutputLineIndex, lastMap.OutputLineIndex);
                        return;
                    }
                }

                // Fallback theo vị trí dòng tương đối
                int lineIndex = TxtKaraokeEngInput.GetLineIndexFromCharacterIndex(selStart);
                if (lineIndex >= 0)
                {
                    int totalInputLines = TxtKaraokeEngInput.LineCount;
                    if (totalInputLines > 0)
                    {
                        int totalOutLines = TxtKaraokeEngOutput.LineCount;
                        int estimatedOutLine = (int)(((double)lineIndex / totalInputLines) * totalOutLines);
                        ScrollToLineInBox(TxtKaraokeEngOutput, estimatedOutLine);
                        ScrollToLineInBox(TxtKaraokeEngEditable, estimatedOutLine);
                    }
                }
            }
            finally
            {
                _isKaraokeEngSyncingSelection = false;
            }
        }

        /// <summary>
        /// Đồng bộ từ Panel 2 (Output) hoặc Panel 3 (Editable) sang Panel 1 và Panel còn lại
        /// </summary>
        private void SyncSelectionFromOutputOrEditable(TextBox sourceBox)
        {
            if (_isKaraokeEngSyncingSelection) return;
            try
            {
                _isKaraokeEngSyncingSelection = true;
                var text = sourceBox.Text;
                if (string.IsNullOrEmpty(text)) return;

                int caret = sourceBox.SelectionStart;
                int lineIndex = sourceBox.GetLineIndexFromCharacterIndex(caret);
                if (lineIndex < 0) return;

                // Tra cứu ngược từ OutputLineIndex về Input
                if (_currentKaraokeEngMappingResult != null && _currentKaraokeEngMappingResult.Mappings.Count > 0)
                {
                    var map = _currentKaraokeEngMappingResult.Mappings.FirstOrDefault(m => m.OutputLineIndex == lineIndex);
                    if (map != null)
                    {
                        // Highlight Panel 1
                        TxtKaraokeEngInput.Select(map.InputStart, map.InputLength);
                        int inputLineIdx = TxtKaraokeEngInput.GetLineIndexFromCharacterIndex(map.InputStart);
                        if (inputLineIdx >= 0)
                        {
                            TxtKaraokeEngInput.ScrollToLine(Math.Max(0, inputLineIdx - 2));
                        }

                        // Đồng bộ sang panel còn lại
                        var otherBox = (sourceBox == TxtKaraokeEngOutput) ? TxtKaraokeEngEditable : TxtKaraokeEngOutput;
                        HighlightLineRangeInOutput(otherBox, lineIndex, lineIndex);
                        return;
                    }
                }

                // Fallback nếu người dùng đã chỉnh sửa nhiều ở Editable
                var otherTargetBox = (sourceBox == TxtKaraokeEngOutput) ? TxtKaraokeEngEditable : TxtKaraokeEngOutput;
                HighlightLineInBoxByIndex(otherTargetBox, lineIndex);

                int totalSourceLines = sourceBox.LineCount;
                if (totalSourceLines > 0)
                {
                    int totalInputLines = TxtKaraokeEngInput.LineCount;
                    int estimatedInputLine = (int)(((double)lineIndex / totalSourceLines) * totalInputLines);
                    ScrollToLineInBox(TxtKaraokeEngInput, estimatedInputLine);
                }
            }
            finally
            {
                _isKaraokeEngSyncingSelection = false;
            }
        }

        private void HighlightLineRangeInOutput(TextBox targetBox, int startLineIndex, int endLineIndex)
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
                targetBox.Select(charStart, charEnd - charStart);
                targetBox.ScrollToLine(Math.Max(0, startLineIndex - 2));
            }
        }

        private void HighlightLineInBoxByIndex(TextBox targetBox, int lineIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || lineIndex < 0) return;
            if (lineIndex >= targetBox.LineCount) lineIndex = targetBox.LineCount - 1;
            if (lineIndex < 0) return;

            int charStart = targetBox.GetCharacterIndexFromLineIndex(lineIndex);
            int charLen = targetBox.GetLineLength(lineIndex);
            if (charStart >= 0 && charLen >= 0)
            {
                targetBox.Select(charStart, charLen);
                targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
            }
        }

        private void ScrollToLineInBox(TextBox targetBox, int lineIndex)
        {
            if (targetBox == null || lineIndex < 0) return;
            if (lineIndex >= targetBox.LineCount) lineIndex = targetBox.LineCount - 1;
            if (lineIndex >= 0)
            {
                targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
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
                var appDir = AppRuntimePaths.BaseDirectory;
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
                var appDir = AppRuntimePaths.BaseDirectory;
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
        /// Custom Song List: Mở file custom song list rules.txt để chỉnh sửa quy tắc riêng cho bài hát
        /// </summary>
        private void BtnCustomSongList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                _customSongListFilePath = Path.Combine(appDir, "custom song list rules.txt");

                if (!File.Exists(_customSongListFilePath))
                {
                    // Tạo file mẫu với hướng dẫn
                    var initialContent = "# Custom Song Split Rules (Format: word:part1/part2)\n# Các quy tắc ở đây sẽ overwrite quy tắc trong Word List Full\n# Ví dụ:\n# nanairo:na/na/i/ro\n# sora:so/ra\n";
                    File.WriteAllText(_customSongListFilePath, initialContent);
                    ShowToastKaraokeEng("📄 Đã tạo file Custom Song List mới!");
                }

                // Mở file bằng Notepad
                Process.Start("notepad.exe", $"\"{_customSongListFilePath}\"");
                ShowToastKaraokeEng("🎵 Đang mở Custom Song List để chỉnh sửa!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add To Word List: Mở cửa sổ popup nhập quy tắc thêm vào Full Word List
        /// </summary>
        private void BtnAddToWordList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDir = AppRuntimePaths.BaseDirectory;
                _wordListFilePath = Path.Combine(appDir, "word list rules.txt");

                var dialog = new AddWordListRulesWindow(_wordListFilePath, (updatedRules) =>
                {
                    _pendingKaraokeEngRules = updatedRules;
                    ProcessKaraokeEngInput();
                    ShowToastKaraokeEng("✨ Đã cập nhật Word List!");
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
                    var appDir = AppRuntimePaths.BaseDirectory;
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

