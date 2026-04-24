using System;
using System.Threading.Tasks;
using System.Windows;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Hardware Info - Fields

        private bool _isHardwareLoading = false;

        #endregion

        #region Hardware Info - Methods

        /// <summary>
        /// Load toàn bộ thông tin phần cứng khi mở app.
        /// </summary>
        private async void LoadHardwareInfo()
        {
            if (_isHardwareLoading) return;

            try
            {
                _isHardwareLoading = true;

                TxtGpuInfo.Text = "Đang tải thông tin GPU...";
                TxtCpuInfo.Text = "Đang tải thông tin CPU...";
                TxtRamInfo.Text = "Đang tải thông tin RAM...";
                TxtMainboardInfo.Text = "Đang tải thông tin bo mạch chủ...";

                var hardwareInfo = await Task.Run(() => new
                {
                    Gpu = HardwareInfoService.GetGpuInfo(),
                    Cpu = HardwareInfoService.GetCpuInfo(),
                    Ram = HardwareInfoService.GetRamInfo(),
                    Mainboard = HardwareInfoService.GetMainboardInfo()
                });

                TxtGpuInfo.Text = hardwareInfo.Gpu;
                TxtCpuInfo.Text = hardwareInfo.Cpu;
                TxtRamInfo.Text = hardwareInfo.Ram;
                TxtMainboardInfo.Text = hardwareInfo.Mainboard;
            }
            catch (Exception ex)
            {
                TxtGpuInfo.Text = $"Lỗi: {ex.Message}";
                TxtCpuInfo.Text = $"Lỗi: {ex.Message}";
                TxtRamInfo.Text = $"Lỗi: {ex.Message}";
                TxtMainboardInfo.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                _isHardwareLoading = false;
            }
        }

        #endregion

        #region Hardware Info - Toast

        private async void ShowToastHardware(string message)
        {
            ToastTextHardware.Text = message;
            ToastBorderHardware.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderHardware.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Hardware Info - Copy Buttons

        private void BtnCopyGpu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtGpuInfo.Text)) return;

            try
            {
                Clipboard.SetText(TxtGpuInfo.Text);
                ShowToastHardware("📋 Đã sao chép thông tin GPU.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopyCpu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCpuInfo.Text)) return;

            try
            {
                Clipboard.SetText(TxtCpuInfo.Text);
                ShowToastHardware("📋 Đã sao chép thông tin CPU.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopyRam_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRamInfo.Text)) return;

            try
            {
                Clipboard.SetText(TxtRamInfo.Text);
                ShowToastHardware("📋 Đã sao chép thông tin RAM.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopyMainboard_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMainboardInfo.Text)) return;

            try
            {
                Clipboard.SetText(TxtMainboardInfo.Text);
                ShowToastHardware("📋 Đã sao chép thông tin bo mạch chủ.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
