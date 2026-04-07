Imports System.Text
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Zero Time - Fields"

    Private _zeroLines As New List(Of SubtitleLine)()
    Private _zeroFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isZeroUpdating As Boolean = False
    Private _zeroPlainTexts As New List(Of String)()

#End Region

#Region "Zero Time - Event Handlers"

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

#Region "Zero Time - Display Methods"

    ''' <summary>
    ''' Set toàn bộ StartTime và EndTime về 0 rồi xuất ra
    ''' Hỗ trợ cả phụ đề SRT/ASS và plain text
    ''' </summary>
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

#End Region

#Region "Zero Time - Toast"

    Private Async Sub ShowToastZero(message As String)
        ToastTextZero.Text = message
        ToastBorderZero.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderZero.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
