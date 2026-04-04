Imports System.Windows
Imports System.Threading.Tasks
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

''' <summary>
''' Cửa sổ chính của ứng dụng xử lý phụ đề
''' Bố cục 4 panel: Original → TimeCode → ConnectGap → Result
''' </summary>
Class MainWindow

    ''' <summary>
    ''' Danh sách phụ đề gốc (từ panel Original)
    ''' </summary>
    Private _originalLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Danh sách phụ đề sau khi chỉnh time code
    ''' </summary>
    Private _timeCodeLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Danh sách phụ đề sau khi connect gap (dựa trên time code lines)
    ''' </summary>
    Private _connectGapLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Định dạng phụ đề hiện tại
    ''' </summary>
    Private _currentFormat As SubtitleFormat = SubtitleFormat.Unknown

    ''' <summary>
    ''' Flag để tránh re-entrant khi TextChanged
    ''' </summary>
    Private _isUpdating As Boolean = False

    ''' <summary>
    ''' Tổng time đã shift (dùng để hiển thị thông báo)
    ''' </summary>
    Private _totalShiftMs As Integer = 0

#Region "Toast Notification"

    ''' <summary>
    ''' Hiển thị thông báo toast tự ẩn sau 2 giây
    ''' </summary>
    Private Async Sub ShowToast(message As String)
        ToastText.Text = message
        ToastBorder.Visibility = Visibility.Visible

        ' Tự ẩn sau 2 giây
        Await Task.Delay(2000)
        ToastBorder.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Event Handlers - Original Panel"

    ''' <summary>
    ''' Khi nội dung phụ đề gốc thay đổi → tự động parse và cập nhật TẤT CẢ panel
    ''' </summary>
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

            ' Phát hiện định dạng và parse
            _currentFormat = SubtitleParser.DetectFormat(content)
            _originalLines = SubtitleParser.Parse(content)

            ' Debug info - hiển thị số dòng parse được
            TxtOriginalFormat.Text = String.Format("({0} - {1} dòng)",
                _currentFormat.ToString().ToUpper(), _originalLines.Count)

            ' === CẬP NHẬT TẤT CẢ PANEL TỪ ORIGINAL ===

            ' Panel 2 - Time Code: clone từ original
            _timeCodeLines = New List(Of SubtitleLine)()
            For Each line In _originalLines
                _timeCodeLines.Add(line.Clone())
            Next
            UpdateTimeCodeDisplay()

            ' Panel 3 - Connect Gap: clone từ time code (vì connect gap áp dụng trên time code)
            _connectGapLines = New List(Of SubtitleLine)()
            For Each line In _timeCodeLines
                _connectGapLines.Add(line.Clone())
            Next
            UpdateConnectGapDisplay()

            ' Panel 4 - Result: hiển thị time code (chưa connect gap)
            UpdateResultDisplay()

        Catch ex As Exception
            TxtOriginalFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            System.Diagnostics.Debug.WriteLine("Parse error: " & ex.ToString())
        Finally
            _isUpdating = False
        End Try
    End Sub

#End Region

