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
        InitFontSizes()
        LoadHardwareInfo()
        InitializeDefaultPrompts()
        LoadAllPrompts()
    End Sub

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
                         TxtTranslateInput, TxtPrompt}

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

#Region "Hardware Info Fields"

    Private _isHardwareLoading As Boolean = False

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
    Private _dialogueManualTexts As New Dictionary(Of Integer, String)()

#End Region

#Region "Translate Tab - Fields"

    Private _translateLines As New List(Of SubtitleLine)()
    Private _translateFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isTranslateUpdating As Boolean = False

#End Region

#Region "ASS Font Adjust Fields"

    Private _isStylesUpdating As Boolean = False

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
            Dim content = SubtitleParser.SanitizeContent(TxtOriginal.Text)
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

    Private Sub BtnDonate_Click(sender As Object, e As RoutedEventArgs)
        Try
            System.Diagnostics.Process.Start("https://tinyurl.com/gmtpcdonate")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyResult_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtResult.Text) Then Return
        Try
            Clipboard.SetText(TxtResult.Text)
            ShowToast("📋 Đã copy Result!")
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
            Dim content = SubtitleParser.SanitizeContent(TxtDialogueInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                _dialogueLines.Clear()
                _dialogueFormat = SubtitleFormat.Unknown
                TxtDialogueFormat.Text = ""
                TxtDialogueOutput.Text = ""
                TxtDialogueMerged.Text = ""
                Return
            End If
            _dialogueFormat = SubtitleParser.DetectFormat(content)
            _dialogueLines = SubtitleParser.Parse(content)
            TxtDialogueFormat.Text = String.Format("({0} - {1} dòng)", _dialogueFormat.ToString().ToUpper(), _dialogueLines.Count)
            UpdateDialogueOutput()
            UpdateDialogueMerged()
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

    ''' <summary>
    ''' Khi nhập text manual vào Panel 3 → cập nhật Panel 4
    ''' </summary>
    Private Sub TxtDialogueManual_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isDialogueUpdating Then Return
        Try
            _isDialogueUpdating = True
            ParseManualText()
            UpdateDialogueMerged()
        Catch ex As Exception
            TxtDialogueMerged.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isDialogueUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Parse text từ Panel 3: Hỗ trợ 2 format
    ''' Format 1 - 3 cột (ngang): STT&lt;TAB&gt;Text gốc&lt;TAB&gt;Text tiếng Việt
    ''' Format 2 - 3 hàng (dọc): Mỗi nhóm 3 dòng (STT / Text gốc / Text tiếng Việt)
    ''' Luôn luôn lấy text ở vị trí thứ 3 (cột 3 hoặc dòng 3) làm text để merge
    ''' </summary>
    Private Sub ParseManualText()
        _dialogueManualTexts.Clear()
        Dim content = TxtDialogueManual.Text
        If String.IsNullOrWhiteSpace(content) Then Return

        Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        If lines.Length = 0 Then Return

        ' Bước 1: Xác định format (3 cột hay 3 hàng)
        Dim is3ColumnFormat = False

        ' Kiểm tra dòng đầu tiên xem có chứa tab và có >= 3 phần tử không
        If lines.Length > 0 Then
            Dim firstLine = lines(0).Trim()
            If firstLine.Contains(vbTab) Then
                Dim parts = firstLine.Split({vbTab}, StringSplitOptions.None)
                If parts.Length >= 3 Then
                    ' Kiểm tra phần tử đầu có phải là số không
                    Dim stt As Integer = 0
                    If Integer.TryParse(parts(0).Trim(), stt) Then
                        is3ColumnFormat = True
                    End If
                End If
            End If
        End If

        ' Bước 2: Parse theo format đã xác định
        If is3ColumnFormat Then
            ' Format 3 cột: Mỗi dòng là STT<TAB>Text gốc<TAB>Text tiếng Việt
            For Each line In lines
                Dim trimmed = line.Trim()
                If String.IsNullOrWhiteSpace(trimmed) Then Continue For

                If trimmed.Contains(vbTab) Then
                    Dim parts = trimmed.Split({vbTab}, StringSplitOptions.None)
                    If parts.Length >= 3 Then
                        Dim stt As Integer = 0
                        If Integer.TryParse(parts(0).Trim(), stt) Then
                            ' Lấy cột 3 (index 2) làm text
                            _dialogueManualTexts(stt) = parts(2).Trim()
                        End If
                    End If
                End If
            Next
        Else
            ' Format 3 hàng: Mỗi nhóm 3 dòng (STT / Text gốc / Text tiếng Việt)
            Dim groupIndex As Integer = 0
            Dim currentStt As Integer = 0
            Dim hasValidStt As Boolean = False

            For i As Integer = 0 To lines.Length - 1
                Dim trimmed = lines(i).Trim()
                If String.IsNullOrWhiteSpace(trimmed) Then Continue For

                Dim positionInGroup = groupIndex Mod 3

                If positionInGroup = 0 Then
                    ' Dòng 1: STT (số thứ tự)
                    Dim stt As Integer = 0
                    If Integer.TryParse(trimmed, stt) Then
                        currentStt = stt
                        hasValidStt = True
                    Else
                        hasValidStt = False
                    End If
                ElseIf positionInGroup = 1 Then
                    ' Dòng 2: Text gốc (bỏ qua, không cần lưu)
                    ' Không cần làm gì
                ElseIf positionInGroup = 2 Then
                    ' Dòng 3: Text tiếng Việt (lưu lại)
                    If hasValidStt AndAlso currentStt > 0 Then
                        _dialogueManualTexts(currentStt) = trimmed
                    End If
                End If

                groupIndex += 1
            Next
        End If
    End Sub

    ''' <summary>
    ''' Cập nhật Panel 4: Time Code từ Panel 1 + Text từ Panel 3
    ''' Format SRT: STT + Time Code + Text từ Panel 3
    ''' </summary>
    Private Sub UpdateDialogueMerged()
        If _dialogueLines.Count = 0 Then
            TxtDialogueMerged.Text = ""
            Return
        End If

        Dim sb = New StringBuilder()
        Dim entryNum As Integer = 0

        For Each line In _dialogueLines
            Dim dialogueText = GetDialogueText(line)
            If String.IsNullOrWhiteSpace(dialogueText) Then Continue For

            entryNum += 1

            ' Kiểm tra xem có text manual cho STT này không
            Dim manualText As String = Nothing
            If _dialogueManualTexts.TryGetValue(entryNum, manualText) AndAlso Not String.IsNullOrWhiteSpace(manualText) Then
                ' Dùng text từ Panel 3
                If _dialogueFormat = SubtitleFormat.SRT Then
                    sb.AppendLine(entryNum.ToString())
                    sb.AppendLine(String.Format("{0} --> {1}",
                        SubtitleLine.FormatSrtTime(line.StartTime),
                        SubtitleLine.FormatSrtTime(line.EndTime)))
                    sb.AppendLine(manualText)
                    sb.AppendLine()
                ElseIf _dialogueFormat = SubtitleFormat.ASS Then
                    Dim assLine = TryCast(line, AssSubtitleLine)
                    If assLine IsNot Nothing Then
                        ' Giữ nguyên time code, thay thế dialogue text
                        ' Format đúng: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
                        sb.AppendLine(String.Format("Dialogue: {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                            assLine.Layer, SubtitleLine.FormatAssTime(line.StartTime),
                            SubtitleLine.FormatAssTime(line.EndTime), assLine.Style,
                            assLine.Name, assLine.MarginL, assLine.MarginR, assLine.MarginV,
                            assLine.Effect, manualText))
                    End If
                Else
                    ' Format khác: ghi ra dạng time code + text
                    sb.AppendLine(String.Format("{0}", manualText))
                End If
            End If
        Next

        TxtDialogueMerged.Text = sb.ToString().TrimEnd()
    End Sub

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

    Private Sub BtnCopyMerged_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtDialogueMerged.Text) Then Return
        Try
            Clipboard.SetText(TxtDialogueMerged.Text)
            ShowToastDialogue("📋 Đã copy Merged!")
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
            Dim content = SubtitleParser.SanitizeContent(TxtTranslateInput.Text)
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

#Region "ASS Font Adjust - Event Handlers"

    ''' <summary>
    ''' Khi nhập styles vào Panel 1 → tự động cập nhật Panel 2
    ''' </summary>
    Private Sub TxtStylesInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isStylesUpdating Then Return
        Try
            _isStylesUpdating = True
            Dim inputText = SubtitleParser.SanitizeContent(TxtStylesInput.Text)
            UpdateStylesOutputWithText(inputText)
        Catch ex As Exception
            TxtStylesCount.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isStylesUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Button - : Giảm font size
    ''' </summary>
    Private Sub BtnFontMinus_Click(sender As Object, e As RoutedEventArgs)
        Dim idx = CmbFontSize.SelectedIndex
        If idx > 0 Then
            CmbFontSize.SelectedIndex = idx - 1
        End If
    End Sub

    ''' <summary>
    ''' Button + : Tăng font size
    ''' </summary>
    Private Sub BtnFontPlus_Click(sender As Object, e As RoutedEventArgs)
        Dim idx = CmbFontSize.SelectedIndex
        If idx < CmbFontSize.Items.Count - 1 Then
            CmbFontSize.SelectedIndex = idx + 1
        End If
    End Sub

    ''' <summary>
    ''' ComboBox font size thay đổi → cập nhật output
    ''' </summary>
    Private Sub CmbFontSize_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        UpdateStylesOutput()
    End Sub

    ''' <summary>
    ''' Cập nhật output styles với font size mới
    ''' </summary>
    Private Sub UpdateStylesOutput()
        UpdateStylesOutputWithText(TxtStylesInput.Text)
    End Sub

    Private Sub UpdateStylesOutputWithText(inputText As String)
        If String.IsNullOrWhiteSpace(inputText) Then
            TxtStylesOutput.Text = ""
            TxtStylesCount.Text = ""
            Return
        End If

        ' Lấy giá trị font size tăng thêm từ ComboBox
        Dim fontSizeDelta As Integer = 0
        If CmbFontSize.SelectedIndex >= 0 Then
            fontSizeDelta = CmbFontSize.SelectedIndex + 1 ' Index 0 = font size 1
        End If

        ' Parse và xử lý từng dòng Style
        Dim lines = inputText.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.None)
        Dim sb = New StringBuilder()
        Dim styleCount As Integer = 0

        For Each line In lines
            Dim trimmed = line.Trim()
            If trimmed.StartsWith("Style:") OrElse trimmed.StartsWith("Style :") Then
                ' Format: Style: Name,Fontname,Fontsize,...
                ' Tách bằng dấu phẩy, field 3 (index 2) là Fontsize
                Dim parts = trimmed.Split(","c)
                If parts.Length >= 3 Then
                    ' Tìm vị trí field font size (sau "Style:" hoặc "Style :")
                    ' parts(0) = "Style: Name" hoặc "Style : Name"
                    ' parts(1) = Fontname
                    ' parts(2) = Fontsize
                    Dim currentFontSize As Integer = 0
                    If Integer.TryParse(parts(2).Trim(), currentFontSize) Then
                        Dim newFontSize = currentFontSize + fontSizeDelta
                        If newFontSize < 1 Then newFontSize = 1
                        parts(2) = newFontSize.ToString()
                        styleCount += 1
                    End If
                End If
                sb.AppendLine(String.Join(",", parts))
            Else
                ' Dòng không phải Style → giữ nguyên
                If Not String.IsNullOrWhiteSpace(trimmed) Then
                    sb.AppendLine(trimmed)
                End If
            End If
        Next

        TxtStylesOutput.Text = sb.ToString().TrimEnd()
        TxtStylesCount.Text = String.Format("({0} styles, +{1}px)", styleCount, fontSizeDelta)
    End Sub

    ''' <summary>
    ''' Button Copy Styles
    ''' </summary>
    Private Sub BtnCopyStyles_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtStylesOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtStylesOutput.Text)
            ShowToastStyles("📋 Đã copy styles!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Async Sub ShowToastStyles(message As String)
        ToastTextStyles.Text = message
        ToastBorderStyles.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderStyles.Visibility = Visibility.Collapsed
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

    ''' <summary>
    ''' Khi click vào nút prompt → dán nội dung prompt vào TxtPrompt
    ''' </summary>
    Private Sub BtnPrompt_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing Then Return

        Dim promptId As Integer = Integer.Parse(btn.Tag.ToString())
        Dim prompt = GetPromptById(promptId)

        If Not String.IsNullOrWhiteSpace(prompt.Content) Then
            TxtPrompt.Text = prompt.Content
            TxtTranslateStatus.Text = String.Format("📋 Đã load prompt: {0}", prompt.DisplayName)
        Else
            TxtTranslateStatus.Text = String.Format("⚠️ Prompt {0} chưa có nội dung!", promptId)
        End If
    End Sub

    ''' <summary>
    ''' Nút Rename: Cho phép người dùng đổi tên prompt được chọn (dựa vào prompt đang hiển thị trong TxtPrompt)
    ''' </summary>
    Private Sub BtnRenamePrompt_Click(sender As Object, e As RoutedEventArgs)
        ' Tìm prompt đang được hiển thị trong TxtPrompt (nếu có)
        Dim currentPromptId As Integer = FindCurrentPromptId()

        If currentPromptId = 0 Then
            ' Nếu không tìm thấy prompt phù hợp, mặc định cho chọn prompt cần rename
            Dim input = Microsoft.VisualBasic.InputBox("Nhập số thứ tự prompt cần rename (1-5):", "Rename Prompt", "1")
            If Integer.TryParse(input, currentPromptId) AndAlso currentPromptId >= 1 AndAlso currentPromptId <= 5 Then
                ' OK
            Else
                TxtTranslateStatus.Text = "⚠️ Số thứ tự không hợp lệ!"
                Return
            End If
        End If

        Dim currentName = GetPromptNameById(currentPromptId)
        Dim newName = Microsoft.VisualBasic.InputBox(String.Format("Đổi tên cho Prompt {0} (hiện tại: {1}):", currentPromptId, currentName), "Rename Prompt", currentName)

        If Not String.IsNullOrWhiteSpace(newName) Then
            SetPromptNameById(currentPromptId, newName.Trim())
            UpdatePromptButtonDisplay()
            SaveSettings()
            TxtTranslateStatus.Text = String.Format("✅ Đã rename Prompt {0} thành: {1}", currentPromptId, newName.Trim())
        End If
    End Sub

    ''' <summary>
    ''' Nút Save Prompts: Lưu toàn bộ prompt names và content vào Settings
    ''' </summary>
    Private Sub BtnSavePrompts_Click(sender As Object, e As RoutedEventArgs)
        SaveAllPrompts()
        ShowToastTranslate("💾 Đã lưu 5 prompts vào Settings!")
    End Sub

    ''' <summary>
    ''' Nút Load Prompts: Load toàn bộ prompt names và content từ Settings
    ''' </summary>
    Private Sub BtnLoadPrompts_Click(sender As Object, e As RoutedEventArgs)
        LoadAllPrompts()
        ShowToastTranslate("📂 Đã load 5 prompts từ Settings!")
    End Sub

    Private Async Sub ShowToastTranslate(message As String)
        ToastTextTranslate.Text = message
        ToastBorderTranslate.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderTranslate.Visibility = Visibility.Collapsed
    End Sub

#Region "Translate Tab - Prompt Management"

    ''' <summary>
    ''' Lấy thông tin prompt theo ID (1-5)
    ''' </summary>
    Private Function GetPromptById(id As Integer) As PromptItem
        Dim name = GetPromptNameById(id)
        Dim content = GetPromptContentById(id)
        Return New PromptItem(id, name, content)
    End Function

    Private Function GetPromptNameById(id As Integer) As String
        Select Case id
            Case 1 : Return My.Settings.PromptName1
            Case 2 : Return My.Settings.PromptName2
            Case 3 : Return My.Settings.PromptName3
            Case 4 : Return My.Settings.PromptName4
            Case 5 : Return My.Settings.PromptName5
            Case Else : Return String.Format("Prompt {0}", id)
        End Select
    End Function

    Private Function GetPromptContentById(id As Integer) As String
        Select Case id
            Case 1 : Return My.Settings.PromptContent1
            Case 2 : Return My.Settings.PromptContent2
            Case 3 : Return My.Settings.PromptContent3
            Case 4 : Return My.Settings.PromptContent4
            Case 5 : Return My.Settings.PromptContent5
            Case Else : Return ""
        End Select
    End Function

    Private Sub SetPromptNameById(id As Integer, name As String)
        Select Case id
            Case 1 : My.Settings.PromptName1 = name
            Case 2 : My.Settings.PromptName2 = name
            Case 3 : My.Settings.PromptName3 = name
            Case 4 : My.Settings.PromptName4 = name
            Case 5 : My.Settings.PromptName5 = name
        End Select
    End Sub

    Private Sub SetPromptContentById(id As Integer, content As String)
        Select Case id
            Case 1 : My.Settings.PromptContent1 = content
            Case 2 : My.Settings.PromptContent2 = content
            Case 3 : My.Settings.PromptContent3 = content
            Case 4 : My.Settings.PromptContent4 = content
            Case 5 : My.Settings.PromptContent5 = content
        End Select
    End Sub

    ''' <summary>
    ''' Cập nhật hiển thị tên trên 5 nút prompt
    ''' </summary>
    Private Sub UpdatePromptButtonDisplay()
        BtnPrompt1.Content = String.Format("{0}. {1}", 1, GetPromptNameById(1))
        BtnPrompt2.Content = String.Format("{0}. {1}", 2, GetPromptNameById(2))
        BtnPrompt3.Content = String.Format("{0}. {1}", 3, GetPromptNameById(3))
        BtnPrompt4.Content = String.Format("{0}. {1}", 4, GetPromptNameById(4))
        BtnPrompt5.Content = String.Format("{0}. {1}", 5, GetPromptNameById(5))
    End Sub

    ''' <summary>
    ''' Tìm ID của prompt đang được hiển thị trong TxtPrompt (nếu khớp content)
    ''' </summary>
    Private Function FindCurrentPromptId() As Integer
        Dim currentContent = TxtPrompt.Text.Trim()
        If String.IsNullOrWhiteSpace(currentContent) Then Return 0

        For id As Integer = 1 To 5
            Dim promptContent = GetPromptContentById(id).Trim()
            If promptContent = currentContent Then
                Return id
            End If
        Next

        Return 0
    End Function

    ''' <summary>
    ''' Lưu toàn bộ 5 prompts vào Settings
    ''' </summary>
    Private Sub SaveAllPrompts()
        ' Lưu content hiện tại trong TxtPrompt vào prompt đang chọn (nếu có)
        Dim currentId = FindCurrentPromptId()
        If currentId > 0 Then
            SetPromptContentById(currentId, TxtPrompt.Text.Trim())
        End If

        My.Settings.Save()
    End Sub

    ''' <summary>
    ''' Load toàn bộ 5 prompts từ Settings và cập nhật UI
    ''' </summary>
    Private Sub LoadAllPrompts()
        UpdatePromptButtonDisplay()

        ' Nếu TxtPrompt đang trống, load prompt đầu tiên có content
        If String.IsNullOrWhiteSpace(TxtPrompt.Text) Then
            For id As Integer = 1 To 5
                Dim content = GetPromptContentById(id)
                If Not String.IsNullOrWhiteSpace(content) Then
                    TxtPrompt.Text = content
                    TxtTranslateStatus.Text = String.Format("📋 Đã load prompt: {0}. {1}", id, GetPromptNameById(id))
                    Exit For
                End If
            Next
        End If
    End Sub

    ''' <summary>
    ''' Khởi tạo prompts mặc định (Anime và Film) nếu chưa có
    ''' </summary>
    Private Sub InitializeDefaultPrompts()
        ' Prompt 1 - Anime
        If String.IsNullOrWhiteSpace(My.Settings.PromptName1) OrElse My.Settings.PromptName1 = "Prompt 1" Then
            My.Settings.PromptName1 = "Anime"
            My.Settings.PromptContent1 = GetDefaultAnimePrompt()
        End If

        ' Prompt 2 - Film
        If String.IsNullOrWhiteSpace(My.Settings.PromptName2) OrElse My.Settings.PromptName2 = "Prompt 2" Then
            My.Settings.PromptName2 = "Film"
            My.Settings.PromptContent2 = GetDefaultFilmPrompt()
        End If

        My.Settings.Save()
    End Sub

    Private Function GetDefaultAnimePrompt() As String
        Return "# SYSTEM ROLE" & vbCrLf & _
               "Bạn là chuyên gia dịch thuật & localize phụ đề anime 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên vibe anime, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." & vbCrLf & vbCrLf & _
               "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" & vbCrLf & _
               "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" & vbCrLf & _
               "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`." & vbCrLf & _
               "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." & vbCrLf & _
               "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\N`) ở cả bản gốc và bản dịch." & vbCrLf & _
               "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." & vbCrLf & vbCrLf & _
               "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" & vbCrLf & _
               "   - Ưu tiên ≤ 42 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'""()[]{}... và dấu CJK như 。、！？)." & vbCrLf & _
               "     Ví dụ: ""Chúng ta nên nhanh chóng đưa nó trở lại cơ sở chăm sóc!"" → 42 ký tự (không tính dấu !)." & vbCrLf & _
               "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." & vbCrLf & _
               "   - Tính độ dài riêng cho từng đoạn trước/sau `\N` nếu có." & vbCrLf & _
               "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." & vbCrLf & _
               "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." & vbCrLf & vbCrLf & _
               "3. LOCALIZE & VIBE ANIME" & vbCrLf & _
               "   - Tính cách nhân vật: Tsundere (cộc, phủ nhận cảm xúc), Kuudere (ngắn, lạnh lùng), Genki (năng lượng), Chuunibyou (kỳ ảo, hoa mỹ) → Thể hiện qua từ vựng & nhịp câu." & vbCrLf & _
               "   - Honorifics: Việt hóa theo quan hệ (`anh/chị/em/cậu`) trừ khi là thuật ngữ đặc thù hoặc fan yêu cầu giữ `-san/-kun/-chan/-sama`." & vbCrLf & _
               "   - Tiếng lóng/Onomatopoeia: Dịch nghĩa hoặc giữ nguyên *in nghiêng* nếu mang tính biểu tượng. KHÔNG dùng chú thích trong phụ đề." & vbCrLf & _
               "   - Tên riêng: Giữ nguyên Romaji/Kanji hoặc Việt hóa nhất quán xuyên suốt." & vbCrLf & vbCrLf & _
               "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" & vbCrLf & _
               "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" & vbCrLf & _
               "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" & vbCrLf & _
               "3. Bản dịch tiếng Việt đã tối ưu ≤ 42 ký tự (không tính dấu) chưa?" & vbCrLf & _
               "4. Đã dọn sạch 100% các câu chữ thừa như ""Dưới đây là kết quả..."", ""Hoàn thành..."" chưa?" & vbCrLf & vbCrLf & _
               "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" & vbCrLf & _
               "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" & vbCrLf & _
               "| :--- | :--- | :--- |" & vbCrLf & _
               "| [STT] | [Text Anh] | [Text Việt] |" & vbCrLf & _
               "| ... | ... | ... |"
    End Function

    Private Function GetDefaultFilmPrompt() As String
        Return "# SYSTEM ROLE" & vbCrLf & _
               "Bạn là chuyên gia dịch thuật & localize phụ đề phim điện ảnh 10+ năm kinh nghiệm. Nhiệm vụ: Dịch chuẩn xác, giữ nguyên cinematic tone, tối ưu cho màn hình phụ đề và xuất định dạng chuẩn để dán trực tiếp vào Excel." & vbCrLf & vbCrLf & _
               "# ⚠️ LUẬT CỨNG (BẮT BUỘC TUÂN THỦ)" & vbCrLf & _
               "1. CẤU TRÚC ĐẦU RA 3 CỘT (CHO EXCEL)" & vbCrLf & _
               "   - Output bắt buộc phải được trình bày dưới dạng Bảng (Table) với 3 cột: `Số thứ tự` | `Nội dung gốc (Tiếng Anh)` | `Nội dung dịch (Tiếng Việt)`." & vbCrLf & _
               "   - ÁNH XẠ 1:1 TUYỆT ĐỐI: Mỗi dòng input = đúng 1 hàng trong bảng output." & vbCrLf & _
               "   - Giữ nguyên toàn bộ tag metadata (`[time]`, `{style}`, `**bold**`, `*italic*`, `\N`, `[whisper]`, `[phone]`) ở cả bản gốc và bản dịch." & vbCrLf & _
               "   - CẤM TÚC: Không gộp, tách, bỏ dòng. Tuyệt đối KHÔNG sinh ra bất kỳ văn bản, lời chào, lời dẫn hay giải thích nào ngoài cái bảng." & vbCrLf & vbCrLf & _
               "2. CHUẨN PHỤ ĐỀ & ĐỘ DÀI" & vbCrLf & _
               "   - Ưu tiên ≤ 42-48 ký tự cho bản dịch Tiếng Việt, không tính punctuation (dấu câu: .,!?;:-'""()[]{}... và dấu CJK như 。、！？)." & vbCrLf & _
               "     Ví dụ: ""We need to get this back to the care facility immediately!"" → Bản dịch Việt ≤ 48 ký tự (không tính dấu !)." & vbCrLf & _
               "   - Nếu vượt quá, bắt buộc phải rút gọn tự nhiên nhưng không được cắt nghĩa lõi." & vbCrLf & _
               "   - Tính độ dài riêng cho từng đoạn trước/sau `\N` nếu có." & vbCrLf & _
               "   - Rút gọn `...` thành `…` để tiết kiệm ký tự." & vbCrLf & _
               "   - Chuẩn hóa dấu câu tiếng Việt: bỏ chồng chéo (`?.!` → `?!` hoặc `.`), cách dấu đúng chuẩn." & vbCrLf & vbCrLf & _
               "3. LOCALIZE & CINEMATIC TONE" & vbCrLf & _
               "   - Genre awareness: " & vbCrLf & _
               "     • Action/Thriller → Câu ngắn, nhịp nhanh, từ mạnh." & vbCrLf & _
               "     • Drama/Romance → Câu mềm mại, giàu cảm xúc, ngắt nhịp tự nhiên." & vbCrLf & _
               "     • Comedy → Ưu tiên truyền tải hiệu ứng hài, có thể linh hoạt sáng tạo." & vbCrLf & _
               "     • Period/Historical → Dùng từ Hán-Việt, cấu trúc trang trọng khi phù hợp." & vbCrLf & _
               "   - Regional dialects: Nhận diện Anh-Mỹ, Anh-Anh, Southern US, Australian... → Truyền tải sắc thái qua từ vựng vùng miền tiếng Việt tương ứng (ví dụ: ""y'all"" → ""các cậu/các bạn"", không dịch word-for-word)." & vbCrLf & _
               "   - Titles & Formality: Việt hóa `Mr./Mrs./Dr./Captain` → `Ông/Bà/Bác sĩ/Thuyền trưởng` trừ khi nhân vật gọi tên riêng hoặc context yêu cầu giữ nguyên." & vbCrLf & _
               "   - Cultural references: Adapt pop culture, idioms, jokes sao cho khán giả Việt hiểu ngay mà không mất intent gốc. KHÔNG dùng chú thích trong phụ đề." & vbCrLf & _
               "   - Profanity handling: Điều chỉnh mức độ ""mạnh/nhẹ"" của từ ngữ theo rating phim (PG-13/R), giữ nguyên thái độ nhân vật." & vbCrLf & _
               "   - Technical jargon: Tra cứu và Việt hóa thuật ngữ chuyên ngành (sci-fi, legal, medical) nhất quán, ưu tiên dễ hiểu." & vbCrLf & _
               "   - Song lyrics: Ghi chú [hát] ở đầu câu nếu cần, ưu tiên giữ nhịp thơ khi có thể." & vbCrLf & _
               "   - On-screen text: Phân biệt xử lý: thoại ưu tiên tự nhiên như giao tiếp, text hiển thị trên màn hình ưu tiên dịch chính xác nội dung." & vbCrLf & vbCrLf & _
               "4. FRANCHISE & PROJECT CONSISTENCY" & vbCrLf & _
               "   - Ghi nhớ và áp dụng nhất quán: tên riêng, thuật ngữ, cách xưng hô xuyên suốt toàn bộ dự án/phim series." & vbCrLf & _
               "   - Khi gặp từ mới/chưa rõ context, ưu tiên giữ nguyên dạng gốc + ghi chú ngắn trong tag nếu cần." & vbCrLf & vbCrLf & _
               "# ✅ QUY TRÌNH TỰ PHẢN BIỆN 2 LẦN trước khi xuất ra kết quả:" & vbCrLf & _
               "1. Đã tạo đúng định dạng bảng 3 cột để copy vào Excel chưa?" & vbCrLf & _
               "2. Số lượng hàng output có khớp 100% với số lượng hàng input không?" & vbCrLf & _
               "3. Bản dịch tiếng Việt đã tối ưu ≤ 42-48 ký tự (không tính dấu) và phù hợp nhịp thoại chưa?" & vbCrLf & _
               "4. Đã dọn sạch 100% các câu chữ thừa như ""Dưới đây là kết quả..."", ""Hoàn thành..."" chưa?" & vbCrLf & _
               "5. [Movie-specific] Đã truyền tải đúng genre tone, regional dialect và cultural intent chưa?" & vbCrLf & _
               "6. [Movie-specific] Thuật ngữ chuyên ngành/tên riêng đã nhất quán với context phim chưa?" & vbCrLf & vbCrLf & _
               "# 📤 CẤU TRÚC OUTPUT DUY NHẤT ĐƯỢC PHÉP TRẢ VỀ:" & vbCrLf & _
               "| Số thứ tự | Nội dung gốc (Tiếng Anh) | Nội dung dịch (Tiếng Việt) |" & vbCrLf & _
               "| :--- | :--- | :--- |" & vbCrLf & _
               "| [STT] | [Text Anh] | [Text Việt] |" & vbCrLf & _
               "| ... | ... | ... |"
    End Function

#End Region

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
            Dim content = SubtitleParser.SanitizeContent(TxtEngsub.Text)
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
        Catch ex As Exception
            TxtEngsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            _engLines.Clear()
        Finally
            UpdateMergeDisplays()
            _isMergeUpdating = False
        End Try
    End Sub

    Private Sub TxtVietsub_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isMergeUpdating Then Return
        Try
            _isMergeUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtVietsub.Text)
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
        Catch ex As Exception
            TxtVietsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            _vietLines.Clear()
        Finally
            UpdateMergeDisplays()
            _isMergeUpdating = False
        End Try
    End Sub

    Private Sub UpdateMergeDisplays()
        ' Luôn gọi để cập nhật cả 2 panel
        Try
            If _engLines Is Nothing Then _engLines = New List(Of SubtitleLine)()
            If _vietLines Is Nothing Then _vietLines = New List(Of SubtitleLine)()

            Dim mergedLines = MergeService.MergeSubtitles(_engLines, _vietLines, _mergeFormat)
            TxtMerge.Text = If(mergedLines IsNot Nothing AndAlso mergedLines.Count > 0, SubtitleParser.ToText(mergedLines, _mergeFormat), "")

            Dim unbreakLines = MergeService.MergeUnbreak(_engLines, _vietLines, _mergeFormat)
            TxtMergeUnbreak.Text = If(unbreakLines IsNot Nothing AndAlso unbreakLines.Count > 0, SubtitleParser.ToText(unbreakLines, _mergeFormat), "")
        Catch ex As Exception
            TxtMerge.Text = ""
            TxtMergeUnbreak.Text = ""
        End Try
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

#Region "Hardware Info - Methods"

    ''' <summary>
    ''' Load toàn bộ thông tin phần cứng khi mở app
    ''' </summary>
    Private Sub LoadHardwareInfo()
        If _isHardwareLoading Then Return
        Try
            _isHardwareLoading = True

            TxtGpuInfo.Text = HardwareInfoService.GetGpuInfo()
            TxtCpuInfo.Text = HardwareInfoService.GetCpuInfo()
            TxtRamInfo.Text = HardwareInfoService.GetRamInfo()
            TxtMainboardInfo.Text = HardwareInfoService.GetMainboardInfo()
        Catch ex As Exception
            TxtGpuInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtCpuInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtRamInfo.Text = String.Format("Lỗi: {0}", ex.Message)
            TxtMainboardInfo.Text = String.Format("Lỗi: {0}", ex.Message)
        Finally
            _isHardwareLoading = False
        End Try
    End Sub

    Private Async Sub ShowToastHardware(message As String)
        ToastTextHardware.Text = message
        ToastBorderHardware.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderHardware.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyGpu_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtGpuInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtGpuInfo.Text)
            ShowToastHardware("📋 Đã copy GPU info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyCpu_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtCpuInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtCpuInfo.Text)
            ShowToastHardware("📋 Đã copy CPU info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyRam_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtRamInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtRamInfo.Text)
            ShowToastHardware("📋 Đã copy RAM info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub BtnCopyMainboard_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMainboardInfo.Text) Then Return
        Try
            Clipboard.SetText(TxtMainboardInfo.Text)
            ShowToastHardware("📋 Đã copy Mainboard info!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Zero Time - Methods"

    Private _zeroLines As New List(Of SubtitleLine)()
    Private _zeroFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isZeroUpdating As Boolean = False
    Private _zeroPlainTexts As New List(Of String)()

    Private Sub TxtZeroInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isZeroUpdating Then Return
        Try
            _isZeroUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtZeroInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                _zeroLines.Clear()
                _zeroFormat = SubtitleFormat.Unknown
                _zeroPlainTexts.Clear()
                TxtZeroFormat.Text = ""
                TxtZeroOutput.Text = ""
                Return
            End If
            _zeroFormat = SubtitleParser.DetectFormat(content)
            If _zeroFormat <> SubtitleFormat.Unknown Then
                ' Phụ đề chuẩn (SRT/ASS)
                _zeroLines = SubtitleParser.Parse(content)
                _zeroPlainTexts.Clear()
                TxtZeroFormat.Text = String.Format("({0} - {1} dòng)", _zeroFormat.ToString().ToUpper(), _zeroLines.Count)
            Else
                ' Plain text: mỗi dòng là 1 entry
                _zeroLines.Clear()
                _zeroPlainTexts = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries).ToList()
                TxtZeroFormat.Text = String.Format("(Text - {0} dòng)", _zeroPlainTexts.Count)
            End If
            UpdateZeroOutput()
        Catch ex As Exception
            TxtZeroFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isZeroUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Set toàn bộ StartTime và EndTime về 0 rồi xuất ra
    ''' Hỗ trợ cả phụ đề SRT/ASS và plain text
    ''' ''' </summary>
    Private Sub UpdateZeroOutput()
        If _zeroLines.Count = 0 AndAlso _zeroPlainTexts.Count = 0 Then
            TxtZeroOutput.Text = ""
            Return
        End If

        Dim sb = New StringBuilder()
        Dim zeroTimeAss = "0:00:00.00"

        ' Nếu là plain text → xuất ra ASS format với time = 0
        If _zeroPlainTexts.Count > 0 Then
            For i As Integer = 0 To _zeroPlainTexts.Count - 1
                Dim text = _zeroPlainTexts(i).Replace("\N", "\\N").Replace("\n", "\\N").Replace(Environment.NewLine, "\N")
                sb.AppendLine(String.Format("Dialogue: 0,{0},{1},Default,,0000,0000,0000,,{2}", zeroTimeAss, zeroTimeAss, text))
            Next
            TxtZeroOutput.Text = sb.ToString().TrimEnd()
            Return
        End If

        ' Nếu là phụ đề SRT/ASS → xuất ASS format
        For Each line In _zeroLines
            Dim assLine = TryCast(line, AssSubtitleLine)
            If assLine IsNot Nothing Then
                ' ASS: giữ nguyên cấu trúc, thay time bằng 0
                sb.AppendLine(String.Format("Dialogue: {0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                    assLine.Layer, zeroTimeAss, zeroTimeAss, assLine.Style,
                    assLine.Name, assLine.MarginL, assLine.MarginR, assLine.MarginV,
                    assLine.Effect, assLine.DialogText))
            Else
                Dim srtLine = TryCast(line, SrtSubtitleLine)
                If srtLine IsNot Nothing Then
                    ' SRT → chuyển sang ASS format
                    Dim text = srtLine.Text.Replace("\N", "\\N").Replace("\n", "\\N").Replace(Environment.NewLine, "\N")
                    sb.AppendLine(String.Format("Dialogue: 0,{0},{1},Default,,0000,0000,0000,,{2}", zeroTimeAss, zeroTimeAss, text))
                End If
            End If
        Next

        TxtZeroOutput.Text = sb.ToString().TrimEnd()
    End Sub

    Private Async Sub ShowToastZero(message As String)
        ToastTextZero.Text = message
        ToastBorderZero.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderZero.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyZero_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtZeroOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtZeroOutput.Text)
            ShowToastZero("📋 Đã copy Zero Time!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Karaoke Vietnamese - Methods"

    Private _isKaraokeUpdating As Boolean = False

    Private Sub TxtKaraokeInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isKaraokeUpdating Then Return
        Try
            _isKaraokeUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtKaraokeInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                TxtKaraokeCount.Text = ""
                TxtKaraokeOutput.Text = ""
                Return
            End If

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            TxtKaraokeCount.Text = String.Format("({0} dòng)", lines.Length)

            ' Xử lý karaoke
            Dim karaokeResult = KaraokeVietnameseService.ProcessLyrics(content)
            TxtKaraokeOutput.Text = karaokeResult
            TxtKaraokeEditable.Text = karaokeResult
        Catch ex As Exception
            TxtKaraokeCount.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isKaraokeUpdating = False
        End Try
    End Sub

    Private Async Sub ShowToastKaraoke(message As String)
        ToastTextKaraoke.Text = message
        ToastBorderKaraoke.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraoke.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyKaraoke_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtKaraokeEditable.Text) Then Return
        Try
            Clipboard.SetText(TxtKaraokeEditable.Text)
            ShowToastKaraoke("📋 Đã copy Karaoke!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Karaoke English - Methods"

    Private _isKaraokeEngUpdating As Boolean = False

    Private Sub TxtKaraokeEngInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isKaraokeEngUpdating Then Return
        Try
            _isKaraokeEngUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtKaraokeEngInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                TxtKaraokeEngCount.Text = ""
                TxtKaraokeEngOutput.Text = ""
                TxtKaraokeEngEditable.Text = ""
                Return
            End If

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            TxtKaraokeEngCount.Text = String.Format("({0} lines)", lines.Length)

            ' Xử lý karaoke English
            Dim karaokeResult = KaraokeVietnameseService.ProcessLyrics(content)
            TxtKaraokeEngOutput.Text = karaokeResult
            TxtKaraokeEngEditable.Text = karaokeResult
        Catch ex As Exception
            TxtKaraokeEngCount.Text = String.Format("(Error: {0})", ex.Message)
        Finally
            _isKaraokeEngUpdating = False
        End Try
    End Sub

    Private Async Sub ShowToastKaraokeEng(message As String)
        ToastTextKaraokeEng.Text = message
        ToastBorderKaraokeEng.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraokeEng.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyKaraokeEng_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtKaraokeEngEditable.Text) Then Return
        Try
            Clipboard.SetText(TxtKaraokeEngEditable.Text)
            ShowToastKaraokeEng("📋 Copied Karaoke!")
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Karaoke Merge - Methods"

    Private _isKaraokeMergeUpdating As Boolean = False

    Private Sub TxtKaraokeMergeInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isKaraokeMergeUpdating Then Return
        Try
            _isKaraokeMergeUpdating = True
            Dim content = SubtitleParser.SanitizeContent(TxtKaraokeMergeInput.Text)
            If String.IsNullOrWhiteSpace(content) Then
                TxtKaraokeMergeCount.Text = ""
                TxtKaraokeMergeOutput.Text = ""
                Return
            End If

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim dialogueCount = lines.Count(Function(l) l.Trim().StartsWith("Dialogue:"))
            TxtKaraokeMergeCount.Text = String.Format("({0} dialogue lines)", dialogueCount)

            ' Xu ly merge karaoke
            TxtKaraokeMergeOutput.Text = KaraokeMergeService.ProcessKaraokeMerge(content)
        Catch ex As Exception
            TxtKaraokeMergeCount.Text = String.Format("(Error: {0})", ex.Message)
        Finally
            _isKaraokeMergeUpdating = False
        End Try
    End Sub

    Private Async Sub ShowToastKaraokeMerge(message As String)
        ToastTextKaraokeMerge.Text = message
        ToastBorderKaraokeMerge.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraokeMerge.Visibility = Visibility.Collapsed
    End Sub

    Private Sub BtnCopyKaraokeMerge_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtKaraokeMergeOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtKaraokeMergeOutput.Text)
            ShowToastKaraokeMerge("📋 Copied Merged Karaoke!")
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Karaoke Sync - Methods"

    Private _isSyncUpdating As Boolean = False

    ''' <summary>
    ''' Khi input thay đổi, cập nhật số dòng
    ''' </summary>
    Private Sub TxtSyncInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isSyncUpdating Then Return
        Try
            _isSyncUpdating = True
            Dim content = TxtSyncInput.Text.Trim()
            If String.IsNullOrWhiteSpace(content) Then
                TxtSyncInputCount.Text = ""
                TxtSyncOutput.Text = ""
                TxtSyncOutputCount.Text = ""
                Return
            End If

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            TxtSyncInputCount.Text = String.Format("({0} dòng)", lines.Length)

            ' Tự động sync khi có input
            SyncTimeCodes()
        Catch ex As Exception
            TxtSyncInputCount.Text = String.Format("(Lỗi: {0})", ex.Message)
        Finally
            _isSyncUpdating = False
        End Try
    End Sub

    ''' <summary>
    ''' Khi time input thay đổi, auto sync
    ''' </summary>
    Private Sub TxtSyncTimeInput_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isSyncUpdating Then Return
        Try
            SyncTimeCodes()
        Catch ex As Exception
            ' Ignore parse errors during typing
        End Try
    End Sub

    ''' <summary>
    ''' Offset toàn bộ time code theo desired start time (giữ nguyên duration)
    ''' </summary>
    Private Sub BtnSyncTime_Click(sender As Object, e As RoutedEventArgs)
        Try
            SyncTimeCodes()
        Catch ex As Exception
            MessageBox.Show("Lỗi sync time: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Thực hiện sync time codes
    ''' Logic: Tính offset từ dòng đầu tiên, áp dụng offset đó cho tất cả các dòng
    ''' </summary>
    Private Sub SyncTimeCodes()
        Dim inputContent = TxtSyncInput.Text.Trim()
        If String.IsNullOrWhiteSpace(inputContent) Then
            TxtSyncOutput.Text = ""
            TxtSyncOutputCount.Text = ""
            Return
        End If

        ' Parse desired start time từ textbox (format: HH:MM:SS.mmm hoặc HH:MM:SS,mmm)
        Dim desiredStartMs As Long = ParseTimeInputToMs(TxtSyncTimeInput.Text)
        If desiredStartMs < 0 Then
            TxtSyncOutput.Text = inputContent
            TxtSyncOutputCount.Text = "(Lỗi: Time format không hợp lệ)"
            Return
        End If

        Dim lines = inputContent.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        
        ' Tìm dòng đầu tiên có time code (Dialogue hoặc Comment)
        Dim firstLineStartTimeMs As Long? = Nothing
        Dim offsetMs As Long = 0

        For Each line In lines
            ' Thử parse SRT format
            Dim srtMatch = System.Text.RegularExpressions.Regex.Match(line, "(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})")
            If srtMatch.Success Then
                Dim startTimeStr = srtMatch.Groups(1).Value
                firstLineStartTimeMs = ParseSrtTimeToMs(startTimeStr)
                Exit For
            End If

            ' Thử parse ASS format (Dialogue hoặc Comment)
            Dim assMatch = System.Text.RegularExpressions.Regex.Match(line, "^(?:Dialogue|Comment):\s*\d+,(\d+:\d{2}:\d{2}\.\d{2}),(\d+:\d{2}:\d{2}\.\d{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If assMatch.Success Then
                Dim startTimeStr = assMatch.Groups(1).Value
                firstLineStartTimeMs = ParseAssTimeToMs(startTimeStr)
                Exit For
            End If
        Next

        ' Nếu không tìm thấy time code nào
        If firstLineStartTimeMs Is Nothing Then
            TxtSyncOutput.Text = inputContent
            TxtSyncOutputCount.Text = "(0 dòng đã sync)"
            Return
        End If

        ' Tính offset: desired - actual
        offsetMs = desiredStartMs - firstLineStartTimeMs

        ' Áp dụng offset cho tất cả các dòng
        Dim sb = New StringBuilder()
        Dim processedCount As Integer = 0

        For Each line In lines
            Dim processedLine = line

            ' Xử lý SRT format: 00:00:00,000 --> 00:00:00,000
            Dim srtMatch = System.Text.RegularExpressions.Regex.Match(line, "(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})")
            If srtMatch.Success Then
                Dim startTimeStr = srtMatch.Groups(1).Value
                Dim endTimeStr = srtMatch.Groups(2).Value

                Dim startMs = ParseSrtTimeToMs(startTimeStr)
                Dim endMs = ParseSrtTimeToMs(endTimeStr)

                ' Offset time, đảm bảo không âm
                Dim newStartMs = Math.Max(0L, startMs + offsetMs)
                Dim newEndMs = Math.Max(0L, endMs + offsetMs)

                ' Giữ nguyên duration
                Dim duration = endMs - startMs
                newEndMs = newStartMs + duration

                Dim newStartStr = MsToSrtTime(newStartMs)
                Dim newEndStr = MsToSrtTime(newEndMs)

                processedLine = line.Replace(startTimeStr, newStartStr).Replace(endTimeStr, newEndStr)
                processedCount += 1
            Else
                ' Xử lý ASS format: Dialogue hoặc Comment
                Dim assMatch = System.Text.RegularExpressions.Regex.Match(line, "^((?:Dialogue|Comment):\s*\d+,)(\d+:\d{2}:\d{2}\.\d{2}),(\d+:\d{2}:\d{2}\.\d{2})(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                If assMatch.Success Then
                    Dim prefix = assMatch.Groups(1).Value
                    Dim startTimeStr = assMatch.Groups(2).Value
                    Dim endTimeStr = assMatch.Groups(3).Value
                    Dim suffix = assMatch.Groups(4).Value

                    Dim startMs = ParseAssTimeToMs(startTimeStr)
                    Dim endMs = ParseAssTimeToMs(endTimeStr)

                    ' Offset time, đảm bảo không âm
                    Dim newStartMs = Math.Max(0L, startMs + offsetMs)
                    Dim newEndMs = Math.Max(0L, endMs + offsetMs)

                    ' Giữ nguyên duration
                    Dim duration = endMs - startMs
                    newEndMs = newStartMs + duration

                    Dim newStartStr = MsToAssTime(newStartMs)
                    Dim newEndStr = MsToAssTime(newEndMs)

                    processedLine = prefix & newStartStr & "," & newEndStr & suffix
                    processedCount += 1
                End If
            End If

            sb.AppendLine(processedLine)
        Next

        TxtSyncOutput.Text = sb.ToString().TrimEnd()
        TxtSyncOutputCount.Text = String.Format("({0} dòng đã sync)", processedCount)
    End Sub

    ''' <summary>
    ''' Parse time input string (HH:MM:SS.mmm hoặc HH:MM:SS,mmm) sang milliseconds
    ''' Trả về -1 nếu format không hợp lệ
    ''' </summary>
    Private Function ParseTimeInputToMs(timeStr As String) As Long
        If String.IsNullOrWhiteSpace(timeStr) Then Return 0L

        ' Chuẩn hóa: thay comma bằng dot để统一 xử lý
        timeStr = timeStr.Trim().Replace(","c, "."c)

        ' Regex: HH:MM:SS.mmm (có thể bỏ HH hoặc MM)
        Dim match = System.Text.RegularExpressions.Regex.Match(timeStr, "^(\d+):(\d{2}):(\d{2})\.(\d+)$")
        If Not match.Success Then
            Return -1L
        End If

        Try
            Dim hours = Integer.Parse(match.Groups(1).Value)
            Dim minutes = Integer.Parse(match.Groups(2).Value)
            Dim seconds = Integer.Parse(match.Groups(3).Value)
            Dim fracSeconds = match.Groups(4).Value

            ' Parse fractional seconds (có thể là 1-3 digits)
            Dim milliseconds As Integer = 0
            If fracSeconds.Length = 1 Then
                milliseconds = Integer.Parse(fracSeconds) * 100
            ElseIf fracSeconds.Length = 2 Then
                milliseconds = Integer.Parse(fracSeconds) * 10
            Else
                milliseconds = Integer.Parse(fracSeconds.Substring(0, 3))
            End If

            Return (hours * 3600L + minutes * 60L + seconds) * 1000L + milliseconds
        Catch
            Return -1L
        End Try
    End Function

    ''' <summary>
    ''' Parse SRT time string (00:00:00,000) sang milliseconds
    ''' </summary>
    Private Function ParseSrtTimeToMs(timeStr As String) As Long
        Dim parts = timeStr.Split(":"c, ","c)
        If parts.Length <> 4 Then Return 0L

        Dim hours = Integer.Parse(parts(0))
        Dim minutes = Integer.Parse(parts(1))
        Dim seconds = Integer.Parse(parts(2))
        Dim milliseconds = Integer.Parse(parts(3))

        Return hours * 3600000L + minutes * 60000L + seconds * 1000L + milliseconds
    End Function

    ''' <summary>
    ''' Convert milliseconds sang SRT time string (00:00:00,000)
    ''' </summary>
    Private Function MsToSrtTime(ms As Long) As String
        Dim isNegative = ms < 0
        ms = Math.Abs(ms)

        Dim hours = ms \ 3600000L
        ms = ms Mod 3600000L
        Dim minutes = ms \ 60000L
        ms = ms Mod 60000L
        Dim seconds = ms \ 1000L
        Dim milliseconds = ms Mod 1000L

        Return String.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}", hours, minutes, seconds, milliseconds)
    End Function

    ''' <summary>
    ''' Parse ASS time string (H:MM:SS.cc) sang milliseconds
    ''' </summary>
    Private Function ParseAssTimeToMs(timeStr As String) As Long
        Dim parts = timeStr.Split(":"c, "."c)
        If parts.Length <> 4 Then Return 0L

        Dim hours = Integer.Parse(parts(0))
        Dim minutes = Integer.Parse(parts(1))
        Dim seconds = Integer.Parse(parts(2))
        Dim centiseconds = Integer.Parse(parts(3))

        Return hours * 3600000L + minutes * 60000L + seconds * 1000L + centiseconds * 10L
    End Function

    ''' <summary>
    ''' Convert milliseconds sang ASS time string (H:MM:SS.cc)
    ''' </summary>
    Private Function MsToAssTime(ms As Long) As String
        Dim isNegative = ms < 0
        ms = Math.Abs(ms)

        Dim hours = ms \ 3600000L
        ms = ms Mod 3600000L
        Dim minutes = ms \ 60000L
        ms = ms Mod 60000L
        Dim seconds = ms \ 1000L
        Dim centiseconds = (ms Mod 1000L) \ 10L

        Return String.Format("{0}:{1:D2}:{2:D2}.{3:D2}", hours, minutes, seconds, centiseconds)
    End Function

    ''' <summary>
    ''' Copy kết quả sync
    ''' </summary>
    Private Sub BtnCopySync_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtSyncOutput.Text) Then Return
        Try
            Clipboard.SetText(TxtSyncOutput.Text)
            ShowToastSync("📋 Đã copy kết quả sync!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Async Sub ShowToastSync(message As String)
        ToastTextSync.Text = message
        ToastBorderSync.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderSync.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
