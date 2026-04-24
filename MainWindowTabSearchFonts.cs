using System;
using System.Collections.Generic;
using System.Drawing;
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

        private HashSet<string> _windowsFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<string> _missingFonts = new List<string>();
        private bool _isSearchFontsUpdating = false;

        #endregion

        #region Search Fonts - Event Handlers

        private void TxtSearchFontsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSearchFontsUpdating)
            {
                return;
            }

            try
            {
                _isSearchFontsUpdating = true;

                var inputText = SubtitleParser.SanitizeContent(TxtSearchFontsInput.Text);
                var stylesOnlyInput = ExtractSearchFontsStylesOnlyInput(inputText);

                if (!string.Equals(TxtSearchFontsInput.Text, stylesOnlyInput, StringComparison.Ordinal))
                {
                    TxtSearchFontsInput.Text = stylesOnlyInput;
                    TxtSearchFontsInput.CaretIndex = TxtSearchFontsInput.Text.Length;
                }

                ParseStylesAndFindMissingFonts(stylesOnlyInput);
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

        private async void BtnSearchFonts_Click(object sender, RoutedEventArgs e)
        {
            if (_missingFonts.Count == 0)
            {
                ShowToastSearchFonts("Không có font nào thiếu!");
                return;
            }

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

            foreach (var fontName in fontsToSearch)
            {
                try
                {
                    var searchUrl = string.Format(
                        "https://www.google.com/search?q={0}+font+download",
                        System.Net.WebUtility.UrlEncode(fontName));

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = searchUrl,
                        UseShellExecute = true
                    });

                    await Task.Delay(500);
                }
                catch
                {
                }
            }

            ShowToastSearchFonts(string.Format("Đã mở {0} tab search!", fontsToSearch.Count));
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }

            ScrollViewer scrollViewer = null;
            var dependencyObject = (DependencyObject)listBox;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
            {
                var child = VisualTreeHelper.GetChild(dependencyObject, i);
                if (child is ScrollViewer viewer)
                {
                    scrollViewer = viewer;
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

        private void LoadWindowsFonts()
        {
            _windowsFonts.Clear();
            try
            {
                using (Graphics.FromHwnd(IntPtr.Zero))
                {
                    foreach (var family in System.Drawing.FontFamily.Families)
                    {
                        _windowsFonts.Add(family.Name);
                    }
                }
            }
            catch
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
                    {
                        if (key == null)
                        {
                            return;
                        }

                        foreach (var valueName in key.GetValueNames())
                        {
                            _windowsFonts.Add(valueName.Replace(" (TrueType)", string.Empty).Replace(" (OpenType)", string.Empty).Trim());
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void ParseStylesAndFindMissingFonts(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                LstMissingFonts.Items.Clear();
                TxtStylesFontCount.Text = string.Empty;
                _missingFonts.Clear();
                return;
            }

            if (_windowsFonts.Count == 0)
            {
                LoadWindowsFonts();
            }

            var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            var fontNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int styleCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) &&
                    !trimmed.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                styleCount++;
                var parts = trimmed.Split(',');
                if (parts.Length < 3)
                {
                    continue;
                }

                var fontName = parts[1].Trim();
                if (!string.IsNullOrWhiteSpace(fontName))
                {
                    fontNames.Add(fontName);
                }
            }

            _missingFonts.Clear();
            LstMissingFonts.Items.Clear();

            foreach (var fontName in fontNames)
            {
                if (_windowsFonts.Contains(fontName))
                {
                    continue;
                }

                _missingFonts.Add(fontName);
                LstMissingFonts.Items.Add(fontName);
            }

            TxtStylesFontCount.Text = string.Format("({0} styles - {1} font thiếu)", styleCount, _missingFonts.Count);

            if (_missingFonts.Count > 0)
            {
                ShowToastSearchFonts(string.Format("Tìm thấy {0} font không có trên Windows!", _missingFonts.Count));
            }
        }

        private string ExtractSearchFontsStylesOnlyInput(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return string.Empty;
            }

            var lines = inputText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var outputLines = new List<string>();
            bool foundStylesSection = false;
            bool inStylesSection = false;
            bool hasStyleLines = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    if (line.Equals("[V4+ Styles]", StringComparison.OrdinalIgnoreCase) ||
                        line.Equals("[V4 Styles]", StringComparison.OrdinalIgnoreCase))
                    {
                        foundStylesSection = true;
                        inStylesSection = true;
                        outputLines.Add(line);
                        continue;
                    }

                    if (inStylesSection)
                    {
                        break;
                    }
                }

                if (inStylesSection)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                    {
                        outputLines.Add(line);
                        if (line.StartsWith("Style", StringComparison.OrdinalIgnoreCase))
                        {
                            hasStyleLines = true;
                        }
                    }

                    continue;
                }

                if (!foundStylesSection &&
                    (line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Style :", StringComparison.OrdinalIgnoreCase)))
                {
                    outputLines.Add(line);
                    if (line.StartsWith("Style", StringComparison.OrdinalIgnoreCase))
                    {
                        hasStyleLines = true;
                    }
                }
            }

            return hasStyleLines ? string.Join(Environment.NewLine, outputLines).TrimEnd() : string.Empty;
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
