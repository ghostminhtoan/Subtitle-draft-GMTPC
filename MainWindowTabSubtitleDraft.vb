Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Subtitle Draft - Fields"

    Private _originalLines As New List(Of SubtitleLine)()
    Private _timeCodeLines As New List(Of SubtitleLine)()
    Private _connectGapLines As New List(Of SubtitleLine)()
    Private _currentFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isUpdating As Boolean = False
    Private _totalShiftMs As Integer = 0

#End Region

#Region "Subtitle Draft - Event Handlers"

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
        ShowToastDraft(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
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
        ShowToastDraft(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
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

    Private Sub BtnConnectGap_Click(sender As Object, e As RoutedEventArgs)
        If _connectGapLines.Count = 0 Then Return
        _connectGapLines = TimeCodeService.ConnectGap(_connectGapLines)
        UpdateConnectGapDisplay()
        UpdateResultDisplay()
        ShowToastDraft("🔗 Đã connect gap!")
    End Sub

    Private Sub BtnCopyResult_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtResult.Text) Then Return
        Try
            Clipboard.SetText(TxtResult.Text)
            ShowToastDraft("📋 Đã copy Result!")
        Catch ex As Exception
            MessageBox.Show("Lỗi: " & ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Subtitle Draft - Display Methods"

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

#Region "Subtitle Draft - Toast"

    Private Async Sub ShowToastDraft(message As String)
        ToastText.Text = message
        ToastBorder.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorder.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
