using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    public partial class MainWindow : Window
    {

#region "ASS Font Adjust - Fields"

    private bool _isStylesUpdating = false;

#endregion

#region "ASS Font Adjust - Event Handlers"

    /// <summary>
    /// Khi nhập styles vào Panel 1 → tự động cập nhật Panel 2
    /// </summary>
    private void TxtStylesInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isStylesUpdating) return;
        try
        {
            _isStylesUpdating = true;
            var inputText = SubtitleParser.SanitizeContent(TxtStylesInput.Text);
            UpdateStylesOutputWithText(inputText);
        }
        catch (Exception ex)
        {
            TxtStylesCount.Text = string.Format("(Lỗi: {0})", ex.Message);
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
    /// ComboBox font size thay đổi → cập nhật output
    /// </summary>
    private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateStylesOutput();
    }

    /// <summary>
    /// Button - Apply Font Change for All Styles
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
            ShowToastStyles("\ud83d\udd04 Đã thay đổi font thành: " + newFontName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            ShowToastStyles("\ud83d\udccb Đã copy styles!");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

#endregion

#region "ASS Font Adjust - Update Methods"

    /// <summary>
    /// Cập nhật output styles với font size mới
    /// </summary>
    private void UpdateStylesOutput()
    {
        UpdateStylesOutputWithText(TxtStylesInput.Text);
    }

    private void UpdateStylesOutputWithText(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            TxtStylesOutput.Text = "";
            TxtStylesCount.Text = "";
            return;
        }

        // Lấy giá trị font size tăng thêm từ ComboBox
        int fontSizeDelta = 0;
        if (CmbFontSize.SelectedIndex >= 0)
        {
            fontSizeDelta = CmbFontSize.SelectedIndex + 1; // Index 0 = font size 1
        }

        // Parse và xử lý từng dòng Style
        var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        int styleCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Style:") || trimmed.StartsWith("Style :"))
            {
                // Format: Style: Name,Fontname,Fontsize,...
                // Tách bằng dấu phẩy, field 3 (index 2) là Fontsize
                var parts = trimmed.Split(',');
                if (parts.Length >= 3)
                {
                    // Tìm vị trí field font size (sau "Style:" hoặc "Style :")
                    // parts(0) = "Style: Name" hoặc "Style : Name"
                    // parts(1) = Fontname
                    // parts(2) = Fontsize
                    int currentFontSize = 0;
                    if (int.TryParse(parts[2].Trim(), out currentFontSize))
                    {
                        var newFontSize = currentFontSize + fontSizeDelta;
                        if (newFontSize < 1) newFontSize = 1;
                        parts[2] = newFontSize.ToString();
                        styleCount += 1;
                    }
                }
                sb.AppendLine(string.Join(",", parts));
            }
            else
            {
                // Dòng không phải Style → giữ nguyên
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
            }
        }

        TxtStylesOutput.Text = sb.ToString().TrimEnd();
        TxtStylesCount.Text = string.Format("({0} styles, +{1}px)", styleCount, fontSizeDelta);
    }

    /// <summary>
    /// Cập nhật output styles với font name mới
    /// </summary>
    private void UpdateStylesOutputWithFontChange(string newFontName)
    {
        var inputText = TxtStylesInput.Text;
        if (string.IsNullOrWhiteSpace(inputText)) return;

        // Lấy giá trị font size tăng thêm từ ComboBox (để giữ nguyên khi thay đổi font)
        int fontSizeDelta = 0;
        if (CmbFontSize.SelectedIndex >= 0)
        {
            fontSizeDelta = CmbFontSize.SelectedIndex + 1;
        }

        // Parse và xử lý từng dòng Style
        var lines = inputText.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        int styleCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Style:") || trimmed.StartsWith("Style :"))
            {
                // Format: Style: Name,Fontname,Fontsize,...
                var parts = trimmed.Split(',');
                if (parts.Length >= 3)
                {
                    // parts(0) = "Style: Name" hoặc "Style : Name"
                    // parts(1) = Fontname ← Thay đổi field này
                    // parts(2) = Fontsize
                    parts[1] = newFontName;

                    // Áp dụng font size delta nếu có
                    int currentFontSize = 0;
                    if (int.TryParse(parts[2].Trim(), out currentFontSize))
                    {
                        var newFontSize = currentFontSize + fontSizeDelta;
                        if (newFontSize < 1) newFontSize = 1;
                        parts[2] = newFontSize.ToString();
                    }

                    styleCount += 1;
                }
                sb.AppendLine(string.Join(",", parts));
            }
            else
            {
                // Dòng không phải Style → giữ nguyên
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
            }
        }

        TxtStylesOutput.Text = sb.ToString().TrimEnd();
        TxtStylesCount.Text = string.Format("({0} styles, font: {1})", styleCount, newFontName);
    }

#endregion

#region "ASS Font Adjust - Toast"

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
