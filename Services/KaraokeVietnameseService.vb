Imports System.Text
Imports System.Text.RegularExpressions

Namespace Services
    ''' <summary>
    ''' Service xử lý text karaoke tiếng Việt
    ''' Quy tắc:
    ''' 1. Đầu câu: chèn ∞ (dính liền với chữ)
    ''' 2. Giữa các từ: chèn ♫
    ''' 3. Cuối câu: không có ký tự đặc biệt
    ''' 4. Tiếng Việt: full word
    ''' 5. Tiếng Anh: chia theo âm tiết (không có ∞/♫ giữa các âm tiết)
    ''' </summary>
    Public Class KaraokeVietnameseService

        ''' <summary>
        ''' Xử lý toàn bộ lời bài hát thành format karaoke
        ''' </summary>
        Public Shared Function ProcessLyrics(lyrics As String) As String
            If String.IsNullOrWhiteSpace(lyrics) Then Return String.Empty

            Dim sb = New StringBuilder()
            Dim lines = lyrics.Split({Environment.NewLine, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)

            For i As Integer = 0 To lines.Length - 1
                Dim line = lines(i).Trim()
                If String.IsNullOrEmpty(line) Then Continue For

                Dim processedLine = ProcessSingleLine(line, isFirstLineOfSong:=(i = 0))
                sb.AppendLine(processedLine)
            Next

            Return sb.ToString().TrimEnd()
        End Function

        ''' <summary>
        ''' Xử lý một dòng lời bài hát
        ''' </summary>
        Private Shared Function ProcessSingleLine(line As String, isFirstLineOfSong As Boolean) As String
            Dim sb = New StringBuilder()

            ' Tách dòng thành các từ
            Dim words = line.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)

            For w As Integer = 0 To words.Length - 1
                Dim word = words(w)
                Dim isVietnamese = IsVietnameseWord(word)

                If isVietnamese Then
                    ' Tiếng Việt: full word
                    Dim isFirstWordInLine = (w = 0)

                    If isFirstWordInLine AndAlso Not isFirstLineOfSong Then
                        ' Đầu câu: chèn ∞
                        sb.AppendFormat("∞{0}", word)
                    Else
                        sb.Append(word)
                    End If

                    ' Nếu không phải từ cuối → thêm ♫
                    If w < words.Length - 1 Then
                        sb.Append("♫")
                    End If
                Else
                    ' Tiếng Anh: chia theo âm tiết
                    Dim syllables = SplitEnglishSyllables(word)
                    Dim isFirstSyllable = (w = 0) AndAlso (Not isFirstLineOfSong)

                    For s As Integer = 0 To syllables.Length - 1
                        If s = 0 AndAlso isFirstSyllable Then
                            ' Đầu câu tiếng Anh: ∞ dính với âm tiết đầu
                            sb.AppendFormat("∞{0}", syllables(s))
                        Else
                            sb.Append(syllables(s))
                        End If

                        ' Giữa các âm tiết trong cùng từ: không thêm gì
                        ' Nhưng nếu là âm tiết cuối và không phải từ cuối → thêm ♫
                        If s = syllables.Length - 1 AndAlso w < words.Length - 1 Then
                            sb.Append("♫")
                        End If
                    Next
                End If
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Kiểm tra từ có phải tiếng Việt không
        ''' Dựa vào: có dấu thanh, hoặc chứa ký tự tiếng Việt đặc trưng
        ''' </summary>
        Private Shared Function IsVietnameseWord(word As String) As Boolean
            If String.IsNullOrEmpty(word) Then Return False

            ' Kiểm tra ký tự tiếng Việt có dấu
            Dim vietnameseChars = "áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴĐ"

            For Each ch In word
                If vietnameseChars.IndexOf(ch) >= 0 Then
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Tách từ tiếng Anh thành các âm tiết
        ''' Phương pháp đơn giản: dựa vào nguyên âm
        ''' </summary>
        Private Shared Function SplitEnglishSyllables(word As String) As String()
            If String.IsNullOrEmpty(word) Then Return {word}

            Dim lowerWord = word.ToLower()
            Dim vowels = "aeiouy"
            Dim result = New List(Of String)()
            Dim current = New StringBuilder()

            For i As Integer = 0 To lowerWord.Length - 1
                Dim ch = lowerWord(i)
                current.Append(word(i)) ' Giữ nguyên case gốc

                If vowels.IndexOf(ch) >= 0 Then
                    ' Đây là nguyên âm
                    ' Nếu ký tự tiếp theo là phụ âm và không phải cuối từ → tách
                    If i + 1 < lowerWord.Length Then
                        Dim nextCh = lowerWord(i + 1)
                        If vowels.IndexOf(nextCh) < 0 Then
                            ' Tiếp theo là phụ âm → có thể tách
                            ' Nhưng cần thêm ít nhất 1 phụ âm nữa để tách
                            If i + 2 < lowerWord.Length Then
                                Dim afterNext = lowerWord(i + 2)
                                If vowels.IndexOf(afterNext) < 0 Then
                                    ' Có 2 phụ âm liên tiếp → tách ở giữa
                                    result.Add(current.ToString())
                                    current.Clear()
                                End If
                            End If
                        End If
                    End If
                End If
            Next

            If current.Length > 0 Then
                result.Add(current.ToString())
            End If

            ' Nếu chỉ có 1 âm tiết và từ dài > 3 ký tự → thử tách đơn giản
            If result.Count = 1 AndAlso word.Length > 3 Then
                result = SplitByPattern(word)
            End If

            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Tách âm tiết theo pattern đơn giản hơn
        ''' </summary>
        Private Shared Function SplitByPattern(word As String) As List(Of String)
            Dim result = New List(Of String)()
            Dim vowels = "aeiouy"
            Dim lowerWord = word.ToLower()

            Dim sylStarts = New List(Of Integer)()
            sylStarts.Add(0)

            ' Tìm vị trí bắt đầu âm tiết mới (sau nguyên âm + phụ âm)
            For i As Integer = 0 To lowerWord.Length - 2
                Dim ch = lowerWord(i)
                Dim nextCh = lowerWord(i + 1)

                If vowels.IndexOf(ch) >= 0 AndAlso vowels.IndexOf(nextCh) < 0 Then
                    ' Nguyên âm + phụ âm → kiểm tra ký tự tiếp
                    If i + 2 < lowerWord.Length Then
                        Dim afterNext = lowerWord(i + 2)
                        If vowels.IndexOf(afterNext) >= 0 Then
                            ' Phụ âm + nguyên âm → tách trước phụ âm
                            sylStarts.Add(i + 1)
                        ElseIf i + 3 < lowerWord.Length Then
                            Dim third = lowerWord(i + 3)
                            If vowels.IndexOf(third) >= 0 Then
                                sylStarts.Add(i + 2)
                            End If
                        End If
                    End If
                End If
            Next

            For i As Integer = 0 To sylStarts.Count - 1
                Dim startIdx = sylStarts(i)
                Dim endIdx = If(i + 1 < sylStarts.Count, sylStarts(i + 1), word.Length)
                result.Add(word.Substring(startIdx, endIdx - startIdx))
            Next

            If result.Count = 0 Then
                result.Add(word)
            End If

            Return result
        End Function

    End Class
End Namespace
