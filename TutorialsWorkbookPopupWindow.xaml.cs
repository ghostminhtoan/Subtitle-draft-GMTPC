using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class TutorialsWorkbookPopupWindow : Window
    {
        public TutorialsWorkbookPopupWindow()
        {
            InitializeComponent();
        }

        public string WorkbookUrl { get; set; }

        public WebView2 WorkbookBrowser
        {
            get { return ExcelBrowser; }
        }

        public void ReloadWorkbook()
        {
            if (ExcelBrowser?.CoreWebView2 != null && ExcelBrowser.Source != null)
            {
                ExcelBrowser.CoreWebView2.Reload();
            }
            else if (!string.IsNullOrWhiteSpace(WorkbookUrl))
            {
                ExcelBrowser.Source = new Uri(WorkbookUrl);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var environment = await WebView2EnvironmentProvider.GetEnvironmentAsync();
                await ExcelBrowser.EnsureCoreWebView2Async(environment);
                if (!string.IsNullOrWhiteSpace(WorkbookUrl))
                {
                    ExcelBrowser.Source = new Uri(WorkbookUrl);
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở workbook: " + ex.Message;
            }
        }

        private void ExcelBrowser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            TxtStatus.Text = e.IsSuccess
                ? string.Format("Workbook OneDrive đã sẵn sàng lúc {0:HH:mm:ss}.", DateTime.Now)
                : "Không thể tải workbook từ OneDrive.";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner = null;
        }
    }
}
