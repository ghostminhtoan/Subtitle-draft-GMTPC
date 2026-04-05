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

                Dim processedLine = ProcessSingleLine(line, isFirstWordOfSong:=(i = 0))
                sb.AppendLine(processedLine)
            Next

            Return sb.ToString().TrimEnd()
        End Function

        ''' <summary>
        ''' Xử lý một dòng lời bài hát
        ''' Mỗi từ/âm tiết xuống dòng riêng biệt
        ''' </summary>
        Private Shared Function ProcessSingleLine(line As String, isFirstWordOfSong As Boolean) As String
            Dim sb = New StringBuilder()

            ' Tách dòng thành các từ
            Dim words = line.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)

            For w As Integer = 0 To words.Length - 1
                Dim word = words(w)
                Dim isVietnamese = IsVietnameseWord(word)
                Dim isFirstWordInLine = (w = 0) AndAlso isFirstWordOfSong

                If isVietnamese Then
                    ' Tiếng Việt: full word
                    If isFirstWordInLine Then
                        ' Đầu bài hát: chèn ∞
                        sb.AppendFormat("∞{0}", word)
                    Else
                        sb.Append(word)
                    End If

                    ' Nếu không phải từ cuối → thêm ♫
                    If w < words.Length - 1 Then
                        sb.Append("♫")
                    End If
                    ' Xuống dòng
                    sb.AppendLine()
                Else
                    ' Tiếng Anh: chia theo âm tiết
                    Dim syllables = SplitEnglishSyllables(word)

                    For s As Integer = 0 To syllables.Length - 1
                        If s = 0 AndAlso isFirstWordInLine Then
                            ' Đầu bài hát tiếng Anh: ∞ dính với âm tiết đầu
                            sb.AppendFormat("∞{0}", syllables(s))
                        Else
                            sb.Append(syllables(s))
                        End If

                        ' Giữa các âm tiết trong cùng từ: không thêm gì
                        ' Nhưng nếu là âm tiết cuối và không phải từ cuối → thêm ♫
                        Dim isLastSyllable = (s = syllables.Length - 1)
                        Dim isLastWord = (w = words.Length - 1)

                        If isLastSyllable AndAlso Not isLastWord Then
                            sb.Append("♫")
                        End If
                        ' Xuống dòng
                        sb.AppendLine()
                    Next
                End If
            Next

            Return sb.ToString().TrimEnd()
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
        ''' Phương pháp: đếm vowel groups (nhóm nguyên âm liên tiếp)
        ''' Mỗi vowel group = 1 âm tiết. Nếu ≤ 1 → không tách.
        ''' </summary>
        Private Shared Function SplitEnglishSyllables(word As String) As String()
            If String.IsNullOrEmpty(word) Then Return {word}
            If word.Length <= 2 Then Return {word} ' Từ quá ngắn → không tách

            Dim lowerWord = word.ToLower()
            Dim vowels = "aeiou"
            Dim result = New List(Of String)()

            ' Đếm số vowel groups
            Dim vowelGroups = CountVowelGroups(lowerWord)
            If vowelGroups <= 1 Then
                ' Chỉ có 1 âm tiết → không tách
                Return {word}
            End If

            ' Tách tại boundary giữa các vowel groups
            result = SplitByVowelGroups(word, lowerWord, vowels)

            If result.Count <= 1 Then
                Return {word}
            End If

            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Đếm số nhóm nguyên âm liên tiếp
        ''' </summary>
        Private Shared Function CountVowelGroups(lowerWord As String) As Integer
            Dim vowels = "aeiou"
            Dim count = 0
            Dim inVowelGroup = False

            For i As Integer = 0 To lowerWord.Length - 1
                Dim ch = lowerWord(i)
                Dim isVowel = (vowels.IndexOf(ch) >= 0)

                ' Xử lý 'y' như nguyên âm nếu không phải ký tự đầu
                If ch = "y"c AndAlso i > 0 AndAlso Not inVowelGroup Then
                    isVowel = True
                End If

                If isVowel AndAlso Not inVowelGroup Then
                    count += 1
                    inVowelGroup = True
                ElseIf Not isVowel Then
                    inVowelGroup = False
                End If
            Next

            Return count
        End Function

        ''' <summary>
        ''' Tách từ theo vowel group boundaries
        ''' Mỗi syllable = (phụ âm trước) + vowel group
        ''' Ví dụ: "baby" → "ba" + "by"
        ''' every → "e" + "ve" + "ry"
        ''' </summary>
        Private Shared Function SplitByVowelGroups(word As String, lowerWord As String, vowels As String) As List(Of String)
            Dim result = New List(Of String)()

            ' Tìm tất cả vowel group: (start, end) positions
            Dim vowelGroups = New List(Of Tuple(Of Integer, Integer))()
            Dim inVowelGroup = False
            Dim vgStart = 0

            For i As Integer = 0 To lowerWord.Length - 1
                Dim ch = lowerWord(i)
                Dim isVowel = (vowels.IndexOf(ch) >= 0)

                If ch = "y"c AndAlso i > 0 AndAlso Not inVowelGroup Then
                    isVowel = True
                End If

                If isVowel AndAlso Not inVowelGroup Then
                    vgStart = i
                    inVowelGroup = True
                ElseIf Not isVowel AndAlso inVowelGroup Then
                    vowelGroups.Add(Tuple.Create(vgStart, i - 1))
                    inVowelGroup = False
                End If
            Next
            If inVowelGroup Then
                vowelGroups.Add(Tuple.Create(vgStart, lowerWord.Length - 1))
            End If

            If vowelGroups.Count <= 1 Then
                result.Add(word)
                Return result
            End If

            ' Tách: mỗi syllable = phụ âm trước (nếu có) + vowel group
            Dim sylStart = 0
            For i As Integer = 0 To vowelGroups.Count - 1
                Dim vg = vowelGroups(i)
                Dim vgEnd = vg.Item2
                ' Syllable kết thúc tại cuối vowel group
                Dim sylEnd = vgEnd + 1
                result.Add(word.Substring(sylStart, sylEnd - sylStart))
                ' Syllable tiếp theo bắt đầu sau vowel group này
                sylStart = sylEnd
            Next

            ' Nếu còn ký tự thừa → gộp vào syllable cuối
            If sylStart < word.Length Then
                Dim lastIdx = result.Count - 1
                result(lastIdx) = result(lastIdx) & word.Substring(sylStart)
            End If

            Return result
        End Function

    End Class
End Namespace
