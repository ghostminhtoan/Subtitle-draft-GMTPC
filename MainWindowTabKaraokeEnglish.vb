Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Karaoke English - Fields"

    Private _isKaraokeEngUpdating As Boolean = False

#End Region

#Region "Karaoke English - Event Handlers"

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

#End Region

#Region "Karaoke English - Toast"

    Private Async Sub ShowToastKaraokeEng(message As String)
        ToastTextKaraokeEng.Text = message
        ToastBorderKaraokeEng.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraokeEng.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Karaoke English - Copy Button"

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

End Class
