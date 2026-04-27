using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Subtitle_draft_GMTPC
{
    public partial class TutorialMarkdownPopupWindow : Window
    {
        public TutorialMarkdownPopupWindow()
        {
            InitializeComponent();
        }

        public string TutorialTitle
        {
            get { return TxtTitle.Text; }
            set { TxtTitle.Text = value; }
        }

        public void ShowHtml(string title, string html)
        {
            TutorialTitle = title ?? "Tutorial";
            TxtStatus.Text = "Đang tải nội dung...";
            MarkdownBrowser.NavigateToString(string.IsNullOrWhiteSpace(html) ? "<html><body></body></html>" : html);
        }

        public WebBrowser Browser
        {
            get { return MarkdownBrowser; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtFooter.Text = "Popup modeless. Ctrl+F / F3 sẽ tìm trong nội dung đang mở.";
        }

        private void MarkdownBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            TxtStatus.Text = "Nội dung đã sẵn sàng lúc " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void MarkdownBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Owner = null;
        }
    }
}
