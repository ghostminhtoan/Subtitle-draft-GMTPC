using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                    return;
                }

                var lines = content.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                TxtKaraokeEngCount.Text = string.Format("({0} lines)", lines.Length);

                var splitRules = _pendingKaraokeEngRules ?? "";
                var customRules = _pendingCustomSongRules ?? "";
                var karaokeResult = KaraokeVietnameseService.ProcessLyricsWithSplitRules(content, splitRules, customRules);
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
        /// Đồng bộ từ Panel 1 (Input) sang Panel 2 và Panel 3
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

                int wordStart = selStart;
                int wordEnd = selStart + Math.Max(1, selLen);

                if (selLen == 0)
                {
                    while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1])) wordStart--;
                    while (wordEnd < text.Length && !char.IsWhiteSpace(text[wordEnd])) wordEnd++;
                }

                if (wordStart >= wordEnd || wordStart >= text.Length) return;
                string selectedWord = text.Substring(wordStart, wordEnd - wordStart).Trim();
                if (string.IsNullOrEmpty(selectedWord)) return;

                // Xác định occurrence index của từ này trong Panel 1
                int occurrenceIndex = 0;
                int searchPos = 0;
                while (searchPos <= wordStart && searchPos < text.Length)
                {
                    while (searchPos < text.Length && char.IsWhiteSpace(text[searchPos])) searchPos++;
                    if (searchPos >= text.Length) break;

                    int curWordEnd = searchPos;
                    while (curWordEnd < text.Length && !char.IsWhiteSpace(text[curWordEnd])) curWordEnd++;

                    string curWord = text.Substring(searchPos, curWordEnd - searchPos);
                    if (string.Equals(curWord, selectedWord, StringComparison.OrdinalIgnoreCase))
                    {
                        if (searchPos == wordStart) break;
                        occurrenceIndex++;
                    }
                    searchPos = curWordEnd;
                }

                HighlightWordInOutputOrEditable(TxtKaraokeEngOutput, selectedWord, occurrenceIndex);
                HighlightWordInOutputOrEditable(TxtKaraokeEngEditable, selectedWord, occurrenceIndex);
            }
            finally
            {
                _isKaraokeEngSyncingSelection = false;
            }
        }

        /// <summary>
        /// Đồng bộ từ Panel 2 (Output) hoặc Panel 3 (Editable) sang 2 panel còn lại
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

                int lineStart = sourceBox.GetCharacterIndexFromLineIndex(lineIndex);
                int lineLength = sourceBox.GetLineLength(lineIndex);
                if (lineStart < 0 || lineLength <= 0) return;

                string lineText = text.Substring(lineStart, lineLength);
                string cleanLine = lineText.Replace("∞", "").Replace("♫", "").Trim();
                if (string.IsNullOrEmpty(cleanLine)) return;

                // Đếm occurrenceIndex của token này trong sourceBox
                int occurrenceIndex = 0;
                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lineIndex && i < lines.Length; i++)
                {
                    var cl = lines[i].Replace("∞", "").Replace("♫", "").Trim();
                    if (string.Equals(cl, cleanLine, StringComparison.OrdinalIgnoreCase))
                    {
                        occurrenceIndex++;
                    }
                }

                // Nhảy và highlight ở Panel 1
                HighlightWordInInput(TxtKaraokeEngInput, cleanLine, occurrenceIndex);

                // Nhảy và highlight ở Panel còn lại
                var otherBox = (sourceBox == TxtKaraokeEngOutput) ? TxtKaraokeEngEditable : TxtKaraokeEngOutput;
                HighlightWordInOutputOrEditable(otherBox, cleanLine, occurrenceIndex);
            }
            finally
            {
                _isKaraokeEngSyncingSelection = false;
            }
        }

        private void HighlightWordInInput(TextBox targetBox, string syllable, int occurrenceIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || string.IsNullOrEmpty(syllable)) return;
            var text = targetBox.Text;
            string cleanSyl = syllable.Trim().ToLowerInvariant();

            int currentOccur = 0;
            int searchPos = 0;

            while (searchPos < text.Length)
            {
                while (searchPos < text.Length && char.IsWhiteSpace(text[searchPos])) searchPos++;
                if (searchPos >= text.Length) break;

                int curWordEnd = searchPos;
                while (curWordEnd < text.Length && !char.IsWhiteSpace(text[curWordEnd])) curWordEnd++;

                string word = text.Substring(searchPos, curWordEnd - searchPos);
                string cleanWord = word.ToLowerInvariant();

                if (cleanWord.Contains(cleanSyl) || cleanSyl.Contains(cleanWord))
                {
                    if (currentOccur == occurrenceIndex)
                    {
                        targetBox.Select(searchPos, word.Length);
                        int lineIdx = targetBox.GetLineIndexFromCharacterIndex(searchPos);
                        if (lineIdx >= 0)
                        {
                            targetBox.ScrollToLine(Math.Max(0, lineIdx - 2));
                        }
                        return;
                    }
                    currentOccur++;
                }

                searchPos = curWordEnd;
            }
        }

        private void HighlightWordInOutputOrEditable(TextBox targetBox, string targetWord, int occurrenceIndex)
        {
            if (targetBox == null || string.IsNullOrEmpty(targetBox.Text) || string.IsNullOrEmpty(targetWord)) return;
            var targetText = targetBox.Text;
            string cleanTarget = targetWord.Trim().ToLowerInvariant();

            var lines = targetText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int currentOccurrence = 0;
            int lineStartIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var cleanLine = line.Replace("∞", "").Replace("♫", "").Trim().ToLowerInvariant();

                if (!string.IsNullOrEmpty(cleanLine) && (cleanTarget.Contains(cleanLine) || cleanLine.Contains(cleanTarget)))
                {
                    if (currentOccurrence == occurrenceIndex)
                    {
                        targetBox.Select(lineStartIndex, line.Length);
                        int lineIndex = targetBox.GetLineIndexFromCharacterIndex(lineStartIndex);
                        if (lineIndex >= 0)
                        {
                            targetBox.ScrollToLine(Math.Max(0, lineIndex - 2));
                        }
                        return;
                    }
                    currentOccurrence++;
                }

                lineStartIndex += line.Length;
                if (lineStartIndex < targetText.Length)
                {
                    if (lineStartIndex + 1 < targetText.Length && targetText[lineStartIndex] == '\r' && targetText[lineStartIndex + 1] == '\n')
                    {
                        lineStartIndex += 2;
                    }
                    else
                    {
                        lineStartIndex += 1;
                    }
                }
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

