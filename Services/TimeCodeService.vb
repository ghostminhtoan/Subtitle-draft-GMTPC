Imports Subtitle_draft_GMTPC.Models

Namespace Services
    ''' <summary>
    ''' Service xử lý time code: shift time, connect gap
    ''' </summary>
    Public Class TimeCodeService

        ''' <summary>
        ''' Dịch chuyển tất cả start time và end time thêm (milliseconds)
        ''' Nếu thời gian nhỏ hơn 0 thì giữ nguyên 0
        ''' </summary>
        Public Shared Function ShiftTime(lines As List(Of SubtitleLine), milliseconds As Integer) As List(Of SubtitleLine)
            If lines Is Nothing OrElse lines.Count = 0 Then Return lines

            Dim result = New List(Of SubtitleLine)()

            For Each line In lines
                Dim cloned = line.Clone()

                ' Shift start time
                Dim newStartMs = SubtitleLine.ToMilliseconds(cloned.StartTime) + milliseconds
                If newStartMs < 0 Then newStartMs = 0
                cloned.StartTime = SubtitleLine.FromMilliseconds(newStartMs)

                ' Shift end time
                Dim newEndMs = SubtitleLine.ToMilliseconds(cloned.EndTime) + milliseconds
                If newEndMs < 0 Then newEndMs = 0
                cloned.EndTime = SubtitleLine.FromMilliseconds(newEndMs)

                ' Rebuild content
                cloned.Content = cloned.RebuildLine()

                result.Add(cloned)
            Next

            Return result
        End Function

        ''' <summary>
        ''' Nối gap giữa các dòng phụ đề
        ''' Gap = StartTime dòng dưới - EndTime dòng trên
        ''' Nếu gap > 0 → extend EndTime dòng trên = StartTime dòng dưới
        ''' Start time luôn giữ nguyên, chỉ sửa End time dòng trên
        ''' </summary>
        Public Shared Function ConnectGap(lines As List(Of SubtitleLine)) As List(Of SubtitleLine)
            If lines Is Nothing OrElse lines.Count = 0 Then Return lines

            Dim result = New List(Of SubtitleLine)()

            ' Clone tất cả dòng trước
            For Each line In lines
                result.Add(line.Clone())
            Next

            ' Duyệt từ trên xuống dưới
            For i As Integer = 0 To result.Count - 2
                Dim currentLine = result(i)
                Dim nextLine = result(i + 1)

                ' Tính gap = Start dòng dưới - End dòng trên
                Dim currentEndMs = SubtitleLine.ToMilliseconds(currentLine.EndTime)
                Dim nextStartMs = SubtitleLine.ToMilliseconds(nextLine.StartTime)
                Dim gap = nextStartMs - currentEndMs

                ' Nếu gap > 0 → extend EndTime dòng trên đến StartTime dòng dưới
                If gap > 0 Then
                    currentLine.EndTime = nextLine.StartTime
                    currentLine.Content = currentLine.RebuildLine()
                End If
            Next

            Return result
        End Function
    End Class
End Namespace
