Imports System.Windows
Imports System.Threading.Tasks
Imports System.Text
Imports Microsoft.Win32
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services
Imports System.Drawing
Imports System.Windows.Media

''' <summary>
''' Global events và settings management
''' </summary>
Partial Class MainWindow

#Region "Window Initialization"

    Public Sub New()
        InitializeComponent()
        SetBrowserEmulation()
    End Sub

    Private Sub SetBrowserEmulation()
        Try
            Dim appName = System.AppDomain.CurrentDomain.FriendlyName
            Using key = Registry.CurrentUser.CreateSubKey("Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION")
                key.SetValue(appName, 11001, RegistryValueKind.DWord)
            End Using
        Catch
        End Try
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        LoadSettings()
        InitFontSizes()
        LoadHardwareInfo()
        InitializeDefaultPrompts()
        LoadAllPrompts()
    End Sub

#End Region

#Region "Global Events - Window"

    ''' <summary>
    ''' Initialize font sizes 1-20 in ComboBox
    ''' </summary>
    Private Sub InitFontSizes()
        CmbFontSize.Items.Clear()
        For i As Integer = 1 To 20
            CmbFontSize.Items.Add(i.ToString())
        Next
        CmbFontSize.SelectedIndex = 0 ' Default to 1
    End Sub

    ''' <summary>
    ''' Toggle Word Wrap: Bật/tắt TextWrapping cho tất cả TextBox
    ''' </summary>
    Private Sub ToggleWordWrap_Click(sender As Object, e As RoutedEventArgs)
        Dim wrap = If(ToggleWordWrap.IsChecked = True, TextWrapping.Wrap, TextWrapping.NoWrap)
        ToggleWordWrap.Content = If(ToggleWordWrap.IsChecked = True, "ON", "OFF")

        ' Áp dụng cho tất cả TextBox
        Dim textBoxes = {TxtEngsub, TxtVietsub, TxtMerge, TxtMergeUnbreak,
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
                         TxtGpuInfo, TxtCpuInfo, TxtRamInfo, TxtMainboardInfo}

        For Each tb In textBoxes
            If tb IsNot Nothing Then
                tb.TextWrapping = wrap
            End If
        Next
    End Sub

    ''' <summary>
    ''' Ctrl+Scroll: Phóng to/thu nhỏ font chữ trong TextBox
    ''' </summary>
    Private Sub TextBox_PreviewMouseWheel(sender As Object, e As MouseWheelEventArgs)
        If Keyboard.Modifiers <> ModifierKeys.Control Then Return

        Dim tb = TryCast(sender, TextBox)
        If tb Is Nothing Then Return

        Dim currentSize = tb.FontSize
        Dim delta = If(e.Delta > 0, 1, -1)
        Dim newSize = currentSize + delta

        ' Giới hạn font size từ 8 đến 48
        If newSize >= 8 AndAlso newSize <= 48 Then
            tb.FontSize = newSize
        End If

        e.Handled = True
    End Sub

    Private Sub BtnDonate_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://tinyurl.com/gmtpcdonate")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Global Events - Settings"

    Private Sub LoadSettings()
        Try
            If Not String.IsNullOrEmpty(My.Settings.TranslatePrompt) Then
                TxtPrompt.Text = My.Settings.TranslatePrompt
            End If
        Catch
        End Try
    End Sub

    Private Sub SaveSettings()
        Try
            My.Settings.TranslatePrompt = TxtPrompt.Text.Trim()
            My.Settings.Save()
        Catch
        End Try
    End Sub

#End Region

End Class
