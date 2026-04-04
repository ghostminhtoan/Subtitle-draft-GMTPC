Imports System.Text
Imports System.Text.RegularExpressions
Imports Subtitle_draft_GMTPC.Models

Namespace Services
    ''' <summary>
    ''' Service phân tích cú pháp phụ đề SRT và ASS
    ''' </summary>
    Public Class SubtitleParser

        ''' <summary>
        ''' Phát hiện định dạng phụ đề từ nội dung
        ''' </summary>
        Public Shared Function DetectFormat(content As String) As SubtitleFormat
            If String.IsNullOrWhiteSpace(content) Then Return SubtitleFormat.Unknown

            Dim trimmed = content.Trim()

            ' ASS thường có [Script Info] hoặc [Events] hoặc dòng Dialogue:
            If trimmed.Contains("[Script Info]") OrElse trimmed.Contains("[Events]") OrElse trimmed.Contains("Dialogue:") Then
                Return SubtitleFormat.Ass
            End If

            ' SRT thường có pattern: so --> so
            If Regex.IsMatch(trimmed, "\d{1,2}:\d{2}:\d{2}[,.]\d{2,3}\s*-->\s*\d{1,2}:\d{2}:\d{2}[,.]\d{2,3}") Then
                Return SubtitleFormat.Srt
            End If

            Return SubtitleFormat.Unknown
        End Function

        ''' <summary>
        ''' Parse nội dung phụ đề thành danh sách các dòng
        ''' </summary>
        Public Shared Function Parse(content As String) As List(Of SubtitleLine)
            Dim format = DetectFormat(content)
            Select Case format
                Case SubtitleFormat.Ass
                    Return ParseAss(content)
                Case SubtitleFormat.Srt
                    Return ParseSrt(content)
                Case Else
                    Return New List(Of SubtitleLine)()
            End Select
        End Function

        ''' <summary>
        ''' Parse phụ đề ASS
        ''' </summary>
        Public Shared Function ParseAss(content As String) As List(Of SubtitleLine)
            Dim result = New List(Of SubtitleLine)()
            If String.IsNullOrWhiteSpace(content) Then Return result

            Dim lines = content.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.None)

            For Each line In lines
                Dim trimmed = line.Trim()
                ' Chỉ lấy các dòng Dialogue
                If trimmed.StartsWith("Dialogue:") Then
                    Try
                        Dim assLine = New AssSubtitleLine(line)
                        result.Add(assLine)
                    Catch
                        ' Bo qua dong loi
                    End Try
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Parse phụ đề SRT
        ''' </summary>
        Public Shared Function ParseSrt(content As String) As List(Of SubtitleLine)
            Dim result = New List(Of SubtitleLine)()
            If String.IsNullOrWhiteSpace(content) Then Return result

            ' Chuan hoa line endings
            Dim normalized = content.Replace(vbCr & vbLf, vbLf).Replace(vbCr, vbLf)
            Dim lines = normalized.Split(vbLf)

            Dim i As Integer = 0
            While i < lines.Length
                Dim line = lines(i).Trim()

                ' Bo qua dong trong
                If String.IsNullOrWhiteSpace(line) Then
                    i += 1
                    Continue While
                End If

                ' Thu parse index (so thu tu)
                Dim index As Integer = 0
                If Integer.TryParse(line, index) Then
                    ' Dong tiep theo phai la time line
                    If i + 1 < lines.Length Then
                        Dim timeLine = lines(i + 1).Trim()
                        Dim timeMatch = Regex.Match(timeLine, "(.+?)\s*-->\s*(.+)")

                        If timeMatch.Success Then
                            Dim startTime = SubtitleLine.ParseSrtTime(timeMatch.Groups(1).Value)
                            Dim endTime = SubtitleLine.ParseSrtTime(timeMatch.Groups(2).Value)

                            ' Gom cac dong text tiep theo cho den khi gap dong trong
                            Dim textLines = New List(Of String)()
                            i += 2
                            While i < lines.Length AndAlso Not String.IsNullOrWhiteSpace(lines(i).Trim())
                                textLines.Add(lines(i).TrimEnd())
                                i += 1
                            End While

                            Dim text = String.Join(Environment.NewLine, textLines)
                            Dim srtLine = New SrtSubtitleLine(index, startTime, endTime, text)
                            srtLine.OriginalText = String.Join(Environment.NewLine, {index.ToString(), timeLine, text})
                            srtLine.Content = srtLine.OriginalText
                            result.Add(srtLine)
                        Else
                            i += 1
                        End If
                    Else
                        i += 1
                    End If
                Else
                    i += 1
                End If
            End While

            Return result
        End Function

        ''' <summary>
        ''' Xuat danh sach SubtitleLine thanh noi dung text hoan chinh
        ''' </summary>
        Public Shared Function ToText(lines As List(Of SubtitleLine), format As SubtitleFormat) As String
            If lines Is Nothing OrElse lines.Count = 0 Then Return String.Empty

            If format = SubtitleFormat.Srt Then
                Return ToSrtText(lines)
            Else
                Return ToAssText(lines)
            End If
        End Function

        ''' <summary>
        ''' Xuat ra dinh dang SRT
        ''' </summary>
        Private Shared Function ToSrtText(lines As List(Of SubtitleLine)) As String
            Dim sb = New StringBuilder()
            Dim index = 1

            For Each line In lines
                Dim srtLine = TryCast(line, SrtSubtitleLine)
                If srtLine IsNot Nothing Then
                    srtLine.Index = index
                    sb.AppendLine(srtLine.RebuildLine())
                    sb.AppendLine()
                    index += 1
                Else
                    ' Neu la AssSubtitleLine, chuyen doi sang format SRT
                    Dim timeLine = String.Format("{0} --> {1}", SubtitleLine.FormatSrtTime(line.StartTime), SubtitleLine.FormatSrtTime(line.EndTime))
                    sb.AppendLine(index.ToString())
                    sb.AppendLine(timeLine)

                    Dim assLine = TryCast(line, AssSubtitleLine)
                    If assLine IsNot Nothing Then
                        sb.AppendLine(assLine.DialogText)
                    Else
                        sb.AppendLine(line.OriginalText)
                    End If
                    sb.AppendLine()
                    index += 1
                End If
            Next

            Return sb.ToString().TrimEnd()
        End Function

        ''' <summary>
        ''' Xuat ra dinh dang ASS - chi xuat cac dong Dialogue, khong xuat header
        ''' </summary>
        Private Shared Function ToAssText(lines As List(Of SubtitleLine)) As String
            Dim sb = New StringBuilder()

            For Each line In lines
                Dim assLine = TryCast(line, AssSubtitleLine)
                If assLine IsNot Nothing Then
                    sb.AppendLine(assLine.RebuildLine())
                Else
                    ' Neu la SrtSubtitleLine, chuyen doi sang format ASS
                    Dim srtLine = TryCast(line, SrtSubtitleLine)
                    If srtLine IsNot Nothing Then
                        Dim dialogueLine = String.Format("Dialogue: 0,{0},{1},Default,,0000,0000,0000,,{2}",
                            SubtitleLine.FormatAssTime(line.StartTime),
                            SubtitleLine.FormatAssTime(line.EndTime),
                            srtLine.Text.Replace(Environment.NewLine, "\N"))
                        sb.AppendLine(dialogueLine)
                    End If
                End If
            Next

            Return sb.ToString().TrimEnd()
        End Function

        ''' <summary>
        ''' Xuat danh sach SubtitleLine thanh noi dung text giu nguyen format goc
        ''' </summary>
        Public Shared Function ToOriginalText(lines As List(Of SubtitleLine), format As SubtitleFormat) As String
            Return ToText(lines, format)
        End Function
    End Class
End Namespace
