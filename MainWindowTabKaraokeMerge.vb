Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Karaoke Merge - Fields"

    Private _isKaraokeMergeUpdating As Boolean = False

#End Region

#Region "Karaoke Merge - Event Handlers"

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

#End Region

#Region "Karaoke Merge - Toast"

    Private Async Sub ShowToastKaraokeMerge(message As String)
        ToastTextKaraokeMerge.Text = message
        ToastBorderKaraokeMerge.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderKaraokeMerge.Visibility = Visibility.Collapsed
    End Sub

#End Region

#Region "Karaoke Merge - Copy Button"

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

End Class
