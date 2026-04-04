Imports System.Windows
Imports System.Threading.Tasks
Imports System.Text
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

''' <summary>
''' Cửa sổ chính của ứng dụng xử lý phụ đề
''' Gồm 3 tab:
''' - Subtitle Draft: Original → TimeCode → ConnectGap → Result
''' - Subtitle Merge: Engsub + Vietsub → Merge → Merge Unbreak
''' - Dialogue only: Panel 1 (Dialogue only) → Panel 2 (Line Number + Dialogue)
''' </summary>
Class MainWindow

#Region "Subtitle Draft Fields"

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
    ''' Định dạng phụ đề hiện tại (Subtitle Draft)
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

#End Region

#Region "Subtitle Merge Fields"

    ''' <summary>
    ''' Danh sách Engsub
    ''' </summary>
    Private _engLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Danh sách Vietsub
    ''' </summary>
    Private _vietLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Định dạng phụ đề hiện tại (Subtitle Merge)
    ''' </summary>
    Private _mergeFormat As SubtitleFormat = SubtitleFormat.Unknown

    ''' <summary>
    ''' Flag để tránh re-entrant khi TextChanged cho Merge
    ''' </summary>
    Private _isMergeUpdating As Boolean = False

#End Region

#Region "Dialogue Only Fields"

    ''' <summary>
    ''' Danh sách phụ đề từ panel Dialogue Input
    ''' </summary>
    Private _dialogueLines As New List(Of SubtitleLine)()

    ''' <summary>
    ''' Định dạng phụ đề của Dialogue tab
    ''' </summary>
    Private _dialogueFormat As SubtitleFormat = SubtitleFormat.Unknown

    ''' <summary>
    ''' Flag để tránh re-entrant khi TextChanged cho Dialogue
    ''' </summary>
    Private _isDialogueUpdating As Boolean = False

#End Region

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

#Region "Dialogue Only - Event Handlers - Input Panel"

    ''' <summary>
    ''' Khi nhập phụ đề vào panel Input → tự động parse và trích xuất thoại
    ''' Output: STT + Tab + Dialogue (để paste vào Excel tự nhận 2 cột)
    ''' </summary>
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

            ' Phát hiện định dạng và parse
            _dialogueFormat = SubtitleParser.DetectFormat(content)
            _dialogueLines = SubtitleParser.Parse(content)

            TxtDialogueFormat.Text = String.Format("({0} - {1} dòng)",
                _dialogueFormat.ToString().ToUpper(), _dialogueLines.Count)

            ' Cập nhật output dialogue
            UpdateDialogueOutput()

        Catch ex As Exception
            TxtDialogueFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            System.Diagnostics.Debug.WriteLine("Dialogue parse error: " & ex.ToString())
        Finally
            _isDialogueUpdating = False
        End Try
    End Sub

#End Region

#Region "Dialogue Only - Update Display Methods"

    ''' <summary>
    ''' Cập nhật output dialogue panel
    ''' Format: STT + Tab + Dialogue (Tab-separated để paste vào Excel tự nhận 2 cột)
    ''' </summary>
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
                ' Thay thế newline trong thoại bằng space để mỗi dòng là 1 row
                Dim cleanText = dialogueText.Replace(Environment.NewLine, " ").Replace(vbCr, " ").Replace(vbLf, " ")
                sb.AppendLine(String.Format("{0}	{1}", lineNum, cleanText))
                lineNum += 1
            End If
        Next

        TxtDialogueOutput.Text = sb.ToString().TrimEnd()
    End Sub

    ''' <summary>
    ''' Lấy text thoại từ dòng phụ đề
    ''' </summary>
    Private Function GetDialogueText(line As SubtitleLine) As String
        Dim assLine = TryCast(line, AssSubtitleLine)
        If assLine IsNot Nothing Then
            Return assLine.DialogText
        End If

        Dim srtLine = TryCast(line, SrtSubtitleLine)
        If srtLine IsNot Nothing Then
            Return srtLine.Text
        End If

        Return line.OriginalText
    End Function

#End Region

