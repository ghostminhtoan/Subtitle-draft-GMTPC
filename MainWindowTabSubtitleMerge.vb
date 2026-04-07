Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Subtitle Merge - Fields"

    Private _engLines As New List(Of SubtitleLine)()
    Private _vietLines As New List(Of SubtitleLine)()
    Private _mergeFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isMergeUpdating As Boolean = False

#End Region

#Region "Subtitle Merge - Event Handlers"

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

#Region "Subtitle Merge - Display Methods"

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

#End Region

#Region "Subtitle Merge - Toast"

    Private Async Sub ShowToastMerge(message As String)
        ToastTextMerge.Text = message
        ToastBorderMerge.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderMerge.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
