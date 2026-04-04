Namespace Models
    ''' <summary>
    ''' Dòng phụ đề định dạng SRT
    ''' Format:
    '''   Index (số thứ tự)
    '''   HH:MM:SS,fff --> HH:MM:SS,fff
    '''   Text
    ''' </summary>
    Public Class SrtSubtitleLine
        Inherits SubtitleLine

        Public Property Index As Integer
        Public Property Text As String

        ''' <summary>
        ''' Constructor rỗng
        ''' </summary>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Constructor từ các thành phần
        ''' </summary>
        Public Sub New(index As Integer, startTime As TimeSpan, endTime As TimeSpan, text As String)
            Me.Index = index
            Me.StartTime = startTime
            Me.EndTime = endTime
            Me.Text = text
        End Sub

        ''' <summary>
        ''' Rebuild lại dòng SRT với time mới
        ''' </summary>
        Public Overrides Function RebuildLine() As String
            Dim timeLine = String.Format("{0} --> {1}", FormatSrtTime(StartTime), FormatSrtTime(EndTime))
            Return String.Format("{0}{1}{2}{1}{3}", Index, Environment.NewLine, timeLine, Text)
        End Function

        ''' <summary>
        ''' Clone dòng SRT
        ''' </summary>
        Public Overrides Function Clone() As SubtitleLine
            Dim newLine = New SrtSubtitleLine()
            newLine.Index = Me.Index
            newLine.StartTime = Me.StartTime
            newLine.EndTime = Me.EndTime
            newLine.Text = Me.Text
            newLine.OriginalText = Me.OriginalText
            newLine.Content = Me.Content
            Return newLine
        End Function
    End Class
End Namespace
