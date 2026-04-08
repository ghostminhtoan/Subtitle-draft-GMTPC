using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
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
        /// Load toàn bộ thông tin phần cứng khi mở app
        /// </summary>
        private void LoadHardwareInfo()
        {
            if (_isHardwareLoading) return;
            try
            {
                _isHardwareLoading = true;

                TxtGpuInfo.Text = HardwareInfoService.GetGpuInfo();
                TxtCpuInfo.Text = HardwareInfoService.GetCpuInfo();
                TxtRamInfo.Text = HardwareInfoService.GetRamInfo();
                TxtMainboardInfo.Text = HardwareInfoService.GetMainboardInfo();
            }
            catch (Exception ex)
            {
                TxtGpuInfo.Text = string.Format("Lỗi: {0}", ex.Message);
                TxtCpuInfo.Text = string.Format("Lỗi: {0}", ex.Message);
                TxtRamInfo.Text = string.Format("Lỗi: {0}", ex.Message);
                TxtMainboardInfo.Text = string.Format("Lỗi: {0}", ex.Message);
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
                ShowToastHardware("📋 Đã copy GPU info!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnCopyCpu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCpuInfo.Text)) return;
            try
            {
                Clipboard.SetText(TxtCpuInfo.Text);
                ShowToastHardware("📋 Đã copy CPU info!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnCopyRam_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRamInfo.Text)) return;
            try
            {
                Clipboard.SetText(TxtRamInfo.Text);
                ShowToastHardware("📋 Đã copy RAM info!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnCopyMainboard_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMainboardInfo.Text)) return;
            try
            {
                Clipboard.SetText(TxtMainboardInfo.Text);
                ShowToastHardware("📋 Đã copy Mainboard info!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
