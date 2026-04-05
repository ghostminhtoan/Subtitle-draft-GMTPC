Imports System.Text
Imports System.Text.RegularExpressions

Namespace Services
    ''' <summary>
    ''' Service merge karaoke ASS: gop cac dong karaoke rieng le thanh cau hoan chinh voi {\k...} tags
    ''' Quy tac:
    ''' - Dau cau co ∞, cuoi cau khong co ∞ va ♫
    ''' - Xoa dau ∞, xoa khoang cach, thay ♫ bang khoang cach
    ''' - Them {\k...} voi thoi gian tinh bang centiseconds
    ''' </summary>
    Public Class KaraokeMergeService

        Public Shared Function ProcessKaraokeMerge(input As String) As String
            If String.IsNullOrWhiteSpace(input) Then Return String.Empty

            Dim sb = New StringBuilder()
            Dim lines = input.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)

            ' Parse tat ca cac dong Dialogue
            Dim parsedLines = New List(Of ParsedDialogueLine)()
            For Each line In lines
                Dim trimmed = line.Trim()
                If trimmed.StartsWith("Dialogue:") OrElse trimmed.StartsWith("Dialogue :") Then
                    Dim parsed = ParseDialogueLine(trimmed)
                    If parsed IsNot Nothing Then
                        parsedLines.Add(parsed)
                    End If
                End If
            Next

            If parsedLines.Count = 0 Then Return String.Empty

            ' Gom nhom thanh cac cau
            Dim sentences = GroupIntoSentences(parsedLines)

            ' Xuat ra ket qua
            For Each sentence In sentences
                sb.AppendLine(BuildMergedLine(sentence))
            Next

            Return sb.ToString().TrimEnd()
        End Function

        ''' <summary>
        ''' Parse mot dong Dialogue ASS
        ''' Format: Dialogue: Layer,Start,End,Style,Name,MarginL,MarginR,MarginV,Effect,Text
        ''' </summary>
        Private Shared Function ParseDialogueLine(line As String) As ParsedDialogueLine
            Try
                ' Tim "Dialogue:" hoac "Dialogue: "
                Dim dialogueIdx = line.IndexOf("Dialogue:")
                If dialogueIdx < 0 Then Return Nothing

                Dim rest = line.Substring(dialogueIdx + 9).TrimStart()

                ' Tach 9 fields dau, field thu 10 la text (co the chua ",")
                Dim parts = New List(Of String)()
                Dim current = New StringBuilder()
                Dim commaCount = 0

                For i As Integer = 0 To rest.Length - 1
                    Dim ch = rest(i)
                    If ch = ","c AndAlso commaCount < 9 Then
                        parts.Add(current.ToString())
                        current.Clear()
                        commaCount += 1
                    Else
                        current.Append(ch)
                    End If
                Next
                parts.Add(current.ToString())

                If parts.Count < 10 Then Return Nothing

                Dim result = New ParsedDialogueLine()
                result.Layer = parts(0)
                result.StartTime = parts(1)
                result.EndTime = parts(2)
                result.Style = parts(3)
                result.Name = parts(4)
                result.MarginL = parts(5)
                result.MarginR = parts(6)
                result.MarginV = parts(7)
                result.Effect = parts(8)
                result.Text = parts(9)

                ' Tinh thoi gian centiseconds
                result.DurationCs = CalculateDurationCs(result.StartTime, result.EndTime)

                Return result
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Tinh thoi gian duration bang centiseconds (1cs = 10ms)
        ''' Format ASS: H:MM:SS.cc
        ''' </summary>
        Private Shared Function CalculateDurationCs(startTime As String, endTime As String) As Integer
            Dim startCs = ParseAssTimeToCs(startTime)
            Dim endCs = ParseAssTimeToCs(endTime)
            Return Math.Max(0, endCs - startCs)
        End Function

        ''' <summary>
        ''' Parse thoi gian ASS thanh centiseconds
        ''' </summary>
        Private Shared Function ParseAssTimeToCs(timeStr As String) As Integer
            If String.IsNullOrWhiteSpace(timeStr) Then Return 0

            timeStr = timeStr.Trim()
            Dim parts = timeStr.Split(":"c)
            If parts.Length <> 3 Then Return 0

            Dim hours = Integer.Parse(parts(0))
            Dim minutes = Integer.Parse(parts(1))

            Dim secParts = parts(2).Split("."c)
            Dim seconds = Integer.Parse(secParts(0))
            Dim centiseconds = 0
            If secParts.Length > 1 Then
                Dim csStr = secParts(1).PadRight(2, "0"c).Substring(0, 2)
                centiseconds = Integer.Parse(csStr)
            End If

            Return hours * 360000 + minutes * 6000 + seconds * 100 + centiseconds
        End Function

        ''' <summary>
        ''' Gom nhom cac dong thanh cau
        ''' - Cau bat dau khi text co ∞
        ''' - Cau ket thuc khi text khong ket thuc bang ♫ va khong co ∞
        ''' - Trong cung mot cau: extend end time cua moi dong cho trung start time cua dong ke tiep
        '''   (connect gap trong cau, khong extend giua 2 cau)
        ''' </summary>
        Private Shared Function GroupIntoSentences(lines As List(Of ParsedDialogueLine)) As List(Of KaraokeSentence)
            Dim sentences = New List(Of KaraokeSentence)()
            Dim currentSentence As KaraokeSentence = Nothing

            For idx As Integer = 0 To lines.Count - 1
                Dim line = lines(idx)
                Dim cleanText = line.Text.Trim()
                Dim hasInfinity = cleanText.Contains("∞")
                Dim endsWithMusicNote = cleanText.EndsWith("♫")
                Dim hasNextLine = (idx + 1 < lines.Count)
                Dim nextLine = If(hasNextLine, lines(idx + 1), Nothing)
                Dim nextCleanText = If(nextLine IsNot Nothing, nextLine.Text.Trim(), "")
                Dim nextHasInfinity = nextCleanText.Contains("∞")

                If hasInfinity Then
                    ' Bat dau cau moi
                    If currentSentence IsNot Nothing AndAlso currentSentence.Words.Count > 0 Then
                        sentences.Add(currentSentence)
                    End If
                    currentSentence = New KaraokeSentence()
                    currentSentence.StartTime = line.StartTime
                    currentSentence.Style = line.Style
                    currentSentence.Name = line.Name
                    currentSentence.Layer = line.Layer
                    currentSentence.MarginL = line.MarginL
                    currentSentence.MarginR = line.MarginR
                    currentSentence.MarginV = line.MarginV
                    currentSentence.Effect = line.Effect
                End If

                If currentSentence Is Nothing Then
                    currentSentence = New KaraokeSentence()
                    currentSentence.StartTime = line.StartTime
                    currentSentence.Style = line.Style
                End If

                ' Them word vao cau
                Dim word = New KaraokeWord()
                word.Text = line.Text
                word.DurationCs = line.DurationCs

                ' Connect gap trong cau:
                ' Neu dong nay co ♫ HOAC (dong sau khong co ∞ va dong nay khong phai cuoi cau)
                ' => extend end time cua dong nay cho trung start time cua dong ke tiep
                Dim isLastWordInSentence = Not endsWithMusicNote AndAlso Not hasInfinity
                If hasNextLine AndAlso Not isLastWordInSentence Then
                    ' Dong trong cau (khong phai cuoi cau) => connect gap
                    word.DurationCs = CalculateDurationCs(line.StartTime, nextLine.StartTime)
                    word.ConnectedEndTime = nextLine.StartTime
                ElseIf hasNextLine AndAlso isLastWordInSentence AndAlso Not nextHasInfinity Then
                    ' Dong cuoi cau nhung dong sau cung thuoc cau nay (edge case) => connect gap
                    word.DurationCs = CalculateDurationCs(line.StartTime, nextLine.StartTime)
                    word.ConnectedEndTime = nextLine.StartTime
                End If

                currentSentence.Words.Add(word)

                ' Cap nhat end time
                If word.ConnectedEndTime IsNot Nothing Then
                    currentSentence.EndTime = word.ConnectedEndTime
                Else
                    currentSentence.EndTime = line.EndTime
                End If

                ' Kiem tra ket thuc cau
                If isLastWordInSentence Then
                    sentences.Add(currentSentence)
                    currentSentence = Nothing
                End If
            Next

            ' Them cau cuoi cung
            If currentSentence IsNot Nothing AndAlso currentSentence.Words.Count > 0 Then
                sentences.Add(currentSentence)
            End If

            Return sentences
        End Function

        ''' <summary>
        ''' Xay dung dong Dialogue da merge
        ''' </summary>
        Private Shared Function BuildMergedLine(sentence As KaraokeSentence) As String
            Dim sb = New StringBuilder()

            ' Xay dung text voi {\k...} tags
            Dim textParts = New List(Of String)()
            For i As Integer = 0 To sentence.Words.Count - 1
                Dim word = sentence.Words(i)
                Dim processedText = ProcessWordText(word.Text)

                If i = 0 Then
                    ' Tu dau tien: {\kX} Word (co space sau {\kX})
                    textParts.Add(String.Format("{{\k{0}}} {1}", word.DurationCs, processedText))
                Else
                    ' Tu tiep theo: {\kX}word (space truoc {\kX}, khong space sau)
                    textParts.Add(String.Format(" {{\k{0}}}{1}", word.DurationCs, processedText))
                End If
            Next

            Dim mergedText = String.Join("", textParts)

            ' Xoa spaces trong style name
            Dim cleanStyle = sentence.Style.Replace(" ", "")

            ' Xay dung dong Dialogue (khong co space sau "Dialogue:")
            sb.AppendFormat("Dialogue:{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                sentence.Layer, sentence.StartTime, sentence.EndTime,
                cleanStyle, sentence.Name, sentence.MarginL, sentence.MarginR,
                sentence.MarginV, sentence.Effect, mergedText)

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Xu ly text cua mot word: xoa ∞, xoa ♫, xoa khoang cach
        ''' </summary>
        Private Shared Function ProcessWordText(text As String) As String
            ' Xoa ∞
            text = text.Replace("∞", "")
            ' Xoa ♫
            text = text.Replace("♫", "")
            ' Xoa khoang cach
            text = text.Replace(" ", "")
            Return text
        End Function

#Region "Models"

        Private Class ParsedDialogueLine
            Public Property Layer As String
            Public Property StartTime As String
            Public Property EndTime As String
            Public Property Style As String
            Public Property Name As String
            Public Property MarginL As String
            Public Property MarginR As String
            Public Property MarginV As String
            Public Property Effect As String
            Public Property Text As String
            Public Property DurationCs As Integer
        End Class

        Private Class KaraokeSentence
            Public Property Layer As String = "0"
            Public Property StartTime As String
            Public Property EndTime As String
            Public Property Style As String
            Public Property Name As String = ""
            Public Property MarginL As String = "0"
            Public Property MarginR As String = "0"
            Public Property MarginV As String = "0"
            Public Property Effect As String = ""
            Public Property Words As New List(Of KaraokeWord)()
        End Class

        Private Class KaraokeWord
            Public Property Text As String
            Public Property DurationCs As Integer
            Public Property ConnectedEndTime As String
        End Class

#End Region

    End Class
End Namespace
