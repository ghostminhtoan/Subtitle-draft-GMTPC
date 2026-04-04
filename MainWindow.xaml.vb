Imports System.Windows
Imports System.Threading.Tasks
Imports System.Text
Imports Microsoft.Win32
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

''' <summary>
''' Cửa sổ chính của ứng dụng xử lý phụ đề
''' Gồm 4 tab: Subtitle Draft, Subtitle Merge, Dialogue only, Translate
''' </summary>
Class MainWindow

#Region "Window Events"

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
    End Sub

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

#Region "Subtitle Draft Fields"

    Private _originalLines As New List(Of SubtitleLine)()
    Private _timeCodeLines As New List(Of SubtitleLine)()
    Private _connectGapLines As New List(Of SubtitleLine)()
    Private _currentFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isUpdating As Boolean = False
    Private _totalShiftMs As Integer = 0

#End Region

#Region "Subtitle Merge Fields"

    Private _engLines As New List(Of SubtitleLine)()
    Private _vietLines As New List(Of SubtitleLine)()
    Private _mergeFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isMergeUpdating As Boolean = False

#End Region

#Region "Dialogue Only Fields"

    Private _dialogueLines As New List(Of SubtitleLine)()
    Private _dialogueFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isDialogueUpdating As Boolean = False

#End Region

#Region "Translate Tab - Fields"

    Private _translateLines As New List(Of SubtitleLine)()
    Private _translateFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isTranslateUpdating As Boolean = False

#End Region

#Region "Toast Notification"

    Private Async Sub ShowToast(message As String)
        ToastText.Text = message
        ToastBorder.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorder.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Event Handlers - Original Panel"

    Private Sub TxtOriginal_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isUpdating Then Return
        Try
            _isUpdating = True
            Dim content = TxtOriginal.Text
            If String.IsNullOrWhiteSpace(content) Then
                _originalLines.Clear()
                _timeCodeLines.Clear()
                _connectGapLines.Clear()
                TxtTimeCode.Text = ""
                TxtConnectGap.Text = ""
                TxtResult.Text = ""
                TxtOriginalFormat.Text = ""
                _currentFormat = SubtitleFormat.Unknown
                Return
            End If
            _currentFormat = SubtitleParser.DetectFormat(content)
            _originalLines = SubtitleParser.Parse(content)
            TxtOriginalFormat.Text = String.Format("({0} - {1} dòng)", _currentFormat.ToString().ToUpper(), _originalLines.Count)
            _timeCodeLines = New List(Of SubtitleLine)()
            For Each line In _originalLines
                _timeCodeLines.Add(line.Clone())
            Next
            UpdateTimeCodeDisplay()
            _connectGapLines = New List(Of SubtitleLine)()
            For Each line In _timeCodeLines
                _connectGapLines.Add(line.Clone())
            Next
            UpdateConnectGapDisplay()
            UpdateResultDisplay()
        Catch ex As Exception
            TxtOriginalFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isUpdating = False
        End Try
    End Sub

#End Region