#Region "Dialogue Only - Toast Notification"

    ''' <summary>
    ''' Hiển thị thông báo toast tự ẩn sau 2 giây cho Dialogue tab
    ''' </summary>
    Private Async Sub ShowToastDialogue(message As String)
        ToastTextDialogue.Text = message
        ToastBorderDialogue.Visibility = Visibility.Visible

        ' Tự ẩn sau 2 giây
        Await Task.Delay(2000)
        ToastBorderDialogue.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Dialogue Only - Button Handlers"

    ''' <summary>
    ''' Button Copy Dialogue: Copy nội dung Dialogue vào clipboard
    ''' Format: STT + Tab + Dialogue (paste vào Excel tự nhận 2 cột)
    ''' </summary>
    Private Sub BtnCopyDialogue_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtDialogueOutput.Text) Then Return

        Try
            Clipboard.SetText(TxtDialogueOutput.Text)
            ShowToastDialogue("📋 Đã copy Dialogue vào clipboard! Paste vào Excel sẽ tự nhận 2 cột.")
        Catch ex As Exception
            MessageBox.Show("Lỗi khi copy: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

#Region "Merge Tab - Toast Notification"

    ''' <summary>
    ''' Hiển thị thông báo toast tự ẩn sau 2 giây cho Merge tab
    ''' </summary>
    Private Async Sub ShowToastMerge(message As String)
        ToastTextMerge.Text = message
        ToastBorderMerge.Visibility = Visibility.Visible

        ' Tự ẩn sau 2 giây
        Await Task.Delay(2000)
        ToastBorderMerge.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Merge Tab - Event Handlers - Engsub Panel"

    ''' <summary>
    ''' Khi nội dung Engsub thay đổi → tự động parse và cập nhật Merge + Merge Unbreak
    ''' </summary>
    Private Sub TxtEngsub_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isMergeUpdating Then Return

        Try
            _isMergeUpdating = True
            Dim content = TxtEngsub.Text

            If String.IsNullOrWhiteSpace(content) Then
                _engLines.Clear()
                ' Nếu không có engsub, merge sẽ chỉ còn vietsub
                UpdateMergeDisplays()
                Return
            End If

            ' Phát hiện định dạng và parse
            ' Nếu chưa có định dạng, dùng định dạng từ Engsub
            Dim detectedFormat = SubtitleParser.DetectFormat(content)
            If _mergeFormat = SubtitleFormat.Unknown AndAlso detectedFormat <> SubtitleFormat.Unknown Then
                _mergeFormat = detectedFormat
            End If

            _engLines = SubtitleParser.Parse(content)
            TxtEngsubFormat.Text = String.Format("({0} - {1} dòng)",
                detectedFormat.ToString().ToUpper(), _engLines.Count)

            ' Cập nhật Merge và Merge Unbreak
            UpdateMergeDisplays()

        Catch ex As Exception
            TxtEngsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            System.Diagnostics.Debug.WriteLine("Engsub parse error: " & ex.ToString())
        Finally
            _isMergeUpdating = False
        End Try
    End Sub

#End Region

#Region "Merge Tab - Event Handlers - Vietsub Panel"

    ''' <summary>
    ''' Khi nội dung Vietsub thay đổi → tự động parse và cập nhật Merge + Merge Unbreak
    ''' </summary>
    Private Sub TxtVietsub_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _isMergeUpdating Then Return

        Try
            _isMergeUpdating = True
            Dim content = TxtVietsub.Text

            If String.IsNullOrWhiteSpace(content) Then
                _vietLines.Clear()
                ' Nếu không có vietsub, merge sẽ chỉ còn engsub
                UpdateMergeDisplays()
                Return
            End If

            ' Phát hiện định dạng và parse
            ' Nếu chưa có định dạng, dùng định dạng từ Vietsub
            Dim detectedFormat = SubtitleParser.DetectFormat(content)
            If _mergeFormat = SubtitleFormat.Unknown AndAlso detectedFormat <> SubtitleFormat.Unknown Then
                _mergeFormat = detectedFormat
            End If

            _vietLines = SubtitleParser.Parse(content)
            TxtVietsubFormat.Text = String.Format("({0} - {1} dòng)",
                detectedFormat.ToString().ToUpper(), _vietLines.Count)

            ' Cập nhật Merge và Merge Unbreak
            UpdateMergeDisplays()

        Catch ex As Exception
            TxtVietsubFormat.Text = String.Format("(Lỗi: {0})", ex.Message)
            System.Diagnostics.Debug.WriteLine("Vietsub parse error: " & ex.ToString())
        Finally
            _isMergeUpdating = False
        End Try
    End Sub

#End Region

#Region "Merge Tab - Update Display Methods"

    ''' <summary>
    ''' Cập nhật hiển thị Merge và Merge Unbreak
    ''' </summary>
    Private Sub UpdateMergeDisplays()
        ' Merge
        Dim mergedLines = MergeService.MergeSubtitles(_engLines, _vietLines, _mergeFormat)
        If mergedLines.Count > 0 Then
            TxtMerge.Text = SubtitleParser.ToText(mergedLines, _mergeFormat)
        Else
            TxtMerge.Text = ""
        End If

        ' Merge Unbreak
        Dim unbreakLines = MergeService.MergeUnbreak(_engLines, _vietLines, _mergeFormat)
        If unbreakLines.Count > 0 Then
            TxtMergeUnbreak.Text = SubtitleParser.ToText(unbreakLines, _mergeFormat)
        Else
            TxtMergeUnbreak.Text = ""
        End If
    End Sub

#End Region

#Region "Merge Tab - Button Handlers"

    ''' <summary>
    ''' Button Copy Merge: Copy nội dung Merge vào clipboard
    ''' </summary>
    Private Sub BtnCopyMerge_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMerge.Text) Then Return

        Try
            Clipboard.SetText(TxtMerge.Text)
            ShowToastMerge("📋 Đã copy Merge vào clipboard!")
        Catch ex As Exception
            MessageBox.Show("Lỗi khi copy: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Button Copy Merge Unbreak: Copy nội dung Merge Unbreak vào clipboard
    ''' </summary>
    Private Sub BtnCopyMergeUnbreak_Click(sender As Object, e As RoutedEventArgs)
        If String.IsNullOrWhiteSpace(TxtMergeUnbreak.Text) Then Return

        Try
            Clipboard.SetText(TxtMergeUnbreak.Text)
            ShowToastMerge("📋 Đã copy Merge Unbreak vào clipboard!")
        Catch ex As Exception
            MessageBox.Show("Lỗi khi copy: " & ex.Message, "Lỗi",
                          MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

#End Region

End Class
