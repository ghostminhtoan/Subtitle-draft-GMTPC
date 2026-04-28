using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class TutorialsWorkbookPopupWindow : Window
    {
        private string _latestWorkbookHtml;

        public TutorialsWorkbookPopupWindow()
        {
            InitializeComponent();
        }

        public string WorkbookUrl { get; set; }

        public void ReloadWorkbook()
        {
            LoadWorkbookAsync(true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWorkbookAsync(false);
        }

        private void WorkbookBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            TxtStatus.Text = "Workbook hiển thị trực tiếp trong app lúc " + DateTime.Now.ToString("HH:mm:ss") + ".";
        }

        private void WorkbookBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
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
            OpenExternalBrowser();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner = null;
        }

        private void BtnOpenExternalBrowser_Click(object sender, RoutedEventArgs e)
        {
            OpenExternalBrowser();
        }

        private async void LoadWorkbookAsync(bool forceRefresh)
        {
            if (string.IsNullOrWhiteSpace(WorkbookUrl))
            {
                TxtStatus.Text = "Chưa có workbook để mở.";
                return;
            }

            try
            {
                TxtStatus.Text = "Đang tải workbook từ Google Sheets...";
                if (forceRefresh || string.IsNullOrWhiteSpace(_latestWorkbookHtml))
                {
                    _latestWorkbookHtml = await GoogleSheetsWorkbookRenderer.LoadWorkbookHtmlAsync(WorkbookUrl);
                }

                WorkbookBrowser.NavigateToString(_latestWorkbookHtml);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không tải được workbook trong app, đang mở trình duyệt... " + ex.Message;
                OpenExternalBrowser();
            }
        }

        private void OpenExternalBrowser()
        {
            if (string.IsNullOrWhiteSpace(WorkbookUrl))
            {
                TxtStatus.Text = "Chưa có workbook để mở.";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(GoogleSheetsWorkbookRenderer.BuildExternalBrowserUrl(WorkbookUrl))
                {
                    UseShellExecute = true
                });
                TxtStatus.Text = "Đã mở workbook trong trình duyệt mặc định lúc " + DateTime.Now.ToString("HH:mm:ss") + ".";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở trình duyệt: " + ex.Message;
            }
        }
    }
}
