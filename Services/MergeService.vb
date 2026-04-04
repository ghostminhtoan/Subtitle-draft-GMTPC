Imports Subtitle_draft_GMTPC.Models

Namespace Services
    ''' <summary>
    ''' Service xử lý merge phụ đề: gộp Engsub và Vietsub
    ''' QUY TẮC: Panel 2 (Vietsub) LUÔN ở trên, Panel 1 (Engsub) LUÔN ở dưới
    ''' </summary>
    Public Class MergeService

        ''' <summary>
        ''' Merge: Panel 2 (Vietsub) TRƯỚC → Panel 1 (Engsub) SAU
        ''' KHÔNG QUAN TÂM ngôn ngữ, chỉ quan trọng panel nào
        ''' </summary>
        Public Shared Function MergeSubtitles(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            If engLines Is Nothing Then engLines = New List(Of SubtitleLine)()
            If vietLines Is Nothing Then vietLines = New List(Of SubtitleLine)()

            If engLines.Count = 0 AndAlso vietLines.Count = 0 Then
                Return New List(Of SubtitleLine)()
            End If

            Dim result = New List(Of SubtitleLine)()

            ' LUÔN thêm Panel 2 (Vietsub) TRƯỚC
            For Each line In vietLines
                result.Add(line.Clone())
            Next

            ' SAU ĐÓ mới thêm Panel 1 (Engsub)
            For Each line In engLines
                result.Add(line.Clone())
            Next

            Return result
        End Function

        ''' <summary>
        ''' Merge (no break line): Giống Merge nhưng xóa newline trong mỗi dòng
        ''' Panel 2 (Vietsub) TRƯỚC → Panel 1 (Engsub) SAU
        ''' </summary>
        Public Shared Function MergeUnbreak(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            ' Gọi MergeSubtitles trước (đã đúng thứ tự Viet → Eng)
            Dim mergedLines = MergeSubtitles(engLines, vietLines, format)

            ' Xóa newline trong mỗi dòng (xử lý null an toàn)
            For Each line In mergedLines
                Dim assLine = TryCast(line, AssSubtitleLine)
                If assLine IsNot Nothing Then
                    If assLine.DialogText IsNot Nothing Then
                        assLine.DialogText = assLine.DialogText.Replace("\N", " ").Replace("\n", " ")
                    End If
                    assLine.Content = assLine.RebuildLine()
                    Continue For
                End If

                Dim srtLine = TryCast(line, SrtSubtitleLine)
                If srtLine IsNot Nothing Then
                    If srtLine.Text IsNot Nothing Then
                        srtLine.Text = srtLine.Text.Replace(Environment.NewLine, " ").Replace(vbLf, " ").Replace(vbCr, " ")
                    End If
                    srtLine.Content = srtLine.RebuildLine()
                End If
            Next

            Return mergedLines
        End Function
    End Class
End Namespace
