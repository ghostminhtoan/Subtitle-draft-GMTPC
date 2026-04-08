using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Win32;
using Subtitle_draft_GMTPC.Models;
using Subtitle_draft_GMTPC.Services;

namespace Subtitle_draft_GMTPC
{
    /// <summary>
    /// Global events và settings management
    /// </summary>
    public partial class MainWindow : Window
    {

#region "Window Initialization"

    public MainWindow()
    {
        InitializeComponent();
        SetBrowserEmulation();
    }

    private void SetBrowserEmulation()
    {
        try
        {
            var appName = System.AppDomain.CurrentDomain.FriendlyName;
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key?.SetValue(appName, 11001, RegistryValueKind.DWord);
            }
        }
        catch
        {
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
        InitFontSizes();
        LoadHardwareInfo();
        InitializeDefaultPrompts();
        LoadAllPrompts();
        InitializeEffectDebounce();
    }

#endregion

#region "Global Events - Window"

    /// <summary>
    /// Initialize font sizes 1-20 in ComboBox
    /// </summary>
    private void InitFontSizes()
    {
        CmbFontSize.Items.Clear();
        for (int i = 1; i <= 20; i++)
        {
            CmbFontSize.Items.Add(i.ToString());
        }
        CmbFontSize.SelectedIndex = 0; // Default to 1
    }

    /// <summary>
    /// Toggle Word Wrap: Bật/tắt TextWrapping cho tất cả TextBox
    /// </summary>
    private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
    {
        var wrap = (ToggleWordWrap.IsChecked == true) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ToggleWordWrap.Content = (ToggleWordWrap.IsChecked == true) ? "ON" : "OFF";

        // Áp dụng cho tất cả TextBox
        var textBoxes = new[] { TxtEngsub, TxtVietsub, TxtMerge, TxtMergeUnbreak,
                         TxtOriginal, TxtTimeCode, TxtConnectGap, TxtResult,
                         TxtDialogueInput, TxtDialogueOutput, TxtDialogueManual, TxtDialogueMerged,
                         TxtStylesInput, TxtStylesOutput,
                         TxtTranslateInput, TxtPrompt,
                         TxtKaraokeInput, TxtKaraokeEditable,
                         TxtZeroInput, TxtZeroOutput,
                         TxtKaraokeEngInput, TxtKaraokeEngEditable,
                         TxtKaraokeMergeInput, TxtKaraokeMergeOutput,
                         TxtSyncInput, TxtSyncOutput,
                         TxtSearchFontsInput,
                         TxtGpuInfo, TxtCpuInfo, TxtRamInfo, TxtMainboardInfo };

        foreach (var tb in textBoxes)
        {
            if (tb != null)
            {
                tb.TextWrapping = wrap;
            }
        }
    }

    /// <summary>
    /// Ctrl+Scroll: Phóng to/thu nhỏ font chữ trong TextBox
    /// </summary>
    private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        var tb = sender as TextBox;
        if (tb == null) return;

        var currentSize = tb.FontSize;
        var delta = e.Delta > 0 ? 1 : -1;
        var newSize = currentSize + delta;

        // Giới hạn font size từ 8 đến 48
        if (newSize >= 8 && newSize <= 48)
        {
            tb.FontSize = newSize;
        }

        e.Handled = true;
    }

    private void BtnDonate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start("https://tinyurl.com/gmtpcdonate");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

#endregion

#region "Global Events - Settings"

    private void LoadSettings()
    {
        try
        {
            if (!string.IsNullOrEmpty(AppSettings.TranslatePrompt))
            {
                TxtPrompt.Text = AppSettings.TranslatePrompt;
            }
        }
        catch
        {
        }
    }

    private void SaveSettings()
    {
        try
        {
            AppSettings.TranslatePrompt = TxtPrompt.Text.Trim();
            AppSettings.Save();
        }
        catch
        {
        }
    }

#endregion

    }
}