#Region "Event Handlers - Time Code Panel"

    ''' <summary>
    ''' Button +200ms: Tăng tất cả time thêm 200ms
    ''' → Tự động cập nhật Connect Gap và Result
    ''' </summary>
    Private Sub BtnTimePlus200_Click(sender As Object, e As RoutedEventArgs)
        If _timeCodeLines.Count = 0 Then Return

        _totalShiftMs += 200

        ' Shift time code lines (cả start và end time)
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, 200)
        UpdateTimeCodeDisplay()

        ' Cập nhật Connect Gap từ Time Code mới
        _connectGapLines = New List(Of SubtitleLine)()
        For Each line In _timeCodeLines
            _connectGapLines.Add(line.Clone())
        Next
        UpdateConnectGapDisplay()

        ' Cập nhật Result
        UpdateResultDisplay()

        ' Toast thông báo
        Dim seconds = Math.Abs(_totalShiftMs) / 1000.0
        Dim sign = If(_totalShiftMs >= 0, "+", "-")
        ShowToast(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
    End Sub

    ''' <summary>
    ''' Button -200ms: Giảm tất cả time 200ms (không âm)
    ''' → Tự động cập nhật Connect Gap và Result
    ''' </summary>
    Private Sub BtnTimeMinus200_Click(sender As Object, e As RoutedEventArgs)
        If _timeCodeLines.Count = 0 Then Return

        _totalShiftMs -= 200

        ' Shift time code lines (cả start và end time)
        _timeCodeLines = TimeCodeService.ShiftTime(_timeCodeLines, -200)
        UpdateTimeCodeDisplay()

        ' Cập nhật Connect Gap từ Time Code mới
        _connectGapLines = New List(Of SubtitleLine)()
        For Each line In _timeCodeLines
            _connectGapLines.Add(line.Clone())
        Next
        UpdateConnectGapDisplay()

        ' Cập nhật Result
        UpdateResultDisplay()

        ' Toast thông báo
        Dim seconds = Math.Abs(_totalShiftMs) / 1000.0
        Dim sign = If(_totalShiftMs >= 0, "+", "-")
        ShowToast(String.Format("⏱️ Đã shift {0}{1}ms ({2}s)", sign, _totalShiftMs, seconds.ToString("0.0")))
    End Sub

    ''' <summary>
    ''' Button Copy: Copy nội dung Time Code vào clipboard
    ''' </summary>
    Private Sub BtnCopyTimeCode_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtTimeCode.Text) Then Return

        Try
            Clipboard.SetText(TxtTimeCode.Text)
            MessageBox.Show("Đã copy nội dung Time Code vào clipboard!", "Copy thành công",
                          MessageBoxButton.OK, MessageBoxImage.Information)
        Catch ex As Exception
            MessageBox.Show("Lỗi khi copy: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Event Handlers - Connect Gap Panel"

    ''' <summary>
    ''' Button Connect Gap: Nối gap giữa các dòng
    ''' Chỉ extend EndTime dòng trên = StartTime dòng dưới
    ''' Start time luôn giữ nguyên
    ''' </summary>
    Private Sub BtnConnectGap_Click(sender As Object, e As RoutedEventArgs)
        If _connectGapLines.Count = 0 Then Return

        _connectGapLines = TimeCodeService.ConnectGap(_connectGapLines)
        UpdateConnectGapDisplay()
        UpdateResultDisplay()

        ShowToast("🔗 Đã connect gap! Start time giữ nguyên, End time được extend.")
    End Sub

#End Region

#Region "Event Handlers - Result Panel"

    ''' <summary>
    ''' Button Translate: Mở link web dịch phụ đề
    ''' </summary>
    Private Sub BtnTranslate_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim url = "https://www.syedgakbar.com/projects/dst"
            System.Diagnostics.Process.Start(url)
        Catch ex As Exception
            MessageBox.Show("Không thể mở trình duyệt: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Button Donate: Mở link donate
    ''' </summary>
    Private Sub BtnDonate_Click(sender As Object, e As RoutedEventArgs)
        Try
            Dim url = "https://tinyurl.com/mmtdonate"
            System.Diagnostics.Process.Start(url)
        Catch ex As Exception
            MessageBox.Show("Không thể mở trình duyệt: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Update Display Methods"

    ''' <summary>
    ''' Cập nhật hiển thị Time Code panel
    ''' </summary>
    Private Sub UpdateTimeCodeDisplay()
        If _timeCodeLines.Count = 0 Then
            TxtTimeCode.Text = ""
            Return
        End If

        Dim text = SubtitleParser.ToText(_timeCodeLines, _currentFormat)
        TxtTimeCode.Text = text
    End Sub

    ''' <summary>
    ''' Cập nhật hiển thị Connect Gap panel
    ''' </summary>
    Private Sub UpdateConnectGapDisplay()
        If _connectGapLines.Count = 0 Then
            TxtConnectGap.Text = ""
            Return
        End If

        Dim text = SubtitleParser.ToText(_connectGapLines, _currentFormat)
        TxtConnectGap.Text = text
    End Sub

    ''' <summary>
    ''' Cập nhật Result panel
    ''' Ưu tiên: Connect Gap (nếu đã bấm nút) > Time Code > Original
    ''' </summary>
    Private Sub UpdateResultDisplay()
        ' Nếu connect gap đã được bấm (có dữ liệu khác time code)
        ' Luôn hiển thị connect gap vì nó là bước cuối cùng
        If _connectGapLines.Count > 0 Then
            TxtResult.Text = SubtitleParser.ToText(_connectGapLines, _currentFormat)
        ElseIf _timeCodeLines.Count > 0 Then
            TxtResult.Text = SubtitleParser.ToText(_timeCodeLines, _currentFormat)
        Else
            TxtResult.Text = ""
        End If
    End Sub

#End Region

End Class
