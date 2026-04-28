using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Subtitle_draft_GMTPC
{
    public partial class TutorialsWorkbookPopupWindow : Window
    {
        public TutorialsWorkbookPopupWindow()
        {
            InitializeComponent();
        }

        public string WorkbookUrl { get; set; }

        public WebBrowser WorkbookBrowser
        {
            get { return ExcelBrowser; }
        }

        public void ReloadWorkbook()
        {
            NavigateWorkbook();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateWorkbook();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở workbook: " + ex.Message;
            }
        }

        private void ExcelBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            TxtStatus.Text = "Workbook OneDrive đã sẵn sàng lúc " + DateTime.Now.ToString("HH:mm:ss") + ".";
        }

        private void ExcelBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
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

            var expected = BuildViewerUrl(WorkbookUrl);
            if (string.Equals(target, expected, StringComparison.OrdinalIgnoreCase))
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
                TxtStatus.Text = "Không thể mở liên kết: " + ex.Message;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner = null;
        }

        private void BtnOpenExternalBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WorkbookUrl))
            {
                TxtStatus.Text = "Chưa có workbook để mở.";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(BuildViewerUrl(WorkbookUrl))
                {
                    UseShellExecute = true
                });
                TxtStatus.Text = "Đã mở workbook trong trình duyệt mặc định.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở trình duyệt: " + ex.Message;
            }
        }

        private void NavigateWorkbook()
        {
            if (string.IsNullOrWhiteSpace(WorkbookUrl))
            {
                TxtStatus.Text = "Chưa có workbook để mở.";
                return;
            }

            ExcelBrowser.Navigate(BuildViewerUrl(WorkbookUrl));
        }

        private static string BuildViewerUrl(string workbookUrl)
        {
            return "https://view.officeapps.live.com/op/view.aspx?src=" + Uri.EscapeDataString(workbookUrl ?? string.Empty);
        }
    }
}
