using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Tutorials - Fields

        private static readonly HttpClient _tutorialsHttpClient = CreateTutorialsHttpClient();

        private readonly Dictionary<string, TutorialDocumentDefinition> _tutorialDocuments = CreateTutorialDocuments();
        private readonly Dictionary<string, string> _tutorialHtmlCache = new Dictionary<string, string>();
        private TutorialMarkdownPopupWindow _tutorialMarkdownPopupWindow;
        private TutorialsWorkbookPopupWindow _tutorialsWorkbookPopupWindow;
        private bool _isTutorialsLoading = false;
        private bool _isTutorialShortcutsExcelLoading = false;
        private bool _tutorialsInitialized = false;
        private bool _tutorialShortcutsExcelInitialized = false;
        private bool _isTutorialSearchPanelOpen = false;
        private bool _tutorialSearchSuppressTextChanged = false;
        private string _tutorialSearchText = string.Empty;

        private const string TutorialShortcutsExcelOneDriveUrl =
            "https://1drv.ms/x/c/d1ed72b79f8c17a6/IQDAswI2f9DlRrB8kq7G-4YSATja50yTHTFJE1rkAhUBMTQ?e=QU0aAY";

        #endregion

        #region Tutorials - Initialization

        private async void InitializeTutorialsAsync()
        {
            if (_tutorialsInitialized || _isTutorialsLoading)
            {
                return;
            }

            _tutorialsInitialized = true;
            await LoadAllTutorialDocumentsAsync(false);
        }

        private static HttpClient CreateTutorialsHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SubtitleDraftGMTPC/1.0");
            return client;
        }

        private static Dictionary<string, TutorialDocumentDefinition> CreateTutorialDocuments()
        {
            return new Dictionary<string, TutorialDocumentDefinition>
            {
                {
                        "overview",
                        new TutorialDocumentDefinition(
                            "Overview",
                        Path.Combine("Tutorials", "Hướng dẫn", "1-gioi-thieu-so-luoc.md"))
                },
                {
                    "workflow",
                    new TutorialDocumentDefinition(
                        "Workflow",
                        Path.Combine("Tutorials", "Tutorial", "Profiles", "README.md"))
                },
                {
                    "shortcut-default",
                    new TutorialDocumentDefinition(
                        "Default Shortcuts",
                        Path.Combine("Tutorials", "Shortcuts", "shortcut MMT - Vietnamese.md"))
                },
                {
                    "shortcut-translate-new",
                    new TutorialDocumentDefinition(
                        "Translate - New Subtitle",
                        Path.Combine("Tutorials", "Tutorial", "Profiles", "profile-01-translate-new-subtitle.md"))
                },
                {
                    "shortcut-translate-existing",
                    new TutorialDocumentDefinition(
                        "Translate - Existing Subtitle",
                        Path.Combine("Tutorials", "Tutorial", "Profiles", "profile-02-translate-existing-subtitle.md"))
                },
                {
                    "shortcut-edit-translated",
                    new TutorialDocumentDefinition(
                        "Edit Translated Subtitle",
                        Path.Combine("Tutorials", "Tutorial", "Profiles", "profile-03-edit-translated.md"))
                },
                {
                    "shortcut-quick-checker",
                    new TutorialDocumentDefinition(
                        "Quick Checker",
                        Path.Combine("Tutorials", "Tutorial", "Profiles", "profile-04-quick-checker.md"))
                }
            };
        }

        #endregion

        #region Tutorials - Load

        private async Task LoadAllTutorialDocumentsAsync(bool forceRefresh)
        {
            if (_isTutorialsLoading)
            {
                return;
            }

            try
            {
                _isTutorialsLoading = true;
                BtnRefreshTutorials.IsEnabled = false;
                TxtTutorialsStatus.Text = "Đang tải tài liệu từ thư mục Tutorials...";

                ShowTutorialLoadingState();

                foreach (var pair in _tutorialDocuments)
                {
                    if (forceRefresh)
                    {
                        _tutorialHtmlCache.Remove(pair.Key);
                    }

                    var browser = GetTutorialBrowser(pair.Key);
                    if (browser == null)
                    {
                        continue;
                    }

                    var html = await GetTutorialHtmlAsync(pair.Key, pair.Value, forceRefresh);
                    browser.NavigateToString(html);
                }

                TxtTutorialsStatus.Text = string.Format(
                    "Đã tải {0} tài liệu từ Tutorials lúc {1:HH:mm:ss}.",
                    _tutorialDocuments.Count,
                    DateTime.Now);

                await LoadTutorialShortcutsExcelAsync(forceRefresh);
                UpdateTutorialMarkdownPopupForCurrentSelection(forceRefresh);

                ReapplyTutorialSearchToCurrentBrowser();
            }
            catch (Exception ex)
            {
                TxtTutorialsStatus.Text = "Không thể tải tutorials từ thư mục Tutorials.";
                ShowTutorialErrorState(ex.Message);
            }
            finally
            {
                BtnRefreshTutorials.IsEnabled = true;
                _isTutorialsLoading = false;
            }
        }

        private async Task<string> GetTutorialHtmlAsync(string key, TutorialDocumentDefinition document, bool forceRefresh)
        {
            if (!forceRefresh && _tutorialHtmlCache.TryGetValue(key, out var cachedHtml))
            {
                return cachedHtml;
            }

            var markdown = await LoadTutorialMarkdownAsync(document);
            var html = GitHubMarkdownHtmlRenderer.RenderDocument(markdown, document.SourceUrl, document.Title);
            _tutorialHtmlCache[key] = html;
            return html;
        }

        private static async Task<string> LoadTutorialMarkdownAsync(TutorialDocumentDefinition document)
        {
            var filePath = document.GetFullPath();
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Không tìm thấy file tutorial.", filePath);
            }

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, true))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task LoadTutorialShortcutsExcelAsync(bool forceRefresh)
        {
            if (_isTutorialShortcutsExcelLoading)
            {
                return;
            }

            if (_tutorialShortcutsExcelInitialized && !forceRefresh)
            {
                return;
            }

            try
            {
                _isTutorialShortcutsExcelLoading = true;
                TxtTutorialsExcelStatus.Text = "Đang mở workbook Excel từ OneDrive...";

                var popup = EnsureTutorialsWorkbookPopupWindow();
                popup.WorkbookUrl = TutorialShortcutsExcelOneDriveUrl;
                if (forceRefresh)
                {
                    popup.ReloadWorkbook();
                }

                _tutorialShortcutsExcelInitialized = true;
                TxtTutorialsExcelStatus.Text = "Workbook OneDrive sẵn sàng để mở popup.";
            }
            catch (Exception ex)
            {
                TxtTutorialsExcelStatus.Text = "Không thể tải workbook Excel.";
                MessageBox.Show("Không thể tải workbook Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isTutorialShortcutsExcelLoading = false;
            }
        }

        private void ShowTutorialLoadingState()
        {
            foreach (var pair in _tutorialDocuments)
            {
                var browser = GetTutorialBrowser(pair.Key);
                if (browser == null)
                {
                    continue;
                }

                browser.NavigateToString(BuildTutorialPlaceholderHtml(pair.Value.Title, "Đang tải nội dung từ Tutorials..."));
            }

            if (_tutorialMarkdownPopupWindow != null && _tutorialMarkdownPopupWindow.IsVisible)
            {
                _tutorialMarkdownPopupWindow.ShowHtml("Tutorials", BuildTutorialPlaceholderHtml("Tutorials", "Đang tải nội dung từ Tutorials..."));
            }
        }

        private void ShowTutorialErrorState(string message)
        {
            foreach (var pair in _tutorialDocuments)
            {
                var browser = GetTutorialBrowser(pair.Key);
                if (browser == null)
                {
                    continue;
                }

                browser.NavigateToString(BuildTutorialPlaceholderHtml(pair.Value.Title, "Lỗi tải dữ liệu: " + message));
            }

            if (_tutorialMarkdownPopupWindow != null && _tutorialMarkdownPopupWindow.IsVisible)
            {
                _tutorialMarkdownPopupWindow.ShowHtml("Tutorials", BuildTutorialPlaceholderHtml("Tutorials", "Lỗi tải dữ liệu: " + message));
            }
        }

        private static string BuildTutorialPlaceholderHtml(string title, string message)
        {
            return "<!DOCTYPE html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" /><meta charset=\"utf-8\" />"
                + "<style>body{margin:0;background:#16181d;color:#e6edf3;font-family:'Segoe UI',Arial,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;}div{max-width:720px;padding:24px;text-align:center;}h2{margin:0 0 12px;color:#fff;}p{margin:0;color:#aeb8c4;line-height:1.6;}</style>"
                + "</head><body><div><h2>" + System.Net.WebUtility.HtmlEncode(title) + "</h2><p>" + System.Net.WebUtility.HtmlEncode(message) + "</p></div></body></html>";
        }

        private WebBrowser GetTutorialBrowser(string key)
        {
            switch (key)
            {
                case "overview":
                    return BrowserTutorialOverview;
                case "workflow":
                    return BrowserTutorialWorkflow;
                case "shortcut-default":
                    return BrowserTutorialShortcutDefault;
                case "shortcut-translate-new":
                    return BrowserTutorialShortcutTranslateNew;
                case "shortcut-translate-existing":
                    return BrowserTutorialShortcutTranslateExisting;
                case "shortcut-edit-translated":
                    return BrowserTutorialShortcutEditTranslated;
                case "shortcut-quick-checker":
                    return BrowserTutorialShortcutQuickChecker;
                default:
                    return null;
            }
        }

        #endregion

        #region Tutorials - Events

        private async void BtnRefreshTutorials_Click(object sender, RoutedEventArgs e)
        {
            await LoadAllTutorialDocumentsAsync(true);
        }

        private async void RefreshTutorialsFromLocalFolder()
        {
            await LoadAllTutorialDocumentsAsync(true);
        }

        private async void RefreshTutorialsFromGitHub()
        {
            await LoadAllTutorialDocumentsAsync(true);
        }

        private void TutorialsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isTutorialsLoading)
            {
                return;
            }

            UpdateTutorialSearchModeForCurrentTab();
            TxtTutorialsStatus.Text = "Đang xem: " + GetCurrentTutorialTitle();
            if (IsShortcutsExcelTabActive())
            {
                OpenTutorialsWorkbookPopup();
            }
            else
            {
                OpenTutorialMarkdownPopupForCurrentSelection();
            }
            ReapplyTutorialSearchToCurrentBrowser();
        }

        private void TutorialBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            ReapplyTutorialSearchToCurrentBrowser();
        }

        private void TutorialBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            var target = e.Uri.AbsoluteUri;
            if (string.Equals(target, "about:blank", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Cancel = true;

            try
            {
                Process.Start(new ProcessStartInfo(target)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở liên kết: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleTutorialSearch_Click(object sender, RoutedEventArgs e)
        {
            ToggleTutorialSearchPanel();
        }

        private void BtnOpenTutorialExcelPopup_Click(object sender, RoutedEventArgs e)
        {
            OpenTutorialsWorkbookPopup();
        }

        private void TxtTutorialSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tutorialSearchSuppressTextChanged)
            {
                return;
            }

            _tutorialSearchText = TxtTutorialSearchBox.Text;
            ApplyTutorialSearch(resetIndex: true);
        }

        private void TxtTutorialSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    NavigateTutorialSearch(false);
                }
                else
                {
                    NavigateTutorialSearch(true);
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseTutorialSearchPanel();
            }
        }

        private void BtnTutorialSearchPrev_Click(object sender, RoutedEventArgs e)
        {
            NavigateTutorialSearch(false);
        }

        private void BtnTutorialSearchNext_Click(object sender, RoutedEventArgs e)
        {
            NavigateTutorialSearch(true);
        }

        private void BtnTutorialSearchClear_Click(object sender, RoutedEventArgs e)
        {
            ClearTutorialSearch();
        }

        private void TutorialSearchOption_Changed(object sender, RoutedEventArgs e)
        {
            ApplyTutorialSearch(resetIndex: true);
        }

        private void UpdateTutorialSearchModeForCurrentTab()
        {
            var excelActive = IsShortcutsExcelTabActive();
            if (ChkTutorialRegex != null)
            {
                ChkTutorialRegex.IsEnabled = !excelActive;
                if (excelActive && ChkTutorialRegex.IsChecked == true)
                {
                    ChkTutorialRegex.IsChecked = false;
                }
            }
        }

        private void OpenTutorialMarkdownPopupForCurrentSelection()
        {
            var selectedKey = GetCurrentTutorialDocumentKey();
            if (string.IsNullOrWhiteSpace(selectedKey) || !_tutorialDocuments.ContainsKey(selectedKey))
            {
                return;
            }

            var document = _tutorialDocuments[selectedKey];
            var popup = EnsureTutorialMarkdownPopupWindow();
            popup.Show();
            popup.Activate();
            popup.Topmost = true;
            popup.Topmost = false;

            var html = _tutorialHtmlCache.ContainsKey(selectedKey)
                ? _tutorialHtmlCache[selectedKey]
                : BuildTutorialPlaceholderHtml(document.Title, "Đang tải nội dung từ Tutorials...");

            popup.ShowHtml(document.Title, html);
        }

        private void UpdateTutorialMarkdownPopupForCurrentSelection(bool forceRefresh)
        {
            if (_tutorialMarkdownPopupWindow == null || !_tutorialMarkdownPopupWindow.IsVisible)
            {
                return;
            }

            var selectedKey = GetCurrentTutorialDocumentKey();
            if (string.IsNullOrWhiteSpace(selectedKey) || !_tutorialDocuments.ContainsKey(selectedKey))
            {
                return;
            }

            var document = _tutorialDocuments[selectedKey];
            string html;
            if (forceRefresh || !_tutorialHtmlCache.TryGetValue(selectedKey, out html))
            {
                html = BuildTutorialPlaceholderHtml(document.Title, "Đang tải nội dung từ Tutorials...");
            }

            _tutorialMarkdownPopupWindow.ShowHtml(document.Title, html);
        }

        private TutorialMarkdownPopupWindow EnsureTutorialMarkdownPopupWindow()
        {
            if (_tutorialMarkdownPopupWindow == null)
            {
                _tutorialMarkdownPopupWindow = new TutorialMarkdownPopupWindow
                {
                    Owner = this
                };
                _tutorialMarkdownPopupWindow.Closed += TutorialMarkdownPopupWindow_Closed;
            }

            return _tutorialMarkdownPopupWindow;
        }

        private string GetCurrentTutorialDocumentKey()
        {
            var selectedMainTab = TutorialsTabControl.SelectedItem as TabItem;
            if (selectedMainTab == null)
            {
                return "overview";
            }

            var mainHeader = Convert.ToString(selectedMainTab.Header);
            if (string.Equals(mainHeader, "Overview", StringComparison.OrdinalIgnoreCase))
            {
                return "overview";
            }

            if (string.Equals(mainHeader, "Workflow", StringComparison.OrdinalIgnoreCase))
            {
                return "workflow";
            }

            if (string.Equals(mainHeader, "Shortcuts", StringComparison.OrdinalIgnoreCase))
            {
                var selectedShortcutsTab = TutorialShortcutsTabControl != null
                    ? TutorialShortcutsTabControl.SelectedItem as TabItem
                    : null;
                var shortcutsHeader = selectedShortcutsTab != null ? Convert.ToString(selectedShortcutsTab.Header) : "Default Shortcuts";

                switch (shortcutsHeader)
                {
                    case "Translate - New Subtitle":
                        return "shortcut-translate-new";
                    case "Translate - Existing Subtitle":
                        return "shortcut-translate-existing";
                    case "Edit Translated Subtitle":
                        return "shortcut-edit-translated";
                    case "Quick Checker":
                        return "shortcut-quick-checker";
                    default:
                        return "shortcut-default";
                }
            }

            return "overview";
        }

        private void TutorialMarkdownPopupWindow_Closed(object sender, System.EventArgs e)
        {
            if (ReferenceEquals(sender, _tutorialMarkdownPopupWindow))
            {
                _tutorialMarkdownPopupWindow = null;
            }
        }

        private string GetCurrentTutorialTitle()
        {
            if (TutorialsTabControl.SelectedItem == null)
            {
                return "Tutorials";
            }

            var selectedMainTab = TutorialsTabControl.SelectedItem as TabItem;
            if (selectedMainTab == null)
            {
                return "Tutorials";
            }

            var mainHeader = Convert.ToString(selectedMainTab.Header);
            if (!string.Equals(mainHeader, "Shortcuts", StringComparison.OrdinalIgnoreCase))
            {
                return mainHeader;
            }

            var selectedShortcutsTab = TutorialShortcutsTabControl.SelectedItem as TabItem;
            return selectedShortcutsTab != null
                ? Convert.ToString(selectedShortcutsTab.Header)
                : mainHeader;
        }

        private bool IsTutorialsTabActive()
        {
            var selectedTab = MainTabControl.SelectedItem as TabItem;
            if (selectedTab == null)
            {
                return false;
            }

            return string.Equals(Convert.ToString(selectedTab.Header), "📚 Tutorials", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Tutorials - Search

        private void ToggleTutorialSearchPanel()
        {
            if (_isTutorialSearchPanelOpen)
            {
                CloseTutorialSearchPanel();
                return;
            }

            _isTutorialSearchPanelOpen = true;
            TutorialSearchPanel.Visibility = Visibility.Visible;
            BtnToggleTutorialSearch.Content = "✕ Đóng tìm kiếm";
            TxtTutorialSearchBox.Focus();
            TxtTutorialSearchBox.SelectAll();
            ApplyTutorialSearch(resetIndex: false);
        }

        private void CloseTutorialSearchPanel()
        {
            _isTutorialSearchPanelOpen = false;
            TutorialSearchPanel.Visibility = Visibility.Collapsed;
            BtnToggleTutorialSearch.Content = "🔎 Tìm trong Tutorials";
        }

        private async void ClearTutorialSearch()
        {
            _tutorialSearchSuppressTextChanged = true;
            TxtTutorialSearchBox.Text = string.Empty;
            _tutorialSearchSuppressTextChanged = false;
            _tutorialSearchText = string.Empty;
            TxtTutorialSearchStatus.Text = string.Empty;
            await ClearTutorialSearchAsync();
            TxtTutorialSearchBox.Focus();
        }

        private async void NavigateTutorialSearch(bool forward)
        {
            if (string.IsNullOrWhiteSpace(TxtTutorialSearchBox.Text))
            {
                TxtTutorialSearchStatus.Text = "Nhập từ khóa cần tìm.";
                return;
            }

            _tutorialSearchText = TxtTutorialSearchBox.Text;
            if (IsShortcutsExcelTabActive())
            {
                OpenTutorialsWorkbookPopup();
            }
            var result = IsShortcutsExcelTabActive()
                ? await NavigateTutorialShortcutsExcelSearchAsync(forward)
                : InvokeTutorialSearchScript(forward ? "tutorialSearchNext" : "tutorialSearchPrev");
            UpdateTutorialSearchStatus(result);
        }

        private async void ApplyTutorialSearch(bool resetIndex)
        {
            _tutorialSearchText = TxtTutorialSearchBox.Text;

            if (string.IsNullOrWhiteSpace(_tutorialSearchText))
            {
                TxtTutorialSearchStatus.Text = string.Empty;
                await ClearTutorialSearchAsync();
                return;
            }

            if (IsShortcutsExcelTabActive())
            {
                OpenTutorialsWorkbookPopup();
            }
            var result = IsShortcutsExcelTabActive()
                ? await ApplyTutorialShortcutsExcelSearchAsync(
                    _tutorialSearchText,
                    ChkTutorialMatchCase.IsChecked == true,
                    ChkTutorialWholeWord.IsChecked == true,
                    ChkTutorialRegex.IsChecked == true,
                    resetIndex)
                : InvokeTutorialSearchScript(
                    "tutorialSearchApply",
                    _tutorialSearchText,
                    ChkTutorialMatchCase.IsChecked == true,
                    ChkTutorialWholeWord.IsChecked == true,
                    ChkTutorialRegex.IsChecked == true,
                    resetIndex);

            UpdateTutorialSearchStatus(result);
        }

        private void ReapplyTutorialSearchToCurrentBrowser()
        {
            if (!_isTutorialSearchPanelOpen)
            {
                return;
            }

            ApplyTutorialSearch(resetIndex: true);
        }

        private string InvokeTutorialSearchScript(string scriptName, params object[] args)
        {
            try
            {
                var browser = GetCurrentTutorialBrowser();
                if (browser?.Document == null)
                {
                    return null;
                }

                var result = browser.InvokeScript(scriptName, args);
                return result != null ? Convert.ToString(result) : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> ApplyTutorialShortcutsExcelSearchAsync(
            string searchText,
            bool matchCase,
            bool wholeWord,
            bool useRegex,
            bool resetIndex)
        {
            var webView = GetCurrentTutorialExcelWebView();
            if (webView?.CoreWebView2 == null)
            {
                return null;
            }

            var find = webView.CoreWebView2.Find;
            if (resetIndex)
            {
                find.Stop();
            }

            var options = webView.CoreWebView2.Environment.CreateFindOptions();
            options.FindTerm = searchText ?? string.Empty;
            options.IsCaseSensitive = matchCase;
            options.ShouldMatchWord = wholeWord;
            options.ShouldHighlightAllMatches = true;
            options.SuppressDefaultFindDialog = true;

            await find.StartAsync(options);
            return BuildTutorialShortcutsExcelFindResult(find);
        }

        private async Task<string> NavigateTutorialShortcutsExcelSearchAsync(bool forward)
        {
            var webView = GetCurrentTutorialExcelWebView();
            if (webView?.CoreWebView2 == null)
            {
                return null;
            }

            var find = webView.CoreWebView2.Find;
            if (find.MatchCount <= 0)
            {
                return await ApplyTutorialShortcutsExcelSearchAsync(
                    TxtTutorialSearchBox.Text,
                    ChkTutorialMatchCase.IsChecked == true,
                    ChkTutorialWholeWord.IsChecked == true,
                    ChkTutorialRegex.IsChecked == true,
                    resetIndex: true);
            }

            if (forward)
            {
                find.FindNext();
            }
            else
            {
                find.FindPrevious();
            }

            return BuildTutorialShortcutsExcelFindResult(find);
        }

        private Task ClearTutorialSearchAsync()
        {
            var webView = GetCurrentTutorialExcelWebView();
            if (webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Find.Stop();
            }

            return Task.CompletedTask;
        }

        private static string BuildTutorialShortcutsExcelFindResult(CoreWebView2Find find)
        {
            if (find == null)
            {
                return null;
            }

            var total = find.MatchCount;
            var current = find.ActiveMatchIndex < 0 ? 0 : find.ActiveMatchIndex;
            return "ok|" + total + "|" + current + "|Shortcuts Excel";
        }

        private void UpdateTutorialSearchStatus(string result)
        {
            if (string.IsNullOrWhiteSpace(_tutorialSearchText))
            {
                TxtTutorialSearchStatus.Text = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                TxtTutorialSearchStatus.Text = "Chưa sẵn sàng tìm trong tài liệu này.";
                return;
            }

            var parts = result.Split(new[] { '|' }, 4);
            if (parts.Length == 0)
            {
                TxtTutorialSearchStatus.Text = "Không đọc được kết quả tìm kiếm.";
                return;
            }

            if (string.Equals(parts[0], "error", StringComparison.OrdinalIgnoreCase))
            {
                TxtTutorialSearchStatus.Text = parts.Length > 1 ? "Regex lỗi: " + parts[1] : "Regex lỗi.";
                return;
            }

            if (!string.Equals(parts[0], "ok", StringComparison.OrdinalIgnoreCase) || parts.Length < 4)
            {
                TxtTutorialSearchStatus.Text = "Không đọc được kết quả tìm kiếm.";
                return;
            }

            int total;
            int current;
            if (!int.TryParse(parts[1], out total) || !int.TryParse(parts[2], out current))
            {
                TxtTutorialSearchStatus.Text = "Không đọc được kết quả tìm kiếm.";
                return;
            }

            var contextTitle = parts[3];
            TxtTutorialSearchStatus.Text = total == 0
                ? "Không có kết quả trong " + contextTitle + "."
                : string.Format("{0}/{1} kết quả trong {2}", current, total, contextTitle);
        }

        private WebBrowser GetCurrentTutorialBrowser()
        {
            if (_tutorialMarkdownPopupWindow != null && _tutorialMarkdownPopupWindow.IsVisible && !IsShortcutsExcelTabActive())
            {
                return _tutorialMarkdownPopupWindow.Browser;
            }

            var selectedMainTab = TutorialsTabControl.SelectedItem as TabItem;
            if (selectedMainTab == null)
            {
                return BrowserTutorialOverview;
            }

            var mainHeader = Convert.ToString(selectedMainTab.Header);
            if (string.Equals(mainHeader, "Overview", StringComparison.OrdinalIgnoreCase))
            {
                return BrowserTutorialOverview;
            }

            if (string.Equals(mainHeader, "Workflow", StringComparison.OrdinalIgnoreCase))
            {
                return BrowserTutorialWorkflow;
            }

            var selectedShortcutsTab = TutorialShortcutsTabControl != null ? TutorialShortcutsTabControl.SelectedItem as TabItem : null;
            var shortcutsHeader = selectedShortcutsTab != null ? Convert.ToString(selectedShortcutsTab.Header) : "Default Shortcuts";

            switch (shortcutsHeader)
            {
                case "Translate - New Subtitle":
                    return BrowserTutorialShortcutTranslateNew;
                case "Translate - Existing Subtitle":
                    return BrowserTutorialShortcutTranslateExisting;
                case "Edit Translated Subtitle":
                    return BrowserTutorialShortcutEditTranslated;
                case "Quick Checker":
                    return BrowserTutorialShortcutQuickChecker;
                default:
                    return BrowserTutorialShortcutDefault;
            }
        }

        private WebView2 GetCurrentTutorialExcelWebView()
        {
            return _tutorialsWorkbookPopupWindow != null && _tutorialsWorkbookPopupWindow.IsVisible
                ? _tutorialsWorkbookPopupWindow.WorkbookBrowser
                : null;
        }

        private bool IsShortcutsExcelTabActive()
        {
            var selectedMainTab = TutorialsTabControl.SelectedItem as TabItem;
            if (selectedMainTab == null)
            {
                return false;
            }

            return string.Equals(Convert.ToString(selectedMainTab.Header), "Shortcuts Excel", StringComparison.OrdinalIgnoreCase);
        }

        private TutorialsWorkbookPopupWindow EnsureTutorialsWorkbookPopupWindow()
        {
            if (_tutorialsWorkbookPopupWindow == null)
            {
                _tutorialsWorkbookPopupWindow = new TutorialsWorkbookPopupWindow
                {
                    Owner = this,
                    WorkbookUrl = TutorialShortcutsExcelOneDriveUrl
                };
                _tutorialsWorkbookPopupWindow.Closed += TutorialsWorkbookPopupWindow_Closed;
            }

            return _tutorialsWorkbookPopupWindow;
        }

        private void OpenTutorialsWorkbookPopup()
        {
            var popup = EnsureTutorialsWorkbookPopupWindow();
            popup.WorkbookUrl = TutorialShortcutsExcelOneDriveUrl;

            if (!popup.IsVisible)
            {
                popup.Show();
            }

            popup.Activate();
            popup.Topmost = true;
            popup.Topmost = false;
            popup.Focus();
        }

        private void TutorialsWorkbookPopupWindow_Closed(object sender, System.EventArgs e)
        {
            if (ReferenceEquals(sender, _tutorialsWorkbookPopupWindow))
            {
                _tutorialsWorkbookPopupWindow = null;
            }
        }

        #endregion

        #region Tutorials - Models

        private sealed class TutorialDocumentDefinition
        {
            private const string GitHubRepoBaseUrl = "https://github.com/ghostminhtoan/Subtitle-draft-GMTPC/blob/master/";

            public TutorialDocumentDefinition(string title, string relativePath)
            {
                Title = title;
                RelativePath = relativePath;
            }

            public string Title { get; }

            public string RelativePath { get; }

            public string GetFullPath()
            {
                return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RelativePath));
            }

            public string SourceUrl
            {
                get
                {
                    return BuildGitHubBlobUrl(RelativePath);
                }
            }

            private static string BuildGitHubBlobUrl(string relativePath)
            {
                var normalizedPath = (relativePath ?? string.Empty).Replace('\\', '/');
                var segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var encodedSegments = new List<string>(segments.Length);

                foreach (var segment in segments)
                {
                    encodedSegments.Add(Uri.EscapeDataString(segment));
                }

                return GitHubRepoBaseUrl + string.Join("/", encodedSegments);
            }
        }

        #endregion
    }
}
