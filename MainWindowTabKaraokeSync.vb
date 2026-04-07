Imports System.Text
Imports System.Windows
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Karaoke Sync - Fields"

    Private _isSyncUpdating As Boolean = False

#End Region

#Region "Karaoke Sync - Event Handlers"

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

#End Region

#Region "Karaoke Sync - Sync Logic"

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

#End Region

#Region "Karaoke Sync - Time Parsing Helpers"

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

#End Region

#Region "Karaoke Sync - Toast"

    Private Async Sub ShowToastSync(message As String)
        ToastTextSync.Text = message
        ToastBorderSync.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderSync.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
