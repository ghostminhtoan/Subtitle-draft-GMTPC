using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Tutorials - Fields

        private static readonly HttpClient _tutorialsHttpClient = CreateTutorialsHttpClient();

        private readonly Dictionary<string, TutorialDocumentDefinition> _tutorialDocuments = CreateTutorialDocuments();
        private readonly Dictionary<string, string> _tutorialHtmlCache = new Dictionary<string, string>();
        private bool _isTutorialsLoading = false;
        private bool _tutorialsInitialized = false;
        private bool _isTutorialSearchPanelOpen = false;
        private bool _tutorialSearchSuppressTextChanged = false;
        private string _tutorialSearchText = string.Empty;

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
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/H%C6%B0%E1%BB%9Bng%20d%E1%BA%ABn/1-gioi-thieu-so-luoc.md")
                },
                {
                    "workflow",
                    new TutorialDocumentDefinition(
                        "Workflow",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Tutorial/Profiles/README.md")
                },
                {
                    "shortcut-default",
                    new TutorialDocumentDefinition(
                        "Default Shortcuts",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Shortcuts/shortcut%20MMT%20-%20Vietnamese.md")
                },
                {
                    "shortcut-translate-new",
                    new TutorialDocumentDefinition(
                        "Translate - New Subtitle",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Tutorial/Profiles/profile-01-translate-new-subtitle.md")
                },
                {
                    "shortcut-translate-existing",
                    new TutorialDocumentDefinition(
                        "Translate - Existing Subtitle",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Tutorial/Profiles/profile-02-translate-existing-subtitle.md")
                },
                {
                    "shortcut-edit-translated",
                    new TutorialDocumentDefinition(
                        "Edit Translated Subtitle",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Tutorial/Profiles/profile-03-edit-translated.md")
                },
                {
                    "shortcut-quick-checker",
                    new TutorialDocumentDefinition(
                        "Quick Checker",
                        "https://github.com/ghostminhtoan/subtitle-edit-shortcuts-GMTPC/blob/master/Tutorials/Tutorial/Profiles/profile-04-quick-checker.md")
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
                TxtTutorialsStatus.Text = "Đang tải tài liệu từ GitHub...";

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
                    "Đã tải {0} tài liệu từ GitHub lúc {1:HH:mm:ss}.",
                    _tutorialDocuments.Count,
                    DateTime.Now);

                ReapplyTutorialSearchToCurrentBrowser();
            }
            catch (Exception ex)
            {
                TxtTutorialsStatus.Text = "Không thể tải tutorials từ GitHub.";
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
            if (_tutorialHtmlCache.TryGetValue(key, out var cachedHtml))
            {
                return cachedHtml;
            }

            var markdown = await FetchTutorialMarkdownAsync(document, forceRefresh);
            var html = GitHubMarkdownHtmlRenderer.RenderDocument(markdown, document.BlobUrl, document.Title);
            _tutorialHtmlCache[key] = html;
            return html;
        }

        private async Task<string> FetchTutorialMarkdownAsync(TutorialDocumentDefinition document, bool forceRefresh)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, document.GetRequestUrl(forceRefresh)))
            {
                if (forceRefresh)
                {
                    request.Headers.CacheControl = new CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true,
                        MaxAge = TimeSpan.Zero
                    };
                    request.Headers.Pragma.ParseAdd("no-cache");
                }

                using (var response = await _tutorialsHttpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
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

                browser.NavigateToString(BuildTutorialPlaceholderHtml(pair.Value.Title, "Đang tải nội dung từ GitHub..."));
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

            TxtTutorialsStatus.Text = "Đang xem: " + GetCurrentTutorialTitle();
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

        private void ClearTutorialSearch()
        {
            _tutorialSearchSuppressTextChanged = true;
            TxtTutorialSearchBox.Text = string.Empty;
            _tutorialSearchSuppressTextChanged = false;
            _tutorialSearchText = string.Empty;
            TxtTutorialSearchStatus.Text = string.Empty;
            InvokeTutorialSearchScript("tutorialSearchClear");
            TxtTutorialSearchBox.Focus();
        }

        private void NavigateTutorialSearch(bool forward)
        {
            if (string.IsNullOrWhiteSpace(TxtTutorialSearchBox.Text))
            {
                TxtTutorialSearchStatus.Text = "Nhập từ khóa cần tìm.";
                return;
            }

            _tutorialSearchText = TxtTutorialSearchBox.Text;
            var scriptName = forward ? "tutorialSearchNext" : "tutorialSearchPrev";
            var result = InvokeTutorialSearchScript(scriptName);
            UpdateTutorialSearchStatus(result);
        }

        private void ApplyTutorialSearch(bool resetIndex)
        {
            _tutorialSearchText = TxtTutorialSearchBox.Text;

            if (string.IsNullOrWhiteSpace(_tutorialSearchText))
            {
                TxtTutorialSearchStatus.Text = string.Empty;
                InvokeTutorialSearchScript("tutorialSearchClear");
                return;
            }

            var result = InvokeTutorialSearchScript(
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

            var selectedShortcutsTab = TutorialShortcutsTabControl.SelectedItem as TabItem;
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

        #endregion

        #region Tutorials - Models

        private sealed class TutorialDocumentDefinition
        {
            public TutorialDocumentDefinition(string title, string blobUrl)
            {
                Title = title;
                BlobUrl = blobUrl;
                RawUrl = blobUrl.Replace("https://github.com/", "https://raw.githubusercontent.com/")
                    .Replace("/blob/", "/");
            }

            public string Title { get; }

            public string BlobUrl { get; }

            public string RawUrl { get; }

            public string GetRequestUrl(bool forceRefresh)
            {
                if (!forceRefresh)
                {
                    return RawUrl;
                }

                return RawUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        #endregion
    }
}
