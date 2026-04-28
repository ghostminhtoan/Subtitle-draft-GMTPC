using System;
using System.Diagnostics;
using System.Windows;

namespace Subtitle_draft_GMTPC
{
    public partial class TutorialsWorkbookPopupWindow : Window
    {
        public TutorialsWorkbookPopupWindow()
        {
            InitializeComponent();
        }

        public string WorkbookUrl { get; set; }

        public void ReloadWorkbook()
        {
            OpenWorkbookExternalBrowser();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWorkbookExternalBrowser();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở workbook: " + ex.Message;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner = null;
        }

        private void BtnOpenExternalBrowser_Click(object sender, RoutedEventArgs e)
        {
            OpenWorkbookExternalBrowser();
        }

        private void OpenWorkbookExternalBrowser()
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
                TxtStatus.Text = "Đã mở workbook trong trình duyệt mặc định lúc " + DateTime.Now.ToString("HH:mm:ss") + ".";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Không thể mở trình duyệt: " + ex.Message;
            }
        }

        private static string BuildViewerUrl(string workbookUrl)
        {
            return "https://view.officeapps.live.com/op/view.aspx?src=" + Uri.EscapeDataString(workbookUrl ?? string.Empty);
        }
    }
}
