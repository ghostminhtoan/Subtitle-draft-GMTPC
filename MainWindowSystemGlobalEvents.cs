using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Win32;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Global events và settings management
    /// </summary>
    public partial class MainWindow : Window
    {
        // Search managers cho mỗi tab
        private SearchManager _searchHardware = new SearchManager();
        private SearchManager _searchDialogue = new SearchManager();
        private SearchManager _searchTranslate = new SearchManager();
        private SearchManager _searchFonts = new SearchManager();
        private SearchManager _searchAssFont = new SearchManager();
        private SearchManager _searchMerge = new SearchManager();
        private SearchManager _searchDraft = new SearchManager();
        private SearchManager _searchZeroTime = new SearchManager();
        private SearchManager _searchKaraokeViet = new SearchManager();
        private SearchManager _searchKaraokeEng = new SearchManager();
        private SearchManager _searchKaraokeMerge = new SearchManager();
        private SearchManager _searchKaraokeSync = new SearchManager();
        private SearchManager _searchEffect = new SearchManager();
        private SearchManager _searchTextToSub = new SearchManager();

        // Search TextBox hiện tại
        private TextBox _currentSearchBox = null;
        private string _currentSearchText = "";
        private string _lastSearchText = "";

#region "Window Initialization"

    public MainWindow()
    {
        InitializeComponent();
        SetBrowserEmulation();
    }

    private void SetBrowserEmulation()
    {
        try
        {
            var appName = System.AppDomain.CurrentDomain.FriendlyName;
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key?.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }
        catch
        {
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
        InitFontSizes();
        ApplyBuildStampToFooterLabels();
        LoadHardwareInfo();
        InitializeTutorialsAsync();
        InitializeDefaultPrompts();
        LoadAllPrompts();
        InitializeEffectDebounce();
        InitializeTextToSubtitleDebounce();
        LoadKaraokeEngSplitRules();
        LoadTextToSubSettings();

        // Register global key events for search
        this.PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    /// <summary>
    /// Global key handler for Ctrl+F and F3
    /// </summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsTutorialsTabActive())
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                ToggleTutorialSearchPanel();
                return;
            }

            if (e.Key == Key.F3)
            {
                e.Handled = true;
                NavigateTutorialSearch(!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                return;
            }
        }

        // Ctrl+F - Show/hide search box
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            ToggleSearchBox();
            return;
        }

        // F3 - Find next/prev
        if (e.Key == Key.F3)
        {
            e.Handled = true;
            bool reverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            FindNext(reverse);
            return;
        }
    }

#endregion