#Region "Event Handlers - Time Code Panel"

    Private Sub BtnTimePlus200_Click(sender As Object, e As RoutedEventArgs)
        If _timeCodeLines.Count = 0 Then Return
        _totalShiftMs += 200
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, 200)
        UpdateTimeCodeDisplay()
        _connectGapLines = New List(Of SubtitleLine)()
        For Each line In _timeCodeLines
            _connectGapLines.Add(line.Clone())
        Next
        UpdateConnectGapDisplay()
        UpdateResultDisplay()
        Dim seconds = Math.Abs(_totalShiftMs) / 1000.0
        Dim sign = If(_totalShiftMs >= 0, "+", "-")
        ShowToast(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
    End Sub

    Private Sub BtnTimeMinus200_Click(sender As Object, e As RoutedEventArgs)
        If _timeCodeLines.Count = 0 Then Return
        _totalShiftMs -= 200
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, -200)
        UpdateTimeCodeDisplay()
        _connectGapLines = New List(Of SubtitleLine)()
        For Each line In _timeCodeLines
            _connectGapLines.Add(line.Clone())
        Next
        UpdateConnectGapDisplay()
        UpdateResultDisplay()
        Dim seconds = Math.Abs(_totalShiftMs) / 1000.0
        Dim sign = If(_totalShiftMs >= 0, "+", "-")
        ShowToast(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
    End Sub

    Private Sub BtnCopyTimeCode_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtTimeCode.Text) Then Return
        Try
            Clipboard.SetText(TxtTimeCode.Text)
            MessageBox.Show("Đã copy!", "Copy", MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Event Handlers - Connect Gap Panel"

    Private Sub BtnConnectGap_Click(sender As Object, e As RoutedEventArgs)
        If _connectGapLines.Count = 0 Then Return
        _connectGapLines = TimeCodeService.ConnectGap(_connectGapLines)
        UpdateConnectGapDisplay()
        UpdateResultDisplay()
        ShowToast("🔗 Đã connect gap!")
    End Sub

#End Region

#Region "Event Handlers - Result Panel"

    Private Sub BtnTranslate_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://www.syedgakbar.com/projects/dst")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnDonate_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://tinyurl.com/mmtdonate")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Update Display Methods"

    Private Sub UpdateTimeCodeDisplay()
        If _timeCodeLines.Count = 0 Then
            TxtTimeCode.Text = ""
            Return
        End If
        TxtTimeCode.Text = SubtitleParser.ToText(_timeCodeLines, _currentFormat)
    End Sub

    Private Sub UpdateConnectGapDisplay()
        If _connectGapLines.Count = 0 Then
            TxtConnectGap.Text = ""
            Return
        End If
        TxtConnectGap.Text = SubtitleParser.ToText(_connectGapLines, _currentFormat)
    End Sub

    Private Sub UpdateResultDisplay()
        If _connectGapLines.Count > 0 Then
            TxtResult.Text = SubtitleParser.ToText(_connectGapLines, _currentFormat)
        ElseIf _timeCodeLines.Count > 0 Then
            TxtResult.Text = SubtitleParser.ToText(_timeCodeLines, _currentFormat)
        Else
            TxtResult.Text = ""
        End If
    End Sub

#End Region

#Region "Dialogue Only - Update Display Methods"

    Private Sub TxtDialogueInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isDialogueUpdating Then Return
        Try
            _isDialogueUpdating = True
            Dim content = TxtDialogueInput.Text
            If String.IsNullOrWhiteSpace(content) Then
                _dialogueLines.Clear()
                _dialogueFormat = SubtitleFormat.Unknown
                TxtDialogueFormat.Text = ""
                TxtDialogueOutput.Text = ""
                Return
            End If
            _dialogueFormat = SubtitleParser.DetectFormat(content)
            _dialogueLines = SubtitleParser.Parse(content)
            TxtDialogueFormat.Text = String.Format("({0} - {1} dòng)", _dialogueFormat.ToString().ToUpper(), _dialogueLines.Count)
            UpdateDialogueOutput()
        Catch ex As Exception
            TxtDialogueFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isDialogueUpdating = False
        End Try
    End Sub

    Private Sub UpdateDialogueOutput()
        If _dialogueLines.Count = 0 Then
            TxtDialogueOutput.Text = ""
            Return
        End If
        Dim sb = New StringBuilder()
        Dim lineNum As Integer = 1
        For Each line In _dialogueLines
            Dim dialogueText = GetDialogueText(line)
            If Not String.IsNullOrWhiteSpace(dialogueText) Then
                Dim cleanText = dialogueText.Replace(Environment.NewLine, " ").Replace(vbCr, " ").Replace(vbLf, " ")
                sb.AppendLine(String.Format("{0}	{1}", lineNum, cleanText))
                lineNum += 1
            End If
        Next
        TxtDialogueOutput.Text = sb.ToString().TrimEnd()
    End Sub

    Private Function GetDialogueText(line As SubtitleLine) As String
        Dim assLine = TryCast(line, AssSubtitleLine)
        If assLine IsNot Nothing Then Return assLine.DialogText
        Dim srtLine = TryCast(line, SrtSubtitleLine)
        If srtLine IsNot Nothing Then Return srtLine.Text
        Return line.OriginalText
    End Function

#End Region

#Region "Dialogue Only - Toast & Buttons"

    Private Async Sub ShowToastDialogue(message As String)
        ToastTextDialogue.Text = message
        ToastBorderDialogue.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderDialogue.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyDialogue_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtDialogueOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtDialogueOutput.Text)
            ShowToastDialogue("📋 Đã copy Dialogue!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Translate Tab - Event Handlers"

    Private Sub TxtTranslateInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isTranslateUpdating Then Return
        Try
            _isTranslateUpdating = True
            Dim content = TxtTranslateInput.Text
            If String.IsNullOrWhiteSpace(content) Then
                _translateLines.Clear()
                _translateFormat = SubtitleFormat.Unknown
                TxtTranslateInputFormat.Text = ""
                Return
            End If
            _translateFormat = SubtitleParser.DetectFormat(content)
            _translateLines = SubtitleParser.Parse(content)
            TxtTranslateInputFormat.Text = String.Format("({0} - {1} dòng)", _translateFormat.ToString().ToUpper(), _translateLines.Count)
        Catch ex As Exception
            TxtTranslateInputFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isTranslateUpdating = False
        End Try
    End Sub

    Private Sub TxtPrompt_LostFocus(sender As Object, e As RoutedEventArgs) Handles TxtPrompt.LostFocus
        SaveSettings()
    End Sub

#End Region

#Region "Translate Tab - Button Handlers"

    Private Sub BtnOpenQwen_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://chat.qwen.ai/")
            TxtTranslateStatus.Text = "✅ Đã mở chat.qwen.ai trên trình duyệt!"
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyForPaste_Click(sender As Object, e As RoutedEventArgs)
        Dim inputText = TxtTranslateInput.Text
        If String.IsNullOrWhiteSpace(inputText) Then
            TxtTranslateStatus.Text = "⚠️ Vui lòng nhập phụ đề vào Panel 1!"
            Return
        End If
        Dim prompt = TxtPrompt.Text.Trim()
        If String.IsNullOrWhiteSpace(prompt) Then
            TxtTranslateStatus.Text = "⚠️ Vui lòng nhập prompt!"
            Return
        End If
        SaveSettings()
        Clipboard.SetText(prompt & vbCrLf & vbCrLf & inputText)
        TxtTranslateStatus.Text = "📋 Đã copy Prompt + Subtitle vào clipboard!"
    End Sub

#End Region

#Region "Merge Tab - Toast"

    Private Async Sub ShowToastMerge(message As String)
        ToastTextMerge.Text = message
        ToastBorderMerge.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderMerge.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Merge Tab - Event Handlers"

    Private Sub TxtEngsub_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isMergeUpdating Then Return
        Try
            _isMergeUpdating = True
            Dim content = TxtEngsub.Text
            If String.IsNullOrWhiteSpace(content) Then
                _engLines.Clear()
                UpdateMergeDisplays()
                Return
            End If
            Dim detectedFormat = SubtitleParser.DetectFormat(content)
            If _mergeFormat = SubtitleFormat.Unknown AndAlso detectedFormat <> SubtitleFormat.Unknown Then
                _mergeFormat = detectedFormat
            End If
            _engLines = SubtitleParser.Parse(content)
            TxtEngsubFormat.Text = String.Format("({0} - {1} dòng)", detectedFormat.ToString().ToUpper(), _engLines.Count)
            UpdateMergeDisplays()
        Catch ex As Exception
            TxtEngsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isMergeUpdating = False
        End Try
    End Sub

    Private Sub TxtVietsub_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isMergeUpdating Then Return
        Try
            _isMergeUpdating = True
            Dim content = TxtVietsub.Text
            If String.IsNullOrWhiteSpace(content) Then
                _vietLines.Clear()
                UpdateMergeDisplays()
                Return
            End If
            Dim detectedFormat = SubtitleParser.DetectFormat(content)
            If _mergeFormat = SubtitleFormat.Unknown AndAlso detectedFormat <> SubtitleFormat.Unknown Then
                _mergeFormat = detectedFormat
            End If
            _vietLines = SubtitleParser.Parse(content)
            TxtVietsubFormat.Text = String.Format("({0} - {1} dòng)", detectedFormat.ToString().ToUpper(), _vietLines.Count)
            UpdateMergeDisplays()
        Catch ex As Exception
            TxtVietsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isMergeUpdating = False
        End Try
    End Sub

    Private Sub UpdateMergeDisplays()
        Dim mergedLines = MergeService.MergeSubtitles(_engLines, _vietLines, _mergeFormat)
        TxtMerge.Text = If(mergedLines.Count > 0, SubtitleParser.ToText(mergedLines, _mergeFormat), "")
        Dim unbreakLines = MergeService.MergeUnbreak(_engLines, _vietLines, _mergeFormat)
        TxtMergeUnbreak.Text = If(unbreakLines.Count > 0, SubtitleParser.ToText(unbreakLines, _mergeFormat), "")
    End Sub

    Private Sub BtnCopyMerge_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMerge.Text) Then Return
        Try
            Clipboard.SetText(TxtMerge.Text)
            ShowToastMerge("📋 Đã copy Merge!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyMergeUnbreak_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMergeUnbreak.Text) Then Return
        Try
            Clipboard.SetText(TxtMergeUnbreak.Text)
            ShowToastMerge("📋 Đã copy Unbreak!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

End Class
