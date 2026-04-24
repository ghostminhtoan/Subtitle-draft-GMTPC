using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {
        #region ASS Font Adjust - Fields

        private bool _isStylesUpdating = false;

        #endregion

        #region ASS Font Adjust - Event Handlers

        /// <summary>
        /// Khi nhập styles vào Panel 1 thì tự động lọc đúng block styles và cập nhật Panel 2.
        /// </summary>
        private void TxtStylesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isStylesUpdating) return;

            try
            {
                _isStylesUpdating = true;

                var sanitizedInput = SubtitleParser.SanitizeContent(TxtStylesInput.Text);
                var stylesOnlyInput = ExtractStylesOnlyInput(sanitizedInput);

                if (!string.Equals(TxtStylesInput.Text, stylesOnlyInput, StringComparison.Ordinal))
                {
                    TxtStylesInput.Text = stylesOnlyInput;
                    TxtStylesInput.CaretIndex = TxtStylesInput.Text.Length;
                }

                UpdateStylesOutputWithText(stylesOnlyInput);
            }
            catch (Exception ex)
            {
                TxtStylesCount.Text = $"(Lỗi: {ex.Message})";
            }
            finally
            {
                _isStylesUpdating = false;
            }
        }

        /// <summary>
        /// Button - : Giảm font size
        /// </summary>
        private void BtnFontMinus_Click(object sender, RoutedEventArgs e)
        {
            var idx = CmbFontSize.SelectedIndex;
            if (idx > 0)
            {
                CmbFontSize.SelectedIndex = idx - 1;
            }
        }

        /// <summary>
        /// Button + : Tăng font size
        /// </summary>
        private void BtnFontPlus_Click(object sender, RoutedEventArgs e)
        {
            var idx = CmbFontSize.SelectedIndex;
            if (idx < CmbFontSize.Items.Count - 1)
            {
                CmbFontSize.SelectedIndex = idx + 1;
            }
        }

        /// <summary>
        /// ComboBox font size thay đổi thì cập nhật output.
        /// </summary>
        private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStylesOutput();
        }

        /// <summary>
        /// Button Apply Font Change for All Styles
        /// </summary>
        private void BtnApplyFont_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newFontName = TxtFontName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(newFontName))
                {
                    ShowToastStyles("⚠️ Vui lòng nhập tên font!");
                    TxtFontName.Focus();
                    return;
                }

                UpdateStylesOutputWithFontChange(newFontName);
                ShowToastStyles("🔄 Đã thay đổi font thành: " + newFontName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Button Copy Styles
        /// </summary>
        private void BtnCopyStyles_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtStylesOutput.Text)) return;

            try
            {
                Clipboard.SetText(TxtStylesOutput.Text);
                ShowToastStyles("📋 Đã copy styles!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ASS Font Adjust - Update Methods

        /// <summary>
        /// Cập nhật output styles với font size mới.
        /// </summary>
        private void UpdateStylesOutput()
        {
            UpdateStylesOutputWithText(TxtStylesInput.Text);
        }

        private void UpdateStylesOutputWithText(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                TxtStylesOutput.Text = string.Empty;
                TxtStylesCount.Text = string.Empty;
                return;
            }

            int fontSizeDelta = 0;
            if (CmbFontSize.SelectedIndex >= 0)
            {
                fontSizeDelta = CmbFontSize.SelectedIndex + 1;
            }

            var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            int styleCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(',');
                    if (parts.Length >= 3)
                    {
                        int currentFontSize;
                        if (int.TryParse(parts[2].Trim(), out currentFontSize))
                        {
                            var newFontSize = currentFontSize + fontSizeDelta;
                            if (newFontSize < 1) newFontSize = 1;
                            parts[2] = newFontSize.ToString();
                            styleCount++;
                        }
                    }

                    sb.AppendLine(string.Join(",", parts));
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
            }

            TxtStylesOutput.Text = sb.ToString().TrimEnd();
            TxtStylesCount.Text = $"({styleCount} styles, +{fontSizeDelta}px)";
        }

        /// <summary>
        /// Cập nhật output styles với font name mới.
        /// </summary>
        private void UpdateStylesOutputWithFontChange(string newFontName)
        {
            var inputText = TxtStylesInput.Text;
            if (string.IsNullOrWhiteSpace(inputText)) return;

            int fontSizeDelta = 0;
            if (CmbFontSize.SelectedIndex >= 0)
            {
                fontSizeDelta = CmbFontSize.SelectedIndex + 1;
            }

            var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            int styleCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(',');
                    if (parts.Length >= 3)
                    {
                        parts[1] = newFontName;

                        int currentFontSize;
                        if (int.TryParse(parts[2].Trim(), out currentFontSize))
                        {
                            var newFontSize = currentFontSize + fontSizeDelta;
                            if (newFontSize < 1) newFontSize = 1;
                            parts[2] = newFontSize.ToString();
                        }

                        styleCount++;
                    }

                    sb.AppendLine(string.Join(",", parts));
                }
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
            }

            TxtStylesOutput.Text = sb.ToString().TrimEnd();
            TxtStylesCount.Text = $"({styleCount} styles, font: {newFontName})";
        }

        private string ExtractStylesOnlyInput(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return string.Empty;
            }

            var lines = inputText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            var sb = new StringBuilder();
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
                        sb.AppendLine(line);
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
                        sb.AppendLine(line);

                        if (line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                        {
                            hasStyleLines = true;
                        }
                    }

                    continue;
                }

                if (!foundStylesSection &&
                    (line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Style :", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Format:", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine(line);

                    if (line.StartsWith("Style:", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("Style :", StringComparison.OrdinalIgnoreCase))
                    {
                        hasStyleLines = true;
                    }
                }
            }

            if (hasStyleLines)
            {
                return sb.ToString().TrimEnd();
            }

            return string.Empty;
        }

        #endregion

        #region ASS Font Adjust - Toast

        private async void ShowToastStyles(string message)
        {
            ToastTextStyles.Text = message;
            ToastBorderStyles.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            ToastBorderStyles.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
