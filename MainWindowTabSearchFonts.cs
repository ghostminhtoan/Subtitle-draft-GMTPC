using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region Search Fonts - Fields

        // Danh sách các font có sẵn trên Windows
        private HashSet<string> _windowsFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Danh sách các font không có trên Windows
        private List<string> _missingFonts = new List<string>();
        private bool _isSearchFontsUpdating = false;

        #endregion

        #region Search Fonts - Event Handlers

        /// <summary>
        /// Khi nhập styles vào Panel 1 → tự động parse và tìm font thiếu
        /// </summary>
        private void TxtSearchFontsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSearchFontsUpdating) return;
            try
            {
                _isSearchFontsUpdating = true;
                var inputText = SubtitleParser.SanitizeContent(TxtSearchFontsInput.Text);
                ParseStylesAndFindMissingFonts(inputText);
            }
            catch (Exception ex)
            {
                TxtStylesFontCount.Text = string.Format("(Lỗi: {0})", ex.Message);
            }
            finally
            {
                _isSearchFontsUpdating = false;
            }
        }

        /// <summary>
        /// Button Search Fonts: Mở Google search cho các font được chọn (hoặc tất cả)
        /// </summary>
        private async void BtnSearchFonts_Click(object sender, RoutedEventArgs e)
        {
            if (_missingFonts.Count == 0)
            {
                ShowToastSearchFonts("Không có font nào thiếu!");
                return;
            }

            // Lấy danh sách font được chọn, nếu không chọn thì lấy tất cả
            var fontsToSearch = new List<string>();
            if (LstMissingFonts.SelectedItems.Count > 0)
            {
                foreach (var item in LstMissingFonts.SelectedItems)
                {
                    fontsToSearch.Add(item.ToString());
                }
            }
            else
            {
                fontsToSearch.AddRange(_missingFonts);
            }

            if (fontsToSearch.Count == 0)
            {
                ShowToastSearchFonts("Vui lòng chọn ít nhất 1 font!");
                return;
            }

            // Mở Google search cho từng font
            foreach (var fontName in fontsToSearch)
            {
                try
                {
                    var searchUrl = string.Format("https://www.google.com/search?q={0}+font+download", System.Net.WebUtility.UrlEncode(fontName));
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = searchUrl,
                        UseShellExecute = true
                    });
                    await Task.Delay(500); // Delay nhỏ để tránh mở quá nhiều tab cùng lúc
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi khi mở link
                }
            }

            ShowToastSearchFonts(string.Format("Đã mở {0} tab search!", fontsToSearch.Count));
        }

        /// <summary>
        /// ListBox PreviewMouseWheel: Cho phép scroll mà không cần focus
        /// </summary>
        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            ScrollViewer scrollViewer = null;
            var dependencyObject = (DependencyObject)listBox;

            // Tìm ScrollViewer trong ListBox
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, i);
                if (child is ScrollViewer)
                {
                    scrollViewer = (ScrollViewer)child;
                    break;
                }
            }

            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }

        #endregion

        #region Search Fonts - Font Parsing & Detection

        /// <summary>
        /// Lấy danh sách font có sẵn trên Windows
        /// </summary>
        private void LoadWindowsFonts()
        {
            _windowsFonts.Clear();
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var families = System.Drawing.FontFamily.Families;
                    foreach (var family in families)
                    {
                        _windowsFonts.Add(family.Name);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback: đọc từ registry
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
                    {
                        if (key != null)
                        {
                            foreach (var valueName in key.GetValueNames())
                            {
                                _windowsFonts.Add(valueName.Replace(" (TrueType)", "").Replace(" (OpenType)", "").Trim());
                            }
                        }
                    }
                }
                catch
                {
                    // Nếu vẫn lỗi, để trống danh sách
                }
            }
        }

        /// <summary>
        /// Parse styles và tìm font không có trên Windows
        /// </summary>
        private void ParseStylesAndFindMissingFonts(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                LstMissingFonts.Items.Clear();
                TxtStylesFontCount.Text = "";
                _missingFonts.Clear();
                return;
            }

            // Load Windows fonts nếu chưa load
            if (_windowsFonts.Count == 0)
            {
                LoadWindowsFonts();
            }

            // Parse từng dòng Style
            var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            var fontNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int styleCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Style:") || trimmed.StartsWith("Style :"))
                {
                    styleCount++;
                    // Format: Style: Name,Fontname,Fontsize,...
                    var parts = trimmed.Split(',');
                    if (parts.Length >= 3)
                    {
                        // parts(0) = "Style: Name" hoặc "Style : Name"
                        // parts(1) = Fontname
                        var fontName = parts[1].Trim();
                        if (!string.IsNullOrWhiteSpace(fontName))
                        {
                            fontNames.Add(fontName);
                        }
                    }
                }
            }

            // Tìm font không có trên Windows
            _missingFonts.Clear();
            LstMissingFonts.Items.Clear();

            // Kiểm tra xem có font đặc biệt nào không
            bool hasSpecialFonts = false;
            var specialPrefixes = new[] { "MTO", "SFU", "UTM", "UVN", "VNI" };
            var specialFontsFound = new List<string>();

            foreach (var fontName in fontNames)
            {
                // Kiểm tra font đặc biệt
                foreach (var prefix in specialPrefixes)
                {
                    if (fontName.ToUpper().StartsWith(prefix))
                    {
                        hasSpecialFonts = true;
                        if (!specialFontsFound.Contains(fontName, StringComparer.OrdinalIgnoreCase))
                        {
                            specialFontsFound.Add(fontName);
                        }
                        break;
                    }
                }

                // Thêm vào danh sách font thiếu nếu không có trên Windows
                if (!_windowsFonts.Contains(fontName))
                {
                    _missingFonts.Add(fontName);
                    LstMissingFonts.Items.Add(fontName);
                }
            }

            TxtStylesFontCount.Text = string.Format("({0} styles - {1} font thiếu)", styleCount, _missingFonts.Count);

            // Nếu có font đặc biệt, hỏi người dùng có muốn cài đặt không
            if (hasSpecialFonts)
            {
                var specialFontList = string.Join(", ", specialFontsFound);
                var result = System.Windows.MessageBox.Show(
                    string.Format("Phát hiện font đặc biệt: {0}{1}Bạn có muốn tải và cài đặt Gouenji.Fansub.Fonts ngay không?", specialFontList, Environment.NewLine),
                    "Cài đặt Font Pack",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Chạy async với fire-and-forget pattern an toàn
                    Task.Run(() => DownloadAndInstallFontPackAsync());
                }
            }

            // Nếu có font thiếu, hiển thị danh sách
            if (_missingFonts.Count > 0)
            {
                ShowToastSearchFonts(string.Format("Tìm thấy {0} font không có trên Windows!", _missingFonts.Count));
            }
        }

        #endregion

        #region Search Fonts - Font Installation

        /// <summary>
        /// Tải và cài đặt font pack từ GitHub (async version)
        /// </summary>
        private async Task DownloadAndInstallFontPack()
        {
            var tempDir = System.IO.Path.GetTempPath();
            var installerPath = System.IO.Path.Combine(tempDir, "Gouenji.Fansub.Fonts.exe");
            var downloadUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe";

            try
            {
                ShowToastSearchFonts("Đang tải Gouenji.Fansub.Fonts.exe...");

                // Tải file
                using (var client = new System.Net.WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), installerPath);
                }

                if (!System.IO.File.Exists(installerPath))
                {
                    ShowToastSearchFonts("Lỗi: Không tải được file!");
                    return;
                }

                ShowToastSearchFonts("Đang cài đặt font pack...");

                // Chạy installer với /passive
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/passive",
                    UseShellExecute = true
                };

                var installerProcess = System.Diagnostics.Process.Start(psi);
                if (installerProcess != null)
                {
                    // Chờ installer kết thúc
                    await Task.Run(() =>
                    {
                        installerProcess.WaitForExit();
                    });

                    // Xóa file installer sau khi cài xong
                    if (System.IO.File.Exists(installerPath))
                    {
                        try
                        {
                            System.IO.File.Delete(installerPath);
                            ShowToastSearchFonts("✅ Cài đặt font pack thành công!");
                        }
                        catch (Exception)
                        {
                            ShowToastSearchFonts("Cài đặt xong nhưng không xóa được file tạm.");
                        }
                    }
                }
                else
                {
                    ShowToastSearchFonts("Lỗi: Không thể chạy installer!");
                }
            }
            catch (Exception ex)
            {
                ShowToastSearchFonts(string.Format("Lỗi tải/cài đặt: {0}", ex.Message));
                // Cleanup nếu có lỗi
                if (System.IO.File.Exists(installerPath))
                {
                    try
                    {
                        System.IO.File.Delete(installerPath);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Tải và cài đặt font pack từ GitHub (synchronous version for Task.Run)
        /// </summary>
        private void DownloadAndInstallFontPackAsync()
        {
            var tempDir = System.IO.Path.GetTempPath();
            var installerPath = System.IO.Path.Combine(tempDir, "Gouenji.Fansub.Fonts.exe");
            var downloadUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe";

            try
            {
                // Tải file
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(new Uri(downloadUrl), installerPath);
                }

                if (!System.IO.File.Exists(installerPath))
                {
                    // Không thể hiển thị toast từ background thread, bỏ qua
                    return;
                }

                // Chạy installer với /passive
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/passive",
                    UseShellExecute = true
                };

                var installerProcess = System.Diagnostics.Process.Start(psi);
                if (installerProcess != null)
                {
                    // Chờ installer kết thúc
                    installerProcess.WaitForExit();

                    // Xóa file installer sau khi cài xong
                    if (System.IO.File.Exists(installerPath))
                    {
                        try
                        {
                            System.IO.File.Delete(installerPath);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi trong background thread
                if (System.IO.File.Exists(installerPath))
                {
                    try
                    {
                        System.IO.File.Delete(installerPath);
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Search Fonts - Toast

        private async void ShowToastSearchFonts(string message)
        {
            ToastTextSearchFonts.Text = message;
            ToastBorderSearchFonts.Visibility = Visibility.Visible;
            await Task.Delay(2500);
            ToastBorderSearchFonts.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
