Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Karaoke Vietnamese - Fields"

    Private _isKaraokeUpdating As Boolean = False

#End Region

#Region "Karaoke Vietnamese - Event Handlers"

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

#End Region

#Region "Karaoke Vietnamese - Toast"

    Private Async Sub ShowToastKaraoke(message As String)
        ToastTextKaraoke.Text = message
        ToastBorderKaraoke.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraoke.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Karaoke Vietnamese - Copy Button"

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

End Class
