Imports Subtitle_draft_GMTPC.Models

Namespace Services
    ''' <summary>
    ''' Service xử lý merge phụ đề: gộp Engsub và Vietsub
    ''' </summary>
    Public Class MergeService

        ''' <summary>
        ''' Gộp Vietsub (trên) và Engsub (dưới) dựa theo timecode
        ''' Vietsub luôn được ưu tiên ở trên, Engsub ở dưới
        ''' Nếu chỉ có 1 trong 2 thì vẫn giữ nguyên
        ''' </summary>
        Public Shared Function MergeSubtitles(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            If engLines Is Nothing Then engLines = New List(Of SubtitleLine)()
            If vietLines Is Nothing Then vietLines = New List(Of SubtitleLine)()

            ' Nếu không có gì thì trả về rỗng
            If engLines.Count = 0 AndAlso vietLines.Count = 0 Then
                Return New List(Of SubtitleLine)()
            End If

            ' Nếu chỉ có Engsub
            If vietLines.Count = 0 Then
                Return engLines.ToList()
            End If

            ' Nếu chỉ có Vietsub
            If engLines.Count = 0 Then
                Return vietLines.ToList()
            End If

            Dim result = New List(Of SubtitleLine)()
            Dim engIndex As Integer = 0
            Dim vietIndex As Integer = 0

            While engIndex < engLines.Count OrElse vietIndex < vietLines.Count
                Dim engLine As SubtitleLine = If(engIndex < engLines.Count, engLines(engIndex), Nothing)
                Dim vietLine As SubtitleLine = If(vietIndex < vietLines.Count, vietLines(vietIndex), Nothing)

                ' So sánh start time để quyết định dòng nào trước
                Dim engStartMs As Long = If(engLine IsNot Nothing, SubtitleLine.ToMilliseconds(engLine.StartTime), Long.MaxValue)
                Dim vietStartMs As Long = If(vietLine IsNot Nothing, SubtitleLine.ToMilliseconds(vietLine.StartTime), Long.MaxValue)

                ' Tính tolerance (50ms) để coi là cùng thời điểm
                Dim tolerance As Long = 50
                Dim isSameTime As Boolean = Math.Abs(engStartMs - vietStartMs) <= tolerance AndAlso engLine IsNot Nothing AndAlso vietLine IsNot Nothing

                If isSameTime Then
                    ' Cùng thời điểm → gộp Vietsub trên, Engsub dưới
                    Dim mergedLine = MergeTwoLines(vietLine, engLine, format)
                    result.Add(mergedLine)
                    engIndex += 1
                    vietIndex += 1
                ElseIf vietStartMs < engStartMs Then
                    ' Vietsub trước → thêm vietsub
                    result.Add(vietLine.Clone())
                    vietIndex += 1
                Else
                    ' Engsub trước → thêm engsub
                    result.Add(engLine.Clone())
                    engIndex += 1
                End If
            End While

            Return result
        End Function

        ''' <summary>
        ''' Gộp 2 dòng phụ đề: Vietsub trên, Engsub dưới
        ''' </summary>
        Private Shared Function MergeTwoLines(vietLine As SubtitleLine, engLine As SubtitleLine, format As SubtitleFormat) As SubtitleLine
            ' Sử dụng time của Vietsub (ưu tiên)
            Dim startTime = vietLine.StartTime
            Dim endTime = vietLine.EndTime

            ' Nếu Engsub có thời gian dài hơn thì dùng của Engsub
            Dim engEndMs = SubtitleLine.ToMilliseconds(engLine.EndTime)
            Dim vietEndMs = SubtitleLine.ToMilliseconds(vietLine.EndTime)
            If engEndMs > vietEndMs Then
                endTime = engLine.EndTime
            End If

            ' Lấy text từ 2 dòng
            Dim vietText = GetLineText(vietLine)
            Dim engText = GetLineText(engLine)

            ' Gộp: Vietsub trên, Engsub dưới
            Dim mergedText As String
            If format = SubtitleFormat.Ass Then
                mergedText = vietText & "\N" & engText
            Else
                mergedText = vietText & Environment.NewLine & engText
            End If

            ' Tạo dòng mới dựa trên format
            If TypeOf vietLine Is AssSubtitleLine Then
                Dim assLine = DirectCast(vietLine.Clone(), AssSubtitleLine)
                assLine.DialogText = mergedText
                assLine.StartTime = startTime
                assLine.EndTime = endTime
                assLine.Content = assLine.RebuildLine()
                Return assLine
            Else
                Dim srtLine = DirectCast(vietLine.Clone(), SrtSubtitleLine)
                srtLine.Text = mergedText
                srtLine.StartTime = startTime
                srtLine.EndTime = endTime
                srtLine.Content = srtLine.RebuildLine()
                Return srtLine
            End If
        End Function

        ''' <summary>
        ''' Lấy text từ dòng phụ đề
        ''' </summary>
        Private Shared Function GetLineText(line As SubtitleLine) As String
            Dim assLine = TryCast(line, AssSubtitleLine)
            If assLine IsNot Nothing Then
                Return assLine.DialogText
            End If

            Dim srtLine = TryCast(line, SrtSubtitleLine)
            If srtLine IsNot Nothing Then
                Return srtLine.Text
            End If

            Return line.Content
        End Function

        ''' <summary>
        ''' Merge Unbreak: Giống Merge nhưng xóa dòng xuống dòng
        ''' - ASS: thay \N, \n bằng space
        ''' - SRT: thay Environment.NewLine bằng space
        ''' </summary>
        Public Shared Function MergeUnbreak(engLines As List(Of SubtitleLine), vietLines As List(Of SubtitleLine), format As SubtitleFormat) As List(Of SubtitleLine)
            Dim mergedLines = MergeSubtitles(engLines, vietLines, format)

            For Each line In mergedLines
                Dim assLine = TryCast(line, AssSubtitleLine)
                If assLine IsNot Nothing Then
                    ' ASS: thay thế \N và \n bằng space
                    assLine.DialogText = assLine.DialogText.Replace("\N", " ").Replace("\n", " ")
                    assLine.Content = assLine.RebuildLine()
                    Continue For
                End If

                Dim srtLine = TryCast(line, SrtSubtitleLine)
                If srtLine IsNot Nothing Then
                    ' SRT: thay thế newline bằng space
                    srtLine.Text = srtLine.Text.Replace(Environment.NewLine, " ").Replace(vbLf, " ").Replace(vbCr, " ")
                    srtLine.Content = srtLine.RebuildLine()
                End If
            Next

            Return mergedLines
        End Function
    End Class
End Namespace
