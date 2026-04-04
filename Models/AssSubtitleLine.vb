Imports System.Text

Namespace Models
    ''' <summary>
    ''' Dòng phụ đề định dạng ASS
    ''' Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
    ''' Ví dụ: 0,0:05:13.51,0:05:14.98,Default,,0000,0000,0000,,Where's my bike?
    ''' Chú ý: ",," giữa MarginV và Text có thể là empty effect
    ''' </summary>
    Public Class AssSubtitleLine
        Inherits SubtitleLine

        Public Property Layer As String
        Public Property Style As String
        Public Property Name As String
        Public Property MarginL As String
        Public Property MarginR As String
        Public Property MarginV As String
        Public Property Effect As String
        Public Property DialogText As String

        ''' <summary>
        ''' Dòng Dialogue header (thường là "Dialogue")
        ''' </summary>
        Public Property Header As String

        ''' <summary>
        ''' Constructor rỗng
        ''' </summary>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Constructor từ dòng raw text
        ''' </summary>
        Public Sub New(originalLine As String)
            OriginalText = originalLine
            ParseLine(originalLine)
        End Sub

        ''' <summary>
        ''' Parse dòng ASS để lấy thông tin
        ''' </summary>
        Private Sub ParseLine(line As String)
            If String.IsNullOrWhiteSpace(line) Then Return

            ' Tìm "Dialogue:" hoặc "Dialogue: "
            Dim dialogueIndex = line.IndexOf("Dialogue:")
            If dialogueIndex >= 0 Then
                Header = "Dialogue"
                Dim rest = line.Substring(dialogueIndex + 9).Trim()
                ParseDialogueContent(rest)
            Else
                ' Nếu không có "Dialogue:" thì coi như toàn bộ là content
                Content = line
            End If
        End Sub

        ''' <summary>
        ''' Parse phần sau "Dialogue:"
        ''' Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,,Text
        ''' Text có thể chứa dấu "," nên phải tìm ",," cuối cùng trước text
        ''' </summary>
        Private Sub ParseDialogueContent(content As String)
            Content = content

            ' ASS format có 9 fields cố định, field thứ 10 là text (có thể chứa ",")
            ' Tách bằng cách đếm dấu "," - lấy 9 fields đầu
            ' Format: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
            ' Index:   0    1     2    3     4     5       6       7       8      9+

            Dim parts = New List(Of String)()
            Dim current = New StringBuilder()
            Dim commaCount = 0

            For i As Integer = 0 To content.Length - 1
                Dim c = content(i)

                If c = ","c AndAlso commaCount < 9 Then
                    ' Dấu "," phân cách field
                    parts.Add(current.ToString())
                    current.Clear()
                    commaCount += 1
                Else
                    current.Append(c)
                End If
            Next

            ' Phần còn lại là text (field thứ 10 trở đi)
            parts.Add(current.ToString())

            If parts.Count >= 10 Then
                Layer = parts(0)
                Dim startTimeStr = parts(1)
                Dim endTimeStr = parts(2)
                Style = parts(3)
                Name = parts(4)
                MarginL = parts(5)
                MarginR = parts(6)
                MarginV = parts(7)
                Effect = parts(8)
                DialogText = parts(9)

                StartTime = ParseAssTime(startTimeStr)
                EndTime = ParseAssTime(endTimeStr)
            End If
        End Sub

        ''' <summary>
        ''' Rebuild lại dòng ASS với time mới
        ''' </summary>
        Public Overrides Function RebuildLine() As String
            Dim timePart = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                Layer,
                FormatAssTime(StartTime),
                FormatAssTime(EndTime),
                Style,
                Name,
                MarginL,
                MarginR,
                MarginV,
                Effect)

            Return String.Format("Dialogue: {0},{1}", timePart, DialogText)
        End Function

        ''' <summary>
        ''' Clone dòng ASS
        ''' </summary>
        Public Overrides Function Clone() As SubtitleLine
            Dim newLine = New AssSubtitleLine()
            newLine.OriginalText = Me.OriginalText
            newLine.StartTime = Me.StartTime
            newLine.EndTime = Me.EndTime
            newLine.Content = Me.Content
            newLine.Layer = Me.Layer
            newLine.Style = Me.Style
            newLine.Name = Me.Name
            newLine.MarginL = Me.MarginL
            newLine.MarginR = Me.MarginR
            newLine.MarginV = Me.MarginV
            newLine.Effect = Me.Effect
            newLine.DialogText = Me.DialogText
            newLine.Header = Me.Header
            Return newLine
        End Function
    End Class
End Namespace
