Imports Subtitle_draft_GMTPC.Models

Namespace Services
    ''' <summary>
    ''' Service xử lý merge phụ đề: gộp Engsub và Vietsub
    ''' Chiến lược: Tất cả Vietsub ở trên, tất cả Engsub ở dưới
    ''' </summary>
    Public Class MergeService

        ''' <summary>
        ''' Merge: Vietsub trước (giữ nguyên timecode), Engsub sau (giữ nguyên timecode)
        ''' Không gộp từng cặp, mà xuất toàn bộ Viet rồi đến toàn bộ Eng
        ''' </summary>
        Public Shared Function MergeSubtitles(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            If engLines Is Nothing Then engLines = New List(Of SubtitleLine)()
            If vietLines Is Nothing Then vietLines = New List(Of SubtitleLine)()

            If engLines.Count = 0 AndAlso vietLines.Count = 0 Then
                Return New List(Of SubtitleLine)()
            End If

            Dim result = New List(Of SubtitleLine)()

            ' Thêm tất cả Vietsub trước
            For Each line In vietLines
                result.Add(line.Clone())
            Next

            ' Thêm tất cả Engsub sau
            For Each line In engLines
                result.Add(line.Clone())
            Next

            Return result
        End Function

        ''' <summary>
        ''' Merge Unbreak: Giống Merge nhưng xóa newline trong mỗi dòng
        ''' </summary>
        Public Shared Function MergeUnbreak(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            Dim mergedLines = MergeSubtitles(engLines, vietLines, format)

            For Each line In mergedLines
                Dim assLine = TryCast(line, AssSubtitleLine)
                If assLine IsNot Nothing Then
                    assLine.DialogText = assLine.DialogText.Replace("\N", " ").Replace("\n", " ")
                    assLine.Content = assLine.RebuildLine()
                    Continue For
                End If

                Dim srtLine = TryCast(line, SrtSubtitleLine)
                If srtLine IsNot Nothing Then
                    srtLine.Text = srtLine.Text.Replace(Environment.NewLine, " ").Replace(vbLf, " ").Replace(vbCr, " ")
                    srtLine.Content = srtLine.RebuildLine()
                End If
            Next

            Return mergedLines
        End Function
    End Class
End Namespace
