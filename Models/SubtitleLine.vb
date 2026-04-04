Imports System.Text

Namespace Models
    ''' <summary>
    ''' Lớp trừu tượng đại diện cho một dòng phụ đề
    ''' </summary>
    Public MustInherit Class SubtitleLine
        ''' <summary>
        ''' Text gốc của dòng (dòng raw trong file)
        ''' </summary>
        Public Property OriginalText As String

        ''' <summary>
        ''' Thời gian bắt đầu
        ''' </summary>
        Public Property StartTime As TimeSpan

        ''' <summary>
        ''' Thời gian kết thúc
        ''' </summary>
        Public Property EndTime As TimeSpan

        ''' <summary>
        ''' Nội dung text sẽ được rebuild (có thể bị thay đổi time)
        ''' </summary>
        Public Property Content As String

        Protected Sub New()
        End Sub

        ''' <summary>
        ''' Chuyển TimeSpan về milliseconds (Long)
        ''' </summary>
        Public Shared Function ToMilliseconds(ts As TimeSpan) As Long
            Return CLng(ts.TotalMilliseconds)
        End Function

        ''' <summary>
        ''' Chuyển milliseconds về TimeSpan
        ''' </summary>
        Public Shared Function FromMilliseconds(ms As Long) As TimeSpan
            If ms < 0 Then ms = 0
            Return TimeSpan.FromMilliseconds(ms)
        End Function

        ''' <summary>
        ''' Format time theo định dạng ASS: H:MM:SS.ff
        ''' </summary>
        Public Shared Function FormatAssTime(ts As TimeSpan) As String
            Dim totalMs = CLng(ts.TotalMilliseconds)
            If totalMs < 0 Then totalMs = 0
            Dim hours = totalMs \ 3600000
            Dim remaining = totalMs Mod 3600000
            Dim minutes = remaining \ 60000
            remaining = remaining Mod 60000
            Dim seconds = remaining \ 1000
            remaining = remaining Mod 1000
            Dim centiseconds = remaining \ 10
            Return String.Format("{0}:{1:D2}:{2:D2}.{3:D2}", hours, minutes, seconds, centiseconds)
        End Function

        ''' <summary>
        ''' Format time theo định dạng SRT: HH:MM:SS,fff
        ''' </summary>
        Public Shared Function FormatSrtTime(ts As TimeSpan) As String
            Dim totalMs = CLng(ts.TotalMilliseconds)
            If totalMs < 0 Then totalMs = 0
            Dim hours = totalMs \ 3600000
            Dim remaining = totalMs Mod 3600000
            Dim minutes = remaining \ 60000
            remaining = remaining Mod 60000
            Dim seconds = remaining \ 1000
            remaining = remaining Mod 1000
            Dim milliseconds = remaining
            Return String.Format("{0:D2}:{1:D2}:{2:D3},{3:D3}", hours, minutes, seconds, milliseconds)
        End Function

        ''' <summary>
        ''' Parse time từ string định dạng ASS: H:MM:SS.ff hoặc HH:MM:SS.ff
        ''' </summary>
        Public Shared Function ParseAssTime(timeStr As String) As TimeSpan
            If String.IsNullOrWhiteSpace(timeStr) Then Return TimeSpan.Zero
            timeStr = timeStr.Trim()
            Try
                ' Format: H:MM:SS.ff hoặc HH:MM:SS.ff
                Dim parts = timeStr.Split(":"c)
                If parts.Length <> 3 Then Return TimeSpan.Zero

                Dim hours = Integer.Parse(parts(0))
                Dim minutes = Integer.Parse(parts(1))
                Dim secParts = parts(2).Split("."c)
                Dim seconds = Integer.Parse(secParts(0))
                Dim centiseconds = 0
                If secParts.Length > 1 Then
                    centiseconds = Integer.Parse(secParts(1))
                End If

                Dim totalMs = hours * 3600000L + minutes * 60000L + seconds * 1000L + centiseconds * 10L
                Return FromMilliseconds(totalMs)
            Catch
                Return TimeSpan.Zero
            End Try
        End Function

        ''' <summary>
        ''' Parse time từ string định dạng SRT: HH:MM:SS,fff
        ''' </summary>
        Public Shared Function ParseSrtTime(timeStr As String) As TimeSpan
            If String.IsNullOrWhiteSpace(timeStr) Then Return TimeSpan.Zero
            timeStr = timeStr.Trim()
            Try
                Dim parts = timeStr.Split(":"c)
                If parts.Length <> 3 Then Return TimeSpan.Zero

                Dim hours = Integer.Parse(parts(0))
                Dim minutes = Integer.Parse(parts(1))
                Dim secParts = parts(2).Split(","c)
                Dim seconds = Integer.Parse(secParts(0))
                Dim milliseconds = 0
                If secParts.Length > 1 Then
                    milliseconds = Integer.Parse(secParts(1))
                End If

                Dim totalMs = hours * 3600000L + minutes * 60000L + seconds * 1000L + milliseconds
                Return FromMilliseconds(totalMs)
            Catch
                Return TimeSpan.Zero
            End Try
        End Function

        ''' <summary>
        ''' Rebuild lại dòng với time mới (phải override)
        ''' </summary>
        Public MustOverride Function RebuildLine() As String

        ''' <summary>
        ''' Clone dòng phụ đề
        ''' </summary>
        Public MustOverride Function Clone() As SubtitleLine
    End Class
End Namespace
