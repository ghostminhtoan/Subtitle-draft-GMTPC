Imports System.Text
Imports Subtitle_draft_GMTPC.Models
Imports Subtitle_draft_GMTPC.Services

Partial Class MainWindow

#Region "Dialogue Only - Fields"

    Private _dialogueLines As New List(Of SubtitleLine)()
    Private _dialogueFormat As SubtitleFormat = SubtitleFormat.Unknown
    Private _isDialogueUpdating As Boolean = False
    Private _dialogueManualTexts As New Dictionary(Of Integer, String)()

#End Region

#Region "Dialogue Only - Event Handlers"

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

#Region "Dialogue Only - Display Methods"

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
    ''' Parse text từ Panel 3: Hỗ trợ 2 format
    ''' Format 1 - 3 cột (ngang): STT&lt;TAB&gt;Text gốc&lt;TAB&gt;Text tiếng Việt
    ''' Format 2 - 3 hàng (dọc): Mỗi nhóm 3 dòng (STT / Text gốc / Text tiếng Việt)
    ''' Luôn luôn lấy text ở cột text (cột 2 hoặc dòng 2) để merge với time code từ Panel 1
    ''' </summary>
    Private Sub ParseManualText()
        _dialogueManualTexts.Clear()
        Dim content = TxtDialogueManual.Text
        If String.IsNullOrWhiteSpace(content) Then Return

        Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        If lines.Length = 0 Then Return

        ' Bước 1: Xác định format (3 cột hay 3 hàng)
        Dim is3ColumnFormat = False

        ' Kiểm tra dòng đầu tiên xem có chứa tab và có >= 2 phần tử không
        If lines.Length > 0 Then
            Dim firstLine = lines(0).Trim()
            If firstLine.Contains(vbTab) Then
                Dim parts = firstLine.Split({vbTab}, StringSplitOptions.None)
                If parts.Length >= 2 Then
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
            ' Lấy cột 2 (index 1) làm text để merge
            For Each line In lines
                Dim trimmed = line.Trim()
                If String.IsNullOrWhiteSpace(trimmed) Then Continue For

                If trimmed.Contains(vbTab) Then
                    Dim parts = trimmed.Split({vbTab}, StringSplitOptions.None)
                    If parts.Length >= 2 Then
                        Dim stt As Integer = 0
                        If Integer.TryParse(parts(0).Trim(), stt) Then
                            ' Lấy cột 2 (index 1) làm text - Text gốc
                            _dialogueManualTexts(stt) = parts(1).Trim()
                        End If
                    End If
                End If
            Next
        Else
            ' Format 3 hàng: Mỗi nhóm 3 dòng (STT / Text gốc / Text tiếng Việt)
            ' Lấy dòng 2 (Text gốc) làm text để merge
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
                    ' Dòng 2: Text gốc (lưu lại làm text để merge)
                    If hasValidStt AndAlso currentStt > 0 Then
                        _dialogueManualTexts(currentStt) = trimmed
                    End If
                ElseIf positionInGroup = 2 Then
                    ' Dòng 3: Text tiếng Việt (bỏ qua)
                    ' Không cần làm gì
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

#Region "Dialogue Only - Toast"

    Private Async Sub ShowToastDialogue(message As String)
        ToastTextDialogue.Text = message
        ToastBorderDialogue.Visibility = Visibility.Visible
        Await Task.Delay(2000)
        ToastBorderDialogue.Visibility = Visibility.Collapsed
    End Sub

#End Region

End Class