#region "Global Events - Window"

    /// <summary>
    /// Initialize font sizes 1-20 in ComboBox
    /// </summary>
    private void InitFontSizes()
    {
        CmbFontSize.Items.Clear();
        for (int i = 1; i <= 20; i++)
        {
            CmbFontSize.Items.Add(i.ToString());
        }
        CmbFontSize.SelectedIndex = 0; // Default to 1
    }

    private void ApplyBuildStampToFooterLabels()
    {
        try
        {
            var executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var buildTime = File.GetLastWriteTime(executablePath);
            var buildStamp = buildTime.ToString("yyyy-MM-dd hh.mm.ss tt dddd", new CultureInfo("en-US"));

            foreach (var textBlock in FindVisualChildren<TextBlock>(this))
            {
                if (textBlock == null || textBlock.Text == null)
                {
                    continue;
                }

                if (textBlock.Text == "Tác giả: ")
                {
                    textBlock.Text = $"Build: {buildStamp} | Tác giả: ";
                }
                else if (textBlock.Text == "Author: ")
                {
                    textBlock.Text = $"Build: {buildStamp} | Author: ";
                }
            }
        }
        catch
        {
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
    {
        if (dependencyObject == null)
        {
            yield break;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(dependencyObject, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Toggle Word Wrap: Bật/tắt TextWrapping cho tất cả TextBox
    /// </summary>
    private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
    {
        var wrap = (ToggleWordWrap.IsChecked == true) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ToggleWordWrap.Content = (ToggleWordWrap.IsChecked == true) ? "ON" : "OFF";

        // Áp dụng cho tất cả TextBox
        var textBoxes = new[] { TxtEngsub, TxtVietsub, TxtMerge, TxtMergeUnbreak,
                         TxtOriginal, TxtTimeCode, TxtConnectGap, TxtResult,
                         TxtDialogueInput, TxtDialogueOutput, TxtDialogueManual, TxtDialogueMerged,
                         TxtStylesInput, TxtStylesOutput,
                         TxtTranslateInput, TxtPrompt,
                         TxtKaraokeInput, TxtKaraokeEditable,
                         TxtZeroInput, TxtZeroOutput,
                         TxtKaraokeEngInput, TxtKaraokeEngEditable,
                         TxtKaraokeMergeInput, TxtKaraokeMergeOutput,
                         TxtSyncInput, TxtSyncOutput,
                         TxtSearchFontsInput,
                         TxtGpuInfo, TxtCpuInfo, TxtRamInfo, TxtMainboardInfo };

        foreach (var tb in textBoxes)
        {
            if (tb != null)
            {
                tb.TextWrapping = wrap;
            }
        }
    }

    /// <summary>
    /// Ctrl+Scroll: Phóng to/thu nhỏ font chữ trong TextBox
    /// </summary>
    private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        var tb = sender as TextBox;
        if (tb == null) return;

        var currentSize = tb.FontSize;
        var delta = e.Delta > 0 ? 1 : -1;
        var newSize = currentSize + delta;

        // Giới hạn font size từ 8 đến 48
        if (newSize >= 8 && newSize <= 48)
        {
            tb.FontSize = newSize;
        }

        e.Handled = true;
    }

    private void BtnDonate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start("https://tinyurl.com/gmtpcdonate");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

#endregion

#region "Global Events - Search (Ctrl+F / F3)"

    /// <summary>
    /// Toggle search panel visibility
    /// </summary>
    private void ToggleSearchBox()
    {
        if (SearchPanel.Visibility == Visibility.Visible)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            SearchPanel.Visibility = Visibility.Visible;
            TxtSearchBox.Focus();
            TxtSearchBox.SelectAll();
        }
    }

    /// <summary>
    /// Find next/previous occurrence
    /// </summary>
    private void FindNext(bool reverse = false)
    {
        if (string.IsNullOrWhiteSpace(_currentSearchText))
        {
            ToggleSearchBox();
            return;
        }

        PerformSearch(findNext: !reverse);
    }

    /// <summary>
    /// Search box text changed
    /// </summary>
    private void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentSearchText = TxtSearchBox.Text;
        // Không tự động tìm, đợi Enter
        TxtSearchStatus.Text = "";
    }

    /// <summary>
    /// Search box key down (Enter for next, Shift+Enter for prev, Escape to close)
    /// </summary>
    private void TxtSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _currentSearchText = TxtSearchBox.Text;
            
            if (string.IsNullOrWhiteSpace(_currentSearchText))
            {
                TxtSearchStatus.Text = "";
                return;
            }
            
            // Shift+Enter = reverse, Enter = forward
            bool reverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            PerformSearch(findNext: !reverse);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            SearchPanel.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Previous button click
    /// </summary>
    private void BtnSearchPrev_Click(object sender, RoutedEventArgs e)
    {
        // Prev = tìm ngược lại (đơn giản hóa: tìm next vì đang focus control)
        PerformSearch(findNext: true);
    }

    /// <summary>
    /// Next button click
    /// </summary>
    private void BtnSearchNext_Click(object sender, RoutedEventArgs e)
    {
        PerformSearch(findNext: true);
    }

    /// <summary>
    /// Close button click
    /// </summary>
    private void BtnSearchClose_Click(object sender, RoutedEventArgs e)
    {
        SearchPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Thực hiện tìm kiếm trong tab hiện tại
    /// </summary>
    private void PerformSearch(bool findNext)
    {
        if (string.IsNullOrWhiteSpace(_currentSearchText))
            return;

        // Xác định tab hiện tại và controls tương ứng
        var selectedTab = MainTabControl.SelectedItem as TabItem;
        if (selectedTab == null)
            return;

        string header = selectedTab.Header?.ToString() ?? "";
        bool found = false;

        // Reset search manager nếu text thay đổi
        var currentSM = GetSearchManagerForCurrentTab();
        if (currentSM != null && currentSM.MatchCount > 0 && _currentSearchText != _lastSearchText)
        {
            currentSM.Reset();
        }

        try
        {
            // Tab Hardware Info
            if (header.Contains("Hardware Info"))
            {
                TextBox[] textboxes = new[] { TxtGpuInfo, TxtCpuInfo, TxtRamInfo, TxtMainboardInfo };
                foreach (var tb in textboxes)
                {
                    if (tb != null && tb.IsFocused)
                    {
                        found = _searchHardware.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
                // Nếu không có control nào focus, tìm trong tất cả
                if (!found)
                {
                    foreach (var tb in textboxes)
                    {
                        if (tb != null && tb.Text.ToLower().Contains(_currentSearchText.ToLower()))
                        {
                            found = _searchHardware.SearchInTextBox(tb, _currentSearchText, findNext);
                            if (found) break;
                        }
                    }
                }
            }
            // Tab Dialogue
            else if (header.Contains("Dialogue"))
            {
                TextBox[] textboxes = new[] { TxtDialogueInput, TxtDialogueOutput, TxtDialogueManual, TxtDialogueMerged };
                foreach (var tb in textboxes)
                {
                    if (tb != null && tb.IsFocused)
                    {
                        found = _searchDialogue.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
                if (!found)
                {
                    foreach (var tb in textboxes)
                    {
                        if (tb != null && tb.Text.ToLower().Contains(_currentSearchText.ToLower()))
                        {
                            found = _searchDialogue.SearchInTextBox(tb, _currentSearchText, findNext);
                            if (found) break;
                        }
                    }
                }
            }
            // Tab Translate
            else if (header.Contains("Translate"))
            {
                TextBox[] textboxes = new[] { TxtTranslateInput, TxtPrompt };
                foreach (var tb in textboxes)
                {
                    if (tb != null && tb.IsFocused)
                    {
                        found = _searchTranslate.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
                if (!found)
                {
                    foreach (var tb in textboxes)
                    {
                        if (tb != null && tb.Text.ToLower().Contains(_currentSearchText.ToLower()))
                        {
                            found = _searchTranslate.SearchInTextBox(tb, _currentSearchText, findNext);
                            if (found) break;
                        }
                    }
                }
            }
            // Tab Search Fonts
            else if (header.Contains("Search Fonts"))
            {
                TextBox[] textboxes = new[] { TxtSearchFontsInput };
                foreach (var tb in textboxes)
                {
                    if (tb != null)
                    {
                        found = _searchFonts.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
            }
            // Tab ASS Font Adjust
            else if (header.Contains("ASS Font Adjust"))
            {
                TextBox[] textboxes = new[] { TxtStylesInput, TxtStylesOutput };
                foreach (var tb in textboxes)
                {
                    if (tb != null)
                    {
                        found = _searchAssFont.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
            }
            // Tab Subtitle Merge
            else if (header.Contains("Subtitle Merge"))
            {
                TextBox[] textboxes = new[] { TxtEngsub, TxtVietsub, TxtMerge, TxtMergeUnbreak };
                foreach (var tb in textboxes)
                {
                    if (tb != null)
                    {
                        found = _searchMerge.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
            }
            // Tab Subtitle Draft
            else if (header.Contains("Subtitle Draft"))
            {
                TextBox[] textboxes = new[] { TxtOriginal, TxtTimeCode, TxtConnectGap, TxtResult };
                foreach (var tb in textboxes)
                {
                    if (tb != null)
                    {
                        found = _searchDraft.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
            }
            // Tab Karaoke (sub-tabs)
            else if (header.Contains("Karaoke") || header.Contains("Effect"))
            {
                // Kiểm tra sub-tab trong Karaoke
                var karaokeTab = MainTabControl.SelectedItem as TabItem;
                if (karaokeTab?.Content is TabControl subTabControl)
                {
                    var selectedSubTab = subTabControl.SelectedItem as TabItem;
                    string subHeader = selectedSubTab?.Header?.ToString() ?? "";

                    if (subHeader.Contains("Zero Time"))
                    {
                        TextBox[] textboxes = new[] { TxtZeroInput, TxtZeroOutput };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchZeroTime.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                    else if (subHeader.Contains("Karaoke Vietnamese"))
                    {
                        TextBox[] textboxes = new[] { TxtKaraokeInput, TxtKaraokeEditable };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchKaraokeViet.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                    else if (subHeader.Contains("Karaoke English"))
                    {
                        TextBox[] textboxes = new[] { TxtKaraokeEngInput, TxtKaraokeEngEditable };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchKaraokeEng.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                    else if (subHeader.Contains("Karaoke Merge"))
                    {
                        TextBox[] textboxes = new[] { TxtKaraokeMergeInput, TxtKaraokeMergeOutput };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchKaraokeMerge.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                    else if (subHeader.Contains("Karaoke Sync"))
                    {
                        TextBox[] textboxes = new[] { TxtSyncInput, TxtSyncOutput };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchKaraokeSync.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                    else if (subHeader.Contains("Effect"))
                    {
                        TextBox[] textboxes = new[] { TxtEffectInput, TxtEffectOutput };
                        foreach (var tb in textboxes)
                        {
                            if (tb != null)
                            {
                                found = _searchEffect.SearchInTextBox(tb, _currentSearchText, findNext);
                                if (found) break;
                            }
                        }
                    }
                }
            }
            // Tab Text to Subtitle
            else if (header.Contains("Text to Subtitle"))
            {
                TextBox[] textboxes = new[] { TxtTextToSubInput, TxtTextToSubOutput };
                foreach (var tb in textboxes)
                {
                    if (tb != null)
                    {
                        found = _searchTextToSub.SearchInTextBox(tb, _currentSearchText, findNext);
                        if (found) break;
                    }
                }
            }

            // Update status
            if (found)
            {
                _lastSearchText = _currentSearchText;
                var sm = GetSearchManagerForCurrentTab();
                if (sm != null)
                {
                    TxtSearchStatus.Text = $"Kết quả: {sm.CurrentPosition + 1}/{sm.MatchCount}";
                }
            }
            else
            {
                TxtSearchStatus.Text = "Không tìm thấy";
            }
        }
        catch (Exception ex)
        {
            TxtSearchStatus.Text = "Lỗi: " + ex.Message;
        }
    }

    /// <summary>
    /// Lấy SearchManager tương ứng với tab hiện tại
    /// </summary>
    private SearchManager GetSearchManagerForCurrentTab()
    {
        var selectedTab = MainTabControl.SelectedItem as TabItem;
        if (selectedTab == null) return null;

        string header = selectedTab.Header?.ToString() ?? "";

        if (header.Contains("Hardware Info")) return _searchHardware;
        if (header.Contains("Dialogue")) return _searchDialogue;
        if (header.Contains("Translate")) return _searchTranslate;
        if (header.Contains("Search Fonts")) return _searchFonts;
        if (header.Contains("ASS Font Adjust")) return _searchAssFont;
        if (header.Contains("Subtitle Merge")) return _searchMerge;
        if (header.Contains("Subtitle Draft")) return _searchDraft;

        if (header.Contains("Karaoke") || header.Contains("Effect"))
        {
            var karaokeTab = MainTabControl.SelectedItem as TabItem;
            if (karaokeTab?.Content is TabControl subTabControl)
            {
                var selectedSubTab = subTabControl.SelectedItem as TabItem;
                string subHeader = selectedSubTab?.Header?.ToString() ?? "";

                if (subHeader.Contains("Zero Time")) return _searchZeroTime;
                if (subHeader.Contains("Karaoke Vietnamese")) return _searchKaraokeViet;
                if (subHeader.Contains("Karaoke English")) return _searchKaraokeEng;
                if (subHeader.Contains("Karaoke Merge")) return _searchKaraokeMerge;
                if (subHeader.Contains("Karaoke Sync")) return _searchKaraokeSync;
                if (subHeader.Contains("Effect")) return _searchEffect;
            }
        }

        // Tab level chính: Text to Subtitle
        if (header.Contains("Text to Subtitle")) return _searchTextToSub;

        return null;
    }

#endregion

#region "Global Events - Settings"

    private void LoadSettings()
    {
        try
        {
            if (!string.IsNullOrEmpty(AppSettings.TranslatePrompt))
            {
                TxtPrompt.Text = AppSettings.TranslatePrompt;
            }
        }
        catch
        {
        }
    }

    private void SaveSettings()
    {
        try
        {
            AppSettings.TranslatePrompt = TxtPrompt.Text.Trim();
            AppSettings.Save();
        }
        catch
        {
        }
    }

#endregion

    }
}
